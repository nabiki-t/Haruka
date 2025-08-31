//=============================================================================
// Haruka Software Storage.
// Session.fs : Defines Session class
// Session class inprement the ISession interface, and
// represent target endpoint of iSCSI session notion.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.Immutable

open Haruka
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// <summary>
///   Record type that holds a connection information.
/// </summary>
[<NoComparison>]
type CIDInfo = {
    /// Interface of the connection object. This connection is represented CID and Counter values.
    Connection : IConnection;

    /// CID value of the connection. ( Same as Connection.CID )
    CID : CID_T;

    /// A connection counter value. ( Same as Connection.ConCounter )
    /// This counter value distinguish between connections that has same CID value.
    /// Notice that, if connection re-instantiate with existing CID is performed, 
    /// there are PDUs that received old connection and new one at the same time.
    Counter : CONCNT_T;
}

/// <summary>
///   Send PDU type value for Session.SendOtherPDU_sub function.
/// </summary>
type SendOtherPDUType =
    /// Send a PDU other than SCSI Response or SCSI Data-In PDU.
    | NORMAL
    /// Re-send PDU for Data SNACK or Status SNACK request.
    | RESEND
    /// Re-send PDU for R-Data SNACK request.
    | R_SNACK_RESEND

/// <summary>
///  Holds iSCSI tasks waiting to be executed.
/// </summary>
[<NoComparison>]
type ProcessWaitQueue =
    {
        /// Holds tasks waiting to be executed
        WaitingQueue : ImmutableDictionary< ITT_T, IIscsiTask >;

        /// Number of running tasks.
        RunningCount : int;

        /// ExpCmdSN
        ExpCmdSN : CMDSN_T;// = cmdsn_me.toPrim newCmdSN

        /// MaxCmdSN
        MaxCmdSN : CMDSN_T;//= cmdsn_me.toPrim ( newCmdSN + cmdsn_me.fromPrim Constants.ACCEPTABLE_CMDSN_COUNT )

        /// CmdSN that will be established command to SCSI layer
        NextProcCmdSN : CMDSN_T;//= cmdsn_me.toPrim newCmdSN

    }


