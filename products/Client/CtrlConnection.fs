//=============================================================================
// Haruka Software Storage.
// CtrlConnection.fs : It hols reader / receiver objects to controller.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Net.Sockets
open System.Threading.Tasks
open System.Xml
open System.Xml.Schema

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type declaration

/// Exception that is raised if unexpected error was occurred sending request.
type RequestError( m_Message : string ) =
    inherit Exception( m_Message )

//=============================================================================
// Class implementation

/// <summary>
///  Definition of CtrlConnection class.
/// </summary>
/// <param name="m_MessageTable">
///  Message resource reader.
/// </param>
/// <param name="m_Stream">
///  Network stream.
/// </param>
/// <param name="m_SessionID">
///  Negosiated session ID.
/// </param>
type CtrlConnection(
        m_MessageTable : StringTable,
        m_Stream : Stream,
        m_SessionID : CtrlSessionID
    ) =

    interface IDisposable with

        /// Dispose method
        override _.Dispose() : unit =
            m_Stream.Dispose()


    static member private AggregateXmlError ( f : unit -> 'T ) : 'T =
        try
            f()
        with
        | :? XmlException
        | :? XmlSchemaValidationException 
        | :? FormatException 
        | :? ConfRWException as x ->
            raise <| RequestError( x.Message )

    /// Dispose method
    member this.Dispose() : unit =
        ( this :> IDisposable ).Dispose()
    
    /// <summary>
    ///  Connect to controller and returns instance of CtrlConnection class.
    /// </summary>
    /// <param name="messageTable">
    ///  Message resource reader.
    /// </param>
    /// <param name="h">
    ///  Server host name.
    /// </param>
    /// <param name="p">
    ///  TCP port number.
    /// </param>
    /// <param name="f">
    ///  Connet forcely or not.
    /// </param>
    /// <returns>
    ///  CtrlConnection instance.
    /// </returns>
    static member Connect ( messageTable : StringTable ) ( h : string ) ( p : int ) ( f : bool )  : Task<CtrlConnection> =
        task {
            let con = new TcpClient()
            do! con.ConnectAsync( h, p )
            let c1 = con.GetStream()
            try
                return! CtrlConnection.ConnectWithStream messageTable c1 f
            with
            | _ as x ->
                try
                    con.Client.Disconnect false
                    con.Client.Close()
                    con.Dispose()
                with
                | _ -> ()
                raise x
                return new CtrlConnection( messageTable, c1, CtrlSessionID() )
        }

    /// <summary>
    ///  Login to controller and create instance of CtrlConnection class.
    /// </summary>
    /// <param name="messageTable">
    ///  Message resource reader.
    /// </param>
    /// <param name="c1">
    ///  Stream instance that was connected to controller.
    /// </param>
    /// <param name="f">
    ///  Connet forcely or not.
    /// </param>
    /// <returns>
    ///  CtrlConnection instance.
    /// </returns>
    static member private ConnectWithStream ( messageTable : StringTable ) ( c1 : Stream ) ( f : bool )  : Task<CtrlConnection> =
        task {
            // send first login request
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Login( f )
                }
            do! Functions.FramingSender c1 reqStr

            // receive first login response
            let! resStr = Functions.FramingReceiver c1
            let res = 
                fun () -> HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr
                |> CtrlConnection.AggregateXmlError

            let wSessID =
                match res.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_LoginResult( x ) ->
                    if x.Result then
                        x.SessionID
                    else
                        raise <| RequestError( messageTable.GetMessage( "ERRMSG_FAILED_CONNECT" ) )
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( messageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "Login" ) )

            return new CtrlConnection( messageTable, c1, wSessID )
        }

    /// <summary>
    ///  Send request to controller and receive response message
    /// </summary>
    /// <param name="s">
    ///  Request message.
    /// </param>
    /// <returns>
    ///  received response message.
    /// </returns>
    member private _.SendRequest ( s : HarukaCtrlerCtrlReq.T_Request ) : Task< HarukaCtrlerCtrlRes.T_Response > =
        task {
            let reqStr =
                fun () -> HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = s
                }
                |> CtrlConnection.AggregateXmlError
            do! Functions.FramingSender m_Stream reqStr
            let! resStr = Functions.FramingReceiver m_Stream
            let res =
                fun () -> HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr
                |> CtrlConnection.AggregateXmlError
            return res.Response
        }

    /// <summary>
    ///  Send request to specified target device, and receive response message.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID to which the request is sent.
    /// </param>
    /// <param name="reqmsg">
    ///  Request message.
    /// </param>
    /// <returns>
    ///  Response message.
    /// </returns>
    member private this.SendTargetDeviceRequest ( tdid : TDID_T ) ( reqmsg : TargetDeviceCtrlReq.T_Request ) : Task< TargetDeviceCtrlRes.T_Response > =
        let req =
            fun () -> HarukaCtrlerCtrlReq.U_TargetDeviceCtrlRequest({
                    SessionID = m_SessionID;
                    TargetDeviceID = tdid;
                    Request =
                        TargetDeviceCtrlReq.ReaderWriter.ToString {
                            Request = reqmsg;
                        }
                })
            |> CtrlConnection.AggregateXmlError
        task {
            // Recognize controller response
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_TargetDeviceCtrlResponse( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    if x.TargetDeviceID <> tdid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "SendTargetDeviceRequest" ) )
                    x.Response
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "SendTargetDeviceRequest" ) )

            // Recognize target device response
            let tdCtrlResult =
                fun () -> TargetDeviceCtrlRes.ReaderWriter.LoadString r2
                |> CtrlConnection.AggregateXmlError
            return tdCtrlResult.Response
        }

    member private this.SendMediaControlRequest ( tdid : TDID_T ) ( lun : LUN_T ) ( mediaid : MEDIAIDX_T ) ( reqmsg : MediaCtrlReq.T_Request ) : Task< MediaCtrlRes.T_Response > =
        task {
            let mediaReqStr = 
                MediaCtrlReq.ReaderWriter.ToString {
                    Request = reqmsg;
                }
            let! mstat =
                TargetDeviceCtrlReq.U_MediaControlRequest({
                    LUN = lun;
                    ID = mediaid;
                    Request = mediaReqStr;
                })
                |> this.SendTargetDeviceRequest tdid
            return
                match mstat with
                | TargetDeviceCtrlRes.U_MediaControlResponse( x ) ->
                    if x.LUN <> lun || x.ID <> mediaid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "SendMediaControlRequest" ) )
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )

                    let mediaResData = 
                        fun () -> MediaCtrlRes.ReaderWriter.LoadString x.Response
                        |> CtrlConnection.AggregateXmlError
                    mediaResData.Response

                | TargetDeviceCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "SendMediaControlRequest" ) )
        }



    /// Get SessionID
    member _.SessionID = m_SessionID

    /// <summary>
    ///  Send Logout request to the controller.
    /// </summary>
    abstract Logout : unit -> Task
    default this.Logout () =
        let req = HarukaCtrlerCtrlReq.U_Logout( m_SessionID )
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_LogoutResult( x ) ->
                if not x.Result then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_FAILED_LOGOUT" ) )
                if x.SessionID <> m_SessionID then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "Logout" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "Logout" ) )
        }

    /// <summary>
    ///  Send NoOperation request to the controller.
    /// </summary>
    abstract NoOperation : unit -> Task
    default this.NoOperation () =
        let req = HarukaCtrlerCtrlReq.U_NoOperation( m_SessionID )
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_NoOperationResult( x ) ->
                if not x.Result then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_FAILED_NO_OPERATION" ) )
                if x.SessionID <> m_SessionID then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "NoOperation" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "NoOperation" ) )
        }

    /// <summary>
    ///  Send GetControllerConfig request to the controller.
    /// </summary>
    /// <returns>
    ///  Controller configuration data.
    /// </returns>
    abstract GetControllerConfig : unit -> Task< HarukaCtrlConf.T_HarukaCtrl >
    default this.GetControllerConfig () =
        let req = HarukaCtrlerCtrlReq.U_GetControllerConfig( m_SessionID )
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_ControllerConfig( x ) ->
                    if x.Config.Length = 0 then
                        raise <| RequestError( x.ErrorMessage )
                    fun () -> HarukaCtrlConf.ReaderWriter.LoadString x.Config
                    |> CtrlConnection.AggregateXmlError
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetControllerConfig" ) )
            return r2
        }

    /// <summary>
    ///  Send SetControllerConfig request to the controller.
    /// </summary>
    /// <param name="conf">
    ///  Controller configuration data that should be send to the controller.
    /// </param>
    abstract SetControllerConfig : conf:HarukaCtrlConf.T_HarukaCtrl -> Task
    default this.SetControllerConfig conf =
        let req = HarukaCtrlerCtrlReq.U_SetControllerConfig({
            SessionID = m_SessionID;
            Config =
                fun () -> HarukaCtrlConf.ReaderWriter.ToString conf
                |> CtrlConnection.AggregateXmlError;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_SetControllerConfigResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "SetControllerConfig" ) )
        }

    /// <summary>
    ///  Send Logout request to the controller.
    /// </summary>
    /// <returns>
    ///  Controller configuration data.
    /// </returns>
    abstract GetTargetDeviceDir : unit -> Task<TDID_T list>
    default this.GetTargetDeviceDir () =
        let req = HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( m_SessionID )
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_TargetDeviceDirs( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    x.TargetDeviceID
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetDeviceDir" ) )
            return r2
        }

    /// <summary>
    ///  Send CreateTargetDeviceDir request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that should be created.
    /// </param>
    abstract CreateTargetDeviceDir : tdid:TDID_T -> Task
    default this.CreateTargetDeviceDir tdid =
        let req = HarukaCtrlerCtrlReq.U_CreateTargetDeviceDir({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_CreateTargetDeviceDirResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateTargetDeviceDir" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateTargetDeviceDir" ) )
        }

    /// <summary>
    ///  Send DeleteTargetDeviceDir request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that should be created.
    /// </param>
    abstract DeleteTargetDeviceDir : tdid:TDID_T -> Task
    default this.DeleteTargetDeviceDir tdid =
        let req = HarukaCtrlerCtrlReq.U_DeleteTargetDeviceDir({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_DeleteTargetDeviceDirResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DeleteTargetDeviceDir" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DeleteTargetDeviceDir" ) )
        }

    /// <summary>
    ///  Send GetTargetDeviceConfig request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that should be get configuration data.
    /// </param>
    /// <returns>
    ///  Target device configuration data.
    /// </returns>
    abstract GetTargetDeviceConfig : tdid:TDID_T -> Task< TargetDeviceConf.T_TargetDevice >
    default this.GetTargetDeviceConfig tdid =
        let req = HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_TargetDeviceConfig( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    if x.TargetDeviceID <> tdid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetDeviceConfig" ) )
                    fun () -> TargetDeviceConf.ReaderWriter.LoadString x.Config
                    |> CtrlConnection.AggregateXmlError
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetDeviceConfig" ) )
            return r2
        }

    /// <summary>
    ///  Send CreateTargetDeviceConfig request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that should be created.
    /// </param>
    /// <param name="config">
    ///  Target device configuration data that should be sent to the controller.
    /// </param>
    abstract CreateTargetDeviceConfig : tdid:TDID_T -> config:TargetDeviceConf.T_TargetDevice -> Task
    default this.CreateTargetDeviceConfig tdid config =
        let confStr =
            fun () -> TargetDeviceConf.ReaderWriter.ToString config
            |> CtrlConnection.AggregateXmlError
        let req = HarukaCtrlerCtrlReq.U_CreateTargetDeviceConfig({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
            Config = confStr;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_CreateTargetDeviceConfigResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateTargetDeviceConfig" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateTargetDeviceConfig" ) )
        }

    /// <summary>
    ///  Send GetTargetGroupID request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that should be get target group IDs.
    /// </param>
    /// <returns>
    ///  List of target group IDs that belongings to specified target device.
    /// </returns>
    abstract GetTargetGroupID : tdid:TDID_T -> Task<TGID_T list>
    default this.GetTargetGroupID tdid =
        let req = HarukaCtrlerCtrlReq.U_GetTargetGroupID({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_TargetGroupID( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    if x.TargetDeviceID <> tdid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetGroupID" ) )
                    x.TargetGroupID
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetGroupID" ) )
            return r2
        }

    /// <summary>
    ///  Send GetTargetGroupConfig request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which target group should be got configuration data is belonging to.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID should be get configuration data.
    /// </param>
    /// <returns>
    ///  Target group configuration data.
    /// </returns>
    abstract GetTargetGroupConfig : tdid:TDID_T -> tgid:TGID_T -> Task<TargetGroupConf.T_TargetGroup>
    default this.GetTargetGroupConfig tdid tgid =
        let req = HarukaCtrlerCtrlReq.U_GetTargetGroupConfig({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
            TargetGroupID = tgid;
        })
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_TargetGroupConfig( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    if x.TargetDeviceID <> tdid || x.TargetGroupID <> tgid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetGroupConfig" ) )
                    fun () -> TargetGroupConf.ReaderWriter.LoadString x.Config
                    |> CtrlConnection.AggregateXmlError
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetGroupConfig" ) )
            return r2
        }

    /// <summary>
    ///  Send GetAllTargetGroupConfig request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID should be get target group configuration data.
    /// </param>
    /// <returns>
    ///  All tTarget group configuration data.
    /// </returns>
    abstract GetAllTargetGroupConfig : tdid:TDID_T -> Task<TargetGroupConf.T_TargetGroup list>
    default this.GetAllTargetGroupConfig tdid =
        let req = HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_AllTargetGroupConfig( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    if x.TargetDeviceID <> tdid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetAllTargetGroupConfig" ) )
                    x.TargetGroup
                    |> List.map ( fun itr ->
                        let w =
                            fun () -> TargetGroupConf.ReaderWriter.LoadString itr.Config
                            |> CtrlConnection.AggregateXmlError
                        if w.TargetGroupID <> itr.TargetGroupID then
                            raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetAllTargetGroupConfig" ) )
                        w
                    )
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetAllTargetGroupConfig" ) )
            return r2
        }

    /// <summary>
    ///  Send CreateTargetGroupConfig request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which target group should be uploaded configuration data is belonging to.
    /// </param>
    /// <param name="config">
    ///  Target group configuration data that should be uploaded to the controller.
    /// </param>
    abstract CreateTargetGroupConfig : tdid:TDID_T -> config:TargetGroupConf.T_TargetGroup -> Task
    default this.CreateTargetGroupConfig tdid config =
        let tgconfStr =
            fun () -> TargetGroupConf.ReaderWriter.ToString config
            |> CtrlConnection.AggregateXmlError
        let req = HarukaCtrlerCtrlReq.U_CreateTargetGroupConfig({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
            TargetGroupID = config.TargetGroupID;
            Config = tgconfStr;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_CreateTargetGroupConfigResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid || x.TargetGroupID <> config.TargetGroupID then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateTargetGroupConfig" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateTargetGroupConfig" ) )
        }

    /// <summary>
    ///  Send DeleteTargetGroupConfig request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which target group should be uploaded configuration data is belonging to.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID should be uploaded configuration data.
    /// </param>
    abstract DeleteTargetGroupConfig : tdid:TDID_T -> tgid:TGID_T -> Task
    default this.DeleteTargetGroupConfig tdid tgid =
        let req = HarukaCtrlerCtrlReq.U_DeleteTargetGroupConfig({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
            TargetGroupID = tgid;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_DeleteTargetGroupConfigResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid || x.TargetGroupID <> tgid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DeleteTargetGroupConfig" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DeleteTargetGroupConfig" ) )
        }

    /// <summary>
    ///  Send GetLUWorkDir request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID should be get LUN list.
    /// </param>
    /// <returns>
    ///  LUNs list that have own working directory.
    /// </returns>
    abstract GetLUWorkDir : tdid:TDID_T -> Task< LUN_T list >
    default this.GetLUWorkDir tdid =
        let req = HarukaCtrlerCtrlReq.U_GetLUWorkDir({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_LUWorkDirs( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    if x.TargetDeviceID <> tdid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetLUWorkDir" ) )
                    x.Name
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetLUWorkDir" ) )
            return r2
        }

    /// <summary>
    ///  Send CreateLUWorkDir request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which LU working directory should be created.
    /// </param>
    /// <param name="lun">
    ///  LUN of LU that should be created own working directory.
    /// </param>
    abstract CreateLUWorkDir : tdid:TDID_T -> lun:LUN_T -> Task
    default this.CreateLUWorkDir tdid lun =
        let req = HarukaCtrlerCtrlReq.U_CreateLUWorkDir({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
            LUN = lun;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_CreateLUWorkDirResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid || x.LUN <> lun then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateLUWorkDir" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateLUWorkDir" ) )
        }

    /// <summary>
    ///  Send DeleteLUWorkDir request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which LU working directory should be deleted.
    /// </param>
    /// <param name="lun">
    ///  LUN of LU that should be deleted own working directory.
    /// </param>
    abstract DeleteLUWorkDir : tdid:TDID_T -> lun:LUN_T -> Task
    default this.DeleteLUWorkDir tdid lun =
        let req = HarukaCtrlerCtrlReq.U_DeleteLUWorkDir({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
            LUN = lun;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_DeleteLUWorkDirResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid || x.LUN <> lun then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DeleteLUWorkDir" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DeleteLUWorkDir" ) )
        }

    /// <summary>
    ///  Send GetTargetDeviceProcs request to the controller.
    /// </summary>
    /// <returns>
    ///  This function returns target device IDs list that process of target device have been running.
    /// </returns>
    abstract GetTargetDeviceProcs : unit -> Task< TDID_T list >
    default this.GetTargetDeviceProcs() =
        let req = HarukaCtrlerCtrlReq.U_GetTargetDeviceProcs( m_SessionID )
        task {
            let! r1 = this.SendRequest req
            let r2 =
                match r1 with
                | HarukaCtrlerCtrlRes.U_TargetDeviceProcs( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    x.TargetDeviceID
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetDeviceProcs" ) )
            return r2
        }

    /// <summary>
    ///  Send KillTargetDeviceProc request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which process should be killed.
    /// </param>
    abstract KillTargetDeviceProc : tdid:TDID_T -> Task
    default this.KillTargetDeviceProc tdid =
        let req = HarukaCtrlerCtrlReq.U_KillTargetDeviceProc({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_KillTargetDeviceProcResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "KillTargetDeviceProc" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "KillTargetDeviceProc" ) )
        }

    /// <summary>
    ///  Send StartTargetDeviceProc request to the controller.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which process should be started.
    /// </param>
    abstract StartTargetDeviceProc : tdid:TDID_T -> Task
    default this.StartTargetDeviceProc tdid =
        let req = HarukaCtrlerCtrlReq.U_StartTargetDeviceProc({
            SessionID = m_SessionID;
            TargetDeviceID = tdid;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_StartTargetDeviceProcResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
                if x.TargetDeviceID <> tdid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "StartTargetDeviceProc" ) )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "StartTargetDeviceProc" ) )
        }

    /// <summary>
    ///  Send CreateMediaFile request to the controller.
    /// </summary>
    /// <param name="fileName">
    ///  File name which will be created.
    /// </param>
    /// <param name="fileSize">
    ///  File size of the file that will be created.
    /// </param>
    /// <returns>
    ///  Process identifier of the started InitMedia process.
    /// </returns>
    abstract CreateMediaFile_PlainFile : fileName:string -> fileSize:int64 -> Task<uint64>
    default this.CreateMediaFile_PlainFile fileName fileSize =
        let req = HarukaCtrlerCtrlReq.U_CreateMediaFile({
            SessionID = m_SessionID;
            MediaType = HarukaCtrlerCtrlReq.U_PlainFile({
                    FileName = fileName;
                    FileSize = fileSize;
            })
        })
        task {
            let! r = this.SendRequest req
            let pid =
                match r with
                | HarukaCtrlerCtrlRes.U_CreateMediaFileResult( x ) ->
                    if not x.Result then
                        raise <| RequestError( x.ErrorMessage )
                    x.ProcID
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "CreateMediaFile_PlainFile" ) )
            return pid
        }

    /// <summary>
    ///  Send GetInitMediaStatus request to the controller.
    /// </summary>
    /// <returns>
    ///  Retrieved process status list.
    /// </returns>
    abstract GetInitMediaStatus : unit -> Task< HarukaCtrlerCtrlRes.T_Procs list >
    default this.GetInitMediaStatus () =
        let req = HarukaCtrlerCtrlReq.U_GetInitMediaStatus( m_SessionID )
        task {
            let! r = this.SendRequest req
            let plist =
                match r with
                | HarukaCtrlerCtrlRes.U_InitMediaStatus( x ) ->
                    if x.ErrorMessage.Length > 0 then
                        raise <| RequestError( x.ErrorMessage )
                    x.Procs
                | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                    raise <| RequestError( x )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetInitMediaStatus" ) )
            return plist
        }

    /// <summary>
    ///  Send KillInitMediaProc request to the controller.
    /// </summary>
    /// <param name="pid">
    ///  Process identifier of the InitMedia process that will be terminated.
    /// </param>
    abstract KillInitMediaProc : pid:uint64 -> Task
    default this.KillInitMediaProc pid =
        let req = HarukaCtrlerCtrlReq.U_KillInitMediaProc({
            SessionID = m_SessionID;
            ProcID = pid;
        })
        task {
            match! this.SendRequest req with
            | HarukaCtrlerCtrlRes.U_KillInitMediaProcResult( x ) ->
                if not x.Result then
                    raise <| RequestError( x.ErrorMessage )
            | HarukaCtrlerCtrlRes.U_UnexpectedError( x ) ->
                raise <| RequestError( x )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "KillInitMediaProc" ) )
        }

    /// <summary>
    ///  Send GetActiveTargetGroups request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which activated target groups should be gotton.
    /// </param>
    /// <returns>
    ///  List of target group IDs and names that have been activated.
    /// </returns>
    abstract GetActiveTargetGroups : tdid:TDID_T -> Task< TargetDeviceCtrlRes.T_ActiveTGInfo list >
    default this.GetActiveTargetGroups tdid =
        task {
            let! tdCtrlResult =
                TargetDeviceCtrlReq.U_GetActiveTargetGroups()
                |> this.SendTargetDeviceRequest tdid
            return
                match tdCtrlResult with
                | TargetDeviceCtrlRes.U_ActiveTargetGroups( y ) ->
                    y.ActiveTGInfo
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetTargetDeviceProcs" ) )
        }

    /// <summary>
    ///  Send GetLoadedTargetGroups request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which loaded target groups should be gotton.
    /// </param>
    /// <returns>
    ///  List of target group IDs and names that have been loaded.
    /// </returns>
    abstract GetLoadedTargetGroups : tdid:TDID_T -> Task< TargetDeviceCtrlRes.T_LoadedTGInfo list >
    default this.GetLoadedTargetGroups tdid =
        task {
            let! tdCtrlResult =
                TargetDeviceCtrlReq.U_GetLoadedTargetGroups()
                |> this.SendTargetDeviceRequest tdid
            return
                match tdCtrlResult with
                | TargetDeviceCtrlRes.U_LoadedTargetGroups( y ) ->
                    y.LoadedTGInfo
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetLoadedTargetGroups" ) )
        }

    /// <summary>
    ///  Send InactivateTargetGroup request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID to which the target group to be inactivated belongs.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID which should be inactivated.
    /// </param>
    abstract InactivateTargetGroup : tdid:TDID_T -> tgid:TGID_T -> Task
    default this.InactivateTargetGroup tdid tgid =
        task {
            let! tdCtrlResult =
                tgid
                |> TargetDeviceCtrlReq.U_InactivateTargetGroup
                |> this.SendTargetDeviceRequest tdid
            match tdCtrlResult with
            | TargetDeviceCtrlRes.U_InactivateTargetGroupResult( y ) ->
                if not y.Result then
                    // In current implementation, Result is always true.
                    raise <| RequestError( "Failed to inactivate target group." )
                if y.ID <> tgid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "InactivateTargetGroup" ) )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "InactivateTargetGroup" ) )
        }

    /// <summary>
    ///  Send ActivateTargetGroup request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID to which the target group to be activated belongs.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID which should be activated.
    /// </param>
    abstract ActivateTargetGroup : tdid:TDID_T -> tgid:TGID_T -> Task
    default this.ActivateTargetGroup tdid tgid =
        task {
            let! tdCtrlResult =
                tgid
                |> TargetDeviceCtrlReq.U_ActivateTargetGroup
                |> this.SendTargetDeviceRequest tdid
            match tdCtrlResult with
            | TargetDeviceCtrlRes.U_ActivateTargetGroupResult( y ) ->
                if not y.Result then
                    raise <| RequestError( y.ErrorMessage )
                if y.ID <> tgid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "ActivateTargetGroup" ) )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "ActivateTargetGroup" ) )
        }

    /// <summary>
    ///  Send UnloadTargetGroup request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID to which the target group to be unloaded belongs.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID which should be unloaded.
    /// </param>
    abstract UnloadTargetGroup : tdid:TDID_T -> tgid:TGID_T -> Task
    default this.UnloadTargetGroup tdid tgid =
        task {
            let! tdCtrlResult =
                tgid
                |> TargetDeviceCtrlReq.U_UnloadTargetGroup
                |> this.SendTargetDeviceRequest tdid
            match tdCtrlResult with
            | TargetDeviceCtrlRes.U_UnloadTargetGroupResult( y ) ->
                if not y.Result then
                    raise <| RequestError( y.ErrorMessage )
                if y.ID <> tgid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "UnloadTargetGroup" ) )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "UnloadTargetGroup" ) )
        }

    /// <summary>
    ///  Send LoadTargetGroup request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID to which the target group to be unloaded belongs.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID which should be unloaded.
    /// </param>
    abstract LoadTargetGroup : tdid:TDID_T -> tgid:TGID_T -> Task
    default this.LoadTargetGroup tdid tgid =
        task {
            let! tdCtrlResult =
                tgid
                |> TargetDeviceCtrlReq.U_LoadTargetGroup
                |> this.SendTargetDeviceRequest tdid
            match tdCtrlResult with
            | TargetDeviceCtrlRes.U_LoadTargetGroupResult( y ) ->
                if not y.Result then
                    raise <| RequestError( y.ErrorMessage )
                if y.ID <> tgid then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "LoadTargetGroup" ) )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "LoadTargetGroup" ) )
        }

    /// <summary>
    ///  Send SetLogParameters request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID should be updated log parameters.
    /// </param>
    /// <param name="logConf">
    ///  Log parameters.
    /// </param>
    abstract SetLogParameters : tdid:TDID_T -> logConf:TargetDeviceConf.T_LogParameters -> Task
    default this.SetLogParameters tdid logConf =
        task {
            let! tdCtrlResult =
                TargetDeviceCtrlReq.U_SetLogParameters({
                    SoftLimit = logConf.SoftLimit;
                    HardLimit = logConf.HardLimit;
                    LogLevel = logConf.LogLevel;
                })
                |> this.SendTargetDeviceRequest tdid
            match tdCtrlResult with
            | TargetDeviceCtrlRes.U_SetLogParametersResult( y ) ->
                if not y then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_FAILED_SET_LOGPARAM" ) )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "LoadTargetGroup" ) )
        }

    /// <summary>
    ///  Send GetLogParameters request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which to be read log parameters.
    /// </param>
    /// <returns>
    ///  Currentry effective log parameters in specified target device.
    /// </returns>
    abstract GetLogParameters : tdid:TDID_T -> Task< TargetDeviceConf.T_LogParameters >
    default this.GetLogParameters tdid =
        task {
            let! tdCtrlResult =
                TargetDeviceCtrlReq.U_GetLogParameters()
                |> this.SendTargetDeviceRequest tdid
            return
                match tdCtrlResult with
                | TargetDeviceCtrlRes.U_LogParameters( y ) ->
                    {
                        HardLimit = y.HardLimit;
                        SoftLimit = y.SoftLimit;
                        LogLevel = y.LogLevel;
                    } : TargetDeviceConf.T_LogParameters
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetLogParameters" ) )
        }

    /// <summary>
    ///  Send GetDeviceName request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID which to be getton target device name.
    /// </param>
    /// <returns>
    ///  target device name.
    /// </returns>
    abstract GetDeviceName : tdid:TDID_T -> Task< string >
    default this.GetDeviceName tdid =
        task {
            let! tdCtrlResult =
                TargetDeviceCtrlReq.U_GetDeviceName()
                |> this.SendTargetDeviceRequest tdid
            return
                match tdCtrlResult with
                | TargetDeviceCtrlRes.U_DeviceName( y ) ->
                    y
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetDeviceName" ) )
        }

    /// <summary>
    ///  Send GetSession request with SessInTargetDevice argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies a target device where the session list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved session list.
    /// </returns>
    abstract GetSession_InTargetDevice : tdid:TDID_T -> Task< TargetDeviceCtrlRes.T_Session list >
    default this.GetSession_InTargetDevice tdid =
        task {
            let! tdSessList =
                TargetDeviceCtrlReq.U_SessInTargetDevice()
                |> TargetDeviceCtrlReq.U_GetSession
                |> this.SendTargetDeviceRequest tdid
            return
                match tdSessList with
                | TargetDeviceCtrlRes.U_SessionList( y ) ->
                    y.Session
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetSession_InTargetDevice" ) )
        }

    /// <summary>
    ///  Send GetSession request with SessInTargetGroup argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID that specifies a target group where the session list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved session list.
    /// </returns>
    abstract GetSession_InTargetGroup : tdid:TDID_T -> tgid:TGID_T -> Task< TargetDeviceCtrlRes.T_Session list >
    default this.GetSession_InTargetGroup tdid tgid =
        task {
            let! tdSessList =
                tgid
                |> TargetDeviceCtrlReq.U_SessInTargetGroup
                |> TargetDeviceCtrlReq.U_GetSession
                |> this.SendTargetDeviceRequest tdid
            return
                match tdSessList with
                | TargetDeviceCtrlRes.U_SessionList( y ) ->
                    y.Session
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetSession_InTargetGroup" ) )
        }        

    /// <summary>
    ///  Send GetSession request with SessInTarget argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="tid">
    ///  Target node ID that specifies a target where the session list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved session list.
    /// </returns>
    abstract GetSession_InTarget : tdid:TDID_T -> tid:TNODEIDX_T -> Task< TargetDeviceCtrlRes.T_Session list >
    default this.GetSession_InTarget tdid tid =
        task {
            let! tdSessList =
                tid
                |> TargetDeviceCtrlReq.U_SessInTarget
                |> TargetDeviceCtrlReq.U_GetSession
                |> this.SendTargetDeviceRequest tdid
            return
                match tdSessList with
                | TargetDeviceCtrlRes.U_SessionList( y ) ->
                    y.Session
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetSession_InTarget" ) )
        }

    /// <summary>
    ///  Send DestructSession request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="tsih">
    ///  TSIH value that specify which session should be destructed.
    /// </param>
    abstract DestructSession : tdid:TDID_T -> tsih:TSIH_T -> Task
    default this.DestructSession tdid tsih =
        task {
            let! tdSessList =
                tsih
                |> TargetDeviceCtrlReq.U_DestructSession
                |> this.SendTargetDeviceRequest tdid 
            match tdSessList with
            | TargetDeviceCtrlRes.U_DestructSessionResult( y ) ->
                if not y.Result then
                    raise <| RequestError( y.ErrorMessage )
                if y.TSIH <> tsih then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DestructSession" ) )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DestructSession" ) )
        }

    /// <summary>
    ///  Send GetConnection request with ConInTargetDevice argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies a target device where the connection list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved connections list.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReceiveBytesCount/SentBytesCount) are aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    /// </remarks>
    abstract GetConnection_InTargetDevice : tdid:TDID_T -> Task< TargetDeviceCtrlRes.T_Connection list >
    default this.GetConnection_InTargetDevice tdid =
        task {
            let! conList =
                TargetDeviceCtrlReq.U_ConInTargetDevice()
                |> TargetDeviceCtrlReq.U_GetConnection
                |> this.SendTargetDeviceRequest tdid
            return
                match conList with
                | TargetDeviceCtrlRes.U_ConnectionList( y ) ->
                    y.Connection
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetConnection_InTargetDevice" ) )
        }

    /// <summary>
    ///  Send GetConnection request with ConInNetworkPortal argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="npid">
    ///  Network portal ID that specifies a network portal where the connection list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved connections list.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReceiveBytesCount/SentBytesCount) are aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    /// </remarks>
    abstract GetConnection_InNetworkPortal : tdid:TDID_T -> npid:NETPORTIDX_T -> Task< TargetDeviceCtrlRes.T_Connection list >
    default this.GetConnection_InNetworkPortal tdid npid =
        task {
            let! conList =
                npid
                |> TargetDeviceCtrlReq.U_ConInNetworkPortal
                |> TargetDeviceCtrlReq.U_GetConnection
                |> this.SendTargetDeviceRequest tdid
            return
                match conList with
                | TargetDeviceCtrlRes.U_ConnectionList( y ) ->
                    y.Connection
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetConnection_InNetworkPortal" ) )
        }

    /// <summary>
    ///  Send GetConnection request with ConInTargetGroup argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="tgid">
    ///  Target group ID that specifies a target group where the connection list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved connections list.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReceiveBytesCount/SentBytesCount) are aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    /// </remarks>
    abstract GetConnection_InTargetGroup : tdid:TDID_T -> tgid:TGID_T -> Task< TargetDeviceCtrlRes.T_Connection list >
    default this.GetConnection_InTargetGroup tdid tgid =
        task {
            let! conList =
                tgid
                |> TargetDeviceCtrlReq.U_ConInTargetGroup
                |> TargetDeviceCtrlReq.U_GetConnection
                |> this.SendTargetDeviceRequest tdid
            return
                match conList with
                | TargetDeviceCtrlRes.U_ConnectionList( y ) ->
                    y.Connection
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetConnection_InTargetGroup" ) )
        }

    /// <summary>
    ///  Send GetConnection request with ConInTarget argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="tid">
    ///  Target node ID that specifies a target where the connection list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved connections list.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReceiveBytesCount/SentBytesCount) are aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    /// </remarks>
    abstract GetConnection_InTarget : tdid:TDID_T -> tid:TNODEIDX_T -> Task< TargetDeviceCtrlRes.T_Connection list >
    default this.GetConnection_InTarget tdid tid =
        task {
            let! conList =
                tid
                |> TargetDeviceCtrlReq.U_ConInTarget
                |> TargetDeviceCtrlReq.U_GetConnection
                |> this.SendTargetDeviceRequest tdid
            return
                match conList with
                | TargetDeviceCtrlRes.U_ConnectionList( y ) ->
                    y.Connection
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetConnection_InTarget" ) )
        }

    /// <summary>
    ///  Send GetConnection request with ConInSession argument to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="tsih">
    ///  TSIH value that specifies a session where the connection list should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved connections list.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReceiveBytesCount/SentBytesCount) are aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    /// </remarks>
    abstract GetConnection_InSession : tdid:TDID_T -> tsih:TSIH_T -> Task< TargetDeviceCtrlRes.T_Connection list >
    default this.GetConnection_InSession tdid tsih =
        task {
            let! conList =
                tsih
                |> TargetDeviceCtrlReq.U_ConInSession
                |> TargetDeviceCtrlReq.U_GetConnection
                |> this.SendTargetDeviceRequest tdid
            return
                match conList with
                | TargetDeviceCtrlRes.U_ConnectionList( y ) ->
                    y.Connection
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetConnection_InSession" ) )
        }

    /// <summary>
    ///  Send GetLUStatus request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  LUN that specifies a LU where the status should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved LU status.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReadBytesCount/WrittenBytesCount/ReadTickCount/WriteTickCount) are
    ///  aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    ///  ReadTickCount and WriteTickCount are measured in milliseconds.
    /// </remarks>
    abstract GetLUStatus : tdid:TDID_T -> lun:LUN_T -> Task< TargetDeviceCtrlRes.T_LUStatus_Success >
    default this.GetLUStatus tdid lun =
        task {
            let! conList =
                lun
                |> TargetDeviceCtrlReq.U_GetLUStatus
                |> this.SendTargetDeviceRequest tdid
            return
                match conList with
                | TargetDeviceCtrlRes.U_LUStatus( y ) ->
                    if y.LUN <> lun then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetLUStatus" ) )
                    if y.LUStatus_Success.IsNone then
                        raise <| RequestError( y.ErrorMessage )
                    y.LUStatus_Success.Value
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetLUStatus" ) )
        }

    /// <summary>
    ///  Send LUReset request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  LUN that specifies a LU should be reset.
    /// </param>
    abstract LUReset : tdid:TDID_T -> lun:LUN_T -> Task
    default this.LUReset tdid lun =
        task {
            let! resetResult =
                lun
                |> TargetDeviceCtrlReq.U_LUReset
                |> this.SendTargetDeviceRequest tdid
            match resetResult with
            | TargetDeviceCtrlRes.U_LUResetResult( y ) ->
                if y.LUN <> lun then
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "LUReset" ) )
                if not y.Result then
                    raise <| RequestError( y.ErrorMessage )
            | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                raise <| RequestError( y )
            | _ ->
                raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "LUReset" ) )
        }

    /// <summary>
    ///  Send GetMediaStatus request to specified target device.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  LUN that specifies a LU where the status should be obtained.
    /// </param>
    /// <param name="mediaid">
    ///  Media node ID that specifies a media where the status should be obtained.
    /// </param>
    /// <returns>
    ///  Retrieved media status.
    /// </returns>
    /// <remarks>
    ///  Resource counters (ReadBytesCount/WrittenBytesCount/ReadTickCount/WriteTickCount) are
    ///  aggregated in units of Constants.RECOUNTER_SPAN_SEC.
    ///  ReadTickCount and WriteTickCount are measured in milliseconds.
    /// </remarks>
    abstract GetMediaStatus : tdid:TDID_T -> lun:LUN_T -> mediaid:MEDIAIDX_T -> Task< TargetDeviceCtrlRes.T_MediaStatus_Success >
    default this.GetMediaStatus tdid lun mediaid =
        task {
            let! mstat =
                TargetDeviceCtrlReq.U_GetMediaStatus({
                    LUN = lun;
                    ID = mediaid;
                })
                |> this.SendTargetDeviceRequest tdid
            return
                match mstat with
                | TargetDeviceCtrlRes.U_MediaStatus( y ) ->
                    if y.LUN <> lun || y.ID <> mediaid then
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetMediaStatus" ) )
                    if y.MediaStatus_Success.IsNone then
                        raise <| RequestError( y.ErrorMessage )
                    y.MediaStatus_Success.Value
                | TargetDeviceCtrlRes.U_UnexpectedError( y ) ->
                    raise <| RequestError( y )
                | _ ->
                    raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "GetMediaStatus" ) )
        }

    /// <summary>
    ///  Send GetAllTraps media control request to specified debug media.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  Specify the LU to which the debug media belongs.
    /// </param>
    /// <param name="mediaid">
    ///  Specifies who to send the message to. Must be debug media.
    /// </param>
    /// <returns>
    ///  Registared traps.
    /// </returns>
    abstract DebugMedia_GetAllTraps : tdid:TDID_T -> lun:LUN_T -> mediaid:MEDIAIDX_T -> Task< MediaCtrlRes.T_Trap list >
    default this.DebugMedia_GetAllTraps tdid lun mediaid =
        task {
            let reqData = MediaCtrlReq.U_Debug(
                MediaCtrlReq.U_GetAllTraps()
            )
            let! resData = this.SendMediaControlRequest tdid lun mediaid reqData
            return
                match resData with
                | MediaCtrlRes.U_Debug( y ) ->
                    match y with
                    | MediaCtrlRes.U_AllTraps( z ) ->
                        z.Trap
                    | _ ->
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DebugMedia_GetAllTraps" ) )
                | MediaCtrlRes.U_Unexpected( y ) ->
                    raise <| RequestError( y )
        }

    /// <summary>
    ///  Send AddTrap media control request to specified debug media.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  Specify the LU to which the debug media belongs.
    /// </param>
    /// <param name="mediaid">
    ///  Specifies who to send the message to. Must be debug media.
    /// </param>
    /// <param name="event">
    ///  Specify the event of trap to be registered.
    /// </param>
    /// <param name="action">
    ///  Specify the action of trap to be registered.
    /// </param>
    /// <returns>
    ///  Registared traps.
    /// </returns>
    abstract DebugMedia_AddTrap : tdid:TDID_T -> lun:LUN_T -> mediaid:MEDIAIDX_T -> event:MediaCtrlReq.T_Event -> action:MediaCtrlReq.T_Action -> Task
    default this.DebugMedia_AddTrap tdid lun mediaid event action =
        task {
            let reqData = MediaCtrlReq.U_Debug(
                MediaCtrlReq.U_AddTrap({ Event = event; Action = action; })
            )
            let! resData = this.SendMediaControlRequest tdid lun mediaid reqData
            return
                match resData with
                | MediaCtrlRes.U_Debug( y ) ->
                    match y with
                    | MediaCtrlRes.U_AddTrapResult( z ) ->
                        if not z.Result then
                            raise <| RequestError( z.ErrorMessage )
                    | _ ->
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DebugMedia_AddTrap" ) )
                | MediaCtrlRes.U_Unexpected( y ) ->
                    raise <| RequestError( y )
        }

    /// <summary>
    ///  Send ClearTraps media control request to specified debug media.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  Specify the LU to which the debug media belongs.
    /// </param>
    /// <param name="mediaid">
    ///  Specifies who to send the message to. Must be debug media.
    /// </param>
    /// <returns>
    ///  Registared traps.
    /// </returns>
    abstract DebugMedia_ClearTraps : tdid:TDID_T -> lun:LUN_T -> mediaid:MEDIAIDX_T -> Task
    default this.DebugMedia_ClearTraps tdid lun mediaid =
        task {
            let reqData = MediaCtrlReq.U_Debug(
                MediaCtrlReq.U_ClearTraps()
            )
            let! resData = this.SendMediaControlRequest tdid lun mediaid reqData
            return
                match resData with
                | MediaCtrlRes.U_Debug( y ) ->
                    match y with
                    | MediaCtrlRes.U_ClearTrapsResult( z ) ->
                        if not z.Result then
                            raise <| RequestError( z.ErrorMessage )
                    | _ ->
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DebugMedia_ClearTraps" ) )
                | MediaCtrlRes.U_Unexpected( y ) ->
                    raise <| RequestError( y )
        }

    /// <summary>
    ///  Send GetCounterValue media control request to specified debug media.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID that specifies the target device to receive the request.
    /// </param>
    /// <param name="lun">
    ///  Specify the LU to which the debug media belongs.
    /// </param>
    /// <param name="mediaid">
    ///  Specifies who to send the message to. Must be debug media.
    /// </param>
    /// <param name="counterno">
    ///  Specify the counter number to be registered.
    /// </param>
    /// <returns>
    ///  Registared traps.
    /// </returns>
    abstract DebugMedia_GetCounterValue : tdid:TDID_T -> lun:LUN_T -> mediaid:MEDIAIDX_T -> counterno:int -> Task< int >
    default this.DebugMedia_GetCounterValue tdid lun mediaid counterno =
        task {
            let reqData = MediaCtrlReq.U_Debug(
                MediaCtrlReq.U_GetCounterValue( counterno )
            )
            let! resData = this.SendMediaControlRequest tdid lun mediaid reqData
            return
                match resData with
                | MediaCtrlRes.U_Debug( y ) ->
                    match y with
                    | MediaCtrlRes.U_CounterValue( z ) ->
                        z
                    | _ ->
                        raise <| RequestError( m_MessageTable.GetMessage( "ERRMSG_UNEXPECTED_RESPONSE", "DebugMedia_GetCounterValue" ) )
                | MediaCtrlRes.U_Unexpected( y ) ->
                    raise <| RequestError( y )
        }


