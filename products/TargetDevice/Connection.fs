//=============================================================================
// Haruka Software Storage.
// Connection.fs : Implementation of Connection class.
// Connection class is a module that implements iSCSI network portal functionality.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.IO
open System.Collections.Generic
open System.Collections.Immutable
open System.Net
open System.Net.Sockets
open System.Diagnostics

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

[<NoComparison>]
type ResendStatusRec = {
    /// Response PDUs that is sent to initiator and not acknowledged yet.
    /// It is index by StatSN.
    m_SentRespPDUs : ImmutableArray< struct( STATSN_T * ILogicalPDU ) >;

    /// SCSI Data-In or R2T PDUs that is sent to initiator and not acknowledged yet
    /// It is indexed by ITT and DataSN.
    m_SentDataInPDUs : ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >;

    // 
    m_R_SNACK_Request : ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >;
}


/// <summary>
///   This class wraps one TCP connection and receives/sends PDUs from/to initiator. 
///   And it puts received CDBs or data into session componens, or get CDBs or
///   data from session component and sends to network.
/// </summary>
/// <param name="m_StatusMaster">
///   Interface of status master component which this connection component created.
/// </param>
/// <param name="m_TargetPortalGroupTag">
///   The Target Portal Group Tag number of waiting TCP port which TCP connection connected to.
///   This value is always 0.
/// </param>
/// <param name="m_NetStream">
///   The TCP connection.
/// </param>
/// <param name="m_ConnectedTime">
///   Connected date time.
/// </param>
/// <param name="m_COParams">
///   Negotiated connection only parameters.
/// </param>
/// <param name="m_SWParams">
///   Negotiated session wide parameters.
/// </param>
/// <param name="m_session">
///   Interface of the session component which this connection belongings to.
/// </param>
/// <param name="m_TSIH">
///   TSIH value of the session that specify m_session argument.
/// </param>
/// <param name="m_CID">
///   CID of this connection.
/// </param>
/// <param name="m_Counter">
///   Connection counter value of this connection.
/// </param>
/// <param name="m_NetPortIdx">
///   Network portal index number where this connection was established.
/// </param>
/// <param name="argNewStatSN">
///   StatSN value.
/// </param>
/// <param name="m_Killer">
///   Killer object.
/// </param>
type Connection
    (
        m_StatusMaster : IStatus,
        m_TargetPortalGroupTag : TPGT_T,
        argNetStream : Stream,
        m_ConnectedTime : DateTime,
        m_COParams : IscsiNegoParamCO,
        m_SWParams : IscsiNegoParamSW,
        m_session : ISession,
        m_TSIH : TSIH_T,
        m_CID : CID_T,
        m_Counter : CONCNT_T,
        m_NetPortIdx : NETPORTIDX_T,
        m_Killer : IKiller
    ) as this =

    let m_StreamForRead = argNetStream
    let m_StreamForWrite = new BufferedStream( argNetStream )

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// semaphore that gurds the m_NetStream when sending data to the initiator.
    let m_SendTask = new TaskQueue()

    let m_ReceiveTask = new LambdaQueue( 1u )

    /// StatSN value of this connection
    let mutable m_StatSN = statsn_me.zero

    /// logger info
    let m_LogInfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueNone, ValueNone )

    /// current MaxRecvDataSegmentLength_I value
    let mutable m_MaxRecvDataSegmentLength_I = m_COParams.MaxRecvDataSegmentLength_I

    /// re-sending status
    let m_ResendStat = OptimisticLock< ResendStatusRec >({
        m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
        m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
        m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
    })

    /// Resource counter for receive data
    let m_ReceiveBytesCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for send data
    let m_SentBytesCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_LogInfo, "Connection", "" ) )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IConnection with

        // ------------------------------------------------------------------------
        // Get connected date time
        override _.ConnectedDate : DateTime =
            m_ConnectedTime

        // ------------------------------------------------------------------------
        // Get the current effective connection only parameters.
        override _.CurrentParams : IscsiNegoParamCO =
            {
                m_COParams with
                    MaxRecvDataSegmentLength_I = m_MaxRecvDataSegmentLength_I;
            }

        // ------------------------------------------------------------------------
        // Get TSIH value of the session which this connection belongs.
        override _.TSIH : TSIH_T =
            m_TSIH

        // ------------------------------------------------------------------------
        // Get CID value of this connection
        override _.CID : CID_T =
            m_CID

        // ------------------------------------------------------------------------
        // Get the next StatSN value to be used.
        override _.NextStatSN : STATSN_T =
            m_StatSN

        // ------------------------------------------------------------------------
        // Get connection counter value of this connection
        override _.ConCounter : CONCNT_T =
            m_Counter

        // ------------------------------------------------------------------------
        // Get connection counter value of this connection
        override _.NetPortIdx : NETPORTIDX_T =
            m_NetPortIdx

        // ------------------------------------------------------------------------
        // request to close connection
        override _.Close() : unit =
            HLogger.Trace( LogID.I_CONNECTION_CLOSED_GRACEFULLY, fun g -> g.Gen2( m_LogInfo, m_TSIH, m_CID ) )
            m_SendTask.Stop()
            try
                m_StreamForWrite.Flush()
                match argNetStream with
                | :? NetworkStream as x ->
                    x.Socket.Disconnect false
                | _ -> ()
                argNetStream.Dispose() 
            with
            | _ as x ->
                // ignore all exceptions
                HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_LogInfo, "Connection close error : " + x.Message ) )

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override this.Terminate() : unit =
            ( this :> IConnection ).Close()

        // ------------------------------------------------------------------------
        /// <summary>
        ///   Process all of the full feature phase requests.
        /// </summary>
        /// <param name="firstPDU">
        ///   Received first PDU of full feature phase.
        /// </param>
        override this.StartFullFeaturePhase () : unit =
            fun () -> task {
                try
                    // receive first PDU in full feature phase.
                    let headerDigest = m_COParams.HeaderDigest.[0]
                    let dataDigest = m_COParams.DataDigest.[0]
                    let mrdsl_t = m_COParams.MaxRecvDataSegmentLength_T
                    let! firstPDU = PDU.Receive(
                        mrdsl_t, headerDigest, dataDigest, ValueSome m_TSIH, ValueSome m_CID, ValueSome m_Counter, m_StreamForRead, Standpoint.Target
                    )

                    // Set initial StatSN value
                    m_StatSN <- firstPDU.ExpStatSN

                    // Push received first PDU to session component
                    m_session.PushReceivedPDU this firstPDU

                    // Receive next PDU
                    do! this.ReceivePDUInFullFeaturePhase()

                with
                | _ as x ->
                    let wc = m_session.GetConnection m_CID m_Counter
                    if wc.IsSome then
                        match x with
                        | :? ConnectionErrorException as y ->
                            // If a communication error occurs, this connection is deleted, but the session continues to exist.
                            HLogger.Trace( LogID.W_CONNECTION_ERROR, fun g -> g.Gen1( m_LogInfo, x.Message ) )
                            ( this :> IConnection ).Close()
                            m_session.RemoveConnection m_CID m_Counter
                        | _ ->
                            // If unexpected error was occures, drop this domain. It causes session recovery.
                            HLogger.UnexpectedException( fun g -> g.GenExp( m_LogInfo, x ) )
                            m_Killer.NoticeTerminate()
                    else
                        // If this connection is already removed, any exeptions are ignored.
                        HLogger.IgnoreException( fun g -> g.GenExp( m_LogInfo, x ) )

            } |> Functions.StartTask

        // ------------------------------------------------------------------------
        /// <summary>
        ///   Send PDU to the initiator.
        /// </summary>
        /// <param name="pdu">PDU that should be send.</param>
        /// <returns>
        ///   Task object, that wait write opration.
        /// </returns>
        /// <remarks>
        ///   If specified PDU is Logout response PDU, this connection is closed.
        ///   This operation is ran in asynchronously.
        /// </remarks>
        override this.SendPDU ( pdu : ILogicalPDU ) : unit =
            let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( pdu.InitiatorTaskTag ), ValueNone )
            m_SendTask.Enqueue( fun () -> task {
                try
                    let! result = this.SendPDUInternal( pdu )
                    if result.IsSome then
                        HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( wloginfo, "Call R-Data SNACK procedure." ) )
                        result.Value()
                with
                | _ as x ->
                    // If unknown error was occurred, unload this component
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_LogInfo, x ) )
                    m_Killer.NoticeTerminate()
            })

        // ------------------------------------------------------------------------
        // Resend PDU to the initiator.
        override _.ReSendPDU ( pdu : ILogicalPDU ) : unit =
            m_SendTask.Enqueue( fun () -> task {
                try
                    // Update StatSN, ExpCmdSN, MaxCmdSN values in the sending PDU.
                    let struct( next_ExpCmdSN, next_MaxCmdSN ) = m_session.UpdateMaxCmdSN()
                    let wUsedStatSN = m_StatSN
                    let send_pdu = pdu.UpdateTargetValuesForResend wUsedStatSN next_ExpCmdSN next_MaxCmdSN

                    HLogger.Trace( LogID.V_TRACE, fun g ->
                        let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( pdu.InitiatorTaskTag ), ValueNone )
                        let msg = sprintf "Opcode=%s" ( Constants.getOpcodeNameFromValue pdu.Opcode )
                        g.Gen1( wloginfo, "PDU re-send to the initiator for SNACK request." + msg )
                    )

                    // Send PDU to the initiator
                    let! sentBytes =
                        PDU.SendPDU(
                            m_MaxRecvDataSegmentLength_I,
                            m_COParams.HeaderDigest.[0],
                            m_COParams.DataDigest.[0],
                            ValueSome( m_TSIH ),
                            ValueSome( m_CID ),
                            ValueSome( m_Counter ),
                            m_ObjID,
                            m_StreamForWrite,
                            send_pdu
                        )
                    if m_SendTask.Count <= 0 || m_SendTask.ProcessedCount % ( uint64 Constants.BDLU_MAX_TASKSET_SIZE / 1UL )  = 0UL then
                        do! m_StreamForWrite.FlushAsync()
                    m_SentBytesCounter.AddCount DateTime.UtcNow ( int64 sentBytes )
                with
                | _ as x ->
                    // If unknown error was occurred, unload this component
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_LogInfo, x ) )
                    m_Killer.NoticeTerminate()
            })

        // ------------------------------------------------------------------------
        // Resend PDU for R-SNACK request to the initiator.
        override _.ReSendPDUForRSnack ( pdu : ILogicalPDU ) : unit =
            m_SendTask.Enqueue( fun () -> task {
                try
                    // Update StatSN, ExpCmdSN, MaxCmdSN values in the sending PDU.
                    let struct( next_ExpCmdSN, next_MaxCmdSN ) = m_session.UpdateMaxCmdSN()
                    let wUsedStatSN = m_StatSN
                    let send_pdu = pdu.UpdateTargetValuesForResend wUsedStatSN next_ExpCmdSN next_MaxCmdSN

                    m_ResendStat.Update( fun oldStat ->
                        if send_pdu.Opcode = OpcodeCd.SCSI_RES then
                            let resPDU = send_pdu :?> SCSIResponsePDU
                            let resPDU_StatSN = resPDU.StatSN

                            // update PDU that has the same StatSN value.
                            let builder = ImmutableArray.CreateBuilder< struct( STATSN_T * ILogicalPDU ) >()
                            builder.Capacity <- oldStat.m_SentRespPDUs.Length
                            for struct( itr_statsn, itr_pdu ) in oldStat.m_SentRespPDUs do
                                if resPDU_StatSN <> itr_statsn then
                                    builder.Add( struct( itr_statsn, itr_pdu ) )
                            builder.Add( struct( resPDU_StatSN, send_pdu ) )

                            {
                                oldStat with
                                    m_SentRespPDUs = builder.DrainToImmutable()
                            }

                        elif pdu.Opcode = OpcodeCd.SCSI_DATA_IN || pdu.Opcode = OpcodeCd.R2T then
                            let datasn = 
                                if pdu.Opcode = OpcodeCd.SCSI_DATA_IN then
                                    ( pdu :?> SCSIDataInPDU ).DataSN
                                else
                                    ( pdu :?> R2TPDU ).R2TSN

                            let builder = ImmutableArray.CreateBuilder< struct( ITT_T * DATASN_T * ILogicalPDU ) >()
                            builder.Capacity <- oldStat.m_SentDataInPDUs.Length
                            for struct( itrITT, itrSataSN, itrPDU ) in oldStat.m_SentDataInPDUs do
                                if ( itrITT = pdu.InitiatorTaskTag && itrSataSN = datasn ) |> not then
                                    builder.Add struct( itrITT, itrSataSN, itrPDU )
                            builder.Add struct( pdu.InitiatorTaskTag, datasn , pdu )
                            {
                                oldStat with
                                    m_SentDataInPDUs = builder.DrainToImmutable()
                            }

                        else
                            oldStat
                    )
                    |> ignore

                    HLogger.Trace( LogID.V_TRACE, fun g ->
                        let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( pdu.InitiatorTaskTag ), ValueNone )
                        let msg = sprintf "Opcode=%s" ( Constants.getOpcodeNameFromValue pdu.Opcode )
                        g.Gen1( wloginfo, "PDU re-send to the initiator for R-Data SNACK request." + msg )
                    )

                    // Send PDU to the initiator
                    let! sentBytes =
                        PDU.SendPDU(
                            m_MaxRecvDataSegmentLength_I,
                            m_COParams.HeaderDigest.[0],
                            m_COParams.DataDigest.[0],
                            ValueSome( m_TSIH ),
                            ValueSome( m_CID ),
                            ValueSome( m_Counter ),
                            m_ObjID,
                            m_StreamForWrite,
                            send_pdu
                        )
                    if m_SendTask.Count <= 0 || m_SendTask.ProcessedCount % ( uint64 Constants.BDLU_MAX_TASKSET_SIZE / 1UL ) = 0UL then
                        do! m_StreamForWrite.FlushAsync()
                    m_SentBytesCounter.AddCount DateTime.UtcNow ( int64 sentBytes )
                with
                | _ as x ->
                    // If unknown error was occurred, unload this component
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_LogInfo, x ) )
                    m_Killer.NoticeTerminate()
            })

        // ------------------------------------------------------------------------
        // Notice that connection parameter values are changed.
        override _.NotifyUpdateConnectionParameter ( argCOParams : IscsiNegoParamCO ) :unit =
            m_MaxRecvDataSegmentLength_I <- argCOParams.MaxRecvDataSegmentLength_I

        // ------------------------------------------------------------------------
        // Notify to delete R2T PDU.
        override _.NotifyR2TSatisfied ( itt : ITT_T ) ( r2tsn : DATASN_T ) : unit =
            let result =
                m_ResendStat.Update( fun olsStat ->
                    let builder = ImmutableArray.CreateBuilder< struct( ITT_T * DATASN_T * ILogicalPDU ) >()
                    builder.Capacity <- olsStat.m_SentDataInPDUs.Length
                    for struct( itrITT, itrDataSN, itrPDU ) in olsStat.m_SentDataInPDUs do
                        if ( itrITT = itt && itrDataSN = r2tsn ) |> not then
                            builder.Add struct( itrITT, itrDataSN, itrPDU )

                    if builder.Count = olsStat.m_SentDataInPDUs.Length then
                        struct( olsStat, false )
                    else
                        let nextStat = {
                            olsStat with
                                m_SentDataInPDUs = builder.DrainToImmutable()
                        }
                        struct( nextStat, true )
                )
            if result |> not then
                HLogger.Trace( LogID.I_PDU_ALREADY_REMOVED, fun g ->
                    let msg = sprintf "Satisfied R2T PDU, ITT=0x%08X, R2TSN=0x%08X" itt r2tsn
                    g.Gen1( m_LogInfo, msg )
                )

        // ------------------------------------------------------------------------
        // Notify to delete acknowledged Data-In PDU by Data-Ack SNACK request.
        override _.NotifyDataAck ( ttt : TTT_T ) ( lun : LUN_T ) ( begrun : DATASN_T ) : unit =

            m_ResendStat.Update ( fun oldStat ->
                // Search ITT value by specified TTT and LUN value that is transferred to the initiator
                // ( and send backed to target ) by Data-In PDU that has 1 in A bit.
                let v =
                    Connection.SearchImmutableArray oldStat.m_SentDataInPDUs ( fun struct( _, _, itrPDU ) ->
                        match itrPDU with
                        | :? SCSIDataInPDU as dp when dp.A && dp.LUN = lun && dp.TargetTransferTag = ttt ->
                            true
                        | _ ->
                            false
                    )
                
                // *** if the Data-In PDU that has 1 on A bit is acknowledged,
                //     above procedure can not find the ITT. 
                //     However A bit is set on last Data-In PDU at read data sequence.
                //     Thus, in this case, all of Data-In PDU are already acknowledged,
                //     received DataACK SNACK can be ignored.
            
                // delete acknowledged Data-In or R2T PDUs.
                match v with
                | ValueSome( struct( _, _, rPDU ) ) ->
                    let itt = rPDU.InitiatorTaskTag
                    let builder = ImmutableArray.CreateBuilder< struct( ITT_T * DATASN_T * ILogicalPDU ) >()
                    builder.Capacity <- oldStat.m_SentDataInPDUs.Length
                    for struct( i_itt, i_datasn, itrPDU_2 ) in oldStat.m_SentDataInPDUs do
                        if ( i_itt = itt && ( datasn_me.lessThan i_datasn begrun ) ) |> not then
                            builder.Add struct( i_itt, i_datasn, itrPDU_2 )
                    {
                        oldStat with
                            m_SentDataInPDUs = builder.DrainToImmutable()
                    }
                | ValueNone ->
                    oldStat
            )
            |> ignore

        // ------------------------------------------------------------------------
        // Get Data-In PDUs or R2T PDUs for resend.
        override _.GetSentDataInPDUForSNACK ( itt : ITT_T ) ( begrun : DATASN_T ) ( runlength : uint32 ) : ILogicalPDU[] =
            let oldsdip = m_ResendStat.obj.m_SentDataInPDUs
            [|
                for struct( i_itt, i_datasn, itrPDU ) in oldsdip do
                    let r =
                        i_itt = itt &&
                        (
                            ( begrun = datasn_me.zero && runlength = 0u ) ||
                            begrun = i_datasn ||
                            datasn_me.lessThan begrun i_datasn 
                        )
                    if r then
                        yield struct( i_itt, i_datasn, itrPDU )
            |]
            |> Array.sortWith ( fun struct( _, a_datasn, _ ) struct( _, b_datasn, _ ) ->
                datasn_me.compare a_datasn b_datasn
            )
            |> if runlength > 0u then Array.truncate ( int runlength ) else id
            |> Array.map ( fun struct( _, _, pdu ) -> pdu )

        // ------------------------------------------------------------------------
        // Get SCSI Response PDUs for resend.
        override _.GetSentResponsePDUForSNACK ( begrun : STATSN_T ) ( runlength : uint32 ) : ILogicalPDU[] =
            let oldsrp =
                m_ResendStat.obj.m_SentRespPDUs
                |> Seq.toArray
                |> Array.sortWith ( fun struct( aStatSN, _ ) struct( bStatSN, _ ) ->
                    statsn_me.compare aStatSN bStatSN
                )
            if oldsrp.Length = 0 then
                [||]
            else
                let struct( minStatSN, _ ) = oldsrp.[0]
                let struct( maxStatSN, _ ) = oldsrp.[ oldsrp.Length - 1 ]

                let rv =
                    if begrun = statsn_me.zero && runlength = 0u then
                        // Reply with all unacknowledged statuses.
                        oldsrp
                    elif runlength = 0u then
                        // Respond with a status of begrun or higher.
                        if statsn_me.lessThan begrun minStatSN then
                            [||]
                        else
                            oldsrp
                            |> Array.filter ( fun struct( itr, _ ) -> begrun = itr || statsn_me.lessThan begrun itr )
                    else
                        // Returns a status that is greater than or equal to begrun and less than begrun+runlength.
                        let es = statsn_me.incr runlength begrun
                        let maxp1 = statsn_me.next maxStatSN
                        if statsn_me.lessThan begrun minStatSN || statsn_me.lessThan maxp1 es then
                            [||]
                        else
                            oldsrp
                            |> Array.filter ( fun struct( itr, _ ) -> ( begrun = itr || statsn_me.lessThan begrun itr ) && statsn_me.lessThan itr es )
                rv
                |> Array.map ( fun struct( _, pdu ) -> pdu )

        // ------------------------------------------------------------------------
        // Get SCSI Response PDU and Data-In PDUs for R-DATA SNACK
        override _.GetSentSCSIResponsePDUForR_SNACK ( itt : ITT_T ) : ( SCSIDataInPDU[] * SCSIResponsePDU ) =
            let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( itt ), ValueNone )

            let struct( dataPDUs, respPDU ) =
                m_ResendStat.Update ( fun oldStat ->

                    let sdip_Builder = ImmutableArray.CreateBuilder< struct( ITT_T * DATASN_T * ILogicalPDU ) >()
                    sdip_Builder.Capacity <- oldStat.m_SentDataInPDUs.Length
                    let dataPDUs =
                        [|
                            for struct( i_itt, i_datasn, itrPDU ) in oldStat.m_SentDataInPDUs do
                                match itrPDU with
                                | :? SCSIDataInPDU as x when i_itt = itt ->
                                    yield x
                                | _ ->
                                    sdip_Builder.Add struct( i_itt, i_datasn, itrPDU )
                        |]
                        |> Array.sortWith ( fun itr1 itr2 -> datasn_me.compare ( itr1.DataSN ) ( itr2.DataSN ) )

                    let srp_Builder = ImmutableArray.CreateBuilder< struct( STATSN_T * ILogicalPDU ) >()
                    srp_Builder.Capacity <- oldStat.m_SentRespPDUs.Length
                    let respPDU = [|
                        for struct( itr_StatSN, itr_PDU ) in oldStat.m_SentRespPDUs do
                            if itr_PDU.InitiatorTaskTag = itt then
                                yield itr_PDU
                            else
                                srp_Builder.Add struct( itr_StatSN, itr_PDU )
                    |]

                    let nextStat = {
                        oldStat with
                            m_SentRespPDUs = srp_Builder.DrainToImmutable();
                            m_SentDataInPDUs = sdip_Builder.DrainToImmutable();
                    }
                    struct( nextStat, struct( dataPDUs, respPDU ) )
                )

            if respPDU.Length <= 0 then
                // If there no response PDU, it is considered critical error.
                HLogger.Trace( LogID.E_MISSING_RSNACK_REQED_STATUS_PDU, fun g -> g.Gen0( wloginfo ) )
                raise <| SessionRecoveryException( "R-SNACK requested SCSI Response PDU is missing.", m_TSIH )

            ( dataPDUs, respPDU.[0] :?> SCSIResponsePDU )

        // ------------------------------------------------------------------------
        // R_SNACKRequest
        override _.R_SNACKRequest ( itt : ITT_T ) ( cont : unit -> unit ) : unit =
            let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( itt ), ValueNone )
            let result =
                m_ResendStat.Update ( fun oldStat ->
                    // Search SCSI Response PDU by specified ITT
                    let wr =
                        let srp = oldStat.m_SentRespPDUs
                        let rec loop ( cnt : int ) : bool =
                            if cnt < srp.Length then
                                let struct( _, itr_PDU ) = srp.[cnt]
                                match itr_PDU with
                                | :? SCSIResponsePDU as x ->
                                    x.InitiatorTaskTag = itt
                                | _ ->
                                    loop ( cnt + 1 )
                            else
                                false
                        loop 0

                    let builder = ImmutableArray.CreateBuilder< struct( ITT_T * ( unit -> unit ) ) >()
                    builder.Capacity <- oldStat.m_R_SNACK_Request.Length
                    for struct( itrITT, itrFunc ) in oldStat.m_R_SNACK_Request do
                        if itrITT <> itt then
                            builder.Add( struct( itrITT, itrFunc ) )
                    if not wr then
                        builder.Add( itt, cont )
                    let nextStat = {
                        oldStat with
                            m_R_SNACK_Request = builder.DrainToImmutable()
                    }
                    struct( nextStat, wr )
                )
            if result then
                // SCSI Response is exist,so response is already returned.
                // Thus, return response PDUs immidiatly.
                HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( wloginfo, "Requested SCSI Response is already returned." ) )
                cont()
            else
                HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( wloginfo, "Requested SCSI Response is not returned yet." ) )

        // ------------------------------------------------------------------------
        // Obtain the total number of received bytes.
        override _.GetReceiveBytesCount() : ResCountResult[] =
            m_ReceiveBytesCounter.Get DateTime.UtcNow

        // ------------------------------------------------------------------------
        // Obtain the total number of sent bytes.
        override _.GetSentBytesCount() : ResCountResult[] =
            m_SentBytesCounter.Get DateTime.UtcNow

    //=========================================================================
    // static method

    static member private SearchImmutableArray<'T> ( v : ImmutableArray< 'T > ) ( sf : ( 'T -> bool ) ) : 'T voption =
        let rec loop ( cnt : int ) =
            if cnt < v.Length then
                if sf v.[cnt] then
                    ValueSome v.[cnt]
                else
                    loop ( cnt + 1 )
            else
                ValueNone
        loop 0

    //=========================================================================
    // Private method

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send PDU to the initiator.
    /// </summary>
    /// <param name="pdu">PDU that should be send.</param>
    /// <returns>
    ///   If sending PDU is a SCSI Response PDU that is requested resend by R-SNACK request,
    ///   This function returns true, otherwise false.
    /// </returns>
    /// <remarks>
    ///   If specified PDU is Logout response PDU, this connection is closed.
    ///   This operation is ran in asynchronously.
    /// </remarks>
    member private this.SendPDUInternal ( pdu : ILogicalPDU ) : Task< ( unit -> unit ) voption > =
        task {
            let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( pdu.InitiatorTaskTag ), ValueNone )

            // Update StatSN, ExpCmdSN, MaxCmdSN values in the sending PDU.
            let struct( next_ExpCmdSN, next_MaxCmdSN ) = m_session.UpdateMaxCmdSN()
            let wUsedStatSN = m_StatSN
            let send_pdu = pdu.UpdateTargetValues wUsedStatSN next_ExpCmdSN next_MaxCmdSN

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_TRACE, fun g ->
                    let msg = sprintf "PDU send request. Opcode=%s,StatSN=%d" ( Constants.getOpcodeNameFromValue pdu.Opcode ) wUsedStatSN
                    g.Gen1( wloginfo, msg )
                )

            // Increment StatSN value.
            if pdu.NeedIncrementStatSN() then
                m_StatSN <- statsn_me.next m_StatSN

            let struct( rSnackRequested, resultFunc, deletePDUinThisMethod ) =
                m_ResendStat.Update ( fun oldStat -> 

                    // search R_SNACK request function
                    let r_snack_func =
                        Connection.SearchImmutableArray oldStat.m_R_SNACK_Request ( fun struct( witt, _ ) -> witt = pdu.InitiatorTaskTag )

                    // For SCSI Data-In or SCSI response PDU, R-SNACK is requested or not.
                    let rSnackRequested =
                        if pdu.Opcode = OpcodeCd.SCSI_DATA_IN || pdu.Opcode = OpcodeCd.SCSI_RES then
                            r_snack_func.IsSome
                        else
                            false

                    let nextStat1 =
                        if pdu.NeedIncrementStatSN() then
                            // If StatSN is updated ( = it will be acknowledged by initiator ), save this PDU to m_SentRespPDUs
                            // If old StatSN is exist, it assume that the old PDU remains, so remove that PDU.

                            let builder = ImmutableArray.CreateBuilder< struct( STATSN_T * ILogicalPDU ) >()
                            builder.Capacity <- oldStat.m_SentRespPDUs.Length + 1
                            for struct( itrStatSN, itrPDU ) in oldStat.m_SentRespPDUs do
                                if itrStatSN <> wUsedStatSN then
                                    builder.Add struct( itrStatSN, itrPDU )
                            builder.Add struct( wUsedStatSN, send_pdu )

                            {
                                oldStat with
                                    m_SentRespPDUs = builder.DrainToImmutable()
                            }

                        elif pdu.Opcode = OpcodeCd.SCSI_DATA_IN || pdu.Opcode = OpcodeCd.R2T then
                            // If Data-In PDU or R2T PDU is send to initiator, save this PDU for acknowledgement by initiator.
                            let datasn = 
                                if pdu.Opcode = OpcodeCd.SCSI_DATA_IN then
                                    ( pdu :?> SCSIDataInPDU ).DataSN
                                else
                                    ( pdu :?> R2TPDU ).R2TSN

                            let builder = ImmutableArray.CreateBuilder< struct( ITT_T * DATASN_T * ILogicalPDU ) >()
                            builder.Capacity <- oldStat.m_SentDataInPDUs.Length + 1
                            for struct( i_itt, i_datasn, itrPDU ) in oldStat.m_SentDataInPDUs do
                                if ( i_itt = pdu.InitiatorTaskTag && i_datasn = datasn ) |> not then
                                    builder.Add struct( i_itt, i_datasn, itrPDU )
                            builder.Add struct( pdu.InitiatorTaskTag, datasn, pdu )
                            {
                                oldStat with
                                    m_SentDataInPDUs = builder.DrainToImmutable()
                            }
                        else
                            oldStat

                    // If the PDU is not saved for retransmission, 
                    // it will disappear after this method completes.
                    let deletePDUinThisMethod = Object.ReferenceEquals( nextStat1, oldStat )

                    let struct( resultFunc, nextStat2 ) =
                        if not( rSnackRequested && pdu.Opcode = OpcodeCd.SCSI_RES ) then
                            struct( ValueNone, nextStat1 )
                        else
                            if r_snack_func.IsSome then

                                // remove R_SNACK request
                                let builder = ImmutableArray.CreateBuilder< struct( ITT_T * ( unit -> unit ) ) >()
                                builder.Capacity <- nextStat1.m_R_SNACK_Request.Length
                                for struct( witt, wf ) in nextStat1.m_R_SNACK_Request do
                                    if witt <> pdu.InitiatorTaskTag then
                                        builder.Add struct( witt, wf )

                                let struct( _, wResultFunc ) = r_snack_func.Value
                                struct(
                                    ValueSome( wResultFunc ),
                                    {
                                        nextStat1 with
                                            m_R_SNACK_Request = builder.DrainToImmutable()
                                    }
                                )
                            else
                                struct( ValueNone, nextStat1 )

                    struct( nextStat2, struct( rSnackRequested, resultFunc, deletePDUinThisMethod ) )
                )

            // Send PDU to the initiator.
            // If R-SNACK is requested, requested SCSI Data-In and SCSI response PDU does not be sent.
            if not rSnackRequested then
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_TRACE, fun g ->
                        let msg = sprintf "Opcode=%s" ( Constants.getOpcodeNameFromValue pdu.Opcode )
                        g.Gen1( wloginfo, "PDU send to initiator. " + msg )
                    )

                // Send PDU
                let! sentBytes =
                    PDU.SendPDU(
                        m_MaxRecvDataSegmentLength_I,
                        m_COParams.HeaderDigest.[0],
                        m_COParams.DataDigest.[0],
                        ValueSome( m_TSIH ),
                        ValueSome( m_CID ),
                        ValueSome( m_Counter ),
                        m_ObjID,
                        m_StreamForWrite,
                        send_pdu
                    )

                // Flush the buffer periodically
                if m_SendTask.Count <= 0 || m_SendTask.ProcessedCount % ( uint64 Constants.BDLU_MAX_TASKSET_SIZE / 1UL ) = 0UL then
                    do! m_StreamForWrite.FlushAsync()
                m_SentBytesCounter.AddCount DateTime.UtcNow ( int64 sentBytes )
            else
                HLogger.Trace( LogID.V_TRACE, fun g ->
                    let msg = sprintf "Opcode=%s" ( Constants.getOpcodeNameFromValue pdu.Opcode )
                    g.Gen1( wloginfo, "PDU send ommited. " + msg )
                )

            // If Logout response PDU was sended, close this connection.
            if pdu.Opcode = OpcodeCd.LOGOUT_RES then
                let lrp = pdu :?> LogoutResponsePDU
                if lrp.CloseAllegiantConnection then
                    ( this :> IConnection ).Close()
                    m_session.RemoveConnection m_CID m_Counter

            // If the PDU will be lost after this method completes, release the buffer before that.
            if deletePDUinThisMethod then
                match pdu with
                | :? SCSIResponsePDU as x ->
                    PooledBuffer.Return x.DataInBuffer
                | :? NOPInPDU as x ->
                    PooledBuffer.Return x.PingData
                | _ -> ()

            return resultFunc
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Receive PDU by async read continuation function.
    /// </summary>
    member private this.ReceivePDUInFullFeaturePhase () : Task<unit> =

        task {
//            let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueNone, ValueNone )
            let headerDigest = m_COParams.HeaderDigest.[0]
            let dataDigest = m_COParams.DataDigest.[0]
            let mrdsl_t = m_COParams.MaxRecvDataSegmentLength_T


            // If termination is requested, stop receive PDUs.
            while not m_Killer.IsNoticed do

                // Receive a PDU from the connection
                try
                    let! lpdu = PDU.Receive(
                        mrdsl_t, headerDigest, dataDigest, ValueSome m_TSIH, ValueSome m_CID, ValueSome m_Counter, m_StreamForRead, Standpoint.Target
                    )

                    let wByteCount = lpdu.ByteCount
                    if wByteCount.IsSome then
                        m_ReceiveBytesCounter.AddCount DateTime.UtcNow ( int64 wByteCount.Value )

                    // *** In following code, HasExpStatSN must be always true.

                    let curStatSN = m_StatSN
                    let maxLimit = statsn_me.incr Constants.MAX_STATSN_DIFF curStatSN 
                    let minLimit = statsn_me.decr Constants.MAX_STATSN_DIFF curStatSN
                    if  m_SWParams.ErrorRecoveryLevel = 0uy &&
                        lpdu.HasExpStatSN &&
                        ( statsn_me.lessThan lpdu.ExpStatSN minLimit ||
                          statsn_me.lessThan maxLimit lpdu.ExpStatSN )
                    then
                        // When current StatSN and initiator's ExpStatSN are significantly different,
                        // it occurs session recovery.
                        HLogger.Trace( LogID.E_STATSN_SIGNIFICANTLY_DIFFERENT, fun g -> g.Gen2( m_LogInfo, curStatSN, lpdu.ExpStatSN ) )
                        m_session.DestroySession()
                    else
                        // Delete acknowledged PDU by received ExpStatSN value.
                        if lpdu.HasExpStatSN then
                            if HLogger.IsVerbose then
                                let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( lpdu.InitiatorTaskTag ), ValueNone )
                                HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( wloginfo, sprintf "Received ExpStatSN=%d" lpdu.ExpStatSN ) )

                            this.DeleteAcknowledgedPDU lpdu.ExpStatSN

                        // Push the received PDU into session component