/// <summary>
///   Session class definition.
///   It implements a target endpoint of session.
///   Session component is generated and holded in the StatusMaster component,
///   and has connection components belonging to this session.
/// </summary>
/// <param name="m_StatusMaster">
///   Interface of the status master object that hosts this session object.
/// </param>
/// <param name="m_CreatedTime">
///   Date time when this session was created.
/// </param>
/// <param name="m_I_TNexus">
///   I_T Nexus value of this session.
/// </param>
/// <param name="m_TSIH">
///   TSIH value of this session.
/// </param>
/// <param name="m_sessionParameter">
///   Effective session parameter values.
/// </param>
/// <param name="newCmdSN">
///   A CmdSN value that should be used to initial value in this session .
/// </param>
/// <param name="m_Killer">
///   An object that notices terminate request to this object.
/// </param>
type Session
    (
        m_StatusMaster : IStatus,
        m_CreatedTime : DateTime,
        m_I_TNexus : ITNexus,
        m_TSIH : TSIH_T, 
        m_sessionParameter : IscsiNegoParamSW,
        newCmdSN : CMDSN_T,
        m_Killer : IKiller
    ) as this =

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// The collection of CID relative information, indexed by CID
    let m_CIDs = new OptimisticLock< ImmutableDictionary< CID_T, CIDInfo > >( ImmutableDictionary.Empty )

    /// Counter of instanciation connections
    let mutable m_ConnectionCounter = 0

    /// Process wait queue
    let m_ProcessWaitQueue = new OptimisticLock< ProcessWaitQueue >({
        WaitingQueue = ImmutableDictionary< ITT_T, IIscsiTask >.Empty;
        RunningCount = 0;
        ExpCmdSN = newCmdSN;
        MaxCmdSN = ( cmdsn_me.incr Constants.BDLU_MAX_TASKSET_SIZE newCmdSN );
        NextProcCmdSN = newCmdSN;
    })

    /// SCSI Task Router object. ( the Procotol Service Interface )
    let m_TaskRouter =
        new TaskRouter( m_StatusMaster, this :> ISession, m_I_TNexus, m_TSIH, m_sessionParameter, m_Killer ) :> IProtocolService

    /// response fence lock object.
    let m_RespFense = new ResponseFence()

    /// TTT generator
    let mutable m_TTTGen = 0u

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone )
            g.Gen2( loginfo, "Session", "I_TNexus=" + ( m_I_TNexus.I_TNexusStr ) )
        )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface ISession with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            m_StatusMaster.RemoveSession m_TSIH

        // --------------------------------------------------------------------
        // Implementation of ISession.CreateDate
        override _.CreateDate : DateTime =
            m_CreatedTime

        // --------------------------------------------------------------------
        // Implementation of ISession.SessionParameter
        override _.SessionParameter : IscsiNegoParamSW =
            m_sessionParameter

        // ------------------------------------------------------------------------
        // Get TSIH value of this session.
        override _.TSIH : TSIH_T =
            m_TSIH

        // ------------------------------------------------------------------------
        // Get I_T Nexus value of this session.
        override _.I_TNexus : ITNexus =
            m_I_TNexus

        // --------------------------------------------------------------------
        // Implementation of ISession.IsExistCID
        override _.IsExistCID ( cid : CID_T ) : bool =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueNone, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone, "Session.IsExistCID" ) )
            try
                // Search the collection of CID relative information by specified CID.
                m_CIDs.obj.ContainsKey( cid )

            with
             | _ as x ->
                // If unexpected error is occurred, perform session recovery.
                HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, ValueNone, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone, x ) )
                m_Killer.NoticeTerminate()
                reraise()

        // --------------------------------------------------------------------
        // Implementation of ISession.AddNewConnection
        override this.AddNewConnection
            ( sock : Stream )
            ( conTime : DateTime )
            ( newCID : CID_T )
            ( netPortIdx : NETPORTIDX_T )
            ( tpgt : TPGT_T )
            ( iSCSIParamsCO : IscsiNegoParamCO ) : bool =

            let loginfo = struct( m_ObjID, ValueSome newCID, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "Session.AddNewConnection" ) )
            try
                if m_Killer.IsNoticed then
                    // If session is already terminated, this request is ignored.
                    HLogger.Trace( LogID.I_SESSION_ALREADY_TERMINATED, fun g -> g.Gen0 loginfo )
                    false
                else
                    this.AddConnection_Sub sock conTime newCID netPortIdx tpgt iSCSIParamsCO
            with
             | _ as x ->
                // If unexpected error is occurred, perform session recovery.
                HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                // If session component is dropped, connection component should be also dropped.
                m_Killer.NoticeTerminate()
                false

        // --------------------------------------------------------------------
        // Implementation of ISession.ReinstateConnection
        override this.ReinstateConnection
            ( sock : Stream )
            ( conTime : DateTime )
            ( newCID : CID_T )
            ( netPortIdx : NETPORTIDX_T )
            ( tpgt : TPGT_T )
            ( iSCSIParamsCO : IscsiNegoParamCO ) : bool =

            let loginfo = struct( m_ObjID, ValueSome newCID, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "Session.ReinstateConnection" ) )
            try
                if m_Killer.IsNoticed then
                    // If session is already terminated, this request is ignored.
                    HLogger.Trace( LogID.I_SESSION_ALREADY_TERMINATED, fun g -> g.Gen0 loginfo )
                    false
                else
                    // delete specified connection.
                    let oldConn : CIDInfo voption =
                        m_CIDs.Update( fun oldCIDs ->
                            let r, v = oldCIDs.TryGetValue newCID
                            if r then
                                let newCIDs = oldCIDs.Remove newCID
                                struct( newCIDs, ValueSome v )
                            else
                                struct( oldCIDs, ValueNone )
                        )

                    // close removed connection
                    if oldConn.IsSome then
                        oldConn.Value.Connection.Close()
                        HLogger.Trace( LogID.I_CONNECTION_REMOVED_IN_SESSION, fun g -> g.Gen1( loginfo, m_I_TNexus.I_TNexusStr ) )
                    else
                        HLogger.Trace( LogID.I_CONNECTION_ALREADY_REMOVED, fun g -> g.Gen1( loginfo, m_I_TNexus.I_TNexusStr ) )

                    ////////////////////////////////////////////////////////////////////////////////////////
                    // If ErrorRecoveryLevel=2, tasks remaining in the queue must be left in preparation
                    // for a task management function request with TASK REASSIGN.
                    // However, since Haruka only supports ErrorRecoveryLevel up to 1, all tasks received
                    // from the dropped connection will be deleted.
                    ////////////////////////////////////////////////////////////////////////////////////////
                    this.ExecuteTasks ValueNone

                    // Add new connection
                    this.AddConnection_Sub sock conTime newCID netPortIdx tpgt iSCSIParamsCO
            with
            | _ as x ->
                // If unexpected error is occurred, perform session recovery.
                HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                m_Killer.NoticeTerminate()
                false

        // --------------------------------------------------------------------
        // Remove closed connection from this session.
        override this.RemoveConnection ( cid : CID_T ) ( concnt : CONCNT_T ) : unit =
            
            let loginfo = struct( m_ObjID, ValueSome cid, ValueSome concnt, ValueSome m_TSIH, ValueNone, ValueNone )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "Session.RemoveConnection" ) )

            try
                if m_Killer.IsNoticed then
                    // If session is already terminated, this request is ignored.
                    HLogger.Trace( LogID.I_SESSION_ALREADY_TERMINATED, fun g -> g.Gen0 loginfo )
                else
                    // delete specified connection.
                    let struct( wOld, wNew ) =
                        m_CIDs.Update( fun oldCIDs ->
                            let r, v = oldCIDs.TryGetValue cid
                            if not r then
                                oldCIDs
                            elif v.Counter <> concnt then
                                oldCIDs
                            else
                                oldCIDs.Remove cid
                        )
                    if Object.ReferenceEquals( wOld, wNew ) then
                        HLogger.Trace( LogID.I_CONNECTION_ALREADY_REMOVED, fun g -> g.Gen1( loginfo, m_I_TNexus.I_TNexusStr ) )
                    else
                        HLogger.Trace( LogID.I_CONNECTION_REMOVED_IN_SESSION, fun g -> g.Gen1( loginfo, m_I_TNexus.I_TNexusStr ) )

                    ////////////////////////////////////////////////////////////////////////////////////////
                    // If ErrorRecoveryLevel=2, tasks remaining in the queue must be left in preparation
                    // for a task management function request with TASK REASSIGN.
                    // However, since Haruka only supports ErrorRecoveryLevel up to 1, all tasks received
                    // from the dropped connection will be deleted.
                    ////////////////////////////////////////////////////////////////////////////////////////
                    this.ExecuteTasks ValueNone

                    ////////////////////////////////////////////////////////////////////////////////////////
                    // If a connection is added between the time a connection is deleted above and
                    // the time the number is checked below,
                    // the number of connections will not be considered to have become 0.
                    ////////////////////////////////////////////////////////////////////////////////////////

                    // If all of connection are removed, close this session
                    if m_CIDs.obj.Count = 0 then
                        HLogger.Trace( LogID.I_ALL_CONNECTION_CLOSED, fun g -> g.Gen1( loginfo, m_I_TNexus.I_TNexusStr ) )

                        ////////////////////////////////////////////////////////////////////////////////////////
                        // If ErrorRecoveryLevel=2, session must retain while DefaultTime2Wait + DefaultTime2Retain second.
                        // But current implementation support ErrorRecoveryLevel=1, so if last connection has been closed,
                        // the session is closed immidiatry. (reffer iSCSI 3720 10.15.3 - 10.15.4 )
                        ////////////////////////////////////////////////////////////////////////////////////////

                        m_Killer.NoticeTerminate()
            with
            | _ as x ->
                // If unknown exception is occurred, perform session recovery
                let loginfo = struct( m_ObjID, ValueSome cid, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone )
                HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                m_Killer.NoticeTerminate()

        // --------------------------------------------------------------------
        // Implementation of ISession.PushReceivedPDU
        override this.PushReceivedPDU ( conn : IConnection ) ( pdu : ILogicalPDU ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "Session.PushReceivedPDU" ) )

            try
                if m_Killer.IsNoticed then
                    // If session is already terminated, this request is ignored.
                    HLogger.Trace( LogID.I_SESSION_ALREADY_TERMINATED, fun g -> g.Gen0 loginfo )
                else
                    // execute tasks
                    this.ExecuteTasks ( ValueSome struct( conn, pdu ) )

            with
            | :? SessionRecoveryException as x ->
                HLogger.Trace( LogID.E_SESSION_RECOVERY, fun g -> g.Gen1( loginfo, x.Message ) )
                m_Killer.NoticeTerminate()
            | _ as x ->
                // If unknown exception is occurred, perform session recovery
                HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                m_Killer.NoticeTerminate()

        // --------------------------------------------------------------------
        //   Get current ExpCmdValue and MaxCmdSN value.
        override _.UpdateMaxCmdSN() : struct( CMDSN_T * CMDSN_T ) =
            // Calculate latest MaxCmdSN value.
            fun oldQ ->
                // Calculate the current queue usage
                let curQLen = oldQ.WaitingQueue.Count + oldQ.RunningCount + ( m_TaskRouter.GetTaskQueueUsage() )

                // Queue usage may temporarily exceed the maximum queue length.
                // In that case, the command will not be accepted.
                let wNextMaxCmdSN =
                    if Constants.BDLU_MAX_TASKSET_SIZE < ( uint32 curQLen ) then
                        cmdsn_me.decr 1u oldQ.ExpCmdSN
                    else
                        cmdsn_me.incr ( Constants.BDLU_MAX_TASKSET_SIZE - uint32 curQLen ) oldQ.ExpCmdSN

                // Depending on queue usage, calculated MaxCmdSN may be less than the MaxCmdSN reported previously.
                // In this case, it always returns the larger value.
                let wNextMaxCmdSN2 =
                    if cmdsn_me.compare oldQ.MaxCmdSN wNextMaxCmdSN > 0 then
                        oldQ.MaxCmdSN
                    else
                        wNextMaxCmdSN

                let nextQ = {
                    oldQ with
                        MaxCmdSN = wNextMaxCmdSN2;
                }
                struct( nextQ, struct( nextQ.ExpCmdSN, nextQ.MaxCmdSN ) )
            |> m_ProcessWaitQueue.Update

        // --------------------------------------------------------------------
        //   Get the interface of specified connection.
        override _.GetConnection ( cid : CID_T ) ( counter : CONCNT_T ) : IConnection voption =
            // search current connection information
            let result, cidInfo = m_CIDs.obj.TryGetValue( cid )
            if not result then
                ValueNone
            elif cidInfo.Counter <> counter then
                ValueNone
            else ValueSome( cidInfo.Connection )

        // --------------------------------------------------------------------
        //   Get the collection of the connections.
        override _.GetAllConnections () : IConnection array =
            let o = m_CIDs.obj
            [|
                for itr in o -> itr.Value.Connection
            |]

        // --------------------------------------------------------------------
        // Get the protocol service interface of SCSI Task Router.
        override _.SCSITaskRouter : IProtocolService =
            m_TaskRouter

        // --------------------------------------------------------------------
        // Check if the session is still alive.
        override _.IsAlive : bool =
            m_Killer.IsNoticed |> not

        // ------------------------------------------------------------------------
        // Destroy all of the object in this session.
        // May be called multiple times for the same object.
        override _.DestroySession() : unit =
            // Notice terminate request to all of object in this session through killer object.
            m_Killer.NoticeTerminate()
            
        // ------------------------------------------------------------------------
        // Send response data to the initiator.
        override _.SendSCSIResponse
                ( reqCmdPDU : SCSICommandPDU )
                ( cid : CID_T )
                ( counter : CONCNT_T )
                ( recvDataLength : uint32 )
                ( argRespCode : iScsiSvcRespCd )
                ( argStatCode : ScsiCmdStatCd )
                ( senseData : PooledBuffer )
                ( resData : PooledBuffer )
                ( allocationLength : uint32 ) 
                ( needResponseFence : ResponseFenceNeedsFlag ) : unit =

            // Warning!!!
            // By the time this is called, reqCmdPDU.DataSegment may have already been recycled and should not be accessed.

            assert not ( senseData.Count > 0 && resData.Count > 0 )

            let itt = reqCmdPDU.InitiatorTaskTag
            let lun = reqCmdPDU.LUN
            let argSendDataBytes = if senseData.Count > 0 then senseData else resData
            let SPDTL = ( uint32 argSendDataBytes.Count )  // SCSI-Presented Data Transfer Length
            let bidirectCmd = reqCmdPDU.R && reqCmdPDU.W
            let readOnlyCmd = reqCmdPDU.R && ( not reqCmdPDU.W )
            let writeOnlyCmd = ( not reqCmdPDU.R ) && reqCmdPDU.W
            let loginfo = struct ( m_ObjID, ValueSome( cid ), ValueSome( counter ), ValueSome( m_TSIH ), ValueSome( itt ), ValueSome( lun ) )

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "Session.SendSCSIResponse." ) )

            // Search alligient connection
            match m_CIDs.obj.TryGetValue( cid ) with
            | true, conInfo when conInfo.CID = cid && conInfo.Counter = counter ->

                // get connection wide parameter values
                let comPara = conInfo.Connection.CurrentParams

                let berdl = reqCmdPDU.BidirectionalExpectedReadDataLength
                let edtl = reqCmdPDU.ExpectedDataTransferLength
                let mbl = m_sessionParameter.MaxBurstLength
                let mrdsl_I = comPara.MaxRecvDataSegmentLength_I

                // *********************************************
                // Note that the Data Segment of the SCSI Response PDU contains a 2-byte header,
                // so the SCSI Response PDU can transfer two bytes less data.
                // *********************************************

                // calclate sending data length
                let realSendDataLength =
                    if senseData.Count > 0 then
                        // Maximum sense data length is 252 bytes and minimum MaxRecvDataSegmentLength is 512 bytes,
                        // So, sense data should always be stored in the SCSI Response PDU.
                        // But, if the limit is exceeded, truncate the sense data so that it can be stored in the response PDU.
                        SPDTL
