//=============================================================================
// Haruka Software Storage.
// IscsiTaskTextNegociation.fs : Defines IscsiTaskTextNegociation class
// IscsiTaskTextNegociation class implements IIscsiTask interface.
// This object represents iSCSI task that negociates as text request
// and text response PDU sequence.
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
///   Constractor of IscsiTaskTextNegociation.
///   It must be used in IscsiTaskTextNegociation class only.
/// </summary>
/// <param name="m_ObjID">
///   Object ID for log message.
/// </param>
/// <param name="m_Session">
///   The interface of the session object which this task belongings to.
/// </param>
/// <param name="m_AllegiantCID">
///   The CID value of the connection where this object belongings to.
/// </param>
/// <param name="m_AllegiantConCounter">
///   The connection counter value of the connection instance where this object belongings to.
/// </param>
/// <param name="m_FirstIFlg">
///   Immidiate flag value of the FIRST PDU of text negotiation started.
/// </param>
/// <param name="m_FirstITT">
///   Initiator task tag value of the FIRST PDU of text negotiation started.
/// </param>
/// <param name="m_CmdSN">
///   CmdSN value of the LAST PDU of text negotiation sequence.
/// </param>
/// <param name="m_CurrentNegoParam">
///   The value of nagotiating text keys.
///   This value is updated when the target received a PDU that is set to 0 in C bit.
/// </param>
/// <param name="m_CurrentNogeStatus">
///   Current negotiation status of text keys.
/// </param>
/// <param name="m_contPDUs">
///   List of received PDUs that has 1 in C bit.
/// </param>
/// <param name="argRespPDU">
///   The sequense of Response PDUs.
/// </param>
/// <param name="m_Executed">
///   GetExecuteTask method had been called.
/// </param>
type IscsiTaskTextNegociation
    (
        m_ObjID : OBJIDX_T,
        m_Session : ISession,
        m_AllegiantCID : CID_T,
        m_AllegiantConCounter : CONCNT_T,
        m_FirstIFlg : bool,
        m_FirstITT : ITT_T,
        m_CmdSN : CMDSN_T,
        m_CurrentNegoParam : TextKeyValues,
        m_CurrentNogeStatus : TextKeyValuesStatus,
        m_contPDUs : TextRequestPDU list,
        argRespPDU : TextResponsePDU list,
        Initiator_Last_C : bool,
        Initiator_Last_F : bool,
        m_Executed : bool
    ) =

    /// The first response PDU currespond to received request PDU.
    let m_NextResponsePDU : TextResponsePDU voption = 
        if argRespPDU.Length = 0 then
            ValueNone
        else
            ValueSome( List.head argRespPDU )

    /// The Response PDUs sequence currespond to received request PDU.
    let m_ContResponsePDUs = 
        if argRespPDU.Length > 1 then
            List.tail argRespPDU
        else
            []

    // negotiation sequense is finished or not
    let m_IsNegotiationFinished =
        ( not Initiator_Last_C ) &&
        Initiator_Last_F &&
        ( m_ContResponsePDUs.Length <= 0 ) &&
        ( m_NextResponsePDU.IsNone || ( m_NextResponsePDU.IsSome && m_NextResponsePDU.Value.F ) )

    do
        if HLogger.IsVerbose then
            HLogger.Trace(
                LogID.V_TRACE,
                fun g -> g.Gen1(
                    m_ObjID,
                    ValueSome( m_AllegiantCID ),
                    ValueSome( m_AllegiantConCounter ),
                    ValueSome( m_Session.TSIH ),
                    ValueNone,
                    ValueNone,
                    "IscsiTaskTextNegociation instance created."
                )
            )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Set to initial parameter values of text key negotiation.
    /// </summary>
    /// <param name="argSession">
    ///   The interface of the session object which this task belongings to.
    /// </param>
    /// <param name="argCID">
    ///   The CID value of the connection where this object belongings to.
    /// </param>
    /// <param name="argCounter">
    ///   The connection counter value of the connection instance where this object belongings to.
    /// </param>
    /// <param name="argPDU">
    ///   Received text negotiation request PDU.
    /// </param>
    /// <param name="initParamSW">
    ///   The current parameter values of the session.
    ///   This values used for initial values of text key negotiation.
    /// </param>
    /// <param name="initParamCO">
    ///   The current parameter values of the connection.
    ///   This values used for initial values of text key negotiation.
    /// </param>
    static member CreateWithInitParams(
        argSession : ISession,
        argCID : CID_T,
        argCounter : CONCNT_T,
        argPDU : TextRequestPDU,
        initParamSW : IscsiNegoParamSW,
        initParamCO : IscsiNegoParamCO ) : IscsiTaskTextNegociation =

        new IscsiTaskTextNegociation(
            objidx_me.NewID(), // Generate object ID for this instance
            argSession,
            argCID,
            argCounter,
            argPDU.I,
            argPDU.InitiatorTaskTag,
            argPDU.CmdSN,
            {
                TextKeyValues.defaultTextKeyValues with
                    InitiatorAlias = TextValueType.Value( initParamSW.InitiatorAlias );
                    MaxRecvDataSegmentLength_I = TextValueType.Value( initParamCO.MaxRecvDataSegmentLength_I );
            },
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [],
            false,
            false,
            false
    )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update current negotiation status. And create next the status and response PDUs.
    /// </summary>
    /// <param name="curParam">
    ///   Current negotiation status.
    /// </param>
    /// <param name="argReceivedPDU">
    ///   The PU of received from the initiator.
    /// </param>
    static member UpdateNegoStatByReqPDU( curParam : IscsiTaskTextNegociation, argReceivedPDU : TextRequestPDU ) : IscsiTaskTextNegociation =
        let objid = objidx_me.NewID()   // Generate object ID for this instance
        let firstIFllg = ValueOption.get ( curParam :> IIscsiTask ).Immidiate
        let firstITT = ValueOption.get ( curParam :> IIscsiTask ).InitiatorTaskTag
        let wSession = curParam.Session
        let struct( cid, counter ) = ( curParam :> IIscsiTask ).AllegiantConnection
        let oldContRespPDU : TextResponsePDU list = curParam.ContResponsePDUs
        let tsih = wSession.TSIH
        //let executedFlg = ( curParam :> IIscsiTask ).Executed
        let loginfo = struct ( objid, ValueSome( cid ), ValueSome( counter ), ValueSome( tsih ), ValueNone, ValueNone )

        if firstITT <> argReceivedPDU.InitiatorTaskTag || firstIFllg <> argReceivedPDU.I then
            let msg = "In iSCSI text request PDU, InitiatorTaskTag or I bit value unmatch."
            HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, tsih )

        if argReceivedPDU.C then

            // If response PDUs sequence is not empty and receive request PDU with C bit set to 1,
            // it considers protocol error.
            if oldContRespPDU.Length > 0 then
                let msg = "In iSCSI text request PDU, response PDUs sequence is remain and it will be expected empty PDU."
                HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, tsih )

            // response empty PDU
            let qresp = {
                F = false;
                C = false;
                LUN = argReceivedPDU.LUN;
                InitiatorTaskTag = argReceivedPDU.InitiatorTaskTag;
                TargetTransferTag = argReceivedPDU.TargetTransferTag;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                TextResponse = Array.empty;
            }

            // If received PDU is a part of sequence of text request PDUs,
            // add the received PDU to the PDUs list.
            new IscsiTaskTextNegociation(
                objid,
                wSession,
                cid,
                counter,
                firstIFllg,
                firstITT,
                argReceivedPDU.CmdSN,
                curParam.CurrentNegoParam,
                curParam.CurrentNegoStatus,
                argReceivedPDU :: curParam.ContPDUs,
                [ qresp ],
                argReceivedPDU.C,
                argReceivedPDU.F,
                false
            )
        else if oldContRespPDU.Length > 0 then
            // If response PDUs sequence is not empty and request PDU is empty,
            // simply send next response PDU.

            if argReceivedPDU.TextRequest.Length > 0 then
                let msg = "In iSCSI text request PDU, response PDUs sequence is remain and it will be expected empty PDU."
                HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wSession.TSIH )


            // If remained response PDU in only one ( C bit in this PDU must be 0 ), 
            // clear WaitSend status.
            let nextStatus_CLR =
                if oldContRespPDU.Length <= 1 then
                    IscsiTextEncode.ClearSendWaitStatus curParam.CurrentNegoStatus
                else
                    curParam.CurrentNegoStatus

            // response next PDU
            new IscsiTaskTextNegociation(
                objid,
                wSession,
                cid,
                counter,
                firstIFllg,
                firstITT,
                argReceivedPDU.CmdSN,
                curParam.CurrentNegoParam,
                nextStatus_CLR,
                curParam.ContPDUs,
                oldContRespPDU,
                argReceivedPDU.C,
                argReceivedPDU.F,
                false
            )

        else
            // Response PDUs sequence is not, and received PDUs sequence is terminated,
            // update the negotiation status and genelate response PDUs sequence.
            
            let nextValues, nextStatus =
                let receivedTextKeyValue =
                    argReceivedPDU :: curParam.ContPDUs
                    |> List.rev
                    |> List.map ( fun itr -> itr.TextRequest )
                    |> List.toArray
                    |> IscsiTextEncode.RecognizeTextKeyData false
                match receivedTextKeyValue with
                | ValueNone ->
                    let msg = "Failed to recognize text key-values."
                    HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                    raise <| SessionRecoveryException ( msg, wSession.TSIH )
                | ValueSome( v ) ->
                    IscsiTextEncode.margeTextKeyValue Standpoint.Target v curParam.CurrentNegoParam curParam.CurrentNegoStatus
            
            // Create sending text key bytes array
            let sendBytes = IscsiTextEncode.CreateTextKeyValueString nextValues nextStatus

            // get current effective MaxRecvDataSegmentLength value
            let mrds_i =
                match wSession.GetConnection cid counter with
                | ValueNone -> 512
                | ValueSome( c ) -> (int)( c.CurrentParams.MaxRecvDataSegmentLength_I )

            // Divite bytes array into 8192 bytes unit.
            let sendTextResponses =
                let v = 
                    [
                        let cnt = sendBytes.Length / mrds_i
                        for i = 0 to cnt - 1 do
                            yield sendBytes.[ i * mrds_i .. ( i + 1 ) * mrds_i - 1 ]
                        if cnt * mrds_i < sendBytes.Length then
                            yield sendBytes.[ cnt * mrds_i .. ]
                    ]
                if v.Length > 0 then v else [ Array.empty ]

            // If next response PDU is only one ( C bit = 0 ), then in this time, clear SendWait status.
            // Clear WaitSend status
            let nextStatus_CLR =
                if sendTextResponses.Length <= 1 then
                    IscsiTextEncode.ClearSendWaitStatus nextStatus
                else
                    nextStatus

            // responde target value to initiator
            IscsiTaskTextNegociation(
                objid,
                wSession,
                cid,
                counter,
                firstIFllg,
                firstITT,
                argReceivedPDU.CmdSN,
                nextValues,
                nextStatus_CLR,
                [],
                sendTextResponses |> List.mapi ( fun idx itr ->
                    {
                        // If initiator request to terminate negotiation, target agree finishing negotiation immidiatry.
                        F = if ( idx < sendTextResponses.Length - 1 ) then
                                false   // If C bit is 1, F bit must be 0.
                            else
                                argReceivedPDU.F;
                        C = ( idx < sendTextResponses.Length - 1 );
                        LUN = argReceivedPDU.LUN;
                        InitiatorTaskTag = argReceivedPDU.InitiatorTaskTag;
                        TargetTransferTag = argReceivedPDU.TargetTransferTag;
                        StatSN = statsn_me.zero;
                        ExpCmdSN = cmdsn_me.zero;
                        MaxCmdSN = cmdsn_me.zero;
                        TextResponse = itr;
                    }
                ),
                argReceivedPDU.C,
                argReceivedPDU.F,
                false
            )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IIscsiTask with

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskType
        override _.TaskType : iSCSITaskType =
            TextNegociation

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskTypeName
        override _.TaskTypeName : string =
            "Text negotication request"

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.InitiatorTaskTag
        override _.InitiatorTaskTag : ITT_T voption =
            ValueSome( m_FirstITT )

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.CmdSN
        override _.CmdSN : CMDSN_T voption =
            ValueSome( m_CmdSN )

        // ------------------------------------------------------------------------
        // Implementation of IIscsiTask.Immidiate
        override _.Immidiate : bool voption =
            ValueSome m_FirstIFlg
            
        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.IsExecutable
        override _.IsExecutable : bool =
            not m_Executed && ( m_NextResponsePDU.IsSome || m_IsNegotiationFinished )

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.AllegiantConnection
        override _.AllegiantConnection : struct( CID_T * CONCNT_T ) =
            struct( m_AllegiantCID, m_AllegiantConCounter )

        // ------------------------------------------------------------------------
        // Execute this command.
        // Execution of this method may be retried, but the task is executed exactly once.
        override _.GetExecuteTask () : struct( ( unit -> unit ) * IIscsiTask ) =
            let ext =
                fun () -> 
                    if m_NextResponsePDU.IsSome then
                        m_Session.SendOtherResponsePDU m_AllegiantCID m_AllegiantConCounter ( m_NextResponsePDU.Value :> ILogicalPDU )

                    if m_IsNegotiationFinished then
                        // Notice update parameter to session object
                        let curSWP = m_Session.SessionParameter
                        {
                            curSWP with
                                InitiatorAlias =
                                    match m_CurrentNegoParam.InitiatorAlias with
                                    | Value( x ) -> x
                                    | _ -> curSWP.InitiatorAlias;
                        }
                        |> m_Session.NoticeUpdateSessionParameter

                        // Notice update parameter to connection object
                        match m_Session.GetConnection m_AllegiantCID m_AllegiantConCounter with
                        | ValueNone -> ()
                        | ValueSome( connection ) ->
                            let curCOP = connection.CurrentParams
                            {
                                curCOP with
                                    MaxRecvDataSegmentLength_I =
                                        match m_CurrentNegoParam.MaxRecvDataSegmentLength_I with
                                        | Value( x ) -> x
                                        | _ -> curCOP.MaxRecvDataSegmentLength_I;
                            }
                            |> connection.NotifyUpdateConnectionParameter

            let nextTask =
                new IscsiTaskTextNegociation (
                    m_ObjID, m_Session, m_AllegiantCID, m_AllegiantConCounter, m_FirstIFlg, m_FirstITT,
                m_CmdSN, m_CurrentNegoParam,  m_CurrentNogeStatus, m_contPDUs, argRespPDU, Initiator_Last_C,
                Initiator_Last_F, true )
            struct( ext, nextTask )

        // ------------------------------------------------------------------------
        //   This task already compleated and removale or not.
        override _.IsRemovable : bool =
            m_Executed && m_IsNegotiationFinished

        // ------------------------------------------------------------------------
        // GetExecuteTask method had been called or not.
        override _.Executed : bool =
            m_Executed

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get refelence of session object.
    /// </summary>
    /// <returns>
    ///   Session object where this negotiation is performed.
    /// </returns>
    member _.Session : ISession =
        m_Session

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get current negotiated resulet.
    /// </summary>
    /// <returns>
    ///   Current negociatable parameters.
    /// </returns>
    member _.CurrentNegoParam : TextKeyValues =
        m_CurrentNegoParam

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get current negotiation status.
    /// </summary>
    /// <returns>
    ///   Current negotiation status.
    /// </returns>
    member _.CurrentNegoStatus : TextKeyValuesStatus =
        m_CurrentNogeStatus
   

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get received partial PDUs list.
    /// </summary>
    /// <returns>
    ///   the PDUs list that has 1 in C bit.
    /// </returns>
    member _.ContPDUs : TextRequestPDU list =
        m_contPDUs
   
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get next response PDU.
    /// </summary>
    /// <returns>
    ///   Next response PDU or None, if response is not exist.
    /// </returns>
    member _.NextResponsePDU : TextResponsePDU voption =
        m_NextResponsePDU

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the second and subsequent response PDUs.
    /// </summary>
    /// <returns>
    ///   Response PDUs list. If second and subsubsequent response PDUs is not exist, it returns empty list.
    /// </returns>
    member _.ContResponsePDUs : TextResponsePDU list =
        m_ContResponsePDUs

