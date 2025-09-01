//=============================================================================
// Haruka Software Storage.
// LoginNegociator.fs : Implementation of LoginNegociator class.
// LoginNegociator class performs login negociation and creates the iSCSI network portal.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition


/// <summary>
///   LoginNegociator class sends/receives login request/response PDU and performs iSCSI login procedure.
///   LoginNegociator components is created by the IscsiTCPSvPort object
///   when a TCP connection is established from the initiator.
///   If login sequence is finished successfly, this class creates instance of Connection class,
///   and regists it to the session component.
/// </summary>
/// <param name="m_StatusMaster">
///   Interface of status master component which this connection component created.
/// </param>
/// <param name="m_NetStream">
///   It represents TCP connection of this components wraps.
/// </param>
/// <param name="m_ConnectedTime">
///   Connected date time.
/// </param>
/// <param name="m_TargetPortalGroupTag">
///   The Target Portal Group Tag number of waiting TCP port which TCP connection connected to.
///   This value is always 0.
/// </param>
/// <param name="m_NetPortIdx">
///   Network portal index number where the connection was established.
/// </param>
/// <param name="m_Killer">
///   Killer object that notices terminate request to this object.
/// </param>
type LoginNegociator
    (
        m_StatusMaster : IStatus,
        m_NetStream : NetworkStream,
        m_ConnectedTime : DateTime,
        m_TargetPortalGroupTag : TPGT_T,
        m_NetPortIdx : NETPORTIDX_T,
        m_Killer : IKiller
    ) as this =

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    do
        m_Killer.Add this

        // ( In this point, connection status is in S3:XPT_UP state.)
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            g.Gen2( m_ObjID, "LoginNegociator", "TPGT=" + m_TargetPortalGroupTag.ToString() )
        )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface ILoginNegociator with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            ()

        // ------------------------------------------------------------------------
        /// <summary>
        ///   Imprementation of ILoginNegociator.Start
        /// </summary>
        /// <remarks>
        ///   "runSync = true" is used only for debug use.
        /// </remarks>
        override this.Start ( runSync : bool ) : bool =

            if runSync then
                let ex =
                    task {
                        try
                            do! this.ProcessISCSIRequest()
                            return ValueNone
                        with
                        | x -> return ValueSome( x )
                    }
                    |> Functions.RunTaskSynchronously
                m_Killer.NoticeTerminate()
                match ex with
                | ValueNone -> ()
                | ValueSome ( x ) -> raise x
            else
                fun () -> task {
                    try
                        do! this.ProcessISCSIRequest ()
                    with
                    | _ as x ->
                        // disconnect the connection.
                        // and connection status is transitioned to S1:FREE state.
                        HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                        m_NetStream.Flush()
                        m_NetStream.Close()
                        m_NetStream.Socket.Disconnect false
                        m_NetStream.Dispose()

                    // regardless of the result this object is collected by GC, but just in case, I request termination to killer object.
                    m_Killer.NoticeTerminate()
                }
                |> Functions.StartTask


            HLogger.Trace( LogID.I_COMPONENT_INITIALIZED, fun g -> g.Gen1( m_ObjID, "LoginNegociator" ) )
            // Always return true
            true


    //=========================================================================
    // Private method
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Process all of the iSCSI requests.
    /// </summary>
    /// <remarks>
    ///   In this point, connection status is in S3:XPT_UP state.
    ///   Start iSCSI login phase and process all of iSCSI request from initiator.
    ///   when exit this functin, connection status transition to S1:FREE state.
    /// </remarks>
    member private this.ProcessISCSIRequest () : Task<unit> =
        task {
            // Receive first PDU
            let! ( initiatorName, targetName, sessionType, recvPDU : LoginRequestPDU ) =
                LoginNegociator.ReceiveFirstPDU m_NetStream m_ObjID

            if String.Equals( sessionType, "Discovery", StringComparison.Ordinal ) then
                // Login request for discovery session
                do! this.ProcessDiscoverySession initiatorName recvPDU 
            else
                // Login request for normal session
                do! this.ProcessNomalSession initiatorName targetName recvPDU 
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Receive the first Login Request iSCSI PDU
    /// </summary>
    /// <param name="argSock">
    ///   The TCP connection which receives Login request PDU.
    /// </param>
    /// <param name="objid">
    ///   Object identifier of called object.
    /// </param>
    /// <returns>
    ///   Touple of following values.
    ///   * Initiator name. The name of initiator that trying login.
    ///   * Target name. The name of target which initiator want to access.
    ///   * Session type. "Normal" or "Discovery".
    ///   * Received PDU. Login request PDU that has protocol version, ISID, TSIH, CID and next negotiation stage.
    /// </returns>
    /// <remarks>
    ///   See RFC 3720 5.3.1
    /// </remarks>
    static member private ReceiveFirstPDU ( argSock : Stream ) ( objid : OBJIDX_T ) : Task< string * string * string * LoginRequestPDU > =
        task {
            let! recvPDU_logi = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, argSock, Standpoint.Target )
            if recvPDU_logi.Opcode <> OpcodeCd.LOGIN_REQ then
                HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( objid, Constants.getOpcodeNameFromValue recvPDU_logi.Opcode ) )
                raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
            let recvPDU = recvPDU_logi :?> LoginRequestPDU

            // In this point, if C bit is true, Text Request Data is partial.
            // But received text request data should have InitiatorName, TargetName text key ( RFC3720 5.3.1 ).
            // ( To send any response PDU, I have to decide login type ( leading login or not, session recovery or not... ) 
            //   for that purpose, I need to know following information : SessionType, InitiatorName, TargetName, ISID, TSIH. )
            // So, this function recognize partially text request data.

            let lastNullChar = Array.tryFindIndexBack( (=) 0x00uy ) recvPDU.TextRequest
            if lastNullChar.IsNone then
                let msg = "In iSCSI Login request PDU, required text key does not exist."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )

                // In this case, it may be considered format error ( RFC3720 6.6 ), 
                // but, in login response pdu, "Missing parameter(0x0207)" status code is defined ( RFC3720 10.13 ),
                // So I decide to send this Status value.

                // send login response PDU ( reject connection )
                let! _= PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, argSock, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.MISSING_PARAMS;
                    } )

                // close this connection
                raise <| SessionRecoveryException ( msg, tsih_me.zero )
        
            let keyValues = IscsiTextEncode.TextKeyData2KeyValues [| recvPDU.TextRequest.[ 0 .. lastNullChar.Value - 1 ] |] 
            if keyValues.IsNone then
                let msg = "In iSCSI Login request PDU, Login Text Key data format error."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                // close this connection
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Get TargetName value ( it may be ommit if SessionType is "Discovery" )
            let targetName =
                match IscsiTextEncode.SearchTextKeyValue "TargetName" keyValues.Value with
                | TextValueType.Value( y ) ->
                    match IscsiTextEncode.ISCSINameValueBytes2String( y ) with
                    | ValueNone ->
                        let msg = "In iSCSI Login request PDU, TargetName value is invalid. format error."
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )
                    | ValueSome( z ) -> z
                | ISV_Missing ->
                    ""
                | _ ->
                    let msg = "In iSCSI Login request PDU, TargetName value should not be reserved value."
                    HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                    raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Get InitiatorName value ( It must not ommited )
            let wSTKV = IscsiTextEncode.SearchTextKeyValue "InitiatorName" keyValues.Value
            if wSTKV = ISV_Missing then
                let msg = "In iSCSI Login request PDU, InitiatorName text key does not exist."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, argSock, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.MISSING_PARAMS;
                    } )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            if wSTKV = ISV_NotUnderstood || wSTKV = ISV_Irrelevant || wSTKV = ISV_Reject then
                let msg = "In iSCSI Login request PDU, InitiatorName value is invalid."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            let initiatorName =
                match IscsiTextEncode.ISCSINameValueBytes2String( wSTKV.GetValue ) with
                | ValueNone ->
                    let msg = "In iSCSI Login request PDU, InitiatorName value is not iSCSI-name-value."
                    HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                    raise <| SessionRecoveryException ( msg, tsih_me.zero )
                | ValueSome( z ) ->
                    z

            // Get SessionType value ( it may be ommit if SessionType is "Normal" (=default) )
            let! sessionType =
                match IscsiTextEncode.SearchTextKeyValue "SessionType" keyValues.Value with
                | ISV_Missing ->
                    task{ return "" }
                | TextValueType.Value( x ) ->
                    task {
                        let xs = Encoding.UTF8.GetString x
                        if String.Equals( xs, "Normal", StringComparison.Ordinal ) || String.Equals( xs, "Discovery", StringComparison.Ordinal ) then
                            return xs
                        else
                            let msg = sprintf "In iSCSI Login request PDU, SessionType value(%s) is invalid." xs
                            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                            let! _=
                                PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, argSock, {
                                    PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                                        Status = LoginResStatCd.UNSUPPORT_SESS_TYPE;
                                } )
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )
                            return ""
                    }
                | _ ->
                    task {
                        let msg = "In iSCSI Login request PDU, SessionType value is invalid."
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                        let! _ =
                            PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, argSock, {
                                PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                                    Status = LoginResStatCd.UNSUPPORT_SESS_TYPE;
                            } )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )
                        return ""
                    }

            assert( initiatorName.Length > 0 )

            // Check TargetName and SessionType values.
            if targetName.Length = 0 && ( String.Equals( sessionType, "Discovery", StringComparison.Ordinal ) |> not ) then
                let msg = "In iSCSI Login request PDU, if SessionType is not Discovery session, TargetName key should exist."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( objid, msg ) )
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, argSock, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.MISSING_PARAMS;
                    } )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Return values
            return ( initiatorName, targetName, sessionType, recvPDU )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Login for discovery session and process all of iSCSI requests from initiator.
    /// </summary>
    /// <param name="initiatorName">
    ///   Initiator name that requests to establish the discovery session.
    /// </param>
    /// <param name="recvPDU">
    ///   First login request PDU. This PDU may have "partial" text key-value. 
    ///   (In this case, C bit is true, and must receive following PDU until C bit set to false )
    /// </param>
    member private this.ProcessDiscoverySession
        ( initiatorName : string )
        ( recvPDU : LoginRequestPDU ) : Task<unit> =

        task {
            assert( initiatorName.Length > 0 )

            // Get default parameters.
            let iSCSIParamsCO = m_StatusMaster.IscsiNegoParamCO
            let iSCSIParamsSW = {
                m_StatusMaster.IscsiNegoParamSW with
                    InitiatorName = initiatorName;
                    TargetPortalGroupTag = m_TargetPortalGroupTag;
            }

            let dummyTargetNodeConfig : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                LUN = [];
                Auth = TargetGroupConf.U_None();
            }

            // Check specified version number
            if recvPDU.VersionMin > 0uy then
                HLogger.Trace( LogID.E_UNSUPPORTED_ISCSI_VERSION, fun g -> g.Gen2( m_ObjID, recvPDU.VersionMax, recvPDU.VersionMin ) )
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.UNSUPPORTED_VERSION;
                    } )
                raise <| SessionRecoveryException ( "Unsupported version is requested.", tsih_me.zero )

            if recvPDU.CSG = LoginReqStateCd.FULL then
                let msg = "Unsupported negotiation stage was selected in discovery session."
                HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                let loginRespPDU : ILogicalPDU = {
                    PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                        Status = LoginResStatCd.INITIATOR_ERR;
                }
                let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, loginRespPDU )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            let! loginPhaseLastPDU, next1_iSCSIParamCO, next1_iSCSIParamSW =
                task {
                    if recvPDU.CSG = LoginReqStateCd.SEQURITY then
                        // Perform SEQURITY negotiation
                        let! secPhaseLastPDU, next2_stage, next2_iSCSIParamCO, next2_iSCSIParamSW =
                            this.SequrityNegotiation dummyTargetNodeConfig recvPDU iSCSIParamsCO iSCSIParamsSW tsih_me.zero

                        // if not ommit operational negotiation stage, try this stage.
                        if next2_stage = LoginReqStateCd.OPERATIONAL then

                            // send last one PDU in security negotiation phase
                            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, secPhaseLastPDU )

                            // receive first PDU in operational negotiation phase
                            let! wLoginPDU = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                            if wLoginPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                                HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wLoginPDU.Opcode ) )
                                raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

                            // perform operational negotiation
                            let next_LRP = wLoginPDU :?> LoginRequestPDU
                            return! this.OperationalNegotiation true false dummyTargetNodeConfig next_LRP next2_iSCSIParamCO next2_iSCSIParamSW false tsih_me.zero
                        else
                            return ( secPhaseLastPDU, next2_iSCSIParamCO, next2_iSCSIParamSW )
                    else
                        // If ommit sequrity negotiation stage, operational negotiation stage is required.
                        return! this.OperationalNegotiation true false dummyTargetNodeConfig recvPDU iSCSIParamsCO iSCSIParamsSW false tsih_me.zero
                }

            // send last one PDU in login phase
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, loginPhaseLastPDU )

            HLogger.Trace( LogID.I_START_DISCOVERY_SESSION, fun g -> g.Gen1( m_ObjID, initiatorName ) )
            let mrdsl_I = next1_iSCSIParamCO.MaxRecvDataSegmentLength_I  // Max Recv Data Segment Length for the Initiator
            let mrdsl_T = next1_iSCSIParamCO.MaxRecvDataSegmentLength_T  // Max Recv Data Segment Length for the Target
            let hDigest = next1_iSCSIParamCO.HeaderDigest.[0]            // Header Digest
            let dDigest = next1_iSCSIParamCO.DataDigest.[0]              // Data Digest
            let localAddress = ( m_NetStream.Socket.LocalEndPoint :?> IPEndPoint ).Address  // local port IP address

            // receive first PDU in discovery session
            let! next2_CPDU = PDU.Receive( mrdsl_T, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )

            let negoloop struct ( beforePDU : ILogicalPDU, curStatSN : STATSN_T ) : Task< LoopState< struct( ILogicalPDU * STATSN_T ), unit > > =
                task {
                    if beforePDU.Opcode = OpcodeCd.LOGOUT_REQ then
                        let logoutReq = beforePDU :?> LogoutRequestPDU
                        if logoutReq.ReasonCode <> LogoutReqReasonCd.CLOSE_SESS then
                            let msg = "Invalid logout reason in discovery session."
                            HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )
                            return LoopState.Terminate()
                        else
                            // Send Logout Response PDU and close the connection
                            let resp =
                                {
                                    Response = LogoutResCd.SUCCESS;
                                    InitiatorTaskTag = logoutReq.InitiatorTaskTag;
                                    StatSN =  logoutReq.ExpStatSN;
                                    ExpCmdSN = if logoutReq.I then logoutReq.CmdSN else cmdsn_me.next logoutReq.CmdSN;
                                    MaxCmdSN = if logoutReq.I then logoutReq.CmdSN else cmdsn_me.next logoutReq.CmdSN;
                                    Time2Wait = 0us;
                                    Time2Retain = 0us;
                                    CloseAllegiantConnection = true;    // Close the connection
                                } :> ILogicalPDU
                            let! _ = PDU.SendPDU( mrdsl_I, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, resp )
                            do! m_NetStream.FlushAsync()
                            m_NetStream.Socket.Disconnect false
                            m_NetStream.Close()
                            m_NetStream.Dispose()
                            HLogger.Trace( LogID.I_END_DISCOVERY_SESSION, fun g -> g.Gen1( m_ObjID, initiatorName ) )
                            return LoopState.Terminate()

                    elif beforePDU.Opcode = OpcodeCd.TEXT_REQ then

                        let! textKey, recvPDU, nextStatSN =
                            this.ReceiveTextRequestSequense ( beforePDU :?> TextRequestPDU ) next1_iSCSIParamCO curStatSN

                        // if keys not allowed in discovery session is used, drop this connection
                        if textKey.AuthMethod <> TextValueType.ISV_Missing ||
                            textKey.CHAP_A <> TextValueType.ISV_Missing ||
                            textKey.CHAP_I <> TextValueType.ISV_Missing ||
                            textKey.CHAP_C <> TextValueType.ISV_Missing ||
                            textKey.CHAP_N <> TextValueType.ISV_Missing ||
                            textKey.CHAP_R <> TextValueType.ISV_Missing ||
                            textKey.HeaderDigest <> TextValueType.ISV_Missing ||
                            textKey.DataDigest <> TextValueType.ISV_Missing ||
                            textKey.MaxConnections <> TextValueType.ISV_Missing ||
                            textKey.TargetName <> TextValueType.ISV_Missing ||
                            textKey.InitiatorName <> TextValueType.ISV_Missing ||
                            textKey.TargetAlias <> TextValueType.ISV_Missing ||
                            textKey.TargetAddress <> TextValueType.ISV_Missing ||
                            textKey.TargetPortalGroupTag <> TextValueType.ISV_Missing ||
                            textKey.InitialR2T <> TextValueType.ISV_Missing ||
                            textKey.ImmediateData <> TextValueType.ISV_Missing ||
                            textKey.MaxRecvDataSegmentLength_I <> TextValueType.ISV_Missing ||
                            textKey.MaxRecvDataSegmentLength_T <> TextValueType.ISV_Missing ||
                            textKey.MaxBurstLength <> TextValueType.ISV_Missing ||
                            textKey.FirstBurstLength <> TextValueType.ISV_Missing ||
                            textKey.DefaultTime2Wait <> TextValueType.ISV_Missing ||
                            textKey.DefaultTime2Retain <> TextValueType.ISV_Missing ||
                            textKey.MaxOutstandingR2T <> TextValueType.ISV_Missing ||
                            textKey.DataPDUInOrder <> TextValueType.ISV_Missing ||
                            textKey.DataSequenceInOrder <> TextValueType.ISV_Missing ||
                            textKey.ErrorRecoveryLevel <> TextValueType.ISV_Missing ||
                            textKey.SessionType <> TextValueType.ISV_Missing ||
                            textKey.UnknownKeys.Length > 0 then
                                let msg = "Invalid text key was received in discovery session."
                                HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                                raise <| SessionRecoveryException ( msg, tsih_me.zero )

                        let responseData =
                            match textKey.SendTargets with
                            | TextValueType.Value( "All" ) ->
                                [|
                                    let enc = Encoding.GetEncoding( "utf-8" )
                                    let targetConfs = m_StatusMaster.GetActiveTarget()
                                    let netPortalConfs = m_StatusMaster.GetNetworkPortal()
                                    for tn in targetConfs do
                                        let targetNameStr = "TargetName=" + tn.TargetName
                                        yield! enc.GetBytes targetNameStr
                                        yield '\u0000'B

                                        for pn in netPortalConfs do
                                            if pn.TargetPortalGroupTag = tn.TargetPortalGroupTag then
                                                let wadr = LoginNegociator.GenTargetAddressString pn localAddress
                                                let targetAddressStr = sprintf "TargetAddress=%s:%d,%d" wadr pn.PortNumber pn.TargetPortalGroupTag
                                                yield! enc.GetBytes targetAddressStr
                                                yield '\u0000'B
                                |]

                            | TextValueType.Value( "" ) ->
                                let msg = "Invalid SendTargets value was received in discovery session."
                                HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                                raise <| SessionRecoveryException ( msg, tsih_me.zero )
                                Array.empty
                            | TextValueType.Value( targetName ) ->
                                [|
                                    let enc = Encoding.GetEncoding( "utf-8" )
                                    let targetConfs = m_StatusMaster.GetActiveTarget()
                                    let netPortalConfs = m_StatusMaster.GetNetworkPortal()
                                    for tn in targetConfs do
                                        if String.Equals( tn.TargetName, targetName, StringComparison.Ordinal ) then
                                            let targetNameStr = "TargetName=" + tn.TargetName
                                            yield! enc.GetBytes targetNameStr
                                            yield '\u0000'B
                                            for pn in netPortalConfs do
                                                if pn.TargetPortalGroupTag = tn.TargetPortalGroupTag then
                                                    let wadr = LoginNegociator.GenTargetAddressString pn localAddress
                                                    let targetAddressStr = sprintf "TargetAddress=%s:%d,%d" wadr pn.PortNumber pn.TargetPortalGroupTag
                                                    yield! enc.GetBytes targetAddressStr
                                                    yield '\u0000'B
                                |]
                            | TextValueType.ISV_Missing ->
                                Array.empty
                            | _ ->
                                let msg = "Invalid SendTargets value was received in discovery session."
                                HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                                raise <| SessionRecoveryException ( msg, tsih_me.zero )
                                Array.empty

                        // Send text response PDU
                        let! nextStatSN2 =
                            this.SendTextResponse_InBytes responseData next1_iSCSIParamCO recvPDU nextStatSN recvPDU.F

                        // Receive next PDU
                        let! wnextLogiPDU =
                            PDU.Receive( mrdsl_T, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )

                        return LoopState.Continue struct( wnextLogiPDU, nextStatSN2 + nextStatSN )

                    else
                        let msg = "Invalid PDU type in discovery session."
                        HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )
                        return LoopState.Terminate()
                }
            do! Functions.loopAsyncWithArgs negoloop struct( next2_CPDU, next2_CPDU.ExpStatSN )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Generate target address string for the discovery session.
    /// </summary>
    /// <param name="npconf">Network portal configuration</param>
    /// <param name="localadr">Local port IP address.</param>
    /// <returns>Target address string</returns>
    static member private GenTargetAddressString ( npconf : TargetDeviceConf.T_NetworkPortal ) ( localadr : IPAddress ) : string =
        let convIPToStr ( a : IPAddress ) ( def : string ) =
            if a.AddressFamily = AddressFamily.InterNetwork then
                // IPv4 address
                a.ToString()
            elif a.AddressFamily = AddressFamily.InterNetworkV6 then
                // IPv6 address
                if a.IsIPv4MappedToIPv6 then
                    // ::FFFF:nnn.nnn.nnn.nnn, It returned as IPv4 format "nnn.nnn.nnn.nnn".
                    ( a.MapToIPv4() ).ToString()
                else
                    // IPv6 address, It returned as "[aaaa:bbbb:...:ffff]"
                    "[" + a.ToString() + "]"
            else
                def

        if npconf.TargetAddress.Length > 0 then
            // Target address is specified.
            let r, v = IPAddress.TryParse npconf.TargetAddress
            if not r then
                // Specified address is considered as host name.
                npconf.TargetAddress
            else
                // Specified address is IP address. If unknown protocol is used, it returns configured string.
                convIPToStr v npconf.TargetAddress
        else
            // If target address is not specified, local address of the deicovery session is used.
            convIPToStr localadr ( localadr.ToString() )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Receive text request PDUs sequense with C bit equals 1, in discovery session.
    /// </summary>
    /// <param name="beforePDU">
    ///   PDUs received earlier.
    /// </param>
    /// <param name="conParam">
    ///   Current effective connection wide parameters.
    /// </param>
    /// <param name="curStatSN">
    ///   Current StatSN value.
    /// </param>
    /// <returns>
    ///   received Text key values, PDU last received, next StatSN value
    /// </returns>
    member private _.ReceiveTextRequestSequense ( beforePDU : TextRequestPDU ) ( conParam : IscsiNegoParamCO ) ( curStatSN : STATSN_T ) : Task< ( TextKeyValues * TextRequestPDU * STATSN_T ) > =
        let mrdsl_I = conParam.MaxRecvDataSegmentLength_I  // Max Recv Data Segment Length for the Initiator
        let mrdsl_T = conParam.MaxRecvDataSegmentLength_T  // Max Recv Data Segment Length for the Target
        let hDigest = conParam.HeaderDigest.[0]            // Header Digest
        let dDigest = conParam.DataDigest.[0]              // Data Digest

        // receive text request pdu sequence with c bit.
        let cbitLoop struct( beforePDU2 : TextRequestPDU, curStatSN2 : STATSN_T, rv : List< TextRequestPDU > )
                : Task< LoopState< struct( TextRequestPDU * STATSN_T * List< TextRequestPDU > ), unit > > =
            task {
                let nextCmdSN =
                    if beforePDU2.I then
                        beforePDU2.CmdSN
                    else
                        cmdsn_me.next beforePDU2.CmdSN

                // send to empty text response PDU
                let! _ =
                    PDU.SendPDU(
                        mrdsl_I, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream,
                        {
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = beforePDU2.InitiatorTaskTag;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            StatSN = curStatSN2;
                            ExpCmdSN = nextCmdSN;
                            MaxCmdSN = nextCmdSN;
                            TextResponse = Array.empty;
                        }
                    )
 
                // receive next one
                let! wnextTextPDU = PDU.Receive( mrdsl_T, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                if wnextTextPDU.Opcode <> OpcodeCd.TEXT_REQ then
                    HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextTextPDU.Opcode ) )
                    raise <| SessionRecoveryException ( "Unexpected PDU was received in discovery session.", tsih_me.zero )

                let recvPDU = wnextTextPDU :?> TextRequestPDU
                if recvPDU.ExpStatSN <> ( statsn_me.next curStatSN2 ) then
                    HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextTextPDU.Opcode ) )
                    raise <| SessionRecoveryException ( "Unexpected ExpStatSN was received in discovery session.", tsih_me.zero )

                if recvPDU.CmdSN <> nextCmdSN then
                    HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextTextPDU.Opcode ) )
                    raise <| SessionRecoveryException ( "Unexpected CmdSN was received in discovery session.", tsih_me.zero )

                rv.Add recvPDU
                if recvPDU.C then
                    return LoopState.Continue( struct( recvPDU, ( statsn_me.next curStatSN2 ), rv ) )
                else
                    return LoopState.Terminate()
            }

        task {
            let rv = new List< TextRequestPDU >()
            rv.Add beforePDU
            if beforePDU.C then
                do! Functions.loopAsyncWithArgs cbitLoop struct( beforePDU, curStatSN, rv )
            let pduList = [| for itr in rv -> itr |]

            // Recognize received login request PDUs
            let reqs_opt, recvPDU =
                (
                    pduList
                    |> Array.map ( fun x -> x.TextRequest )
                    |> IscsiTextEncode.RecognizeTextKeyData false,
                    Array.last pduList
                )

            if reqs_opt.IsNone then
                // format error
                let msg = "In iSCSI text request PDU, Text request data is invalid in discovery session."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            return ( reqs_opt.Value, recvPDU, ( statsn_me.incr ( uint32 pduList.Length - 1u ) curStatSN ) )
        }


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Negotiation response bytes data to initiator.
    /// </summary>
    /// <param name="sendBytes">
    ///   Response data bytes.
    /// </param>
    /// <param name="conParam">
    ///   Current effective connection wide parameters.
    /// </param>
    /// <param name="recvPDU">
    ///   The login request PDU that cource of this login response PDU.
    /// </param>
    /// <param name="curStatSN">
    ///   Current StatSN value.
    /// </param>
    /// <param name="finalFlg">
    ///   if finalFlg is true, F bit in response PDU is set to 1.
    /// </param>
    member private _.SendTextResponse_InBytes
            ( sendBytes : byte[] )
            ( conParam : IscsiNegoParamCO )
            ( recvPDU : TextRequestPDU )
            ( curStatSN : STATSN_T )
            ( finalFlg : bool ) : Task< STATSN_T > =
        task {
            let mrdsl_I = conParam.MaxRecvDataSegmentLength_I  // Max Recv Data Segment Length for the Initiator
            let mrdsl_T = conParam.MaxRecvDataSegmentLength_T  // Max Recv Data Segment Length for the Target
            let hDigest = conParam.HeaderDigest.[0]            // Header Digest
            let dDigest = conParam.DataDigest.[0]              // Data Digest

            // Divite bytes array into MaxRecvDataSegmentLength_I bytes unit.
            let sendTextResponses =
                let v = Array.chunkBySize ( int mrdsl_I ) sendBytes
                if v.Length > 0 then v else [| Array.empty |]

            let cbitloop struct( idx : int, recvPDU2 : TextRequestPDU ) :
                    Task< LoopState< struct( int * TextRequestPDU ), unit > > =
                task {
                    // Decide C bit value
                    let cBitValue = ( idx < sendTextResponses.Length - 1 )

                    // Decide next CmdSN value.
                    let nextCmdSN =
                        if recvPDU2.I then
                            recvPDU2.CmdSN
                        else
                            cmdsn_me.next recvPDU2.CmdSN;

                    // Send Text Response PDU
                    let! _ = PDU.SendPDU( mrdsl_I, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, 
                        {
                            F = finalFlg;
                            C = cBitValue;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = recvPDU.InitiatorTaskTag;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            StatSN = statsn_me.incr ( uint32 idx ) curStatSN;
                            ExpCmdSN = nextCmdSN;
                            MaxCmdSN = nextCmdSN;
                            TextResponse = sendTextResponses.[idx];
                        }
                    )
        
                    // if C bit equals 1, receive empty text request PDU
                    if cBitValue then
                        let! wnextTextPDU =
                            PDU.Receive( mrdsl_T, hDigest, dDigest, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )

                        if wnextTextPDU.Opcode <> OpcodeCd.TEXT_REQ then
                            HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextTextPDU.Opcode ) )
                            raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

                        let emptyTextRequestPDU = wnextTextPDU :?> TextRequestPDU
            
                        // Check received PDU
                        if emptyTextRequestPDU.TextRequest.Length > 0 || emptyTextRequestPDU.C then
                            // protocol error
                            let msg = "Response of Text response PDU with C bit set to 1 ( that is Login request PDU from Initiator ), TextRequest is not empty."
                            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )

                        if emptyTextRequestPDU.CmdSN <> nextCmdSN then
                            HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextTextPDU.Opcode ) )
                            raise <| SessionRecoveryException ( "Unexpected CmdSN was received in discovery session.", tsih_me.zero )

                        if emptyTextRequestPDU.ExpStatSN <> statsn_me.incr ( uint32 idx + 1u ) curStatSN then
                            HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextTextPDU.Opcode ) )
                            raise <| SessionRecoveryException ( "Unexpected ExpStatSN was received in discovery session.", tsih_me.zero )

                        return LoopState.Continue( struct ( idx + 1, emptyTextRequestPDU ) )
                    else
                        return LoopState.Terminate()

                }
            do! Functions.loopAsyncWithArgs cbitloop struct( 0, recvPDU )
            return statsn_me.fromPrim ( uint32 sendTextResponses.Length );
        }
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Login for normal session and process all of iSCSI requests from initiator.
    /// </summary>
    /// <param name="initiatorName">
    ///   Initiator name that requests to establish the normal session.
    /// </param>
    /// <param name="targetName">
    ///   The name of target which initiator want to access.
    /// </param>
    /// <param name="recvPDU">
    ///   First login request PDU. This PDU may have "partial" text key-value. 
    ///   (In this case, C bit is true, and must receive following PDU until C bit set to false )
    /// </param>
    member
        private this.ProcessNomalSession
        ( initiatorName : string )
        ( targetName : string )
        ( recvPDU : LoginRequestPDU ) : Task<unit> =

        task {
            assert( initiatorName <> "" )
            assert( targetName <> "" )

            // Create I_T nexus identifier
            let I_TNexusIdent = new ITNexus( initiatorName, recvPDU.ISID, targetName, m_TargetPortalGroupTag )

            // search existing session or not.
            let itnTSIH = m_StatusMaster.GetTSIH I_TNexusIdent

            // Get connection wide default parameters.
            let iSCSIParamsCO = m_StatusMaster.IscsiNegoParamCO

            if ( itnTSIH = tsih_me.zero && recvPDU.TSIH <> tsih_me.zero ) ||
                ( itnTSIH <> tsih_me.zero && recvPDU.TSIH <> tsih_me.zero && itnTSIH <> recvPDU.TSIH ) then
                // Specified session is missing. Login failed.
                let msg =
                    sprintf
                        "Login failed. Specified session is missing. I_T nexus=%s, corresponding TSIH=%d, specified TSIH=%d"
                        ( I_TNexusIdent.I_TNexusStr ) itnTSIH recvPDU.TSIH
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )

                // In this case, it may be considered format error ( RFC3720 6.6 ), 
                // but, in login response pdu, "Missing parameter(0x0207)" status code is defined ( RFC3720 10.13 ),
                // So I decide to send this Status value.

                // send login response PDU ( reject connection )
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.SESS_NOT_EXIST;
                    } )
                // close this connection
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            let isLeadingCon, isConnRebuild, iSCSIParamsSW1 =
                if recvPDU.TSIH = tsih_me.zero then
                    // Create new session or rebuild existing session.
                    // This connection is the leading connection.
                    true, false, m_StatusMaster.IscsiNegoParamSW
                else
                    // Create new connection and add to existing session, or rebuild existing connection.
                    assert( itnTSIH <> tsih_me.zero )
                    assert( itnTSIH = recvPDU.TSIH )

                    // Get session interface representing session which logging to .
                    let sessionIF_opt = m_StatusMaster.GetSession itnTSIH

                    if sessionIF_opt.IsNone then
                        HLogger.Trace( LogID.E_MISSING_SESSION, fun g -> g.Gen1( m_ObjID, itnTSIH ) )
                        raise <| SessionRecoveryException ( "Missing session. Maybe it occurred consistency error.", tsih_me.zero )
                    let sessionIF = sessionIF_opt.Value

                    // Get current session parameters
                    let sessionPa = sessionIF.SessionParameter

                    // Receive CID is existing connection or not
                    let wOldConn = 
                        sessionIF.GetAllConnections()
                        |> Array.tryFind ( fun itr -> itr.CID = recvPDU.CID )

                    // When an implicit logout of a connection is performed, the StatSN value is inherited from the previous connection.
                    // Therefore, the initiator must set the next StatSN value to ExpStatSN.
                    // Since the login sequence assumes ErrorRecoveLevel = 0, if there is a discrepancy, it is considered an error and the login is rejected.
                    // Note that if ExpStatSN is 0, it is permitted because it indicates that the initiator believes the old connection no longer exists.
                    if wOldConn.IsSome then
                        let nextStatSN = wOldConn.Value.NextStatSN
                        if  recvPDU.ExpStatSN <> statsn_me.zero && nextStatSN <> recvPDU.ExpStatSN then
                            let msg = sprintf "Invalid ExpStatSN(%d) value. Expected %d." ( statsn_me.toPrim recvPDU.ExpStatSN ) ( statsn_me.toPrim nextStatSN )
                            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    false, wOldConn.IsSome, sessionPa

            let dropSessionTSIH, newTSIH =
                if isLeadingCon then
                    if itnTSIH = tsih_me.zero then
                        tsih_me.zero, m_StatusMaster.GenNewTSIH()
                    else
                        itnTSIH, m_StatusMaster.GenNewTSIH()
                else
                    tsih_me.zero, itnTSIH

            // Check specified version number
            if recvPDU.VersionMin > 0uy then
                HLogger.Trace( LogID.E_UNSUPPORTED_ISCSI_VERSION, fun g -> g.Gen2( m_ObjID, recvPDU.VersionMax, recvPDU.VersionMin ) )
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.UNSUPPORTED_VERSION;
                    } )

                // close this connection
                raise <| SessionRecoveryException ( "Unsupported version is requested.", tsih_me.zero )

            // Search TargetNode of login target .
            let findTargetResult =
                m_StatusMaster.GetActiveTargetGroup()
                |> Seq.map ( fun itg -> 
                    itg.Target |> Seq.map ( fun it -> itg.TargetGroupID, it )
                )
                |> Seq.concat
                |> Seq.tryFind ( fun ( _, tconf ) -> tconf.TargetName = targetName && tconf.TargetPortalGroupTag = m_TargetPortalGroupTag )
            if findTargetResult.IsNone then
                // close this connection
                HLogger.Trace( LogID.E_UNKNOWN_TARGET_NAME, fun g -> g.Gen1( m_ObjID, targetName ) )
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            Status = LoginResStatCd.NOT_FOUND;
                    } )
                raise <| SessionRecoveryException ( "TargetName missing.", tsih_me.zero )
            let targetGroupID, targetNodeConfig = findTargetResult.Value
        
            let iSCSIParamsSW =
                {
                    iSCSIParamsSW1 with
                        InitiatorName = initiatorName;
                        TargetGroupID = targetGroupID;
                        TargetConf = targetNodeConfig;
                        TargetPortalGroupTag = m_TargetPortalGroupTag;
                }

            assert( recvPDU.CSG <> LoginReqStateCd.FULL )

            let! loginPhaseLastPDU, next1_iSCSIParamCO, next1_iSCSIParamSW =
                task {
                    if recvPDU.CSG = LoginReqStateCd.SEQURITY then
                        // Try to sequrity negotiation stage
                        let! secPhaseLastPDU, next2_stage, next2_iSCSIParamCO, next2_iSCSIParamSW =
                            this.SequrityNegotiation targetNodeConfig recvPDU iSCSIParamsCO iSCSIParamsSW newTSIH

                        // if not ommit operational negotiation stage, try this stage.
                        if next2_stage = LoginReqStateCd.OPERATIONAL then

                            // send last one PDU in security negotiation phase
                            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, secPhaseLastPDU )

                            // receive first PDU in operational negotiation phase
                            let! wLoginPDU = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                            if wLoginPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                                HLogger.Trace( LogID.E_PROTOCOL_ERROR, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wLoginPDU.Opcode ) )
                                raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

                            // perform operational negotiation
                            let next_LRP = wLoginPDU :?> LoginRequestPDU
                            return! this.OperationalNegotiation isLeadingCon isConnRebuild targetNodeConfig next_LRP next2_iSCSIParamCO next2_iSCSIParamSW true newTSIH
                        else
                            return ( secPhaseLastPDU, next2_iSCSIParamCO, next2_iSCSIParamSW )
                    else
                        // If ommit sequrity negotiation stage, operational negotiation stage is required.
                        return! this.OperationalNegotiation isLeadingCon isConnRebuild targetNodeConfig recvPDU iSCSIParamsCO iSCSIParamsSW false newTSIH
                }

            // Create or add connection or session
            this.CreateOrAddNewConnection
                isLeadingCon                        // Leading connection or not ( = if true, create new session )
                isConnRebuild                       // Rebuild connection or not ( If true, drop existing CID connection )
                dropSessionTSIH                     // Whether to drop the existing session.
                newTSIH                             // The TSIH value to be set for a newly established session.
                I_TNexusIdent                       // I_T next identifier of session
                recvPDU.CID                         // CID of new connection ( and CID of drop target connection, if isConnRebuild is true )
                recvPDU.CmdSN                       // Initial CmdSN of newly create session.
                recvPDU.ExpStatSN                   // The first ExpStatSN. ( used for receive acknowledge of old connection )
                next1_iSCSIParamCO                  // Negotiated connection only parameters.
                next1_iSCSIParamSW                  // Negotiated session wide parameters.


            // send last one PDU in login phase
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, loginPhaseLastPDU )
            ()

        }
        
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Try to sequrity negotiation stage.
    ///   If initiator and target are authenticated, return next stage, first PDU of next stage, 
    ///   and negotiated parameters.
    ///   If not, exception is thrown. In this case, this connection must dropped.
    ///   Regardless result, LoginNegociator object is must be destroyed. 
    /// </summary>
    /// <param name="targetConfig">
    ///   The configuration information of target  being logged in.
    /// </param>
    /// <param name="firstPDU">
    ///   The first PDU of this negotiation stage.
    ///   (In this case, C bit is true, and must receive following PDU until C bit set to false)
    /// </param>
    /// <param name="coParam">
    ///   Initial values of connection only parameters. 
    /// </param>
    /// <param name="swParam">
    ///   Initial values of session wide parameters. 
    /// </param>
    /// <param name="newTSIH">
    ///   The TSIH value to be sent in the last PDU of the login sequence.
    /// </param>
    /// <returns>
    ///   Touple of following values.
    ///   * Last one PDU of secutiry negotiation stage.
    ///   * next negotiation stage.( LoginReqStateCd.OPERATIONAL or LoginReqStateCd.FULL )
    ///   * Negotiated connection only parameters.
    ///   * Negotiated session wide parameters.
    /// </returns>
    member private this.SequrityNegotiation
        ( targetConfig : TargetGroupConf.T_Target )
        ( firstPDU : LoginRequestPDU )
        ( coParam : IscsiNegoParamCO )
        ( swParam : IscsiNegoParamSW )
        ( newTSIH : TSIH_T ) : 
        Task<struct ( LoginResponsePDU * LoginReqStateCd * IscsiNegoParamCO * IscsiNegoParamSW )> =
        
        task {
            let wAuthMethod =
                match targetConfig.Auth with
                | TargetGroupConf.T_Auth.U_CHAP( _ ) ->
                    AuthMethodCandidateValue.AMC_CHAP
                | TargetGroupConf.T_Auth.U_None( _ ) ->
                    AuthMethodCandidateValue.AMC_None

            // create data structure for negotiation
            let targetAuthMethod = {
                TextKeyValues.defaultTextKeyValues with
                    AuthMethod = TextValueType.Value( [| wAuthMethod |] );
                    TargetPortalGroupTag = TextValueType.Value( tpgt_me.toPrim swParam.TargetPortalGroupTag )
            }
            let initialNegoStatus = {
                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                    NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                    NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitSend;
            }
            
            // Negotiation for AuthMethod
            let! negoAM_Result =
                this.decideAuthMethodNegotiation firstPDU targetAuthMethod initialNegoStatus wAuthMethod

            // In above procedure, AuthMethod is negotiated in target expectation value.
            assert( negoAM_Result.AuthMethod <> ISV_Missing )
            assert( negoAM_Result.AuthMethod <> ISV_NotUnderstood )
            assert( negoAM_Result.AuthMethod <> ISV_Irrelevant )
            assert( negoAM_Result.AuthMethod <> ISV_Reject )
            assert( negoAM_Result.AuthMethod.GetValue.Length = 1 )
            assert( negoAM_Result.AuthMethod.GetValue.[0] = wAuthMethod )
        
            // Authentication with CHAP
            if wAuthMethod = AuthMethodCandidateValue.AMC_CHAP then
                do! this.SequrityNegotiation_WithCHAP targetConfig
        
            // wait for login request with T bit is 1.
            let waitTrance struct( negoValue, negoStat ) :
                    Task< LoopState<
                        struct( TextKeyValues * TextKeyValuesStatus ),
                        struct( LoginResponsePDU * LoginReqStateCd )
                    > > =
                task {
                    // receive login request pdu
                    let! wnextLogiPDU =
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                    if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                        HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextLogiPDU.Opcode ) )
                        raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
 
                    let! requestTextKey, requestPDU =
                        this.ReceiveLoginRequest ( wnextLogiPDU :?> LoginRequestPDU )

                    // update request value
                    // marge target candidate and initiator request
                    let next_negoResult, next_negoStat1 = IscsiTextEncode.margeTextKeyValue Standpoint.Target requestTextKey negoValue negoStat

                    // Error Check
                    // AuthMethod, TargetName and InitiatorName must not be re-negotiated
                    if next_negoResult.AuthMethod <> ISV_Missing || next_negoResult.TargetName <> ISV_Missing || next_negoResult.InitiatorName <> ISV_Missing then
                        HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, "AuthMethod" ) )
                        raise <| SessionRecoveryException ( "Unknown negotiation error.", tsih_me.zero )

                    // decide transit flg
                    // ( If all of target value is sended and initiator says 'T', transit to next stage )
                    let targetTvalue =
                        IscsiTextEncode.CheckAllKeyStatus next_negoStat1 ( fun v -> v &&& NegoStatusValue.NSG_WaitSend <> NegoStatusValue.NSG_WaitSend )
                    if targetTvalue && requestPDU.T then

                        // If both Tbits are 1, this is the last PDU in the login sequence.
                        let lastPDU = {
                            PDU.CreateLoginResponsePDUfromLoginRequestPDU( requestPDU ) with
                                T = true;
                                NSG = requestPDU.NSG;
                                TSIH = newTSIH;
                        }
                        return LoopState.Terminate( struct( lastPDU, requestPDU.NSG ) )
                    else
                        let! next_negoStat2 = this.SendNegotiationResponse next_negoResult next_negoStat1 true ValueNone requestPDU
                        return LoopState.Continue( struct( next_negoResult, next_negoStat2 ) )
                }
        
            let! struct( secPhaseLastPDU, nsg ) =
                Functions.loopAsyncWithArgs waitTrance struct( TextKeyValues.defaultTextKeyValues, TextKeyValuesStatus.defaultTextKeyValuesStatus )

            let retCOParams = {
                coParam with
                    AuthMethod = negoAM_Result.AuthMethod.GetValue;
            }

            let retSWParams = {
                swParam with
                    InitiatorAlias =
                        match negoAM_Result.InitiatorAlias with
                        | Value( x ) -> x
                        | _ -> ""
            }

            // return last PDU of security negotiation, NSG value and negotiated values.
            return struct ( secPhaseLastPDU, nsg, retCOParams, retSWParams )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Negotiation for AuthMethod value.
    /// </summary>
    /// <param name="beforePDU">
    ///   Received first PDU of Login request sequence.
    /// </param>
    /// <param name="negoResult">
    ///   initial value of text key values.
    /// </param>
    /// <param name="negoStat">
    ///   initial status of text key values.
    /// </param>
    /// <param name="authMethod">
    ///   AuthMethod value that will be accepted by target.
    /// </param>
    /// <returns>
    ///   Negotiation result parameters.
    /// </returns>
    member private this.decideAuthMethodNegotiation
        ( beforePDU : LoginRequestPDU )
        ( negoResult : TextKeyValues )
        ( negoStat : TextKeyValuesStatus ) 
        ( authMethod : AuthMethodCandidateValue ) : Task< TextKeyValues > =

        task {
            // receive login request
            let! recvTextKey, recvPDU =
                this.ReceiveLoginRequest beforePDU

            // marge target candidate and initiator request
            let next_negoResult, next_negoStat1 =
                IscsiTextEncode.margeTextKeyValue Standpoint.Target recvTextKey negoResult negoStat

            match next_negoResult.AuthMethod with
            | ISV_Missing
            | ISV_NotUnderstood
            | ISV_Irrelevant
            | ISV_Reject ->
                // Consider protocol error.
                HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, "AuthMethod" ) )
                raise <| SessionRecoveryException ( "Unknown negotiation error.", tsih_me.zero )

            | Value( x ) ->
                // if negotiation result is empty, it is considered that target and initiator expectation is mismatch.
                if x.Length = 0 || x.[0] <> authMethod then
                    HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, "AuthMethod mismatch." ) )
                    let! _ =
                        PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                            PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                                Status = LoginResStatCd.AUTH_FAILURE;
                        } )
                    raise <| SessionRecoveryException ( "AuthMethod mismatch", tsih_me.zero )

            // send negotiation result to initiator
            let! next_negoStat2 = this.SendNegotiationResponse next_negoResult next_negoStat1 false ValueNone recvPDU

            // if negotiation insufficient, try to next one
            if not ( IscsiTextEncode.IsAllKeyNegotiated next_negoStat2 ) then
                // try next
                let! wnextLogiPDU =
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                    HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextLogiPDU.Opcode ) )
                    raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
                return! this.decideAuthMethodNegotiation
                            ( wnextLogiPDU :?> LoginRequestPDU )
                            next_negoResult
                            next_negoStat2
                            authMethod
            else
                // negotiation result
                return next_negoResult
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Receive login request sequence and recognize text ket-value. 
    /// </summary>
    /// <param name="beforePDU">
    ///   Received first PDU of Login request sequence.
    /// </param>
    /// <returns>
    ///   Touple of following values.
    ///   * Text key-value. recognized from received all of login request PDUs sequence.
    ///   * Login request PDU that last one of the sequence.
    /// </returns>
    member
        private _.ReceiveLoginRequest
            ( beforePDU : LoginRequestPDU ) :
            Task<struct ( TextKeyValues * LoginRequestPDU )> =

        task {
            // receive login request pdu sequence with c bit.
            let cbitLoop struct ( beforePDU : LoginRequestPDU, rv : List<LoginRequestPDU> ) :
                    Task<LoopState< struct( LoginRequestPDU * List<LoginRequestPDU> ), unit >> =
                task {
                    // send to empty login response PDU
                    let! _ =
                        PDU.SendPDU(
                            8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream,
                            PDU.CreateLoginResponsePDUfromLoginRequestPDU( beforePDU )
                        )
 
                    // receive next one
                    let! wnextLogiPDU =
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                    if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                        HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextLogiPDU.Opcode ) )
                        raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
                    let recvPDU = wnextLogiPDU :?> LoginRequestPDU
                    rv.Add recvPDU
                    if recvPDU.C then
                        return LoopState.Continue( struct( recvPDU, rv ) )
                    else
                        return LoopState.Terminate()
                }

            let rv = new List<LoginRequestPDU>()
            rv.Add beforePDU
            if beforePDU.C then
                do! Functions.loopAsyncWithArgs cbitLoop struct( beforePDU, rv )
            let pduList = [| for itr in rv -> itr |]

            let reqs_opt, pdu =
                (
                    pduList
                    |> Array.map ( fun x -> x.TextRequest )
                    |> IscsiTextEncode.RecognizeTextKeyData false,
                    Array.last pduList
                )

            if reqs_opt.IsNone then
                // format error
                let msg = "In iSCSI Login request PDU, Text request data is invalid."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // return value( text key-value and last pdu )
            return struct ( reqs_opt.Value, pdu )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Negotiation response to initiator.
    /// </summary>
    /// <param name="textValue">
    ///   Text key-value data that wanted to send to initiator in login response.
    /// </param>
    /// <param name="negoStatus">
    ///   Current negotiation status. This data used for selecting text key-value to send.
    /// </param>
    /// <param name="argT">
    ///   If target agree transition requested from initiator, this value is set to true.
    /// </param>
    /// <param name="tsih">
    ///   TSIH value that must be return to the initiator. If this value is ValueNone, TSIH value in the LoginRequestPDU is used.
    /// </param>
    /// <param name="recvPDU">
    ///   The login request PDU that cource of this login response PDU.
    /// </param>
    /// <returns>
    ///   Negotiation status of after sending login request PDU.
    /// </returns>
    member private this.SendNegotiationResponse
            ( textValue : TextKeyValues )
            ( negoStatus : TextKeyValuesStatus )
            ( argT : bool )
            ( tsih : TSIH_T voption )
            ( recvPDU : LoginRequestPDU ) :
                Task< TextKeyValuesStatus > =
        task {
            // Create sending text key bytes array
            let sendBytes = IscsiTextEncode.CreateTextKeyValueString textValue negoStatus

            do! this.SendNegotiationResponse_InBytes sendBytes argT tsih recvPDU

            // return negotiation status when target values sended.
            return IscsiTextEncode.ClearSendWaitStatus negoStatus
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Negotiation response bytes data to initiator.
    /// </summary>
    /// <param name="sendBytes">
    ///   Response data bytes.
    /// </param>
    /// <param name="argT">
    ///   If target agree transition requested from initiator, this value is set to true.
    /// </param>
    /// <param name="tsih">
    ///   TSIH value that must be return to the initiator. If this value is ValueNone, TSIH value in the LoginRequestPDU is used.
    /// </param>
    /// <param name="recvPDU">
    ///   The login request PDU that cource of this login response PDU.
    /// </param>
    member private _.SendNegotiationResponse_InBytes ( sendBytes : byte[] ) ( argT : bool ) ( tsih : TSIH_T voption ) ( recvPDU : LoginRequestPDU ) : Task<unit> =
        task {
            // Divite bytes array into 8192 bytes unit.
            let sendTextResponses =
                let v = Array.chunkBySize 8192 sendBytes
                if v.Length > 0 then v else [| Array.empty |]

            for i = 0 to sendTextResponses.Length - 1 do
            
                // Decide C bit value
                let cBitValue = ( i < sendTextResponses.Length - 1 )

                // Send Login Response PDU
                let! _ =
                    PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                            C = cBitValue;
                            T = if cBitValue then false else argT && recvPDU.T;
                            NSG = if ( not cBitValue ) && argT && recvPDU.T then recvPDU.NSG else LoginReqStateCd.SEQURITY; // If T is 0, NSG is reserved
                            TSIH = if tsih.IsNone then recvPDU.TSIH else tsih.Value
                            TextResponse = sendTextResponses.[i];
                    } )
        
                // if C bit equals 1, receive empty login request PDU
                if cBitValue then
                    let! wnextLogiPDU =
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                    if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                        HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextLogiPDU.Opcode ) )
                        raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
                    let emptyLoginRequestPDU = wnextLogiPDU :?> LoginRequestPDU
            
                    // Check received PDU
                    if emptyLoginRequestPDU.TextRequest.Length > 0 || emptyLoginRequestPDU.C then
                        // protocol error
                        let msg = "Response of Login response PDU with C bit set to 1 ( that is Login request PDU from Initiator ), TextRequest is not empty."
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Sequrity negotiation with CHAP
    ///   If failed to authntification, exception is thrown.
    /// </summary>
    /// <param name="targetConfig">
    ///   The configuration information of target  being logged in.
    /// </param>
    member private this.SequrityNegotiation_WithCHAP ( targetConfig : TargetGroupConf.T_Target ) : Task<unit> =
        task {
            // ramdom object
            let rnd1 = new Random()
            let cspRand = RandomNumberGenerator.Create()
            let challangeBuffer : byte[] = Array.zeroCreate 1024
            let chapParams =
                match targetConfig.Auth with
                | TargetGroupConf.T_Auth.U_CHAP( x ) -> x
                | _ ->
                    raise <| SessionRecoveryException ( "Unexpected error.", tsih_me.zero )

            // Working function of to send authentication failure status.
            let sendAuthFailureStatus ( pdu : LoginRequestPDU ) =
                PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                    PDU.CreateLoginResponsePDUfromLoginRequestPDU( pdu ) with
                        Status = LoginResStatCd.AUTH_FAILURE;
                } )
                |> Functions.TaskIgnore

            // Receive CHAP_A value
            let! wnextLogiPdu =
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
            if wnextLogiPdu.Opcode <> OpcodeCd.LOGIN_REQ then
                HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextLogiPdu.Opcode ) )
                raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
            let! step1_RecvVal, step1_RecvPDU = 
                this.ReceiveLoginRequest ( wnextLogiPdu :?> LoginRequestPDU )

            // Check proposed value
            match step1_RecvVal.CHAP_A with
            | ISV_Missing
            | ISV_NotUnderstood
            | ISV_Irrelevant
            | ISV_Reject ->
                let msg = "Protocol error. CHAP_A value is invalid."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step1_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )
            | Value( x ) ->
                if not ( Array.exists ( (=) 5us ) x ) then
                    let msg = "Proposed CHAP_A value is not supported."
                    HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                    do! sendAuthFailureStatus step1_RecvPDU
                    raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // decide send identifier value
            let sendIdentVal = uint16( rnd1.Next() % 0xFF )

            // Create challange value
            cspRand.GetBytes challangeBuffer
        
            // Send CHAP_A, CHAP_I, CHAP_C
            let! _ =
                PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                    PDU.CreateLoginResponsePDUfromLoginRequestPDU( step1_RecvPDU ) with
                        TextResponse =
                            IscsiTextEncode.CreateTextKeyValueString
                                {
                                    TextKeyValues.defaultTextKeyValues with
                                        CHAP_A = TextValueType.Value( [| 5us |] );
                                        CHAP_I = TextValueType.Value( sendIdentVal );
                                        CHAP_C = TextValueType.Value( challangeBuffer );
                                }
                                {
                                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                        NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                                }
                } )

            // Receive CHAP_N CHAP_R ( or CHAP_I CHAP_C ) values
            let! enextLogiPDU =
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
            if enextLogiPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue enextLogiPDU.Opcode ) )
                raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

            let! step2_RecvVal, step2_RecvPDU = 
                this.ReceiveLoginRequest ( enextLogiPDU :?> LoginRequestPDU )

            // Check CHAP_N CHAP_R values
            if step2_RecvVal.CHAP_N = ISV_Missing || step2_RecvVal.CHAP_N = ISV_NotUnderstood ||
                step2_RecvVal.CHAP_N = ISV_Irrelevant || step2_RecvVal.CHAP_N = ISV_Reject then
                let msg = "Protocol error. CHAP_N value is invalid."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            if step2_RecvVal.CHAP_R = ISV_Missing || step2_RecvVal.CHAP_R = ISV_NotUnderstood ||
                step2_RecvVal.CHAP_R = ISV_Irrelevant || step2_RecvVal.CHAP_R = ISV_Reject then
                let msg = "Protocol error. CHAP_R value is invalid."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Check user name
            if String.Equals( step2_RecvVal.CHAP_N.GetValue, chapParams.InitiatorAuth.UserName, StringComparison.Ordinal ) |> not then
                let msg = "Invalid user name or password."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )
        
            // Calculate expected response value
            let expectedHashValue =
                ( MD5.Create() ).ComputeHash
                    [|
                        yield byte sendIdentVal;
                        yield! Encoding.UTF8.GetBytes chapParams.InitiatorAuth.Password
                        yield! challangeBuffer
                    |]
        
            // Check response value
            if expectedHashValue <> step2_RecvVal.CHAP_R.GetValue then
                let msg = "Invalid user name or password."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Check CHAP_I CHAP_C value
            if step2_RecvVal.CHAP_I =  ISV_NotUnderstood || step2_RecvVal.CHAP_I =  ISV_Irrelevant || step2_RecvVal.CHAP_I =  ISV_Reject then
                let msg = "Protocol error. CHAP_I value is invalid."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ))
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            if step2_RecvVal.CHAP_C =  ISV_NotUnderstood || step2_RecvVal.CHAP_C =  ISV_Irrelevant || step2_RecvVal.CHAP_C =  ISV_Reject then
                let msg = "Protocol error. CHAP_C value is invalid."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )
        
            // CHAP_I and CHAP_C must be both omitted or both specified.
            if step2_RecvVal.CHAP_I = ISV_Missing && step2_RecvVal.CHAP_C <> ISV_Missing ||
                step2_RecvVal.CHAP_I <> ISV_Missing && step2_RecvVal.CHAP_C = ISV_Missing then
                let msg = "Protocol error. CHAP_I or CHAP_C value is invalid."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // If target authentication is nesessary but initiator does not request, or
            // target authentication is omitted but initiator requested to do,
            // it's considered authentication failure.
            if step2_RecvVal.CHAP_I = ISV_Missing && chapParams.TargetAuth.UserName.Length > 0 ||
                step2_RecvVal.CHAP_I <> ISV_Missing && chapParams.TargetAuth.UserName.Length = 0 then
                let msg = "Invalid user name or password."
                HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                do! sendAuthFailureStatus step2_RecvPDU
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // If target authentication is omitted and initiator does not request,
            // it's considered the authentication is succeed.
            if step2_RecvVal.CHAP_I = ISV_Missing && chapParams.TargetAuth.UserName.Length = 0 then
                HLogger.Trace( LogID.I_AUTHENTICATION_SUCCEED, fun g -> g.Gen1( m_ObjID, targetConfig.TargetName ) )
                // send empty login response PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream,
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( step2_RecvPDU )
                    )
                    |> Functions.TaskIgnore
            else
                // Calculate CHAP response value
                let responseHashValue =
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte step2_RecvVal.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes chapParams.TargetAuth.Password;
                            yield! step2_RecvVal.CHAP_C.GetValue;
                        |]

                // Send CHAP_N and CHAP_R 
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                        PDU.CreateLoginResponsePDUfromLoginRequestPDU( step2_RecvPDU ) with
                            TextResponse =
                                IscsiTextEncode.CreateTextKeyValueString
                                    {
                                        TextKeyValues.defaultTextKeyValues with
                                            CHAP_N = TextValueType.Value( chapParams.TargetAuth.UserName );
                                            CHAP_R = TextValueType.Value( responseHashValue );
                                    }
                                    {
                                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                            NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                            NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                    }
                        } )
                    |> Functions.TaskIgnore
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Parform operational negotiation stage.
    /// </summary>
    /// <param name="isLeadingCon">
    ///   Logged in connection is leading connection or not.
    ///   If this connection is leading connection, a session is created newly,
    ///   and existing old session is dropped nessesary.
    /// </param>
    /// <param name="isRebuildCon">
    ///   Existing connection is droped or not.
    /// </param>
    /// <param name="targetConfig">
    ///   The configuration information of target  being logged in.
    /// </param>
    /// <param name="firstPDU">
    ///   The first PDU of this negotiation stage.
    ///   (In this case, C bit is true, and must receive following PDU until C bit set to false)
    /// </param>
    /// <param name="coParam">
    ///   Initial values of connection only parameters. 
    /// </param>
    /// <param name="swParam">
    ///   Initial values of session wide parameters. 
    /// </param>
    /// <param name="isAuthentified">
    ///   If sequrity negotiation is performed, this value is set to true.
    ///   If target node needs to authentification and sequrity negotiation is ommited,
    ///   this connection must be dropped.
    /// </param>
    /// <param name="isDiscoverySession">
    ///   If this operational negotiation is performed for discovery session,
    ///   this value must be true, otherwise false.
    /// </param>
    /// <param name="newTSIH">
    ///   The TSIH value to be sent in the last PDU of the login sequence.
    /// </param>
    /// <returns>
    ///   Touple of following values.
    ///   * First one PDU of next negotiation stage.
    ///   * Negotiated connection only parameters.
    ///   * Negotiated session wide parameters.
    /// </returns>
    member private this.OperationalNegotiation
        ( isLeadingCon : bool )
        ( isRebuildCon : bool )
        ( targetConfig : TargetGroupConf.T_Target )
        ( firstPDU : LoginRequestPDU )
        ( coParam : IscsiNegoParamCO )
        ( swParam : IscsiNegoParamSW ) 
        ( isAuthentified : bool )
        ( newTSIH : TSIH_T ) : 
        Task< struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW ) > =

        task {
            // If selected target needs authentication but it did not performe sequrity negotiation,
            // it must reject login and drop connection.
            match targetConfig.Auth with
            | TargetGroupConf.T_Auth.U_None( _ ) ->
                ()
            | _ ->
                if not isAuthentified then
                    let msg = "Authentication required."
                    HLogger.Trace( LogID.E_AUTHENTICATION_FAILURE, fun g -> g.Gen1( m_ObjID, msg ) )
                    let! _ =
                        PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_ObjID, m_NetStream, {
                            PDU.CreateLoginResponsePDUfromLoginRequestPDU( firstPDU ) with
                                Status = LoginResStatCd.AUTH_FAILURE;
                        } )
                    raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Create initial negotiation values
            let firstNegoValue =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( coParam.HeaderDigest );
                        DataDigest = TextValueType.Value( coParam.DataDigest );
                        MaxConnections =
                            if isLeadingCon then
                                TextValueType.Value( swParam.MaxConnections )
                            else
                                TextValueType.ISV_Missing;
                        TargetAlias = TextValueType.Value( targetConfig.TargetAlias );
                        InitiatorAlias = TextValueType.Value( swParam.InitiatorAlias );
                        TargetPortalGroupTag =
                            if not isAuthentified then 
                                TextValueType.Value( tpgt_me.toPrim swParam.TargetPortalGroupTag )
                            else
                                TextValueType.ISV_Missing;
                        InitialR2T =
                            if isLeadingCon then 
                                TextValueType.Value( swParam.InitialR2T )
                            else
                                TextValueType.ISV_Missing;
                        ImmediateData = 
                            if isLeadingCon then
                                TextValueType.Value( swParam.ImmediateData )
                            else
                                TextValueType.ISV_Missing;
                        MaxRecvDataSegmentLength_I = TextValueType.Value( coParam.MaxRecvDataSegmentLength_I );
                        MaxRecvDataSegmentLength_T = TextValueType.Value( coParam.MaxRecvDataSegmentLength_T );
                        MaxBurstLength =
                            if isLeadingCon then
                                TextValueType.Value( swParam.MaxBurstLength )
                            else
                                TextValueType.ISV_Missing;
                        FirstBurstLength =
                            if isLeadingCon then
                                TextValueType.Value( swParam.FirstBurstLength )
                            else
                                TextValueType.ISV_Missing;
                        DefaultTime2Wait =
                            if isLeadingCon then
                                TextValueType.Value( swParam.DefaultTime2Wait )
                            else
                                TextValueType.ISV_Missing;
                        DefaultTime2Retain =
                            if isLeadingCon then
                                TextValueType.Value( swParam.DefaultTime2Retain )
                            else
                                TextValueType.ISV_Missing;
                        MaxOutstandingR2T =
                            if isLeadingCon then
                                TextValueType.Value( swParam.MaxOutstandingR2T )
                            else
                                TextValueType.ISV_Missing;
                        DataPDUInOrder =
                            if isLeadingCon then
                                TextValueType.Value( swParam.DataPDUInOrder )
                            else
                                TextValueType.ISV_Missing;
                        DataSequenceInOrder =
                            if isLeadingCon then 
                                TextValueType.Value( swParam.DataSequenceInOrder )
                            else
                                TextValueType.ISV_Missing;
                        ErrorRecoveryLevel =
                            if isLeadingCon then
                                TextValueType.Value( swParam.ErrorRecoveryLevel )
                            else
                                TextValueType.ISV_Missing;
                }


            let firstNegoStat =
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                        NegoStat_DataDigest = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxConnections =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend;
                        NegoStat_InitiatorAlias = NegoStatusValue.NSV_Negotiated;
                        NegoStat_TargetPortalGroupTag =
                            if not isAuthentified then
                                NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_InitialR2T =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_ImmediateData =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitReceive;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxBurstLength =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_FirstBurstLength =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_DefaultTime2Wait =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_DefaultTime2Retain =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_MaxOutstandingR2T =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_DataPDUInOrder =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_DataSequenceInOrder =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                        NegoStat_ErrorRecoveryLevel =
                            if isLeadingCon then
                                NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend
                            else
                                NegoStatusValue.NSV_Negotiated;
                }

            // Perform operation negotiation
            let negoloop struct( negoValue, negoStat, beforePDU ) : 
                    Task< LoopState<
                        struct( TextKeyValues * TextKeyValuesStatus * LoginRequestPDU ),
                        struct( LoginResponsePDU * TextKeyValues * TextKeyValuesStatus )
                    > > =
                task {
                    // Receive sequence of login request PDUs
                    let! textKey, ( recvPDU : LoginRequestPDU ) =
                        this.ReceiveLoginRequest beforePDU

                    // if keys not allowed in operation negotiation stage is used, drop this connection
                    if textKey.AuthMethod <> TextValueType.ISV_Missing ||
                        textKey.CHAP_A <> TextValueType.ISV_Missing ||
                        textKey.CHAP_I <> TextValueType.ISV_Missing ||
                        textKey.CHAP_C <> TextValueType.ISV_Missing ||
                        textKey.CHAP_N <> TextValueType.ISV_Missing ||
                        textKey.CHAP_R <> TextValueType.ISV_Missing ||
                        textKey.SendTargets <> TextValueType.ISV_Missing ||
                        textKey.TargetAddress <> TextValueType.ISV_Missing then
                        let msg = "Invalid text key was received."
                        HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // Check reject value
                    if not isLeadingCon then
                        // if use existing session, LO parameters must not be handled.
                        if textKey.MaxConnections <> TextValueType.ISV_Missing ||
                            textKey.InitialR2T <> TextValueType.ISV_Missing ||
                            textKey.ImmediateData <> TextValueType.ISV_Missing ||
                            textKey.MaxBurstLength <> TextValueType.ISV_Missing ||
                            textKey.FirstBurstLength <> TextValueType.ISV_Missing ||
                            textKey.DefaultTime2Wait <> TextValueType.ISV_Missing ||
                            textKey.DefaultTime2Retain <> TextValueType.ISV_Missing ||
                            textKey.MaxOutstandingR2T <> TextValueType.ISV_Missing ||
                            textKey.DataPDUInOrder <> TextValueType.ISV_Missing ||
                            textKey.DataSequenceInOrder <> TextValueType.ISV_Missing ||
                            textKey.ErrorRecoveryLevel <> TextValueType.ISV_Missing then
                            let msg = "Invalid text key was received."
                            HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // If sequrity negotiation is performed, some text keys are not allowed to use in operational stage.
                    if isAuthentified then
                        if textKey.SessionType <> TextValueType.ISV_Missing ||
                            textKey.InitiatorName <> TextValueType.ISV_Missing ||
                            textKey.TargetName <> TextValueType.ISV_Missing ||
                            textKey.TargetPortalGroupTag <> TextValueType.ISV_Missing then
                            let msg = "Invalid text key was received."
                            HLogger.Trace( LogID.E_UNKNOWN_NEGOTIATION_ERROR, fun g -> g.Gen1( m_ObjID, msg ) )
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // marge parameters       
                    let next_negoValue, next_negoStat =
                        IscsiTextEncode.margeTextKeyValue Standpoint.Target textKey negoValue negoStat 
            
                    // decide transit flg
                    // ( If all of target value is sended and initiator says 'T', transit to next stage )
                    let targetTvalue =
                        IscsiTextEncode.CheckAllKeyStatus next_negoStat ( fun v -> v &&& NegoStatusValue.NSG_WaitSend <> NegoStatusValue.NSG_WaitSend )
                    if targetTvalue && recvPDU.T then

                        // If both Tbits are 1, this is the last PDU in the login sequence.
                        let lastPDU = {
                            PDU.CreateLoginResponsePDUfromLoginRequestPDU( recvPDU ) with
                                T = true;
                                NSG = recvPDU.NSG;
                                TSIH = newTSIH;
                        }
                        return LoopState.Terminate( struct( lastPDU, next_negoValue, next_negoStat ) )

                    else
                        // The login phase is still ongoing
                        let! next2_negoStat = this.SendNegotiationResponse next_negoValue next_negoStat targetTvalue ValueNone recvPDU

                        // Receive next PDU
                        let! wnextLogiPDU =
                            PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, m_NetStream, Standpoint.Target )
                        if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_REQ then
                            HLogger.Trace( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC, fun g -> g.Gen1( m_ObjID, Constants.getOpcodeNameFromValue wnextLogiPDU.Opcode ) )
                            raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

                        return LoopState.Continue( struct( next_negoValue, next2_negoStat, ( wnextLogiPDU :?> LoginRequestPDU ) ) )
                }

            let! loginPhaseLastPDU, negoResultValue, negoResultStat =
                Functions.loopAsyncWithArgs negoloop struct( firstNegoValue, firstNegoStat, firstPDU )

            // Create result parameter
            let resultCoParam =
                {
                    coParam with
                        HeaderDigest = 
                            if negoResultStat.NegoStat_HeaderDigest = NegoStatusValue.NSV_Negotiated &&
                                negoResultValue.HeaderDigest.HasValue then
                                [| negoResultValue.HeaderDigest.GetValue.[0] |]
                            else
                                [| DigestType.DST_None |];
                        DataDigest = 
                            if negoResultStat.NegoStat_DataDigest = NegoStatusValue.NSV_Negotiated &&
                                negoResultValue.DataDigest.HasValue then
                                [| negoResultValue.DataDigest.GetValue.[0] |]
                            else
                                [| DigestType.DST_None |];
                        MaxRecvDataSegmentLength_T =
                            if negoResultValue.MaxRecvDataSegmentLength_T.HasValue then
                                negoResultValue.MaxRecvDataSegmentLength_T.GetValue
                            else
                                8192u;
                        MaxRecvDataSegmentLength_I = 
                            if negoResultStat.NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated &&
                                negoResultValue.MaxRecvDataSegmentLength_I.HasValue then
                                negoResultValue.MaxRecvDataSegmentLength_I.GetValue
                            else
                                8192u;
                }
            let resultSwParam =
                {
                    swParam with
                        MaxConnections =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_MaxConnections = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.MaxConnections.HasValue then
                                    negoResultValue.MaxConnections.GetValue
                                else
                                    1us
                            else
                                swParam.MaxConnections;
                        InitiatorAlias =
                            if negoResultStat.NegoStat_InitiatorAlias = NegoStatusValue.NSV_Negotiated &&
                                negoResultValue.InitiatorAlias.HasValue then
                                negoResultValue.InitiatorAlias.GetValue
                            else
                                "";
                        InitialR2T =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_InitialR2T = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.InitialR2T.HasValue then
                                    negoResultValue.InitialR2T.GetValue
                                else
                                    true
                            else
                                swParam.InitialR2T;
                        ImmediateData =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_ImmediateData = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.ImmediateData.HasValue then
                                    negoResultValue.ImmediateData.GetValue
                                else
                                    true
                            else
                                swParam.ImmediateData;
                        MaxBurstLength =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_MaxBurstLength = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.MaxBurstLength.HasValue then
                                    negoResultValue.MaxBurstLength.GetValue
                                else
                                    262144u
                            else
                                swParam.MaxBurstLength;
                        FirstBurstLength =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_FirstBurstLength = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.FirstBurstLength.HasValue then
                                    negoResultValue.FirstBurstLength.GetValue
                                else
                                    65536u
                            else
                                swParam.FirstBurstLength;
                        DefaultTime2Wait =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_DefaultTime2Wait = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.DefaultTime2Wait.HasValue then
                                    negoResultValue.DefaultTime2Wait.GetValue
                                else
                                    2us
                            else
                                swParam.DefaultTime2Wait;
                        DefaultTime2Retain =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_DefaultTime2Retain = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.DefaultTime2Retain.HasValue then
                                    negoResultValue.DefaultTime2Retain.GetValue
                                else
                                    20us
                            else
                                swParam.DefaultTime2Retain;
                        MaxOutstandingR2T =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_MaxOutstandingR2T = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.MaxOutstandingR2T.HasValue then
                                    negoResultValue.MaxOutstandingR2T.GetValue
                                else
                                    1us
                            else
                                swParam.MaxOutstandingR2T;
                        DataPDUInOrder =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_DataPDUInOrder = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.DataPDUInOrder.HasValue then
                                    negoResultValue.DataPDUInOrder.GetValue
                                else
                                    true
                            else
                                swParam.DataPDUInOrder;
                        DataSequenceInOrder =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_DataSequenceInOrder = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.DataSequenceInOrder.HasValue then
                                    negoResultValue.DataSequenceInOrder.GetValue
                                else
                                    true
                            else
                                swParam.DataSequenceInOrder;
                        ErrorRecoveryLevel =
                            if isLeadingCon then
                                if negoResultStat.NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSV_Negotiated &&
                                    negoResultValue.ErrorRecoveryLevel.HasValue then
                                    negoResultValue.ErrorRecoveryLevel.GetValue
                                else
                                    0uy
                            else
                                swParam.ErrorRecoveryLevel;
                }

            return struct ( loginPhaseLastPDU, resultCoParam, resultSwParam )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Request to create or add new connection to StatusMaster.
    /// </summary>
    /// <param name="isLeadingCon">
    ///   Logged in connection is leading connection or not.
    ///   If this connection is leading connection, a session is created newly,
    ///   and existing old session is dropped nessesary.
    /// </param>
    /// <param name="isConnRebuild">
    ///   Existing connection is droped or not.
    /// </param>
    /// <param name="dropSessionTSIH">
    ///   TSIH of the session to be dropped.
    ///   This value is ignored if isLeadingCon is false.
    /// </param>
    /// <param name="newTSIH">
    ///   The TSIH value to be set for a newly established session.
    ///   If isLeadingCon is true, newTSIH must contain a valid value.
    ///   Otherwise, this value must be ignored.
    /// </param>
    /// <param name="i_tNexusIdent">
    ///   I_T Next identifier of session that is newly created, rebuild, or add connection.
    /// </param>
    /// <param name="recvCID">
    ///   Received connection ID. if isConnRebuild is true, drop the connection
    ///   represented by this CID. ( connection using same CID must be exist ).
    ///   If isConnRebuild is false, new connection is use this CID.
    ///   ( connection using same CID must not be exist ).
    /// </param>
    /// <param name="newCmdSN">
    ///   Initial CmdSN value of newly created session. If isLeadingCon is false, this value is ignored.
    /// </param>
    /// <param name="firstExpStatSN">
    ///   ExpStatSN value of the first login request PDU.
    ///   This value is used to only connection recovery.
    /// </param>
    /// <param name="nextFirstPDU">
    ///   Received first PDU of full feature phase.
    /// </param>
    /// <param name="iSCSIParamsCO">
    ///   Negiciated iSCSI connection only parameters.
    /// </param>
    /// <param name="iSCSIParamsSW">
    ///   Negiciated iSCSI session wide parameters.
    /// </param>
    member private _.CreateOrAddNewConnection
        ( isLeadingCon : bool )
        ( isConnRebuild : bool )
        ( dropSessionTSIH : TSIH_T )
        ( newTSIH : TSIH_T )
        ( i_tNexusIdent : ITNexus )
        ( recvCID : CID_T )
        ( newCmdSN : CMDSN_T )
        ( firstExpStatSN : STATSN_T )
        ( iSCSIParamsCO : IscsiNegoParamCO )
        ( iSCSIParamsSW : IscsiNegoParamSW ) : unit =

        // If isLeadingCon is true, isConnRebuild must not be true
        assert( not ( isLeadingCon && isConnRebuild ) )

        let loginfo = struct ( m_ObjID, ValueSome( recvCID ), ValueNone, ValueSome( newTSIH ), ValueNone, ValueNone )

        // If termination request is received, give up to add connection.
        if m_Killer.IsNoticed then
            let msg = "Termination requested."
            HLogger.Trace( LogID.E_FAILED_ADD_CONNECTION, fun g -> g.Gen2( loginfo, i_tNexusIdent.I_TNexusStr, msg ) )
            raise <| SessionRecoveryException ( msg, tsih_me.zero )

        let newSession_opt =
            if isLeadingCon then
                if dropSessionTSIH <> tsih_me.zero then
                    // drop existing session
                    match m_StatusMaster.GetSession dropSessionTSIH with
                    | ValueSome( x ) -> x.DestroySession()
                    | _ -> ()

                // Create new session.
                m_StatusMaster.CreateNewSession i_tNexusIdent newTSIH iSCSIParamsSW newCmdSN
            else
                // add new or rebuild connection to existing session.
                m_StatusMaster.GetSession newTSIH
        
        if newSession_opt.IsNone then
            HLogger.Trace( LogID.E_MISSING_SESSION, fun g -> g.Gen0 loginfo )
            raise <| SessionRecoveryException ( "Missing session. Maybe it occurred consistency error.", tsih_me.zero )

        let newSession = newSession_opt.Value

        // Create new Connection object
        
        if isLeadingCon || ( not isConnRebuild ) then
            // Check the initiator name is same to session object holdes one.
            if not ( newSession.SessionParameter.InitiatorName = iSCSIParamsSW.InitiatorName ) then
                let msg = sprintf "Consistency error. A different initiator attempted to log in to an existing session. New initiator name=%s, Session's initiator name=%s."
                            iSCSIParamsSW.InitiatorName
                            ( newSession.SessionParameter.InitiatorName )
                HLogger.Trace( LogID.E_FAILED_ADD_CONNECTION, fun g -> g.Gen2( loginfo, i_tNexusIdent.I_TNexusStr, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // Add to new connection to session component
            if not ( newSession.AddNewConnection m_NetStream m_ConnectedTime recvCID m_NetPortIdx m_TargetPortalGroupTag iSCSIParamsCO ) then
                let msg = "Consistency error. May be specified connection is already exist."
                HLogger.Trace( LogID.E_FAILED_ADD_CONNECTION, fun g -> g.Gen2( loginfo, i_tNexusIdent.I_TNexusStr, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )
        else
            // drop existing connection and create new connection
            if not ( newSession.ReinstateConnection m_NetStream m_ConnectedTime recvCID m_NetPortIdx m_TargetPortalGroupTag iSCSIParamsCO ) then
                let msg = "Consistency error. May be specified connection is not exist."
                HLogger.Trace( LogID.E_FAILED_REBUILD_CONNECTION, fun g -> g.Gen2( loginfo, i_tNexusIdent.I_TNexusStr, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.zero )
        ()