//                            |> min ( uint32 allocationLength )
                        |> min ( mbl - 2u )
                        //|> min ( if bidirectCmd then ( berdl - 2u ) else ( edtl - 2u ) )
                        |> min ( mrdsl_I - 2u )
                    else
                        SPDTL
                        |> min ( uint32 allocationLength )
                        |> min mbl
                        |> min ( if bidirectCmd then berdl else edtl )

                // data length of SCSI response PDU
                let dataPDUList =
                    if mrdsl_I - 2u < realSendDataLength || resData.Count > 0 then
                        // If the sense data is longer than MaxRecvDataSegmentLength, use the SCSI Data-In PDU.
                        // Normal response data that is not sense data, always uses SCSI Data-In PDUs.

                        // Divite sendDataBytes to some of Data-In PDUs
                        let rec loop sp cnt ( li : ILogicalPDU list ) =
                            if sp >= realSendDataLength then
                                li
                            else
                                let seglen = min mrdsl_I ( realSendDataLength - sp )
                                let lastPDUFlag = sp + mrdsl_I >= realSendDataLength
                                let w : ILogicalPDU = {
                                    F = lastPDUFlag;
                                    A = if m_sessionParameter.ErrorRecoveryLevel > 0uy then lastPDUFlag else false;
                                    O = false;  // ignored
                                    U = false;  // ignored
                                    S = false;  // Haruka does not use this flag
                                    Status = ScsiCmdStatCd.GOOD;  // ignored
                                    LUN =
                                        if m_sessionParameter.ErrorRecoveryLevel > 0uy && lastPDUFlag then
                                            lun
                                        else
                                            lun_me.zero;
                                    InitiatorTaskTag = itt;
                                    TargetTransferTag =
                                        if m_sessionParameter.ErrorRecoveryLevel > 0uy && lastPDUFlag then
                                            this.GenerateTTTValue()
                                        else
                                            ttt_me.fromPrim 0xffffffffu;
                                    StatSN = statsn_me.zero;
                                    ExpCmdSN = cmdsn_me.zero;
                                    MaxCmdSN = cmdsn_me.zero;
                                    DataSN = cnt;
                                    BufferOffset = sp;
                                    ResidualCount = 0u;
                                    DataSegment = argSendDataBytes.GetArraySegment ( int sp ) ( int seglen )
                                    ResponseFence = ResponseFenceNeedsFlag.Immediately;
                                }
                                loop ( sp + seglen ) ( datasn_me.next cnt ) ( w :: li )
                        loop 0u datasn_me.zero []
                    else
                        []

                // Create SCSI response PDU
                let resp : ILogicalPDU = {
                    o = if bidirectCmd then
                            berdl < SPDTL
                        else
                            false;
                    u = if bidirectCmd then
                            berdl > SPDTL
                        else
                            false;
                    O = if bidirectCmd || writeOnlyCmd then
                            edtl < recvDataLength
                        else 
                            edtl < SPDTL;
                    U = if bidirectCmd || writeOnlyCmd then
                            edtl > recvDataLength
                        else
                            edtl > SPDTL;

                    Response = argRespCode;
                    Status = argStatCode;
                    InitiatorTaskTag = itt;
                    SNACKTag = snacktag_me.zero;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    // Haruka does not support bidirectional command.
                    // At write only command, ExpDataSN must be zero, but always dataPDUList is empty.
                    // So simply, it sets dataPDUList count to ExpDataSN.
                    ExpDataSN =
                            datasn_me.fromPrim( uint32 dataPDUList.Length );
                    BidirectionalReadResidualCount =
                        if bidirectCmd then
                            if berdl > SPDTL then
                                berdl - SPDTL
                            else
                                SPDTL - berdl
                        else
                            0u;
                    ResidualCount =
                        if bidirectCmd || writeOnlyCmd then
                            if edtl > recvDataLength then
                                edtl - recvDataLength
                            else
                                recvDataLength - edtl
                        else
                            if edtl > SPDTL then
                                edtl - SPDTL
                            else
                                SPDTL - edtl;

                    SenseLength = uint16 senseData.Count;
                    SenseData = 
                        if senseData.Count > 0 && mrdsl_I - 2u >= realSendDataLength then
                            argSendDataBytes.GetArraySegment 0 ( int realSendDataLength )
                        else
                            ArraySegment.Empty;
                    ResponseData = ArraySegment.Empty; // The response data always uses a SCSI Data-In PDU.
                    ResponseFence = needResponseFence;
                    DataInBuffer = argSendDataBytes;
                }