//                        m_ReceiveTask.Enqueue ( fun () ->
                            m_session.PushReceivedPDU this lpdu
//                        )

                with
                | :? RejectPDUException as x ->
                    if not m_Killer.IsNoticed then
                        // Send Reject PDU
                        m_session.RejectPDUByHeader m_CID m_Counter x.Header x.Reason
        }

    /// <summary>
    ///  Delete acknowledged PDU from m_SentRespPDUs or m_SentDataInPDUs
    /// </summary>
    /// <param name="statsn">Received StatSN value</param>
    member private _.DeleteAcknowledgedPDU ( statsn : STATSN_T ) : unit =

        let acknowledgedPDUs =
            m_ResendStat.Update ( fun oldStat ->

                let srp_Builder = ImmutableArray.CreateBuilder< struct( STATSN_T * ILogicalPDU ) >()
                srp_Builder.Capacity <- oldStat.m_SentRespPDUs.Length

                let ackdPDUs = List< struct( STATSN_T * ILogicalPDU ) >( oldStat.m_SentRespPDUs.Length )
                for struct( itrStatSN, itrPDU ) in oldStat.m_SentRespPDUs do
                    if statsn_me.lessThan itrStatSN statsn then
                        ackdPDUs.Add struct( itrStatSN, itrPDU )
                    else
                        srp_Builder.Add struct( itrStatSN, itrPDU )

                // If deleted PDU is SCSI Response PDU, delete related PDUs from m_SentDataInPDUs
                let scsiResITT = List< ITT_T >( ackdPDUs.Count )
                for struct( _, itrPDU ) in ackdPDUs do
                    if itrPDU.Opcode = OpcodeCd.SCSI_RES then
                        scsiResITT.Add itrPDU.InitiatorTaskTag

                let sdip_Builder = ImmutableArray.CreateBuilder< struct( ITT_T * DATASN_T * ILogicalPDU ) >()
                sdip_Builder.Capacity <- oldStat.m_SentDataInPDUs.Length
                for struct( i_itt, i_datasn, itrPDU ) in oldStat.m_SentDataInPDUs do
                    if scsiResITT.Contains i_itt |> not then
                        sdip_Builder.Add struct( i_itt, i_datasn, itrPDU )

                let nextStat = {
                    oldStat with
                        m_SentRespPDUs = srp_Builder.DrainToImmutable();
                        m_SentDataInPDUs = sdip_Builder.DrainToImmutable();
                }

                struct( nextStat, ackdPDUs )
            )

        for struct( itrStatSN, itrPDU ) in acknowledgedPDUs do

            HLogger.Trace(
                LogID.V_TRACE,
                fun g ->
                    let wloginfo = struct ( m_ObjID, ValueSome( m_CID ), ValueSome( m_Counter ), ValueSome( m_TSIH ), ValueSome( itrPDU.InitiatorTaskTag ), ValueNone )
                    let msg = 
                        sprintf
                            "StatSN(%d) acknowledged. Opcode=%s, ExpStatSN=%d"
                            itrStatSN
                            ( Constants.getOpcodeNameFromValue itrPDU.Opcode )
                            statsn
                    g.Gen1( wloginfo, msg )
            )

            // Delete acknowledged PDUs
            match itrPDU with
            | :? SCSIResponsePDU as x ->
                PooledBuffer.Return x.DataInBuffer
            | :? NOPInPDU as x ->
                PooledBuffer.Return x.PingData
            | _ -> ()

            match itrPDU.NeedResponseFence with
            | ResponseFenceNeedsFlag.R_Mode
            | ResponseFenceNeedsFlag.W_Mode as x ->
                m_session.NoticeUnlockResponseFence( x )
            | _ -> ()


