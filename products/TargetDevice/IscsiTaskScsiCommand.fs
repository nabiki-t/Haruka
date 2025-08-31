//=============================================================================
// Haruka Software Storage.
// IscsiTaskScsiCommand.fs : Defines IscsiTaskScsiCommand class
// IscsiTaskScsiCommand class implements IIscsiTask interface.
// This object represents SCSI Command of iSCSI task in session object.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// <summary>
/// Data type of internal R2T information.
/// </summary>
type r2tinfo = {
    offset : uint32;
    length : uint32;
    ttt : TTT_T;
    sn : DATASN_T;
    isOutstanding : bool;   // true : sended, false : not sending
    sendTime : DateTime
};

/// <summary>
///  Data receiving status.
/// </summary>
type DATARECVSTAT =
    /// Now on receiving unsolicited data.
    | UNSOLICITED
    /// Now on receiving solicited data, or all of data received.
    | SOLICITED

//=============================================================================
// Class definition

/// <summary>
///   IscsiTaskScsiCommand class definition. This constractor is used internal only.
/// </summary>
/// <param name="m_ObjID">
///   Object ID for log message.
/// </param>
/// <param name="m_Session">
///   The session object that is received the SCSI Command PDU.
/// </param>
/// <param name="m_AllegiantCID">
///   CID of the connection whitch received SCSI Command PDU.
/// </param>
/// <param name="m_AllegiantConCounter">
///   The connection counter of the connection whitch received SCSI Command PDU.
/// </param>
/// <param name="m_Command">
///   SCSI Command PDU.
/// </param>
/// <param name="m_DataOutPDUs">
///   The collection of SCSI Data-Out PDU received from the initiator.
/// </param>
/// <param name="m_R2TPDU">
///   Response PDUs (=R2T PDUs)
///   If R2T PDUs is not created, this vector is empty.
///   If SCSI Command is write operation and this vector is empty,
///   R2T PDUs is not created yet and it indicates to must be created eventually.///   
/// </param>
/// <param name="m_Status">
///   Data receiving status.
/// </param>
/// <param name="m_NextR2TSNValue">
///   Sequence number that used to R2TSN and TTT, to generate next R2T PDU.
/// </param>
/// <param name="m_Executed">
///   GetExecuteTask method had been called.
/// </param>
type IscsiTaskScsiCommand
    (
        m_ObjID : OBJIDX_T,
        m_Session : ISession,
        m_AllegiantCID : CID_T,
        m_AllegiantConCounter : CONCNT_T,
        m_Command : SCSICommandPDU voption,
        m_DataOutPDUs : SCSIDataOutPDU list,
        m_R2TPDU : r2tinfo [],
        m_Status : DATARECVSTAT,
        m_NextR2TSNValue : uint32,
        m_Executed : bool
    ) =

    // All of unsilicited data was received and there are no R2Ts to send or unsolicited,
    // it can be execute and remove from the queue.
    let m_AllDataReceived =
        m_Status = DATARECVSTAT.SOLICITED && m_R2TPDU.Length = 0

    do
        if HLogger.IsVerbose then
            let itt, lun =
                match m_Command with
                | ValueNone -> ( ValueNone, ValueNone )
                | ValueSome( x ) -> ( ValueSome( x.InitiatorTaskTag ), ValueSome( x.LUN ) )
            let cmdsnstr =
                match m_Command with
                | ValueNone -> ""
                | ValueSome( x ) -> sprintf " CmdSN=0x%08X" x.CmdSN
            HLogger.Trace(
                LogID.V_TRACE,
                fun g -> g.Gen1(
                    m_ObjID,
                    ValueSome( m_AllegiantCID ),
                    ValueSome( m_AllegiantConCounter ),
                    ValueSome( m_Session.TSIH ),
                    itt,
                    lun,
                    "IscsiTaskScsiCommand instance created." + cmdsnstr
                )
            )

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Check all of data PDU in current data sequence.
    /// </summary>
    /// <param name="dops">
    ///  The list of SCSI Data Out PDUs.
    /// </param>
    /// <param name="ttt">
    ///  The value of Target Transfer Tag to specify check target "current data sequence".
    /// </param>
    /// <returns>
    ///  If all data received, returns true.
    /// </returns>
    /// <remarks>
    ///  This function check by DataSN values, and ignore real data is all collected or not.
    ///  So, If there are gaps in the data sequence, this function returns true.
    /// </remarks>
    static member private isAllDataReceived ( dops :  SCSIDataOutPDU list ) ( ttt : TTT_T ) : bool =
        let wlist =
            dops
            |> List.filter ( fun i -> i.TargetTransferTag = ttt )
            |> List.sortWith ( fun i1 i2 -> datasn_me.compare i1.DataSN i2.DataSN )
        match wlist |> List.tryFindBack ( _.F ) with
        | None -> false
        | Some( itr ) ->
            let rec loop ( idx : DATASN_T ) : SCSIDataOutPDU list -> DATASN_T =
                function
                | [] -> idx
                | hd :: tl ->
                    if hd.DataSN = idx then
                        loop idx tl
                    elif hd.DataSN = ( datasn_me.next idx ) then
                        loop ( datasn_me.next idx ) tl
                    else
                        idx
            let r = loop ( datasn_me.fromPrim 0xFFFFFFFFu ) wlist
            ( itr.DataSN = r ) || ( datasn_me.lessThan itr.DataSN r )

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Generate normal R2T PDU information.
    /// </summary>
    /// <param name="cmd">
    ///  SCSI Command PDU.
    /// </param>
    /// <param name="dops">
    ///  The list of SCSI Data Out PDUs.
    /// </param>
    /// <param name="mbl">
    ///  The value of negotiated MaxBurstLength.
    /// </param>
    /// <returns>
    ///  Array of R2T information, and next R2TSN value.
    /// </returns>
    static member private generateR2TInfo ( cmd : SCSICommandPDU ) ( dops :  SCSIDataOutPDU list ) ( mbl : uint32 ) : ( r2tinfo[] * uint32 ) =
        if cmd.W then
            let v =
                if cmd.ExpectedDataTransferLength = 0u then
                    [||]
                else
                    let startPos =
                        dops
                        |> List.fold ( fun m itr ->
                            let itr_endpos = itr.BufferOffset + ( itr.DataSegment |> PooledBuffer.ulength )
                            if itr.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu && m < itr_endpos then
                                itr_endpos
                            else
                                m
                           ) ( cmd.DataSegment |> PooledBuffer.ulength )
                    let endPos = cmd.ExpectedDataTransferLength - 1u
                    IscsiTaskScsiCommand.genR2TInfoForGap startPos endPos mbl 0u
                    |> List.toArray
            if v.Length = 0 then
                Array.empty, 0u
            else
                let maxR2TSN =
                    v
                    |> Array.fold ( fun m itr -> if datasn_me.lessThan m itr.sn then itr.sn else m ) ( datasn_me.zero )
                ( v, datasn_me.toPrim maxR2TSN + 1u )
        else
            // If this command has no SCSI data-Out PDUs, it return empty array.
            Array.empty, 0u

    /// <summary>
    ///  Generate R2T information that is plugging specified gap.
    /// </summary>
    /// <param name="startPos">
    ///  Start of range of the gap. The byte specified this argument is included in recovery range.
    /// </param>
    /// <param name="endPos">
    ///  End of range of the gap. The byte specified this argument is included in recovery range.
    /// </param>
    /// <param name="mbl">
    ///  The value of negotiated MaxBurstLength.
    /// </param>
    /// <param name="firstSN">
    ///  First StatSN value.
    /// </param>
    static member private genR2TInfoForGap ( startPos : uint32 ) ( endPos : uint32 ) ( mbl : uint32 ) ( firstSN : uint32 ) : r2tinfo list =
        let rec loop cont sp sn =
            if sp > endPos then
                cont []
            else
                let wnum2 = if sn = 0xFFFFFFFFu then 0u else sn
                let nextcont = fun r ->
                    cont ( {
                        offset = sp;
                        length = min mbl ( endPos - sp + 1u );
                        ttt = ttt_me.fromPrim wnum2;
                        sn = datasn_me.fromPrim wnum2;
                        isOutstanding = false;
                        sendTime = new DateTime()
                        } :: r
                    )
                loop nextcont ( sp + mbl ) ( wnum2 + 1u )
        loop id startPos firstSN

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Generate recovery R2T PDU information.
    /// </summary>
    /// <param name="cmd">
    ///  SCSI Command PDU.
    /// </param>
    /// <param name="dops">
    ///  The list of SCSI Data Out PDUs.
    /// </param>
    /// <param name="mbl">
    ///  The value of negotiated MaxBurstLength.
    /// </param>
    /// <param name="nextR2TSN">
    ///  R2TSN value that used in next R2T PDU.
    /// </param>
    /// <returns>
    ///  Array of R2T information.
    /// </returns>
    /// <remarks>
    ///  If SCSICommandPDU is not write operation, this function returns empty array.
    ///  And, there are no gap in received data, similarly it returns empty array.
    /// </remarks>
    static member private generateRecoveryR2TInfo
            ( cmd : SCSICommandPDU )
            ( dops :  SCSIDataOutPDU list )
            ( mbl : uint32 )
            ( nextR2TSN : uint32 ) : ( r2tinfo[] * uint32 ) =

        let rec loop cont gs asn ( ali : SCSIDataOutPDU list ) =
            match ali with
            | [] ->
                if gs < cmd.ExpectedDataTransferLength then
                    IscsiTaskScsiCommand.genR2TInfoForGap gs ( cmd.ExpectedDataTransferLength - 1u ) mbl asn
                    |> cont
                else
                    cont []
            | hd :: tl ->
                if gs < hd.BufferOffset then
                    let w = IscsiTaskScsiCommand.genR2TInfoForGap gs ( hd.BufferOffset - 1u ) mbl asn
                    loop
                        ( fun r -> cont ( w @ r ) )
                        ( hd.BufferOffset + ( hd.DataSegment |> PooledBuffer.ulength ) )
                        ( asn + uint32 w.Length )
                        tl
                else
                    let nextgs = max ( hd.BufferOffset + ( hd.DataSegment |> PooledBuffer.ulength ) ) gs
                    loop cont nextgs asn tl
        if cmd.W then
            let v =
                dops
                |> List.sortBy ( fun i -> i.BufferOffset )
                |> loop id ( cmd.DataSegment |> PooledBuffer.ulength ) nextR2TSN
                |> List.toArray
            if v.Length = 0 then
                Array.empty, nextR2TSN
            else
                let maxR2TSN =
                    v
                    |> Array.fold ( fun m itr -> if datasn_me.lessThan m itr.sn then itr.sn else m ) ( datasn_me.fromPrim nextR2TSN )
                ( v, datasn_me.toPrim maxR2TSN + 1u )
        else
            // If this command has no SCSI data-Out PDUs, it return empty array.
            Array.empty, nextR2TSN


    // ------------------------------------------------------------------------
    /// <summary>
    ///  Factory method of IscsiTaskScsiCommand class.
    ///  This method is used when a new SCSICommandPDU is received.
    /// </summary>
    /// <param name="argSession">
    ///   The session object that is received the SCSI Command PDU.
    /// </param>
    /// <param name="argCID">
    ///   CID value of the connection that is received the SCSI Command PDU.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the connection that is received the SCSI Command PDU.
    /// </param>
    /// <param name="argCommandPDU">
    ///   Received SCSI Command PDU values.
    /// </param>
    static member ReceivedNewSCSICommandPDU( argSession : ISession, argCID : CID_T, argCounter : CONCNT_T, argCommandPDU : SCSICommandPDU ) : IscsiTaskScsiCommand =
        // get effective session parameter values.
        let workSesParam = argSession.SessionParameter
        let r2ts, nextStat, nextsn =
            if argCommandPDU.W then
                // Create R2T PDU info, when SCSI Command PDU is write operation and initial R2T is needed.
                if workSesParam.InitialR2T || argCommandPDU.F then
                    let v =
                        IscsiTaskScsiCommand.generateR2TInfo
                            argCommandPDU
                            []
                            workSesParam.MaxBurstLength
                    fst v,  DATARECVSTAT.SOLICITED, snd v
                else
                    if ( PooledBuffer.ulength argCommandPDU.DataSegment ) >= argCommandPDU.ExpectedDataTransferLength then
                        // All of data are received.
                        Array.empty, DATARECVSTAT.SOLICITED, 0u
                    else
                        // waiting for unsolicited data pdus.
                        Array.empty, DATARECVSTAT.UNSOLICITED, 0u
            else
                // There are no data pdus to receive.
                Array.empty, SOLICITED, 0u
        // Generate new guid for this instance and call internal constrautor.
        new IscsiTaskScsiCommand( objidx_me.NewID(), argSession, argCID, argCounter, ValueSome argCommandPDU, [], r2ts, nextStat, nextsn, false )

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Factory method of IscsiTaskScsiCommand class.
    ///  This method is used when a new SCSI Data-Out PDU is received.
    /// </summary>
    /// <param name="argSession">
    ///   The session object that is received the SCSI Command PDU.
    /// </param>
    /// <param name="argCID">
    ///   CID value of the connection that is received the SCSI Command PDU.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the connection that is received the SCSI Command PDU.
    /// </param>
    /// <param name="argDataOutPDU">
    ///   Received SCSI Data-Out PDU values.
    /// </param>
    static member ReceivedNewSCSIDataOutPDU( argSession : ISession, argCID : CID_T, argCounter : CONCNT_T, argDataOutPDU : SCSIDataOutPDU ) : IscsiTaskScsiCommand voption =
        // Generate object ID for this instance.
        let objid = objidx_me.NewID()
        // Get effective the sesstion parameter values.
        let workSesParam = argSession.SessionParameter

        if workSesParam.InitialR2T then
            // protocol error.
            // If InitialR2T is yes, SCSI Data-Out PDU should not be received before SCSI Command PDU.
            let msg = "Unexpected SCSI Data-Out PDU was received. Curresponde R2T PDU is not sent yet."
            HLogger.Trace(
                LogID.E_PROTOCOL_ERROR,
                fun g -> g.Gen1(
                    objid,
                    ValueSome( argCID ),
                    ValueSome( argCounter ),
                    ValueSome( argSession.TSIH ),
                    ValueNone,
                    ValueNone,
                    msg
                )
            )
            ValueNone
        elif argDataOutPDU.TargetTransferTag <> ttt_me.fromPrim 0xFFFFFFFFu then
            // protocol error.
            let msg =
                sprintf
                    "In unsolicited SCSI Data-Out PDU, TTT must be reserved value. TTT=0x%08X"
                    ( ttt_me.toPrim argDataOutPDU.TargetTransferTag )
            HLogger.Trace(
                LogID.E_PROTOCOL_ERROR,
                fun g -> g.Gen1(
                    objid,
                    ValueSome( argCID ),
                    ValueSome( argCounter ),
                    ValueSome( argSession.TSIH ),
                    ValueNone,
                    ValueNone,
                    msg
                )
            )
            ValueNone
        else
            new IscsiTaskScsiCommand( objid, argSession, argCID, argCounter, ValueNone, [ argDataOutPDU ], Array.empty, DATARECVSTAT.UNSOLICITED, 0u, false )
            |> ValueSome

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Factory method of IscsiTaskScsiCommand class.
    ///  This method is used when a SCSI Data-Out PDU is received.
    /// </summary>
    /// <param name="argTask">
    ///   The iSCSI task object that holds received a SCSI Command or SCSI Data-Out PDUs.
    /// </param>
    /// <param name="argPDU">
    ///  A received SCSI Data-Out PDU.
    /// </param>
    static member ReceivedContinuationSCSIDataOutPDU( argTask : IscsiTaskScsiCommand, argPDU : SCSIDataOutPDU ) : IscsiTaskScsiCommand =
        let objid = objidx_me.NewID()  // Generate object ID for this instance.
        let struct( cid, counter ) = ( argTask :> IIscsiTask ).AllegiantConnection
        let wSession : ISession = argTask.Session
        let wCommandPDU : SCSICommandPDU voption = argTask.SCSICommandPDU
        let wDataPDU : SCSIDataOutPDU list = argPDU :: argTask.SCSIDataOutPDUs
        let sessParam = wSession.SessionParameter
        let currentR2TPDUs = argTask.R2TPDU
        let currentStatus = argTask.Status
        let currentNextR2TSN = argTask.NextR2TSNValue
        let tsih = wSession.TSIH
        let witt = argPDU.InitiatorTaskTag
        let executedFlg = ( argTask :> IIscsiTask ).Executed
        let loginfo =
            if wCommandPDU.IsSome then
                struct ( objid, ValueSome cid, ValueSome counter, ValueSome tsih, ValueSome witt, ValueSome wCommandPDU.Value.LUN )
            else
                struct ( objid, ValueSome cid, ValueSome counter, ValueSome tsih, ValueSome witt, ValueNone )

        if argTask.IsAllDataReceived then
            // If all of data are received, new data pdu is ignored.
            HLogger.Trace( LogID.W_DATA_PDU_IGNORED, fun g -> g.Gen1( loginfo, "All of data are already received." ) )
            argTask // drop argPDU
        else
            match currentStatus with
            | DATARECVSTAT.UNSOLICITED ->
                if argPDU.TargetTransferTag <> ttt_me.fromPrim 0xFFFFFFFFu then
                    // protocol error.
                    let msg =
                        sprintf
                            "In unsolicited SCSI Data-Out PDU, TTT must be reserved value. TTT=0x%08X"
                            ( ttt_me.toPrim argPDU.TargetTransferTag )
                    HLogger.Trace( LogID.W_DATA_PDU_IGNORED, fun g -> g.Gen1( loginfo, msg ) )
                    argTask // drop argPDU

                elif wCommandPDU.IsSome && argPDU.F (* IscsiTaskScsiCommand.isAllDataReceived wDataPDU ( ttt_me.fromPrim 0xFFFFFFFFu ) *) then
                    // If SCSI Command PDU was already received and, all of unsolicided data are received,
                    // the status is transitioned to DATARECVSTAT.SOLICITED
                    let vR2T, nextSN = 
                        IscsiTaskScsiCommand.generateR2TInfo wCommandPDU.Value wDataPDU sessParam.MaxBurstLength
                    new IscsiTaskScsiCommand(
                        objid,
                        wSession,
                        cid,
                        counter,
                        wCommandPDU,
                        wDataPDU,
                        vR2T,
                        DATARECVSTAT.SOLICITED,
                        nextSN,
                        false
                    )
                else
                    // wait more unsolicided data, or SCSI command PDU
                    new IscsiTaskScsiCommand(
                        objid,
                        wSession,
                        cid,
                        counter,
                        wCommandPDU,
                        wDataPDU,
                        Array.empty,
                        DATARECVSTAT.UNSOLICITED,
                        0u,
                        false
                    )
            | DATARECVSTAT.SOLICITED ->
                if argPDU.TargetTransferTag = ( ttt_me.fromPrim 0xFFFFFFFFu ) then
                    // If it receives unsolicited data PDU, it simply appends this PDU to data-out PDU list.
                    new IscsiTaskScsiCommand(
                        objid,
                        wSession,
                        cid,
                        counter,
                        wCommandPDU,
                        wDataPDU,
                        currentR2TPDUs,
                        currentStatus,
                        currentNextR2TSN,
                        false
                    )
                else
                    match currentR2TPDUs |> Array.tryFindIndex ( fun itr -> itr.ttt = argPDU.TargetTransferTag ) with
                    | None ->
                        // If corresponding R2T is missing, ignore this data PDU.
                        HLogger.Trace( LogID.W_DATA_PDU_IGNORED, fun g ->
                            let msg = sprintf "Corresponding R2T missing. TTT=0x%08X" ( ttt_me.toPrim argPDU.TargetTransferTag )
                            g.Gen1( loginfo, msg )
                        )
                        argTask // drop argPDU
                    | Some( r2tidx ) ->
                        // Check all of solicited data-out PDUs in corresponding R2T are received or not.
                        let wf = argPDU.F // IscsiTaskScsiCommand.isAllDataReceived wDataPDU argPDU.TargetTransferTag
                        let nextR2TInfo, nextR2TSN =
                            if not wf then
                                currentR2TPDUs, currentNextR2TSN
                            else
                                // Notify to the connection to delete R2T PDU stored in the connection object
                                match wSession.GetConnection cid counter with
                                | ValueNone ->
                                    ()  // ignore
                                | ValueSome( conn ) ->
                                    conn.NotifyR2TSatisfied witt currentR2TPDUs.[r2tidx].sn

                                // If all of solicited data-out PDUs in corresponding R2T are received, delete this R2T info.
                                let wr2ta = Array.removeAt r2tidx currentR2TPDUs
                                if wr2ta.Length = 0 then
                                    // If all of R2T are resolved, generate next recovery R2Ts
                                    let recoveryR2T =
                                        IscsiTaskScsiCommand.generateRecoveryR2TInfo
                                            wCommandPDU.Value
                                            wDataPDU
                                            sessParam.MaxBurstLength
                                            currentNextR2TSN

                                    HLogger.Trace( LogID.V_TRACE, fun g ->
                                        let msg = sprintf "##### recoveryR2T.length : %d, %d" ( fst recoveryR2T ).Length ( snd recoveryR2T )
                                        g.Gen1( loginfo, msg )
                                    )
                                    for itr in fst recoveryR2T do
                                        HLogger.Trace( LogID.V_TRACE, fun g ->
                                            let msg = sprintf "offset=%d, length=%d" itr.offset itr.length
                                            g.Gen1( loginfo, msg )
                                        )


                                    if wSession.SessionParameter.ErrorRecoveryLevel = 0uy && ( fst recoveryR2T ).Length > 0 then
                                        // In error recovery level is zero, if it failed to receive all of output data,
                                        // it occurs session recovery. 
                                        HLogger.Trace( LogID.E_FAILED_RECEIVE_ALL_DATA_OUT_BYTES, fun g -> g.Gen0 loginfo )
                                        wSession.DestroySession()
                                        ( Array.empty, snd recoveryR2T )
                                    else
                                        recoveryR2T

                                else
                                    wr2ta, currentNextR2TSN
                        new IscsiTaskScsiCommand(
                            objid,
                            wSession,
                            cid,
                            counter,
                            wCommandPDU,
                            wDataPDU,
                            nextR2TInfo,
                            currentStatus,
                            nextR2TSN,
                            false
                        )

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Factory method of IscsiTaskScsiCommand class.
    ///  This method is used when a SCSI Command PDU is received.
    /// </summary>
    /// <param name="argTask">
    ///   The iSCSI task object that holds received a SCSI Data-Out PDU.
    /// </param>
    /// <param name="argPDU">
    ///   Received SCSI Command PDU.
    /// </param>
    static member ReceivedContinuationSCSICommandPDU( argTask : IscsiTaskScsiCommand, argPDU : SCSICommandPDU ) : IscsiTaskScsiCommand =
        let objid = objidx_me.NewID()  // Generate object for this instance.
        let struct( cid, counter ) = ( argTask :> IIscsiTask ).AllegiantConnection
        let wSession : ISession = argTask.Session
        let wCommandPDU : SCSICommandPDU voption = argTask.SCSICommandPDU
        let sessParam = wSession.SessionParameter
        let tsih = wSession.TSIH
        //let executedFlg = ( argTask :> IIscsiTask ).Executed
        let loginfo = struct ( objid, ValueSome( cid ), ValueSome( counter ), ValueSome( tsih ), ValueSome( argPDU.InitiatorTaskTag ), ValueSome( argPDU.LUN ) )

        if wCommandPDU.IsSome then
            // Duplicate ITT is exist, received SCSI Command PDU is silently ignored.
            HLogger.Trace( LogID.W_SCSI_COMMAND_PDU_IGNORED, fun g ->
                let msg =
                    sprintf "Duplicate Initiator Task Tag (0x%08X) is exist." ( itt_me.toPrim wCommandPDU.Value.InitiatorTaskTag )
                g.Gen1( loginfo, msg )
            )
            argTask // ignore argPDU
        elif sessParam.InitialR2T then
            // If InitialR2T is yes, SCSI Data-Out PDU should not be received before SCSI Command PDU.
            HLogger.Trace( LogID.W_DATA_PDU_IGNORED, fun g ->
                let msg = "Unexpected SCSI Data-Out PDU was received. Curresponde R2T PDU is not sent yet."
                g.Gen1( loginfo, msg )
            )
            new IscsiTaskScsiCommand(
                objid,
                wSession,
                cid,
                counter,
                ValueSome argPDU,
                [],             // drop reseived SCSI data-Out PDUs
                argTask.R2TPDU,
                argTask.Status,
                argTask.NextR2TSNValue,
                false
            )
        elif not argPDU.W then
            // A SCSI Data-Out PDU is already received, so primaly SCSI Command should be write operation.
            HLogger.Trace( LogID.W_DATA_PDU_IGNORED, fun g ->
               let msg = "SCSI Command PDU is not write operation, but SCSI Data-Out PDU are already received. These data PDUs are dropped."
               g.Gen1( loginfo, msg )
            )
            new IscsiTaskScsiCommand(
                objid,
                wSession,
                cid,
                counter,
                ValueSome argPDU,
                [],             // drop reseived SCSI data-Out PDUs
                argTask.R2TPDU,
                argTask.Status,
                argTask.NextR2TSNValue,
                false
            )
        //elif IscsiTaskScsiCommand.isAllDataReceived argTask.SCSIDataOutPDUs ( ttt_me.fromPrim 0xFFFFFFFFu ) then
        elif argPDU.F || ( argTask.SCSIDataOutPDUs |> List.exists ( _.F ) ) then
            // If all of unsolicided data are received,
            // the status is transitioned to DATARECVSTAT.SOLICITED
            let vR2T, nextSN = 
                IscsiTaskScsiCommand.generateR2TInfo argPDU argTask.SCSIDataOutPDUs sessParam.MaxBurstLength
            new IscsiTaskScsiCommand(
                objid,
                wSession,
                cid,
                counter,
                ValueSome argPDU,
                argTask.SCSIDataOutPDUs,
                vR2T,
                DATARECVSTAT.SOLICITED,
                nextSN,
                false
            )
        else
            // wait more unsolicided data, or SCSI command PDU
            new IscsiTaskScsiCommand(
                objid,
                wSession,
                cid,
                counter,
                ValueSome argPDU,
                argTask.SCSIDataOutPDUs,
                Array.empty,
                DATARECVSTAT.UNSOLICITED,
                0u,
                false
            )
            

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IIscsiTask with

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskType
        override _.TaskType : iSCSITaskType =
            SCSICommand

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskTypeName
        override _.TaskTypeName : string =
            "SCSI Command request"

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.InitiatorTaskTag
        override _.InitiatorTaskTag : ITT_T voption =
            match m_Command with
            | ValueSome( x ) ->
                ValueSome( x.InitiatorTaskTag )
            | ValueNone -> 
                match m_DataOutPDUs with
                | hd :: _ ->
                    ValueSome( hd.InitiatorTaskTag )
                | _ ->
                    ValueNone

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.CmdSN
        override _.CmdSN : CMDSN_T voption =
            match m_Command with
            | ValueSome( x ) ->
                ValueSome( x.CmdSN )
            | ValueNone -> 
                ValueNone

        // ------------------------------------------------------------------------
        // Implementation of IIscsiTask.Immidiate
        override _.Immidiate : bool voption =
            match m_Command with
            | ValueNone -> ValueNone
            | ValueSome( v ) -> ValueSome v.I

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.IsExecutable
        override _.IsExecutable : bool =
            // If All of SCSI Data-Out PDU are received, or a R2T PDU to send to initiator is existed,
            // the Execute method is enable.
            not m_Executed && ( m_AllDataReceived || m_R2TPDU.Length > 0 )

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.AllegiantConnection
        override _.AllegiantConnection : struct( CID_T * CONCNT_T ) =
            struct( m_AllegiantCID, m_AllegiantConCounter )

        // ------------------------------------------------------------------------
        // Execute this command.
        // Execution of this method may be retried, but the task is executed exactly once.
        override _.GetExecuteTask () : struct( ( unit -> unit ) * IIscsiTask ) =

            // Create async procedure list for send R2T PSUs
            let createProcListForSendR2T ( conn : IConnection ) ( maxOutstandR2T : int ) =
                let rec loop cont idx cnt =
                    if idx < m_R2TPDU.Length && cnt < maxOutstandR2T then
                        if m_R2TPDU.[idx].isOutstanding then
                            loop cont ( idx + 1 ) ( cnt + 1 )
                        else
                            m_R2TPDU.[idx] <- {
                                m_R2TPDU.[idx] with
                                    isOutstanding = true;
                            }
                            let sendR2TPDU = {
                                LUN = m_Command.Value.LUN;
                                InitiatorTaskTag = m_Command.Value.InitiatorTaskTag;
                                TargetTransferTag = m_R2TPDU.[idx].ttt;
                                StatSN = statsn_me.zero;
                                ExpCmdSN = cmdsn_me.zero;
                                MaxCmdSN = cmdsn_me.zero;
                                R2TSN = m_R2TPDU.[idx].sn;
                                BufferOffset = m_R2TPDU.[idx].offset;
                                DesiredDataTransferLength = m_R2TPDU.[idx].length;
                            }
                            let nextcont = fun r -> cont ( ( fun () -> conn.SendPDU sendR2TPDU ) :: r )
                            loop nextcont ( idx + 1 ) ( cnt + 1 )
                    else
                        cont []
                loop id 0 0

            let ext =
                fun () ->
                    match m_Status with
                    | DATARECVSTAT.UNSOLICITED ->
                        // Nothig to do
                        ()
                    | DATARECVSTAT.SOLICITED when m_AllDataReceived ->
                        // If all of SCSI Data-Out PDUs are received, 
                        // the SCSI Command PDU and SCSI Data-Out PDUs are translated to SCSI task router.
                        let taskRouter = m_Session.SCSITaskRouter
                        taskRouter.SCSICommand m_AllegiantCID m_AllegiantConCounter m_Command.Value m_DataOutPDUs

                    | _ ->
                        let maxOutstandR2T = m_Session.SessionParameter.MaxOutstandingR2T |> int

                        // search connection
                        match m_Session.GetConnection m_AllegiantCID m_AllegiantConCounter with
                        | ValueNone ->
                            // Silently ignore
                            ()
                        | ValueSome( connection ) ->
                            createProcListForSendR2T connection maxOutstandR2T
                            |> List.iter ( fun itr -> itr() )

            let nextTask =
                new IscsiTaskScsiCommand(
                    m_ObjID, m_Session, m_AllegiantCID, m_AllegiantConCounter, m_Command, m_DataOutPDUs, m_R2TPDU, m_Status, m_NextR2TSNValue, true
                )
            struct( ext, nextTask )

        // ------------------------------------------------------------------------
        //   This task already compleated and removale or not.
        override _.IsRemovable =
            m_AllDataReceived && m_Executed

        // ------------------------------------------------------------------------
        // GetExecuteTask method had been called or not.
        override _.Executed = m_Executed

    // ------------------------------------------------------------------------
    /// Get Session object interface
    member _.Session : ISession =
        m_Session

    // ------------------------------------------------------------------------
    /// Get SCSI Command PDU
    member _.SCSICommandPDU : SCSICommandPDU voption =
        m_Command

    // ------------------------------------------------------------------------
    /// Get SCSI Data-Out PDUs
    member _.SCSIDataOutPDUs : SCSIDataOutPDU list =
        m_DataOutPDUs

    // ------------------------------------------------------------------------
    /// Get R2T PDUs
    member _.R2TPDU : r2tinfo []=
        m_R2TPDU

    // ------------------------------------------------------------------------
    /// Get Status
    member _.Status : DATARECVSTAT =
        m_Status

    // ------------------------------------------------------------------------
    /// Get m_NextR2TSNValue
    member _.NextR2TSNValue : uint32 =
        m_NextR2TSNValue

    // ------------------------------------------------------------------------
    /// Get m_AllDataReceived
    member _.IsAllDataReceived : bool =
        m_AllDataReceived