#if false
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "writeOnlyCmd = %b" writeOnlyCmd )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "edtl = %d" edtl )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "recvDataLength = %d" recvDataLength )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SPDTL = %d" SPDTL )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU o = %b" ( resp :?> SCSIResponsePDU ).o )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU u = %b" ( resp :?> SCSIResponsePDU ).u )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU O = %b" ( resp :?> SCSIResponsePDU ).O )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU U = %b" ( resp :?> SCSIResponsePDU ).U )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU Response = %d" ( byte ( resp :?> SCSIResponsePDU ).Response ) )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU Status = %d" ( byte ( resp :?> SCSIResponsePDU ).Status ) )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU InitiatorTaskTag = %d" ( resp :?> SCSIResponsePDU ).InitiatorTaskTag )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU SNACKTag = %d" ( resp :?> SCSIResponsePDU ).SNACKTag )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU StatSN = %d" ( resp :?> SCSIResponsePDU ).StatSN )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU ExpCmdSN = %d" ( resp :?> SCSIResponsePDU ).ExpCmdSN )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU MaxCmdSN = %d" ( resp :?> SCSIResponsePDU ).MaxCmdSN )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU ExpDataSN = %d" ( resp :?> SCSIResponsePDU ).ExpDataSN )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU BidirectionalReadResidualCount = %d" ( resp :?> SCSIResponsePDU ).BidirectionalReadResidualCount )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU ResidualCount = %d" ( resp :?> SCSIResponsePDU ).ResidualCount )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU SenseLength = %d" ( resp :?> SCSIResponsePDU ).SenseLength )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU SenseData.Length = %d" ( resp :?> SCSIResponsePDU ).SenseData.Count  )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU ResponseData.Length = %d" ( resp :?> SCSIResponsePDU ).ResponseData.Count  )
                HLogger.Trace1( LogID.V_TRACE, loginfo, sprintf "SCSI response PDU ResponseFence = %s" (( resp :?> SCSIResponsePDU ).ResponseFence.ToString()) )
