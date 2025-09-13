//=============================================================================
// Haruka Software Storage.
// IscsiTaskOnePDUCommand.fs : Defines IscsiTaskOnePDUCommand class
// IscsiTaskOnePDUCommand class implements IIscsiTask interface.
// This object represents iSCSI task that constructed by one PDU.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///   This constructor creates a iSCSI task object that is constituted only one PDU.
/// </summary>
/// <param name="m_Session">
///   The interface of the session object which this task belongings to.
/// </param>
/// <param name="m_AllegiantCID">
///   The CID value of the connection where this object belongings to.
/// </param>
/// <param name="m_AllegiantConCounter">
///   The connection counter value of the connection instance where this object belongings to.
/// </param>
/// <param name="m_Request">
///   Received PDU.
/// </param>
/// <param name="m_Executed">
///   GetExecuteTask method had been called.
/// </param>
type IscsiTaskOnePDUCommand
    (
        m_Session : ISession,
        m_AllegiantCID : CID_T,
        m_AllegiantConCounter : CONCNT_T,
        m_Request : ILogicalPDU,
        m_Executed : bool
    ) =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// log information // ( objId, cid, conCounter, tsih, itt, lun )
    let m_LogInfo = struct ( m_ObjID, ValueSome m_AllegiantCID, ValueSome m_AllegiantConCounter, ValueSome m_Session.TSIH, ValueSome m_Request.InitiatorTaskTag, ValueNone )

    do
        if HLogger.IsVerbose then
            HLogger.Trace( LogID.V_TRACE, fun g ->
                let msg = sprintf "IscsiTaskOnePDUCommand instance created.Opcode=%s, CmdSN=%d" ( Constants.getOpcodeNameFromValue m_Request.Opcode ) ( cmdsn_me.toPrim m_Request.CmdSN )
                g.Gen1( m_LogInfo, msg )
            )
    
    member _.Request : ILogicalPDU =
        m_Request


    //=========================================================================
    // Interface method

    interface IIscsiTask with

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskType
        override _.TaskType : iSCSITaskType =
            match m_Request.Opcode with
            | OpcodeCd.NOP_OUT ->
                NOPOut
            | OpcodeCd.SCSI_TASK_MGR_REQ ->
                SCSITaskManagement
            | OpcodeCd.LOGOUT_REQ ->
                Logout
            | OpcodeCd.SNACK ->
                SNACK
            | _ ->
                let msg = sprintf "Unknown opcode(0x%02X)" ( byte m_Request.Opcode )
                HLogger.Trace( LogID.F_INTERNAL_ASSERTION, fun g -> g.Gen1( m_LogInfo, msg ) )
                raise <| new InternalAssertionException( msg )

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskTypeName
        override _.TaskTypeName : string =
            match m_Request.Opcode with
            | OpcodeCd.NOP_OUT ->
                "NOP-Out"
            | OpcodeCd.SCSI_TASK_MGR_REQ ->
                "SCSI Task management request"
            | OpcodeCd.LOGOUT_REQ ->
                "Logout request"
            | OpcodeCd.SNACK ->
                "SNACK"
            | _ ->
                sprintf "Unknown opcode(0x%02X)" ( byte m_Request.Opcode )

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.InitiatorTaskTag
        override _.InitiatorTaskTag : ITT_T voption =
            ValueSome( m_Request.InitiatorTaskTag )

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.CmdSN
        override _.CmdSN : CMDSN_T voption =
            ValueSome( m_Request.CmdSN )

        // ------------------------------------------------------------------------
        // Implementation of IIscsiTask.Immidiate
        override _.Immidiate : bool voption =
            ValueSome m_Request.Immidiate

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.IsExecutable
        override _.IsExecutable : bool =
            not m_Executed

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.AllegiantConnection
        override _.AllegiantConnection : struct( CID_T * CONCNT_T ) =
            struct ( m_AllegiantCID, m_AllegiantConCounter )

        // --------------------------------------------------------------------
        // Execute this command.
        // Execution of this method may be retried, but the task is executed exactly once.
        override this.GetExecuteTask () : struct( ( unit -> unit ) * IIscsiTask ) =
            let ext =
                fun () ->
                    match m_Request.Opcode with
                    | OpcodeCd.NOP_OUT ->
                        this.ExecuteNOPOut()
                    | OpcodeCd.SCSI_TASK_MGR_REQ ->
                        this.ExecuteTaskManagementRequest()
                    | OpcodeCd.LOGOUT_REQ ->
                        this.ExecuteLogoutRequest()
                    | OpcodeCd.SNACK ->
                        this.ExecuteSnackRequest()
                    | _ as x ->
                        let msg = sprintf "Unknown opcode(0x%02X)" ( byte x )
                        HLogger.Trace( LogID.F_INTERNAL_ASSERTION, fun g -> g.Gen1( m_LogInfo, msg ) )
                        raise <| new InternalAssertionException( msg )

            let nextTask = 
                new IscsiTaskOnePDUCommand(
                    m_Session, m_AllegiantCID, m_AllegiantConCounter, m_Request, true 
                )
            struct( ext, nextTask )

        // --------------------------------------------------------------------
        //   This task already compleated and removale or not.
        override _.IsRemovable : bool =
            // When this task is executed, all response PDU ( = always only one ) is returned to the initiator.
            m_Executed

        // ------------------------------------------------------------------------
        // GetExecuteTask method had been called or not.
        override _.Executed : bool =
            m_Executed

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute NOP_OUT PDU.
    ///   It send to Nop-In PDU to the initiator through the connection whitch received the Nop-Out PDU.
    /// </summary>
    member private _.ExecuteNOPOut() : unit =
        // send Nop-In PDU
        let nopOutPDU = m_Request :?> NOPOutPDU

        // if Initiator Task Tag is reserved value, it is considered ping response, so target does not send NOP-IN pdu.
        if nopOutPDU.InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu then
            // silently ignore this NOP-Out request.
            HLogger.Trace( LogID.I_NOP_OUT_PING_RESPONSE_RECEIVED, fun g -> g.Gen1( m_LogInfo, nopOutPDU.TargetTransferTag ) )
            // * Ping response PDU is processed in session class. So, originally, this function do not be ran.

        elif nopOutPDU.TargetTransferTag <> ttt_me.fromPrim 0xFFFFFFFFu then
            // If initiator send NOP-OUT PDU for ping request, Target Transfer Tag must be reserved value
            let msg = "In NOP-Out PDU, if the initiator send ping request, Target Transfer Tag must be 0xFFFFFFFF."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_LogInfo, msg ) )
            raise <| SessionRecoveryException ( msg, m_Session.TSIH )

        else
            match m_Session.GetConnection m_AllegiantCID m_AllegiantConCounter with
            | ValueNone ->
                if HLogger.IsVerbose then HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_LogInfo, "Connection missing. Request Ignored." ) )
            | ValueSome( conn ) ->
                if HLogger.IsVerbose then HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_LogInfo, "Nop-Out Request." ) )
                let mrsli = conn.CurrentParams.MaxRecvDataSegmentLength_I
                let respPingData =
                    PooledBuffer.Truncate ( int mrsli ) nopOutPDU.PingData
                m_Session.SendOtherResponsePDU
                    m_AllegiantCID
                    m_AllegiantConCounter
                    {
                        LUN = nopOutPDU.LUN;
                        InitiatorTaskTag = nopOutPDU.InitiatorTaskTag;
                        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                        StatSN = statsn_me.zero;
                        ExpCmdSN = cmdsn_me.zero;
                        MaxCmdSN = cmdsn_me.zero;
                        PingData = respPingData;
                    }


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute SCSI_TASK_MGR_REQ PDU.
    ///   It dispatch task management request to the SCSI task router. Or, if the request is not supported, 
    ///   respond to task management response to the initiator.
    /// </summary>
    /// <returns>
    ///   Task object, that process the opration.
    /// </returns>
    member private this.ExecuteTaskManagementRequest() : unit =
        let taskRouter = m_Session.SCSITaskRouter
        let pdu = m_Request :?> TaskManagementFunctionRequestPDU
        match pdu.Function with
        | TaskMgrReqCd.ABORT_TASK ->
            taskRouter.AbortTask ( this :> IIscsiTask ) pdu.LUN pdu.ReferencedTaskTag
        | TaskMgrReqCd.ABORT_TASK_SET ->
            taskRouter.AbortTaskSet ( this :> IIscsiTask ) pdu.LUN
        | TaskMgrReqCd.CLEAR_ACA ->
            taskRouter.ClearACA ( this :> IIscsiTask ) pdu.LUN
        | TaskMgrReqCd.CLEAR_TASK_SET ->
            taskRouter.ClearTaskSet ( this :> IIscsiTask ) pdu.LUN
        | TaskMgrReqCd.LOGICAL_UNIT_RESET ->
            taskRouter.LogicalUnitReset ( this :> IIscsiTask ) pdu.LUN
        | TaskMgrReqCd.TARGET_WARM_RESET
        | TaskMgrReqCd.TARGET_COLD_RESET ->
            // Haruka not supports the target reset request
            m_Session.SendOtherResponsePDU
                m_AllegiantCID
                m_AllegiantConCounter
                {
                    Response = TaskMgrResCd.TASK_MGR_NOT_SUPPORT;
                    InitiatorTaskTag = pdu.InitiatorTaskTag;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    ResponseFence = ResponseFenceNeedsFlag.R_Mode;
                }
        | TaskMgrReqCd.TASK_REASSIGN ->
            // Haruka not supports the task reassign task management request
            m_Session.SendOtherResponsePDU
                m_AllegiantCID
                m_AllegiantConCounter
                {
                    Response = TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT;
                    InitiatorTaskTag = pdu.InitiatorTaskTag;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    ResponseFence = ResponseFenceNeedsFlag.R_Mode;
                }
        | _ ->
            let msg = sprintf "Unknown Task Management Function(0x%02X)" ( byte pdu.Function )
            HLogger.Trace( LogID.F_INTERNAL_ASSERTION, fun g -> g.Gen1( m_LogInfo, msg ) )
            raise <| new InternalAssertionException( msg )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute LOGOUT_REQ PDU.
    ///   It send to logout response PDU to the initiator.
    ///   In the connection object, the TCP connection is dropped after logout response PDU.
    /// </summary>
    member private _.ExecuteLogoutRequest() : unit =
        let reqPDU = m_Request :?> LogoutRequestPDU

        HLogger.Trace( LogID.I_LOGOUT_REQUESTED, fun g ->
            g.Gen1( m_LogInfo, Constants.getLogoutReqReasonNameFromValue reqPDU.ReasonCode )
        )
        let defresp = {
            Response = LogoutResCd.SUCCESS;
            InitiatorTaskTag = reqPDU.InitiatorTaskTag;
            StatSN = statsn_me.zero;
            ExpCmdSN = cmdsn_me.zero;
            MaxCmdSN = cmdsn_me.zero;
            Time2Wait = 0us;
            Time2Retain = 0us;
            CloseAllegiantConnection = true;
        }

        if reqPDU.ReasonCode = LogoutReqReasonCd.RECOVERY then
            // Connection recovery is not supported.
            m_Session.SendOtherResponsePDU m_AllegiantCID m_AllegiantConCounter {
                defresp with
                    Response = LogoutResCd.RECOVERY_NOT_SUPPORT
                    CloseAllegiantConnection = false;
            }
        else
            // get close target connections
            let closeConns =
                if reqPDU.ReasonCode = LogoutReqReasonCd.CLOSE_SESS then
                    m_Session.GetAllConnections()
                else
                    m_Session.GetAllConnections()
                    |> Array.filter ( fun itr -> itr.CID = reqPDU.CID )

            // If there are no connections to close, respond accordingly.
            if closeConns.Length <= 0 then
                m_Session.SendOtherResponsePDU m_AllegiantCID m_AllegiantConCounter {
                    defresp with
                        Response = LogoutResCd.CID_NOT_FOUND;
                        CloseAllegiantConnection = false;
                }
            else
                // close connections without current connection.
                let cv2 = closeConns |> Array.filter ( fun itr -> itr.CID <> m_AllegiantCID || itr.ConCounter <> m_AllegiantConCounter )
                for itr in cv2 do
                    itr.Close()
                    m_Session.RemoveConnection itr.CID itr.ConCounter

                // Returns the response and optionally closes the current connection.
                m_Session.SendOtherResponsePDU m_AllegiantCID m_AllegiantConCounter {
                    defresp with
                        CloseAllegiantConnection = cv2.Length <> closeConns.Length;
                }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute SNACK PDU.
    /// </summary>
    /// <returns>
    ///   Task object, that process the opration.
    /// </returns>
    member private this.ExecuteSnackRequest() : unit =
        let snackpdu = m_Request :?> SNACKRequestPDU
        if m_Session.SessionParameter.ErrorRecoveryLevel = 0uy then
            // if error recovery level is zero, SNACK request is not supported.
            HLogger.Trace( LogID.W_SNACK_REQ_REJECTED, fun g -> g.Gen0 m_LogInfo )
            m_Session.RejectPDUByLogi m_AllegiantCID m_AllegiantConCounter m_Request RejectReasonCd.COM_NOT_SUPPORT
        else
            match m_Session.GetConnection m_AllegiantCID m_AllegiantConCounter with
            | ValueNone ->
                // ignore request
                if HLogger.IsVerbose then HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_LogInfo, "Connection missing. Request Ignored." ) )
            | ValueSome( conn ) ->
                match snackpdu.Type with
                | SnackReqTypeCd.DATA_R2T ->
                    this.ExecuteSnackRequest_DataR2T conn snackpdu
                | SnackReqTypeCd.STATUS ->
                    this.ExecuteSnackRequest_Status conn snackpdu
                | SnackReqTypeCd.DATA_ACK ->
                    conn.NotifyDataAck snackpdu.TargetTransferTag snackpdu.LUN ( datasn_me.fromPrim snackpdu.BegRun )
                | SnackReqTypeCd.RDATA_SNACK ->
                    conn.R_SNACKRequest snackpdu.InitiatorTaskTag ( fun () -> this.ExecuteSnackRequest_RDataSnack conn snackpdu )
                | _ ->
                    ()

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute Data R2T SNACK PDU.
    /// </summary>
    /// <params name="conn">
    ///   Connection that used to return response to the initiator.
    /// </params>
    /// <params name="snackpdu">
    ///   SNACK PDU.
    /// </params>
    member private _.ExecuteSnackRequest_DataR2T( conn : IConnection ) ( snackpdu : SNACKRequestPDU ) : unit =
        let pdus = conn.GetSentDataInPDUForSNACK snackpdu.InitiatorTaskTag ( datasn_me.fromPrim snackpdu.BegRun ) snackpdu.RunLength
        if pdus.Length = 0 then
            // If there are no specified PDUs, the SNACK PDU is rejected.
            m_Session.RejectPDUByLogi m_AllegiantCID m_AllegiantConCounter snackpdu RejectReasonCd.PROTOCOL_ERR
        else
            // Resend PDUs
            for itr in pdus do
                m_Session.ResendPDU m_AllegiantCID m_AllegiantConCounter itr

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute Status SNACK PDU.
    /// </summary>
    /// <params name="conn">
    ///   Connection that used to return response to the initiator.
    /// </params>
    /// <params name="snackpdu">
    ///   SNACK PDU.
    /// </params>
    member private _.ExecuteSnackRequest_Status( conn : IConnection ) ( snackpdu : SNACKRequestPDU ) : unit =
        let pdus = conn.GetSentResponsePDUForSNACK ( statsn_me.fromPrim snackpdu.BegRun ) snackpdu.RunLength
        if pdus.Length = 0 then
            // If there are no specified PDUs, the SNACK PDU is rejected.
            m_Session.RejectPDUByLogi m_AllegiantCID m_AllegiantConCounter snackpdu RejectReasonCd.PROTOCOL_ERR
        else
            for i in pdus do
                m_Session.ResendPDU m_AllegiantCID m_AllegiantConCounter i

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute R-Data SNACK PDU.
    /// </summary>
    /// <params name="conn">
    ///   Connection that used to return response to the initiator.
    /// </params>
    /// <params name="snackpdu">
    ///   SNACK PDU.
    /// </params>
    member private _.ExecuteSnackRequest_RDataSnack( conn : IConnection ) ( snackpdu : SNACKRequestPDU ) : unit =
        HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_LogInfo, "R-Data SNACK execute." ) )

        let oldDataInPDUs, oldRespPDU =
            conn.GetSentSCSIResponsePDUForR_SNACK snackpdu.InitiatorTaskTag

        let isExistSenseData = oldRespPDU.SenseData.Count > 0
        let argSendDataBytes = oldRespPDU.DataInBuffer
        let comPara = conn.CurrentParams
        let mbl = m_Session.SessionParameter.MaxBurstLength
        let erl = m_Session.SessionParameter.ErrorRecoveryLevel
        let mrdsl_I = comPara.MaxRecvDataSegmentLength_I
        let lun = oldRespPDU.LUN

        let someOfDI_Acked = 
            oldDataInPDUs.Length > 0 && datasn_me.lessThan ( datasn_me.zero ) oldDataInPDUs.[0].DataSN
                        
        // Maximum sense data length is 252 bytes and minimum MaxRecvDataSegmentLength is 512 bytes,
        // So, sense data should always be stored in the SCSI Response PDU.
        // And, it should not be truncated by MaxRecvDataSegmentLength_I.
        let realSendDataLength = uint32 argSendDataBytes.Length

        // Search the DataIn PDU that has 1 in A bit.
        //let ackDataInPDU = oldDataInPDUs |> Array.tryFind ( fun itr -> itr.A )

        let newDataInPDUList =
            if mrdsl_I - 2u < realSendDataLength || someOfDI_Acked then
                // If send data is longer than MaxRecvDataSegmentLength at the initiator,
                // or, some of Data-In PDUs are already acknowledged, all of data are sent by Data-In PDU.
                // Otherwise, all data are sent by SCSI response PDU, and there are no Data-In PDU.

                // Divite sendDataBytes to some of Data-In PDUs
                let startDataPos, startDataSN =
                    if someOfDI_Acked then
                        oldDataInPDUs.[0].BufferOffset, oldDataInPDUs.[0].DataSN
                    else
                        0u, datasn_me.zero
                let pduSegs = Functions.DivideRespDataSegment startDataPos ( realSendDataLength - startDataPos ) mbl mrdsl_I
                pduSegs
                |> List.mapi ( fun cnt struct( dpos, dlen, fflag ) ->
                    {
                        F = fflag;
                        A = if erl > 0uy then fflag else false;
                        O = false;  // ignored
                        U = false;  // ignored
                        S = false;  // Haruka does not use this flag
                        Status = ScsiCmdStatCd.GOOD;  // ignored
                        LUN =
                            if erl > 0uy && fflag then
                                lun
                            else
                                lun_me.zero;
                        InitiatorTaskTag = snackpdu.InitiatorTaskTag;
                        TargetTransferTag =
                            if erl > 0uy && fflag then
                                m_Session.NextTTT
                            else
                                ttt_me.fromPrim 0xffffffffu;
                        StatSN = statsn_me.zero;
                        ExpCmdSN = cmdsn_me.zero;
                        MaxCmdSN = cmdsn_me.zero;
                        DataSN = datasn_me.fromPrim( uint cnt ) + startDataSN;
                        BufferOffset = dpos;
                        ResidualCount = 0u;
                        DataSegment = argSendDataBytes.GetArraySegment( int dpos ) ( int dlen );
                        ResponseFence = ResponseFenceNeedsFlag.Immediately;
                    } :> ILogicalPDU
                )
            else
                []

        let newRespPDU = {
            oldRespPDU with
                SNACKTag = snacktag_me.fromPrim( ttt_me.toPrim snackpdu.TargetTransferTag )
                ExpDataSN = 
                    if someOfDI_Acked then
                        datasn_me.incr ( uint32 newDataInPDUList.Length ) oldDataInPDUs.[0].DataSN
                    else
                        datasn_me.fromPrim( uint32 newDataInPDUList.Length );
                ResponseData = 
                    if not isExistSenseData && comPara.MaxRecvDataSegmentLength_I - 2u >= realSendDataLength && not someOfDI_Acked then
                        argSendDataBytes.ArraySegment
                    else
                        ArraySegment.Empty;
        }

        // resend Data-In PDUs
        for i in newDataInPDUList do
            m_Session.ResendPDUForRSnack m_AllegiantCID m_AllegiantConCounter i

        // Unlock response fence lock
        // When this function called, response fence lock has been earned.
        // so, before sending SCSI response PDU, it must unlock response fence lock.
        if oldRespPDU.ResponseFence = ResponseFenceNeedsFlag.R_Mode || oldRespPDU.ResponseFence = ResponseFenceNeedsFlag.W_Mode then
            m_Session.NoticeUnlockResponseFence oldRespPDU.ResponseFence

        // resend SCSI Response PDU
        m_Session.ResendPDUForRSnack m_AllegiantCID m_AllegiantConCounter newRespPDU