#endif

                resp :: dataPDUList
                |> List.rev
                |> List.iter ( fun itrPdu ->
                    match itrPdu.NeedResponseFence with
                    | ResponseFenceNeedsFlag.Irrelevant ->
                        ()  // Silentry ignore

                    | ResponseFenceNeedsFlag.Immediately ->
                        // Send PDU immidiatly without response fence lock
                        conInfo.Connection.SendPDU itrPdu

                    | ResponseFenceNeedsFlag.R_Mode ->
                        // Need R-Mode lock at response fence.
                        m_RespFense.NormalLock ( fun () ->
                            conInfo.Connection.SendPDU itrPdu
                        )

                    | ResponseFenceNeedsFlag.W_Mode ->
                        // Need W-Mode lock at response fence.
                        m_RespFense.RFLock ( fun () ->
                            conInfo.Connection.SendPDU itrPdu
                        )
                )
            | _ ->
                // Silentry ignore
                ()

        // ------------------------------------------------------------------------
        // Send reject PDU
        override this.RejectPDUByLogi ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) ( argReason : RejectReasonCd ) : unit =
            // search current connection information
            match m_CIDs.obj.TryGetValue( cid ) with
            | true, cidInfo when cidInfo.Counter = counter ->
                // Send reject PDU
                this.RejectPDUByLogi_ToConnection cidInfo.Connection pdu argReason
            | _ ->
                // silently ignore
                ()

        // ------------------------------------------------------------------------
        // Send reject PDU to the initiator with header bytes data..
        override this.RejectPDUByHeader ( cid : CID_T ) ( counter : CONCNT_T ) ( header : byte[] ) ( argReason : RejectReasonCd ) : unit =
            // search current connection information
            match m_CIDs.obj.TryGetValue( cid ) with
            | true, cidInfo when cidInfo.Counter = counter ->
                this.RejectPDUByHeader_ToConnection cidInfo.Connection header argReason
            | _ ->
                ()

        // ------------------------------------------------------------------------
        // Send response PDU other than SCSI response, TMF or reject.
        override this.SendOtherResponsePDU ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) : unit =
            this.SendOtherPDU_sub cid counter pdu SendOtherPDUType.NORMAL

        // ------------------------------------------------------------------------
        // Resend PDU with response fence.
        override this.ResendPDU ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) : unit =
            this.SendOtherPDU_sub cid counter pdu SendOtherPDUType.RESEND

        // ------------------------------------------------------------------------
        // Resend PDU for R-SNACK request with response fence.
        override this.ResendPDUForRSnack ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) : unit =
            this.SendOtherPDU_sub cid counter pdu SendOtherPDUType.R_SNACK_RESEND

        // ------------------------------------------------------------------------
        // Notice that session parameter values are changed.
        override _.NoticeUpdateSessionParameter ( _ : IscsiNegoParamSW ) : unit =
            // In current specification, the value that can be updated in FFPO is only InitiatorAlias.
            // And InitiatorAlias value is not used in Haruka.
            // So, silentry ignored this notice.
            ()

        // ------------------------------------------------------------------------
        // Unlock response fence.
        override _.NoticeUnlockResponseFence ( mode : ResponseFenceNeedsFlag ) : unit =
            if HLogger.IsVerbose then
                let smode = ResponseFenceNeedsFlag.toString mode
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "Session.NoticeUnlockResponseFence. mode=" + smode ) )
            m_RespFense.Free()

    //=========================================================================
    // private method
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///  Add new connection to the session.
    /// </summary>
    /// <param name="sock">
    ///  Socket of the newly established connection.
    /// </param>
    /// <param name="newCID">
    ///  Connection ID of the new connection.
    /// </param>
    /// <param name="netPortIdx">
    ///  Network portal index number where this connection was established.
    /// </param>
    /// <param name="tpgt">
    ///  Target Portal Group Tag
    /// </param>
    /// <param name="iSCSIParamsCO">
    ///  Negociated connection only parameters.
    /// </param>
    /// <param name="nextFirstPDU">
    ///  Next PDU data, that have to be processed by newly created connection.
    /// </param>
    member private this.AddConnection_Sub
        ( sock : Stream )
        ( conTime : DateTime )
        ( newCID : CID_T )
        ( netPortIdx : NETPORTIDX_T )
        ( tpgt : TPGT_T )
        ( iSCSIParamsCO : IscsiNegoParamCO ) : bool =

        // Generate connection counter value
        let newConCounter = Interlocked.Increment ( &m_ConnectionCounter ) |> concnt_me.fromPrim

        // Create instance of connection class
        let newConnection =
            new Connection(
                m_StatusMaster,
                tpgt,
                sock,
                conTime,
                iSCSIParamsCO,
                m_sessionParameter,
                this :> ISession,
                m_TSIH,
                newCID,
                newConCounter,
                netPortIdx,
                m_Killer
            )

        // Create new CID relative information
        let cidinfo : CIDInfo =
            {
                Connection = newConnection;
                CID = newCID;
                Counter = newConCounter;
            }

        let loginfo = struct( m_ObjID, ValueSome newCID, ValueSome newConCounter, ValueSome m_TSIH, ValueNone, ValueNone )
        let maxCons = m_sessionParameter.MaxConnections

        let struct( succeed : bool, logid : LogID, msgfunc : ( GenLogMsg -> string ) ) = 
            m_CIDs.Update( fun oldVal ->
                let wCurCidCnt = oldVal.Count
                if wCurCidCnt >= (int)maxCons then
                    let mf = fun ( g : GenLogMsg ) -> g.Gen2( loginfo, maxCons, wCurCidCnt )
                    struct( oldVal, struct( false, LogID.E_TOO_MANY_CONNECTIONS, mf ) )
                elif oldVal.ContainsKey newCID then
                    let msg = "Consistency error. Specified CID is already exist."
                    let mf = fun ( g : GenLogMsg ) -> g.Gen2( loginfo, m_I_TNexus.I_TNexusStr, msg )
                    struct( oldVal, struct( false, LogID.E_FAILED_ADD_CONNECTION, mf ) )
                else
                    let newVal = oldVal.Add( newCID, cidinfo )
                    let mf = fun ( g : GenLogMsg ) -> g.Gen1( loginfo, m_I_TNexus.I_TNexusStr )
                    struct( newVal, struct( true, LogID.I_NEW_CONNECTION_ADDED_TO_SESSION, mf ) )
            )
        HLogger.Trace( logid, msgfunc )
        if succeed then
            ( newConnection :> IConnection ).StartFullFeaturePhase()
        succeed

    // ------------------------------------------------------------------------
    /// <summary>
    ///  This function updates the task queue and executes any tasks that are executable.
    /// </summary>
    /// <param name="recvPDU">
    ///  Received PDU information. Pair of the connection and Logical PDU.
    /// </param>
    member private this.ExecuteTasks ( recvPDU : struct( IConnection * ILogicalPDU ) voption ) : unit =
        let runTaskCount =
            // extracts executable tasks
            let runTask2 =
                this.UpdateProcessWaitQueue recvPDU
                |> m_ProcessWaitQueue.Update

            // run tasks
            for itr in runTask2 do
                itr()
            runTask2.Count

        // Decrement the number of running tasks.
        fun oldQ -> {
            oldQ with
                RunningCount = max 0 ( oldQ.RunningCount - runTaskCount )
        }
        |> m_ProcessWaitQueue.Update
        |> ignore

    // ------------------------------------------------------------------------
    /// <summary>
    ///  This function insert a task into the queue based on the received PDU,
    ///  extracts executable tasks, and deletes unnecessary tasks.
    /// </summary>
    /// <param name="recvPDU">
    ///  Received PDU information. Pair of the connection and Logical PDU.
    /// </param>
    /// <param name="currentQ">
    ///  Current process wait queue.
    /// </param>
    /// <returns>
    ///  Updated process wait queue and task to be executed.
    /// </returns>
    member private this.UpdateProcessWaitQueue 
            ( recvPDU : struct( IConnection * ILogicalPDU ) voption )
            ( currentQ : ProcessWaitQueue ) :
            struct ( ProcessWaitQueue * List< unit -> unit > ) =
        
        let loginfo =
            if recvPDU.IsSome then
                let struct( wcon, wpdu ) = recvPDU.Value
                struct ( m_ObjID, ValueSome wcon.CID, ValueSome wcon.ConCounter, ValueSome m_TSIH, ValueSome wpdu.InitiatorTaskTag, ValueNone )
            else
                struct ( m_ObjID, ValueNone, ValueNone, ValueSome m_TSIH, ValueNone, ValueNone )

        let struct ( refuseTask, nextWaitTaskList ) =
            if recvPDU.IsSome then
                let struct( wcon, wpdu ) = recvPDU.Value
                match wpdu.Opcode with
                | OpcodeCd.NOP_OUT ->
                    this.UpdateProcessWaitQueue_NopIN wcon ( wpdu :?> NOPOutPDU ) currentQ
                | OpcodeCd.SCSI_TASK_MGR_REQ ->
                    this.UpdateProcessWaitQueue_TaskMgrReq wcon ( wpdu :?> TaskManagementFunctionRequestPDU ) currentQ
                | OpcodeCd.LOGOUT_REQ ->
                    this.UpdateProcessWaitQueue_Logout wcon ( wpdu :?> LogoutRequestPDU ) currentQ
                | OpcodeCd.SNACK ->
                    // SNACK PDU is inserted directly into vImmidiate_RunList variable.
                    None, currentQ.WaitingQueue
                | OpcodeCd.SCSI_COMMAND ->
                    this.UpdateProcessWaitQueue_SCSICommand wcon ( wpdu :?> SCSICommandPDU ) currentQ
                | OpcodeCd.TEXT_REQ ->
                    this.UpdateProcessWaitQueue_TextReq wcon ( wpdu :?> TextRequestPDU ) currentQ
                | OpcodeCd.SCSI_DATA_OUT ->
                    this.UpdateProcessWaitQueue_SCSIDataOut wcon ( wpdu :?> SCSIDataOutPDU ) currentQ
                | _ ->
                    let msg = sprintf "Unknown opcode(0x%02X)" ( byte wpdu.Opcode )
                    HLogger.Trace( LogID.F_INTERNAL_ASSERTION, fun g -> g.Gen1( loginfo, msg ) )
                    raise <| new InternalAssertionException( msg )
            else
                None, currentQ.WaitingQueue

        if refuseTask.IsSome then
            // If rejected, return the task to send reject and skip following process.
            let newQ = {
                currentQ with
                    WaitingQueue = nextWaitTaskList;
            }
            struct ( newQ, List< unit -> unit >( [| refuseTask.Value |] ) )
        else
            // Search executable immidiate task
            let ( vImmidiate_RunList : List< unit -> unit > ), ( nextWaitTaskList2 : KeyValuePair< ITT_T, IIscsiTask > [] ) =
                let wTaskList = List< unit -> unit >( nextWaitTaskList.Count + 1 )

                // SNACK PDUs have ITT fields, but this value specifies the task to be processed
                // and is not a unique identifier for identifying the task.
                // So, SNACK PDU must not be inserted to m_ProcessWaitQueue.
                // In addition, SNACK PDU is always handled as immediate task and it is always runnable alone.
                if recvPDU.IsSome then
                    let struct( wcon, wpdu ) = recvPDU.Value
                    if wpdu.Opcode = OpcodeCd.SNACK then 
                        let iscsitask = new IscsiTaskOnePDUCommand( this :> ISession, wcon.CID, wcon.ConCounter, wpdu, false ) :> IIscsiTask
                        let struct( extask, _ ) = iscsitask.GetExecuteTask()
                        wTaskList.Add extask

                let wwtl2 =
                    [|
                        for itr in nextWaitTaskList do
                            if ( ValueOption.defaultValue false itr.Value.Immidiate ) && itr.Value.IsExecutable then
                                let struct( a, b ) = itr.Value.GetExecuteTask()
                                wTaskList.Add a
                                KeyValuePair( itr.Key, b )
                            else
                                itr
                    |]
                wTaskList, wwtl2 

            let nonImmidiateTaskIdx =
                let wvidx = List<int>( nextWaitTaskList2.Length )
                nextWaitTaskList2
                |> Array.iteri ( fun idx itr ->
                    if not( ValueOption.defaultValue true itr.Value.Immidiate ) then
                        wvidx.Add idx
                )
                wvidx

            // update ExpCmdSN
            let rec loop1 argCmdSN =
                let r =
                    nonImmidiateTaskIdx.Exists ( fun itr ->
                        let t = nextWaitTaskList2.[ itr ]
                        match t.Value.CmdSN with
                        | ValueSome( v2 ) -> v2 = argCmdSN
                        | ValueNone -> false
                    )
                if r then
                    loop1 ( cmdsn_me.next argCmdSN )
                else
                    argCmdSN
            let nextExpCmdSN = loop1( currentQ.ExpCmdSN )

            // Delete a task that no longer has a source connection
            let nextWaitTaskList3 =
                let wCIDs = m_CIDs.obj
                nextWaitTaskList2
                |> Array.filter ( fun itr ->
                        let struct( cid, concnt ) = itr.Value.AllegiantConnection
                        let r,v = wCIDs.TryGetValue cid
                        if not r || v.Counter <> concnt then
                            HLogger.Trace(
                                LogID.W_ISCSI_TASK_REMOVED,
                                fun g -> g.Gen3( loginfo, itr.Value.InitiatorTaskTag, itr.Value.CmdSN, "Connection removed" )
                            )
                            false
                        else
                            true
                    )

            let nonImmidiateTaskIdx2 =
                if nextWaitTaskList3.Length = nextWaitTaskList2.Length then
                    nonImmidiateTaskIdx
                else
                    // The referenced array has been updated and needs to be recreated.
                    let wvidx = List<int>( nextWaitTaskList3.Length )
                    nextWaitTaskList3
                    |> Array.iteri ( fun idx itr ->
                        if not( ValueOption.defaultValue true itr.Value.Immidiate ) then
                            wvidx.Add idx
                    )
                    wvidx

            // Search non-immidiate executable task
            // Note that the following process will update nextWaitTaskList3 as it proceeds.
            let rec loop2 ( argNextProcCmdSN : CMDSN_T ) ( li : List< unit -> unit > ) : CMDSN_T =
                if nextExpCmdSN = argNextProcCmdSN || cmdsn_me.lessThan nextExpCmdSN argNextProcCmdSN then
                    argNextProcCmdSN
                else
                    let currentCmdSNTaskIdx =
                        nonImmidiateTaskIdx2.FindAll ( fun itr ->
                            let cmdsn = nextWaitTaskList3.[itr].Value.CmdSN
                            ( ValueOption.isSome cmdsn && ValueOption.get cmdsn = argNextProcCmdSN )
                        )
                    
                    if currentCmdSNTaskIdx.Count = 0 then
                        // If any CmdSN number is missing, skip this number
                        loop2 ( cmdsn_me.next argNextProcCmdSN ) li

                    else
                        let executables = [|
                            for itr in currentCmdSNTaskIdx do
                                if nextWaitTaskList3.[itr].Value.IsExecutable then
                                    yield itr
                        |]
                        if executables.Length <= 0 then
                            argNextProcCmdSN
                        else
                            for idx in executables do
                                let struct( a, b ) = nextWaitTaskList3.[idx].Value.GetExecuteTask()
                                nextWaitTaskList3.[idx] <- KeyValuePair( nextWaitTaskList3.[idx].Key, b )
                                li.Add a

                            let removableCount =
                                executables
                                |> Array.sumBy ( fun itr -> if nextWaitTaskList3.[itr].Value.IsRemovable then 1 else 0 )

                            if executables.Length = removableCount then
                                loop2 ( cmdsn_me.next argNextProcCmdSN ) li
                            else
                                argNextProcCmdSN

            let nonImm_RunList = List< unit -> unit >( nonImmidiateTaskIdx2.Count )
            let nextNextProcCmdSN = loop2( currentQ.NextProcCmdSN ) nonImm_RunList

            // search removable task and delete that task
            let rmITT = 
                nextWaitTaskList3
                |> Array.choose ( fun itr -> if itr.Value.IsRemovable then Some itr.Key else None )
            let nextWaitTaskList4 =
                let v =
                    nextWaitTaskList3
                    |> Seq.filter ( fun itr -> Array.exists ( (=) itr.Key ) rmITT |> not )
                v.ToImmutableDictionary()

            // gen executable task list
            let resultTaskList = List< unit -> unit >( vImmidiate_RunList.Count + nonImm_RunList.Count )
            resultTaskList.AddRange vImmidiate_RunList
            resultTaskList.AddRange nonImm_RunList

            let nextQ = {
                WaitingQueue = nextWaitTaskList4;
                RunningCount = currentQ.RunningCount + resultTaskList.Count;
                ExpCmdSN = nextExpCmdSN;
                MaxCmdSN = currentQ.MaxCmdSN;       // MaxCmdSN is updated to the latest value when the value is referenced.
                NextProcCmdSN = nextNextProcCmdSN;
            }
            struct ( nextQ, resultTaskList )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Check CmdSN value that acceptable or not.
    /// </summary>
    /// <param name="argImmidiate">
    ///   Immidiate flag value. If received task is immidiate, specify true.
    /// </param>
    /// <param name="argCmdSN">
    ///   CmdSN value.
    /// </param>
    /// <returns>
    ///   It specified CmdSN is acceptable returns true, otherwise false.
    /// </returns>
    member private _.CheckCmdSNValue ( argImmidiate : bool ) ( argCmdSN : CMDSN_T ) ( currentQ : ProcessWaitQueue ) : bool =
        if argImmidiate then
            true
        elif cmdsn_me.lessThan argCmdSN currentQ.ExpCmdSN then 
            false
        elif cmdsn_me.lessThan currentQ.MaxCmdSN argCmdSN then 
            false
        else
            true

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update or insert a NopOUT iSCSI task to m_ProcessWaitQueue, and execute runnable tasks.
    /// </summary>
    /// <param name="conn">
    ///   Connection that is received a PDU specified by pdu argument.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    /// <returns>
    ///   If received PDU is no acceptable, it returns task to send reject PDU, otherwise returns next task list status.
    /// </returns>
    member private this.UpdateProcessWaitQueue_NopIN ( conn : IConnection ) ( pdu : NOPOutPDU ) ( currentQ : ProcessWaitQueue ) : 
            struct ( ( unit -> unit ) option * ImmutableDictionary< ITT_T, IIscsiTask > ) =
        let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )

        if pdu.InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu then
            // Ping response PDU is silently ignored.
            HLogger.Trace( LogID.I_NOP_OUT_PING_RESPONSE_RECEIVED, fun g -> g.Gen1( loginfo, pdu.TargetTransferTag ) )
            None, currentQ.WaitingQueue

        elif not( this.CheckCmdSNValue pdu.I pdu.CmdSN currentQ ) then
            // Reject and ignore
            let msg = sprintf "Invalid CmdSN value(0x%08X)." pdu.CmdSN
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g -> g.Gen1( loginfo, msg ) )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        elif currentQ.WaitingQueue.ContainsKey( pdu.InitiatorTaskTag ) then
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                let msg = sprintf "Specified ITT(0x%08X) is in alive." pdu.InitiatorTaskTag
                g.Gen1( loginfo, msg )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        else
            // Insert task to the process wait queue.
            let task = new IscsiTaskOnePDUCommand( this :> ISession, conn.CID, conn.ConCounter, pdu, false ) :> IIscsiTask
            None, currentQ.WaitingQueue.Add( pdu.InitiatorTaskTag, task )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update or insert a task management request iSCSI task to m_ProcessWaitQueue, and execute runnable tasks.
    /// </summary>
    /// <param name="cidInfo">
    ///   Connection that is received a PDU specified by pdu argument.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    /// <returns>
    ///   If received PDU is no acceptable, it returns task to send reject PDU, otherwise returns next task list status.
    /// </returns>
    member private this.UpdateProcessWaitQueue_TaskMgrReq ( conn : IConnection ) ( pdu : TaskManagementFunctionRequestPDU ) ( currentQ : ProcessWaitQueue ) : 
            struct ( ( unit -> unit ) option * ImmutableDictionary< ITT_T, IIscsiTask > ) =
        let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )

        if not <| this.CheckCmdSNValue pdu.I pdu.CmdSN currentQ then
            // Reject and ignore
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                let msg = sprintf "Invalid CmdSN value(0x%08X)." pdu.CmdSN
                g.Gen1( loginfo, msg )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        elif currentQ.WaitingQueue.ContainsKey pdu.InitiatorTaskTag then
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                let msg = sprintf "Specified ITT(0x%08X) is in alive." pdu.InitiatorTaskTag
                g.Gen1( loginfo, msg )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        else
            // Insert task to the process wait queue.
            let task = new IscsiTaskOnePDUCommand( this :> ISession, conn.CID, conn.ConCounter, pdu, false ) :> IIscsiTask
            None, currentQ.WaitingQueue.Add( pdu.InitiatorTaskTag, task )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update or insert a logout request iSCSI task to m_ProcessWaitQueue, and execute runnable tasks.
    /// </summary>
    /// <param name="cidInfo">
    ///   Connection that is received a PDU specified by pdu argument.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    /// <returns>
    ///   If received PDU is no acceptable, it returns task to send reject PDU, otherwise returns next task list status.
    /// </returns>
    member private this.UpdateProcessWaitQueue_Logout ( conn : IConnection ) ( pdu : LogoutRequestPDU ) ( currentQ : ProcessWaitQueue ) : 
            struct ( ( unit -> unit ) option * ImmutableDictionary< ITT_T, IIscsiTask > ) =
        let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )

        if not <| this.CheckCmdSNValue pdu.I pdu.CmdSN currentQ then
            // Reject and ignore
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                let msg = sprintf "Invalid CmdSN value(0x%08X)." pdu.CmdSN
                g.Gen1( loginfo, msg )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        elif currentQ.WaitingQueue.ContainsKey pdu.InitiatorTaskTag then
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                let msg = sprintf "Specified ITT(0x%08X) is in alive." pdu.InitiatorTaskTag
                g.Gen1( loginfo, msg )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        else
            // Insert task to the process wait queue.
            let task = new IscsiTaskOnePDUCommand( this :> ISession, conn.CID, conn.ConCounter, pdu, false ) :> IIscsiTask
            None, currentQ.WaitingQueue.Add( pdu.InitiatorTaskTag, task )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update or insert a SCSI Command request iSCSI task to m_ProcessWaitQueue, and execute runnable tasks.
    /// </summary>
    /// <param name="cidInfo">
    ///   Connection that is received a PDU specified by pdu argument.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    /// <returns>
    ///   If received PDU is no acceptable, it returns task to send reject PDU, otherwise returns next task list status.
    /// </returns>
    member private this.UpdateProcessWaitQueue_SCSICommand ( conn : IConnection ) ( pdu : SCSICommandPDU ) ( currentQ : ProcessWaitQueue ) : 
            struct ( ( unit -> unit ) option * ImmutableDictionary< ITT_T, IIscsiTask > ) =
        let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )

        if not <| this.CheckCmdSNValue pdu.I pdu.CmdSN currentQ then
            // Reject and ignore
            HLogger.Trace( LogID.W_SCSI_COMMAND_PDU_IGNORED, fun g ->
                g.Gen1( loginfo, sprintf "Invalid CmdSN value(0x%08X)." pdu.CmdSN )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        elif ( not m_sessionParameter.ImmediateData ) && ( 0 < PooledBuffer.length pdu.DataSegment ) then
            // Reject and ignore
            HLogger.Trace( LogID.W_SCSI_COMMAND_PDU_IGNORED, fun g ->
                g.Gen1( loginfo, "ImmediateData was negotiated NO, but A SCSI Command PDU was received that had a non-zero length DataSegment." )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        elif currentQ.WaitingQueue.ContainsKey pdu.InitiatorTaskTag |> not then
            // Receive SCSI Command PDU, before any of SCSI Data-Out PDU. Create new iSCSI task object.
            let task = 
                IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU(
                    this :> ISession,
                    conn.CID,
                    conn.ConCounter,
                    pdu
                ) :> IIscsiTask
            None, currentQ.WaitingQueue.Add( pdu.InitiatorTaskTag, task )

        else
            let v = currentQ.WaitingQueue.[ pdu.InitiatorTaskTag ]
            if v.TaskType <> iSCSITaskType.SCSICommand then
                // A SCSI Data-Out PDU already received, update iSCSI Task object by received SCSI Command PDU.
                // ... But, if the found task object is not IscsiTaskScsiCommand, it considered a protocol error
                HLogger.Trace( LogID.W_SCSI_COMMAND_PDU_IGNORED, fun g ->
                    let msg =
                        sprintf "Specified ITT(0x%08X) is in alive and it is %s commnad. But received PDU is SCSI command."
                                pdu.InitiatorTaskTag
                                v.TaskTypeName
                    g.Gen1( loginfo, msg )
                )
                let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
                Some refuseTask, currentQ.WaitingQueue
            else
                // Update existing iSCSI task object.
                let task =
                    IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU(
                        v :?> IscsiTaskScsiCommand,
                        pdu
                    ) :> IIscsiTask
                None, currentQ.WaitingQueue.SetItem( pdu.InitiatorTaskTag, task )


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update or insert a text negotiation request iSCSI task to m_ProcessWaitQueue, and execute runnable tasks.
    /// </summary>
    /// <param name="conn">
    ///   Connection that is received a PDU specified by pdu argument.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    /// <returns>
    ///   If received PDU is no acceptable, it returns task to send reject PDU, otherwise returns next task list status.
    /// </returns>
    member private this.UpdateProcessWaitQueue_TextReq ( conn : IConnection ) ( pdu : TextRequestPDU ) ( currentQ : ProcessWaitQueue ) : 
            struct ( ( unit -> unit ) option * ImmutableDictionary< ITT_T, IIscsiTask > ) =
        let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )

        if not <| this.CheckCmdSNValue pdu.I pdu.CmdSN currentQ then
            // Reject and ignore
            HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                let msg = sprintf "Invalid CmdSN value(0x%08X)." pdu.CmdSN
                g.Gen1( loginfo, msg )
            )
            let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
            Some refuseTask, currentQ.WaitingQueue

        elif currentQ.WaitingQueue.ContainsKey pdu.InitiatorTaskTag |> not then
            // Create new iSCSI task object.
            let task =
                IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                    IscsiTaskTextNegociation.CreateWithInitParams(
                        this :> ISession,
                        conn.CID,
                        conn.ConCounter,
                        pdu,
                        m_sessionParameter,
                        conn.CurrentParams
                    ),
                    pdu
                ) :> IIscsiTask
            None, currentQ.WaitingQueue.Add( pdu.InitiatorTaskTag, task )

        else
            let v = currentQ.WaitingQueue.[ pdu.InitiatorTaskTag ]
            if v.TaskType <> iSCSITaskType.TextNegociation then
                // If the found task object is not IscsiTaskTextNegociation, it considered a protocol error
                HLogger.Trace( LogID.W_OTHER_PDU_IGNORED, fun g ->
                    let msg =
                        sprintf
                            "Specified ITT(0x%08X) is in alive and it is %s commnad. But received PDU is Text negotiation request."
                            pdu.InitiatorTaskTag
                            v.TaskTypeName
                    g.Gen1( loginfo, msg )
                )
                let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
                Some refuseTask, currentQ.WaitingQueue

            elif pdu.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu then
                // If a text negotiation iSCSI task is existing but requested PDU has reserved value in TTT,
                // it considers negotiation reset request. So, drop existing iSCSI task and create new text negotiation task.
                HLogger.Trace( LogID.I_NEGOTIATION_RESET, fun g -> g.Gen1( loginfo, pdu.InitiatorTaskTag ) )
                let task =
                    IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                        IscsiTaskTextNegociation.CreateWithInitParams(
                            this :> ISession,
                            conn.CID,
                            conn.ConCounter,
                            pdu,
                            m_sessionParameter,
                            conn.CurrentParams
                        ),
                        pdu
                    ) :> IIscsiTask
                None, currentQ.WaitingQueue.SetItem( pdu.InitiatorTaskTag, task )

            elif ( ValueOption.isSome v.Immidiate ) && ( ValueOption.get v.Immidiate ) <> pdu.I then
                // If immidiate flag value in existing task and reqested pdu are different,
                // it considers protocol error and causes negotiation reset.
                HLogger.Trace( LogID.W_NEGOTIATION_RESET, fun g -> g.Gen2( loginfo, pdu.InitiatorTaskTag, "Unmatch immidiate flag value." ) )
                let nextQ = currentQ.WaitingQueue.Remove pdu.InitiatorTaskTag
                let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
                Some refuseTask, nextQ

            elif ( not pdu.I ) && ( ValueOption.isSome v.CmdSN ) && ( pdu.CmdSN = ( ValueOption.get v.CmdSN ) || cmdsn_me.lessThan pdu.CmdSN ( ValueOption.get v.CmdSN ) ) then
                // If at non immidiate text negotiation, CmdSN value in requested PDU less than or equals existing task's CmdSN value,
                // it considers protocol error and causes negotiation reset.
                HLogger.Trace( LogID.W_NEGOTIATION_RESET, fun g -> g.Gen2( loginfo, pdu.InitiatorTaskTag, "Invalid CmdSN value." ) )
                let nextQ = currentQ.WaitingQueue.Remove pdu.InitiatorTaskTag
                let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
                Some refuseTask, nextQ

            else
                // Update existing iSCSI task object.
                let task =
                    IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                        ( v :?> IscsiTaskTextNegociation ),
                        pdu
                    ) :> IIscsiTask
                None, currentQ.WaitingQueue.SetItem( pdu.InitiatorTaskTag, task )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update or insert a SCSI Data-Out iSCSI task to m_ProcessWaitQueue, and execute runnable tasks.
    /// </summary>
    /// <param name="conn">
    ///   Connection that is received a PDU specified by pdu argument.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    /// <returns>
    ///   If received PDU is no acceptable, it returns task to send reject PDU, otherwise returns next task list status.
    /// </returns>
    member private this.UpdateProcessWaitQueue_SCSIDataOut ( conn : IConnection ) ( pdu : SCSIDataOutPDU ) ( currentQ : ProcessWaitQueue ) : 
            struct ( ( unit -> unit ) option * ImmutableDictionary< ITT_T, IIscsiTask > ) =
        let loginfo = struct ( m_ObjID, ValueSome conn.CID, ValueSome conn.ConCounter, ValueSome m_TSIH, ValueSome pdu.InitiatorTaskTag, ValueNone )

        if currentQ.WaitingQueue.ContainsKey pdu.InitiatorTaskTag |> not then
            let wtask =
                IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU(
                    this :> ISession,
                    conn.CID,
                    conn.ConCounter,
                    pdu
                )
            if wtask.IsSome then
                None, currentQ.WaitingQueue.Add( pdu.InitiatorTaskTag, wtask.Value :> IIscsiTask )
            else
                // Reject and ignore
                let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
                Some refuseTask, currentQ.WaitingQueue
        else
            let v = currentQ.WaitingQueue.[ pdu.InitiatorTaskTag ]
            if v.TaskType <> iSCSITaskType.SCSICommand then
                // If the found task object is not IscsiTaskScsiCommand, it considered a protocol error
                HLogger.Trace( LogID.W_DATA_PDU_IGNORED, fun g ->
                    let msg =
                        sprintf
                            "Specified ITT(0x%08X) is in alive and it is %s commnad. But received PDU is SCSI Data-Out."
                            pdu.InitiatorTaskTag
                            v.TaskTypeName
                    g.Gen1( loginfo, msg )
                )
                let refuseTask = fun () -> this.RejectPDUByLogi_ToConnection conn pdu RejectReasonCd.INVALID_PDU_FIELD
                Some refuseTask, currentQ.WaitingQueue
            else
                // Update SCSI Command task object that has already received SCSI Command or SCSI Data-Out PDUs.
                let task =
                    IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU(
                        v :?> IscsiTaskScsiCommand,
                        pdu
                    ) :> IIscsiTask;
                None, currentQ.WaitingQueue.SetItem( pdu.InitiatorTaskTag, task )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send reject PDU to the initiator throws specified connection with header bytes data..
    /// </summary>
    /// <param name="conn">
    ///   Interface of connection object.
    /// </param>
    /// <param name="header">
    ///   Header bytes data of rejected PDU that is sent to the initiator.
    /// </param>
    /// <param name="argReason">
    ///   Reason code.
    /// </param>
    member private this.RejectPDUByLogi_ToConnection ( conn : IConnection ) ( pdu : ILogicalPDU ) ( argReason : RejectReasonCd ) : unit =
        // Generate original header data
        let headerData = PDU.GetHeader( pdu )

        // Send reject PDU
        this.RejectPDUByHeader_ToConnection  conn headerData argReason

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send reject PDU to the initiator throws specified connection with header bytes data..
    /// </summary>
    /// <param name="conn">
    ///   Interface of connection object.
    /// </param>
    /// <param name="header">
    ///   Header bytes data of rejected PDU that is sent to the initiator.
    /// </param>
    /// <param name="argReason">
    ///   Reason code.
    /// </param>
    member private _.RejectPDUByHeader_ToConnection ( conn : IConnection ) ( header : byte[] ) ( argReason : RejectReasonCd ) : unit =
        // create reject pdu
        let rejectPDU = {
            Reason = argReason;
            StatSN = statsn_me.zero;
            ExpCmdSN = cmdsn_me.zero;
            MaxCmdSN = cmdsn_me.zero;
            DataSN_or_R2TSN = datasn_me.zero;   // Haruka not support PDU re-sending
            HeaderData = header;
        }

        // Reject PDU needs R-Mode lock as response fence.
        m_RespFense.NormalLock( fun () ->
            // Send reject PDU
            conn.SendPDU rejectPDU
        )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Resend PDU with response fence.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="pdu">
    ///   SCSI Response or SCSI Data-In PDU that generated by R-SNACK request.
    /// </param>
    /// </param name="sendType">
    ///   send pdu type.
    /// </param>
    member private _.SendOtherPDU_sub ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) ( sendType : SendOtherPDUType ) : unit =

        match m_CIDs.obj.TryGetValue( cid ) with
        | true, cidInfo when cidInfo.Counter = counter ->
            // Send response PDU procedure
            let wf () =
                match sendType with
                | SendOtherPDUType.NORMAL ->
                    cidInfo.Connection.SendPDU pdu
                | SendOtherPDUType.RESEND ->
                    cidInfo.Connection.ReSendPDU pdu
                | SendOtherPDUType.R_SNACK_RESEND ->
                    cidInfo.Connection.ReSendPDUForRSnack pdu

            match pdu.NeedResponseFence with
            | ResponseFenceNeedsFlag.Irrelevant ->
                () // silently ignore
            | ResponseFenceNeedsFlag.Immediately ->
                wf()
            | ResponseFenceNeedsFlag.R_Mode ->
                // Need R-Mode lock at response fence
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_ObjID, "Response Fence Lock(R_Mode)" ) )
                m_RespFense.NormalLock( wf )
            | ResponseFenceNeedsFlag.W_Mode ->
                // Need W-Mode lock at response fence
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_ObjID, "Response Fence Lock(W_Mode)" ) )
                m_RespFense.RFLock( wf )
        | _ ->
            ()

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Generate unique value used to TTT.
    /// </summary>
    member _.GenerateTTTValue() : TTT_T =
        let rec loop() =
            let v = Interlocked.Increment( &m_TTTGen )
            if v = 0xFFFFFFFFu then
                loop()
            else
                v
        ttt_me.fromPrim( loop() )
