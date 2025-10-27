//=============================================================================
// Haruka Software Storage.
// Controller.fs : Controller.fs defines Controller class that implements
//                 all of haruka controller process functionality.

//=============================================================================
// Namespace declaration

namespace Haruka.Controller

//=============================================================================
// Import declaration

open System
open System.IO
open System.IO.Pipes
open System.Collections.Generic
open System.Collections.Concurrent
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open System.Net
open System.Net.Sockets

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// Data type that represents the management clients is logged in or not.
[<NoComparison>]
type MgrCliSessionStatus =
    | UnUsed
    | LoggedIn of ( CtrlSessionID * DateTime )

[<NoComparison>]
type TargetDeviceProcInfo = {
    /// process information
    m_Proc : Process;

    /// Last start time ( minute )
    m_LastStartTime : int64;

    /// Restart count
    m_RestartCount : int;
}

//=============================================================================
// Class implementation

/// <summary>
///  Controller class.
/// </summary>
/// <param name="m_ConfPath">
///  Path name of directory that contains configuration files.
/// </param>
/// <param name="m_Killer">
///  referense of HKiller object.
/// </param>
/// <param name="m_TD_ExePath">
///  The executable file name of the TargetDevice.
/// </param>
/// <param name="m_IM_ExePath">
///  The executable file name of the InitMedia.
/// </param>
type Controller (
    m_ConfPath : string,
    m_Killer : IKiller,
    m_TD_ExePath : string,
    m_IM_ExePath : string
) as this =

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// Controller configurations
    let m_CtrlConf : HarukaCtrlConf.T_HarukaCtrl =
        Controller.LoadConfig m_ObjID m_ConfPath

    /// Log aggregator object
    let m_logAgr =
        // Initialize Logger object for controller process
        let pout = new AnonymousPipeServerStream( PipeDirection.Out )
        let pin = new AnonymousPipeClientStream( PipeDirection.In, pout.ClientSafePipeHandle )
        match m_CtrlConf.LogParameters with
        | Some( x ) ->
            HLogger.SetLogParameters(
                x.SoftLimit,
                x.HardLimit,
                Constants.DEFAULT_LOG_OUTPUT_CYCLE,
                x.LogLevel,
                new StreamWriter( pout )
            )
        | None ->
            HLogger.SetLogParameters(
                Constants.LOGPARAM_DEF_SOFTLIMIT,
                Constants.LOGPARAM_DEF_HARDLIMIT,
                Constants.DEFAULT_LOG_OUTPUT_CYCLE,
                LogLevel.LOGLEVEL_INFO,
                new StreamWriter( pout )
            )
        HLogger.Initialize()
        HLogger.Trace( LogID.I_CONTROLLER_PROC_STARTED, fun g ->
            let pidStr = Process.GetCurrentProcess().Id |> sprintf "%d"
            g.Gen2( m_ObjID, m_ConfPath, pidStr )
        )

        // Create log aggregator object
        let logDirName =
            Functions.AppendPathName m_ConfPath Constants.CONTROLLER_LOG_DIR_NAME

        let conf =
            let d : HarukaCtrlConf.T_LogMaintenance = {
                    OutputDest = HarukaCtrlConf.U_ToStdout( Constants.LOGMNT_DEF_TOTALLIMIT )
            }
            Option.defaultValue d m_CtrlConf.LogMaintenance

        let agl = new LogAggregator( logDirName, conf, m_Killer )
        agl.Initialize()

        // Add this process for log child 
        agl.AddChild( new StreamReader( pin ) )
        agl

    /// Target device child processes
    /// Requires exclusive control by m_Sema
    let m_TargetDeviceProcs = new ConcurrentDictionary< TDID_T, TargetDeviceProcInfo >()

    /// Media creation child process
    /// Requires exclusive control by m_Sema
    let m_InitMediaProcs = new Dictionary< uint64, MediaCreateProc >()

    /// Session ID for management client.
    /// Requires exclusive control by m_Sema
    let mutable m_MgrCliSessID = MgrCliSessionStatus.UnUsed

    /// lock object for member of this class
    let m_Sema = new SemaphoreSlim( 1 )

    /// TCP server port
    let m_Listener =
        // decide controller TCP port config.
        let conf =
            let d : HarukaCtrlConf.T_RemoteCtrl = {
                PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM;
                Address = "::1";
                WhiteList = [];
            }
            Option.defaultValue d m_CtrlConf.RemoteCtrl

        // Resolv host name to local port address.
        let addr =
            if conf.Address.Length > 0 then
                try
                    match IPAddress.TryParse conf.Address with
                    | ( true, x ) -> [| x |]
                    | ( false, _ ) ->
                        let r = Dns.GetHostEntry conf.Address
                        r.AddressList
                with
                | _ as x ->
                    HLogger.Trace( LogID.E_FAILED_RESOLV_ADDRESS, fun g -> g.Gen2( m_ObjID, conf.Address, x.Message ) )
                    reraise()
            else
                HLogger.Trace( LogID.W_CTRL_LOCAL_ADDRESS_NOT_SPECIFIED, fun g -> g.Gen0 m_ObjID )
                [| IPAddress.IPv6Any |]

        // Create TCP server port
        try
            addr
            |> Array.map ( fun itr ->
                let l = new TcpListener( itr, int conf.PortNum )
                if conf.Address.Length <= 0 then
                    l.Server.SetSocketOption( SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0 )
                HLogger.Trace( LogID.I_CREATE_TCP_SERVER_PORT, fun g -> g.Gen2( m_ObjID, conf.Address, conf.PortNum ) )
                struct( l, conf.WhiteList )
            )
        with
        | _ as x ->
            HLogger.Trace( LogID.E_FAILED_CREATE_TCP_SERVER_PORT, fun g -> g.Gen3( m_ObjID, conf.Address, conf.PortNum, x.Message ) )
            reraise()

    do
        m_Killer.Add ( this :> IComponent )
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, "Controller", "" ) )

    //-------------------------------------------------------------------------
    // interface imprementation

    interface IComponent with

        // ------------------------------------------------------------------------
        //   Notince terminate request.
        member _.Terminate() : unit =
            m_Sema.Wait()
            try
                // Kill all of child procs
                let wprocs = m_TargetDeviceProcs.Values |> Seq.toArray
                m_TargetDeviceProcs.Clear()
                for itr in wprocs do
                    itr.m_Proc.Kill()
            finally
                m_Sema.Release() |> ignore

            // close TCP server port
            m_Listener |> Array.iter ( fun struct( tl, _ ) -> tl.Stop () )

    //-------------------------------------------------------------------------
    // public method

    /// <summary>
    ///  Get currentry effective configuration values.
    /// </summary>
    member this.CtrlConf : HarukaCtrlConf.T_HarukaCtrl =
        m_CtrlConf

    /// <summary>
    ///  Load target device process at startup.
    /// </summary>
    member this.LoadInitialTargetDeviceProcs () : unit =
        let rx = Constants.TARGET_DEVICE_DIR_NAME_REGOBJ
        Directory.GetDirectories m_ConfPath
        |> Array.map Path.GetFileName
        |> Array.filter rx.IsMatch
        |> Array.truncate Constants.MAX_TARGET_DEVICE_COUNT
        |> Array.iter ( tdid_me.fromString >> this.StartNewTDProcessAndAddEntry >> ignore )

    /// <summary>
    ///  Start waiting for requests.
    /// </summary>
    member this.WaitRequest() : unit =
        HLogger.Trace( LogID.I_ENTER_CTRL_REQ_LOOP, fun g -> g.Gen0 m_ObjID )

        for struct( tl, cond ) in m_Listener do
            fun() -> task {
                try
                    // Start TCP server port
                    tl.Start ()

                    while not m_Killer.IsNoticed do
                        // Wait for connections
                        let! s = tl.AcceptSocketAsync()
                        let endPoint = s.RemoteEndPoint :?> IPEndPoint
                        let sourcePort = endPoint.Port
                        let filterResult =
                            if cond.IsEmpty then
                                // If IP conditions are not specified, it consider accept all connections.
                                true
                            else
                                IPCondition.Match ( s.RemoteEndPoint :?> IPEndPoint ).Address cond

                        if not filterResult then
                            HLogger.Trace( LogID.W_CONN_REJECTED_DUE_TO_WHITELIST, fun g ->
                                let endPointStr = endPoint.ToString ()
                                let sourcePortStr = String.Format( "{0}", sourcePort )
                                g.Gen2( m_ObjID, endPointStr, sourcePortStr )
                            )
                            s.Close()
                        else
                            HLogger.Trace( LogID.I_ACCEPT_CONNECTION, fun g ->
                                let endPointStr = endPoint.ToString ()
                                let sourcePortStr = String.Format( "{0}", sourcePort )
                                g.Gen2( m_ObjID, endPointStr, sourcePortStr )
                            )
                            this.ProcRequestTask s
                            |> Functions.StartTask

                    tl.Stop ()
                    HLogger.Trace( LogID.I_EXIT_CTRL_REQ_LOOP, fun g -> g.Gen0 m_ObjID )
                with
                | :? SocketException as x ->
                    if m_Killer.IsNoticed then
                        // This case is normal, ignore the exception.
                        ()
                | _ as x ->
                    // This may be due to a failure to open the port or some other error.
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
            }
            |> Functions.StartTask

    //-------------------------------------------------------------------------
    // private method

    /// <summary>
    ///  Proc acceptec connection.
    /// </summary>
    /// <param name="s">
    ///  Connected TCP connection.
    /// </param>
    /// <returns>
    ///  Task that has procedure of controller function.
    /// </returns>
    member private this.ProcRequestTask ( s : Socket ) : ( unit -> Task<unit> ) =
        fun () -> task {
            try
                use c = new NetworkStream( s )
                while not m_Killer.IsNoticed do
                    let! lineStr = Functions.FramingReceiver c
                    let! rb =
                        try
                            this.ProcessRequestString lineStr
                        with
                        | _ as x ->
                            HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                            task {
                                return
                                    // UnexpectedError
                                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( x.Message )
                                    }
                            }
                    do! Functions.FramingSender c rb
            with
            | _ as x ->
                HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
        }

    /// <summary>
    ///  Called at the end of the process.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID.
    /// </param>
    member private _.OnExitChildProc ( tdid : TDID_T ) : unit =
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.I_TD_PROC_TERMINATE_DETECTED, fun g -> g.Gen1( m_ObjID, tdidStr ) )

        fun () -> task {
            do! m_Sema.WaitAsync()
            try
                let utcNow = DateTime.UtcNow
                let currentTime = utcNow.Ticks / 600000000L
                let r, pinfo = m_TargetDeviceProcs.TryGetValue( tdid )
                if not r then
                    // If the entry of tarminated process is already removed, restart will not to do.
                    HLogger.Trace( LogID.I_TD_PROC_ENTRY_ALREADY_REMOVED, fun g -> g.Gen1( m_ObjID, tdidStr ) )

                elif pinfo.m_LastStartTime = currentTime && pinfo.m_RestartCount >= Constants.MAX_CHILD_PROC_RESTART_COUNT then
                    // Restart count over. Remove entry.
                    m_TargetDeviceProcs.Remove( tdid ) |> ignore
                    pinfo.m_Proc.Dispose()
                    HLogger.Trace( LogID.E_TD_PROC_RETRY_COUNT_OVER, fun g ->
                        let pidStr = String.Format( "{0}", pinfo.m_Proc.Id )
                        g.Gen2( m_ObjID, tdidStr, pidStr )
                    )
                else
                    try
                        HLogger.Trace( LogID.W_TRY_RESTART_TD_PROC, fun g ->
                            let pidStr = String.Format( "{0}", pinfo.m_Proc.Id )
                            g.Gen2( m_ObjID, tdidStr, pidStr )
                        )

                        // Update restart count.
                        let newPinfo = {
                            pinfo with
                                m_LastStartTime = currentTime;
                                m_RestartCount =
                                    if pinfo.m_LastStartTime = currentTime then
                                        pinfo.m_RestartCount + 1
                                    else
                                        1;
                        }
                        m_TargetDeviceProcs.Remove( tdid ) |> ignore
                        m_TargetDeviceProcs.AddOrUpdate( tdid, newPinfo, ( fun k o -> newPinfo ) ) |> ignore

                        // Restart child process
                        pinfo.m_Proc.Start() |> ignore

                        let pid = pinfo.m_Proc.Id
                        let newProcIDStr = pid.ToString()
                        HLogger.Trace( LogID.I_TARGET_DEVICE_PROC_STARTED, fun g -> g.Gen2( m_ObjID, tdidStr, newProcIDStr ) )
                    with
                    | _ as x ->
                        // If failed to start child process, remove entry.
                        HLogger.Trace( LogID.E_FAILED_START_TD_PROC, fun g -> g.Gen2( m_ObjID, tdidStr, x.Message ) )
                        m_TargetDeviceProcs.Remove( tdid ) |> ignore
                        pinfo.m_Proc.Dispose()
            finally
                m_Sema.Release() |> ignore

        }
        |> Functions.StartTask

    /// <summary>
    ///  Start waiting for requests from local client.
    /// </summary>
    member private this.ProcessRequestString ( lineStr : string ) : Task<string> =
        let req = HarukaCtrlerCtrlReq.ReaderWriter.LoadString lineStr
        task {
            do! m_Sema.WaitAsync()
            try
                return!
                    match req.Request with
                    | HarukaCtrlerCtrlReq.T_Request.U_Login( x ) ->
                        this.Login x
                    | HarukaCtrlerCtrlReq.T_Request.U_Logout( x ) ->
                        this.Logout x
                    | HarukaCtrlerCtrlReq.T_Request.U_NoOperation( x ) ->
                        this.NoOperation x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetControllerConfig( x ) ->
                        this.GetControllerConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_SetControllerConfig( x ) ->
                        this.SetControllerConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceDir( x ) ->
                        this.GetTargetDeviceDir x
                    | HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir( x ) ->
                        this.CreateTargetDeviceDir x
                    | HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetDeviceDir( x ) ->
                        this.DeleteTargetDeviceDir x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceConfig( x ) ->
                        this.GetTargetDeviceConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceConfig( x ) ->
                        this.CreateTargetDeviceConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID( x ) ->
                        this.GetTargetGroupID x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupConfig( x ) ->
                        this.GetTargetGroupConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig( x ) ->
                        this.GetAllTargetGroupConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig( x ) ->
                        this.CreateTargetGroupConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetGroupConfig( x ) ->
                        this.DeleteTargetGroupConfig x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir( x ) ->
                        this.GetLUWorkDir x
                    | HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir( x ) ->
                        this.CreateLUWorkDir x
                    | HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir( x ) ->
                        this.DeleteLUWorkDir x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceProcs( x ) ->
                        this.GetTargetDeviceProcs x
                    | HarukaCtrlerCtrlReq.T_Request.U_KillTargetDeviceProc( x ) ->
                        this.KillTargetDeviceProc x
                    | HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( x ) ->
                        this.StartTargetDeviceProc x
                    | HarukaCtrlerCtrlReq.T_Request.U_TargetDeviceCtrlRequest( x ) ->
                        this.TargetDeviceCtrlRequest x
                    | HarukaCtrlerCtrlReq.T_Request.U_CreateMediaFile( x ) ->
                        this.CreateMediaFile x
                    | HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( x ) ->
                        this.GetInitMediaStatus x
                    | HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc( x ) ->
                        this.KillInitMediaProc x
            finally
                m_Sema.Release() |> ignore
        }

    /// <summary>
    ///  Process "Login" control request.
    /// </summary>
    /// <param name="force">
    ///  Whether to destroy existing sessions and force login or not.
    /// </param>
    member private _.Login ( force : bool ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "Login(%b)" force ) )

        let sendMsg ( r : bool ) ( g : CtrlSessionID ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_LoginResult( {
                    Result = r;
                    SessionID = g;
                })
            }

        let loginResult =
            if force then
                // If force is true, always, it can always login.
                true
            else
                match m_MgrCliSessID with
                | LoggedIn( _, t ) ->
                    // If the session has expired, it can log in.
                    let diff = DateTime.UtcNow - t
                    diff.TotalSeconds >= Constants.CONTROLLER_SESSION_LIFE_TIME
                | _ ->
                    // If not Loged in, it can log in.
                    true
        
        if loginResult then
            let newID = CtrlSessionID.NewID()
            HLogger.Trace( LogID.I_MGR_CLI_LOGGED_IN, fun g -> g.Gen1( m_ObjID, newID.ToString() ) )
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "Login", "" ) )
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( newID, DateTime.UtcNow )
            sendMsg true newID
        else
            HLogger.Trace( LogID.W_CTRL_REQ_ERROR_END, fun g -> g.Gen2( m_ObjID, "Login", "" ) )
            sendMsg false ( CtrlSessionID() )
        |> Task.FromResult

    /// <summary>
    ///  Process "Logout" control request.
    /// </summary>
    /// <param name="sessID">
    ///  Session ID.
    /// </param>
    member private _.Logout ( sessID : CtrlSessionID ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "Logout" ) )

        let sendMsg ( r : bool ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( {
                    Result = r;
                    SessionID = sessID;
                })
            }

        let logoutResult =
            match m_MgrCliSessID with
            | LoggedIn( efID, t ) ->
                let diff = DateTime.UtcNow - t
                ( efID = sessID ) || 
                ( diff.TotalSeconds >= Constants.CONTROLLER_SESSION_LIFE_TIME )
            | _ ->
                true

        if logoutResult then
            HLogger.Trace( LogID.I_MGR_CLI_LOGOUT, fun g -> g.Gen1( m_ObjID, sessID.ToString() ) )
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "Logout", "" ) )
            m_MgrCliSessID <- MgrCliSessionStatus.UnUsed
            sendMsg true
        else
            HLogger.Trace( LogID.W_CTRL_REQ_ERROR_END, fun g -> g.Gen2( m_ObjID, "Logout", "" ) )
            sendMsg false
        |> Task.FromResult


    /// <summary>
    ///  Process "NoOperation" control request.
    /// </summary>
    /// <param name="sessID">
    ///  Session ID.
    /// </param>
    member private _.NoOperation ( sessID : CtrlSessionID ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "NoOperation" ) )
        let curStatus = m_MgrCliSessID

        let sendMsg ( r : bool ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( {
                    Result = r;
                    SessionID = sessID;
                })
            }

        if Controller.CheckLoginStatus curStatus sessID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( sessID, DateTime.UtcNow )
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "NoOperation", "" ) )
            sendMsg true
        else
            HLogger.Trace( LogID.W_CTRL_REQ_ERROR_END, fun g -> g.Gen2( m_ObjID, "NoOperation", "" ) )
            sendMsg false
        |> Task.FromResult

    /// <summary>
    ///  Process "Get contoroller config" control request.
    /// </summary>
    /// <param name="sessID">
    ///  Session ID.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.GetControllerConfig ( sessID : CtrlSessionID ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetControllerConfig" ) )
        let fName = Functions.AppendPathName m_ConfPath Constants.CONTROLLER_CONF_FILE_NAME
        let curStatus = m_MgrCliSessID
        
        let sendMsg ( wresult : string ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig( {
                    Config = wresult;
                    ErrorMessage = wmsg;
                })
            }
                
        if Controller.CheckLoginStatus curStatus sessID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( sessID, DateTime.UtcNow )
            task {
                let! wr = Functions.ReadAllTextAsync fName
                let wresult, wmsg = Functions.GetOkValue "" wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_READ_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                else
                    let msg = sprintf "(Read size=%d)" wresult.Length
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetControllerConfig", msg ) )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg "" "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "SetControllerConfig" control request.
    /// </summary>
    /// <param name="ctrlConf">
    ///  The controller configuration which will be written.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.SetControllerConfig ( ctrlConf : HarukaCtrlerCtrlReq.T_SetControllerConfig ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "SetControllerConfig" ) )
        let fName = Functions.AppendPathName m_ConfPath Constants.CONTROLLER_CONF_FILE_NAME
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult( {
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus ctrlConf.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( ctrlConf.SessionID, DateTime.UtcNow )
            task {
                let! wr = Functions.WriteAllTextAsync fName ctrlConf.Config
                let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_WRITE_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                else
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(Wrote size=%d)" ctrlConf.Config.Length
                        g.Gen2( m_ObjID, "SetControllerConfig", msg )
                    )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult 

    /// <summary>
    ///  Process "Get target device dir" control request.
    /// </summary>
    /// <param name="sessID">
    ///  Session ID.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.GetTargetDeviceDir ( sessID : CtrlSessionID ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetTargetDeviceDir" ) )
        let rx = Constants.TARGET_DEVICE_DIR_NAME_REGOBJ
        let curStatus = m_MgrCliSessID

        let rdir, msg =
            if Controller.CheckLoginStatus curStatus sessID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( sessID, DateTime.UtcNow )
                try
                    let r =
                        Directory.GetDirectories m_ConfPath
                        |> Seq.map Path.GetFileName
                        |> Seq.sort
                        |> Seq.truncate Constants.MAX_TARGET_DEVICE_COUNT
                        |> Seq.filter rx.IsMatch
                        |> Seq.map tdid_me.fromString
                        |> Seq.toList
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(Dir count=%d)" r.Length
                        g.Gen2( m_ObjID, "GetTargetDeviceDir", msg )
                    )
                    r, ""
                with
                | x ->
                    HLogger.Trace( LogID.W_FAILED_GET_DIR_INFO, fun g -> g.Gen2( m_ObjID, m_ConfPath, x.Message ) )
                    [], x.Message
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                [], "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs( {
                    TargetDeviceID = rdir;
                    ErrorMessage = msg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Process "Create target device dir" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID where the working directory will be created.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.CreateTargetDeviceDir ( arg : HarukaCtrlerCtrlReq.T_CreateTargetDeviceDir ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "CreateTargetDeviceDir(%s)" tdidStr ) )
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Result = wresult;
                    ErrorMessage =  wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let dirName = Functions.AppendPathName m_ConfPath tdidStr
                let dirRegex = Constants.TARGET_DEVICE_DIR_NAME_REGOBJ

                // count current target device dir count
                let count =
                    Directory.GetDirectories m_ConfPath
                    |> Seq.filter ( Path.GetFileName >> dirRegex.IsMatch )
                    |> Seq.length
                if count >= Constants.MAX_TARGET_DEVICE_COUNT then
                    HLogger.Trace( LogID.W_TARGET_DEVICE_COUNT_OVER, fun g -> g.Gen1( m_ObjID, count.ToString() ) )
                    return sendMsg false "Number of target devicees exceeds limit."
                else
                    let! wr = Functions.CreateDirectoryAsync dirName
                    let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                    if Result.isError wr then
                        HLogger.Trace( LogID.W_FAILED_CREATE_DIR, fun g -> g.Gen2( m_ObjID, dirName, wmsg ) )
                    else
                        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "CreateTargetDeviceDir", "" ) )
                    return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            Task.FromResult( sendMsg false "Session ID mismatch" )

    /// <summary>
    ///  Process "Delete target device dir" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID where the working directory will be deleted.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.DeleteTargetDeviceDir ( arg : HarukaCtrlerCtrlReq.T_DeleteTargetDeviceDir ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "DeleteTargetDeviceDir(%s)" tdidStr ) )
        let dirName = Functions.AppendPathName m_ConfPath tdidStr
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let! wr = Functions.DeleteDirectoryRecursvelyAsync dirName
                let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_DELETE_DIR, fun g -> g.Gen2( m_ObjID, dirName, wmsg ) )
                else
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "DeleteTargetDeviceDir", "" ) )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "Get target device config" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID where the target group configuration string will be acquired.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.GetTargetDeviceConfig ( arg : HarukaCtrlerCtrlReq.T_GetTargetDeviceConfig ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "GetTargetDeviceConfig(%s)" tdidStr ) )
        let tdDirPath = Functions.AppendPathName m_ConfPath tdidStr
        let fName = Functions.AppendPathName tdDirPath Constants.TARGET_DEVICE_CONF_FILE_NAME
        let curStatus = m_MgrCliSessID
        
        let sendMsg ( wresult : string ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Config = wresult;
                    ErrorMessage = wmsg;
                })
            }
                
        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let! wr = Functions.ReadAllTextAsync fName
                let wresult, wmsg = Functions.GetOkValue "" wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_READ_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                else
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(Read length=%d)" wresult.Length
                        g.Gen2( m_ObjID, "DeleteTargetDeviceDir", msg )
                    )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            Task.FromResult( sendMsg "" "Session ID mismatch" )

    /// <summary>
    ///  Process "Create target device config" control request.
    /// </summary>
    /// <param name="tdConf">
    ///  The target device ID and the configuration which will be written.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.CreateTargetDeviceConfig ( tdConf : HarukaCtrlerCtrlReq.T_CreateTargetDeviceConfig ) : Task<string> =
        let tdid = tdConf.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "CreateTargetDeviceConfig(%s)" tdidStr ) )
        let tdDirPath = Functions.AppendPathName m_ConfPath tdidStr
        let fName = Functions.AppendPathName tdDirPath Constants.TARGET_DEVICE_CONF_FILE_NAME
        
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult( {
                    TargetDeviceID = tdConf.TargetDeviceID;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus tdConf.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( tdConf.SessionID, DateTime.UtcNow )
            task {
                let! wr = Functions.WriteAllTextAsync fName tdConf.Config
                let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_WRITE_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                else
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "CreateTargetDeviceConfig", "" ) )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "Get target group ID" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID where the target group IDs will be acquired.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.GetTargetGroupID ( arg : HarukaCtrlerCtrlReq.T_GetTargetGroupID ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "GetTargetGroupID(%s)" tdidStr ) )
        let dirName = Functions.AppendPathName m_ConfPath tdidStr
        let curStatus = m_MgrCliSessID
                        
        let r, msg =
            if Controller.CheckLoginStatus curStatus arg.SessionID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
                try
                    let tgRegex = Constants.TARGET_GRP_CONFIG_FILE_NAME_REGOBJ
                    let fNames = 
                        Directory.GetFiles dirName
                        |> Seq.map Path.GetFileName
                        |> Seq.filter tgRegex.IsMatch
                        |> Seq.sort
                        |> Seq.truncate Constants.MAX_TARGET_GROUP_COUNT_IN_TD
                        |> Seq.map tgid_me.fromString
                        |> Seq.toList
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(Target group count=%d)" fNames.Length
                        g.Gen2( m_ObjID, "GetTargetGroupID", msg )
                    )
                    fNames, ""
                with
                | x ->
                    HLogger.Trace( LogID.W_FAILED_GET_DIR_INFO, fun g -> g.Gen2( m_ObjID, dirName, x.Message ) )
                    [], x.Message
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                [], "Session ID mismatch"

        let rb = 
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( {
                    TargetDeviceID = arg.TargetDeviceID;
                    TargetGroupID = r;
                    ErrorMessage = msg;
                })
            }

        Task.FromResult rb

    /// <summary>
    ///  Process "Get target group config" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID and target group ID where the target group configuration string will be acquired.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.GetTargetGroupConfig ( arg : HarukaCtrlerCtrlReq.T_GetTargetGroupConfig ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        let tgid = arg.TargetGroupID
        let tgidStr = tgid_me.toString tgid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "GetTargetGroupConfig(TD:%s,TG:%s)" tdidStr tgidStr ) )
        let fName =
            tgidStr
            |> Functions.AppendPathName tdidStr
            |> Functions.AppendPathName m_ConfPath
        let curStatus = m_MgrCliSessID
        
        let sendMsg ( wresult : string ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig( {
                    TargetDeviceID = arg.TargetDeviceID;
                    TargetGroupID = arg.TargetGroupID;
                    Config = wresult;
                    ErrorMessage = wmsg;
                })
            }
                
        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let! wr = Functions.ReadAllTextAsync fName
                let wresult, wmsg = Functions.GetOkValue "" wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_READ_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                else
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(Read length=%d)" wresult.Length
                        g.Gen2( m_ObjID, "GetTargetGroupID", msg )
                    )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg "" "Session ID mismatch"
            |> Task.FromResult


    /// <summary>
    ///  Process "Get all target group config" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID and target group ID where the target group configuration string will be acquired.
    /// </param>
    /// <returns>
    ///  String that will be sent to management client.
    /// </returns>
    member private _.GetAllTargetGroupConfig ( arg : HarukaCtrlerCtrlReq.T_GetAllTargetGroupConfig ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "GetAllTargetGroupConfig(%s)" tdidStr ) )
        let dirName = Functions.AppendPathName m_ConfPath tdidStr
        let curStatus = m_MgrCliSessID
        
        let sendMsg ( wresult : HarukaCtrlerCtrlRes.T_TargetGroup list ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( {
                    TargetDeviceID = arg.TargetDeviceID;
                    TargetGroup = wresult;
                    ErrorMessage = wmsg;
                })
            }
                
        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                try
                    let tgRegex = Constants.TARGET_GRP_CONFIG_FILE_NAME_REGOBJ
                    let fNames = 
                        Directory.GetFiles dirName
                        |> Seq.filter ( Path.GetFileName >> tgRegex.IsMatch )
                        |> Seq.sort
                        |> Seq.truncate Constants.MAX_TARGET_GROUP_COUNT_IN_TD
                        |> Seq.toArray

                    let wcnt = Array.length fNames
                    let wv = Array.zeroCreate<HarukaCtrlerCtrlRes.T_TargetGroup>( wcnt )
                    for i = 0 to wcnt - 1 do
                        let! wr = Functions.ReadAllTextAsync fNames.[i]
                        match wr with
                        | Result.Ok( x ) ->
                            wv.[i] <- {
                                TargetGroupID = 
                                    fNames.[i]
                                    |> Path.GetFileName
                                    |> tgid_me.fromString
                                Config = x
                            }
                        | Result.Error( x ) ->
                            HLogger.Trace( LogID.W_FAILED_READ_CONF_FILE, fun g -> g.Gen2( m_ObjID, fNames.[i], x ) )
                            raise <| Exception( x )

                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(Read file count=%d)" wv.Length
                        g.Gen2( m_ObjID, "GetTargetGroupID", msg )
                    )
                    return sendMsg ( wv |> Array.toList ) ""
                with
                | _ as x ->
                    HLogger.Trace( LogID.W_FAILED_GET_DIR_INFO, fun g -> g.Gen2( m_ObjID, dirName, x.Message ) )
                    return sendMsg [] x.Message
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            Task.FromResult( sendMsg [] "Session ID mismatch" )


    /// <summary>
    ///  Process "Create target group config" control request.
    /// </summary>
    /// <param name="tgConf">
    ///  The target group configuration which will be written.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.CreateTargetGroupConfig ( tgConf : HarukaCtrlerCtrlReq.T_CreateTargetGroupConfig ) : Task<string> =
        let tdid = tgConf.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        let tgid = tgConf.TargetGroupID
        let tgidStr = tgid_me.toString tgid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "CreateTargetGroupConfig(TD:%s,TG:%s)" tdidStr tgidStr ) )
        let tdDirPath = Functions.AppendPathName m_ConfPath tdidStr
        let fName = Functions.AppendPathName tdDirPath tgidStr
        
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( {
                    TargetDeviceID = tgConf.TargetDeviceID;
                    TargetGroupID = tgConf.TargetGroupID;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus tgConf.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( tgConf.SessionID, DateTime.UtcNow )
            task {
                let tgRegex = Constants.TARGET_GRP_CONFIG_FILE_NAME_REGOBJ

                // count number of current target device dir
                let count =
                    try
                        Directory.GetFiles tdDirPath
                        |> Seq.filter ( Path.GetFileName >> tgRegex.IsMatch )
                        |> Seq.length
                    with
                    | :? DirectoryNotFoundException ->
                        0

                if count >= Constants.MAX_TARGET_GROUP_COUNT_IN_TD then
                    HLogger.Trace( LogID.W_TARGET_GROUP_COUNT_OVER, fun g -> g.Gen2( m_ObjID, tdidStr, count.ToString() ) )
                    return sendMsg false "Number of target group exceeds limit."
                else
                    let! wr = Functions.WriteAllTextAsync fName tgConf.Config
                    let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                    if Result.isError wr then
                        HLogger.Trace( LogID.W_FAILED_WRITE_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                    else
                        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetTargetGroupID", "" ) )
                    return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "Delete target group config" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID and target group ID where the target group configuration will be deleted.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.DeleteTargetGroupConfig ( arg : HarukaCtrlerCtrlReq.T_DeleteTargetGroupConfig ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        let tgid = arg.TargetGroupID
        let tgidStr = tgid_me.toString tgid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "DeleteTargetGroupConfig(TD:%s,TG:%s)" tdidStr tgidStr ) )
        let fName =
            tgidStr
            |> Functions.AppendPathName tdidStr
            |> Functions.AppendPathName m_ConfPath
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    TargetGroupID = arg.TargetGroupID;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let! wr = Functions.DeleteFileAsync fName
                let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                if Result.isError wr then
                    HLogger.Trace( LogID.W_FAILED_DELETE_CONF_FILE, fun g -> g.Gen2( m_ObjID, fName, wmsg ) )
                else
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "DeleteTargetGroupConfig", "" ) )
                return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "Get LU work dir" control request.
    /// </summary>
    /// <param name="arg">
    ///  The target device ID where the list of LUNs that working directory had been created will be acquired.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.GetLUWorkDir ( arg : HarukaCtrlerCtrlReq.T_GetLUWorkDir ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "GetLUWorkDir(%s)" tdidStr ) )
        let rx = Constants.LU_WORK_DIR_NAME_REGOBJ
        let dirName = Functions.AppendPathName m_ConfPath tdidStr
        let curStatus = m_MgrCliSessID

        let r, msg =
            if Controller.CheckLoginStatus curStatus arg.SessionID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
                try
                    let r =
                        dirName
                        |> Directory.GetDirectories
                        |> Seq.map Path.GetFileName
                        |> Seq.filter rx.IsMatch
                        |> Seq.map ( fun itr -> itr.[ Constants.LU_WORK_DIR_PREFIX.Length .. ] )
                        |> Seq.map ( lun_me.fromStringValue )
                        |> Seq.sort
                        |> Seq.truncate Constants.MAX_LOGICALUNIT_COUNT_IN_TD
                        |> Seq.toList
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                        let msg = sprintf "(DirCnt=%d)" r.Length
                        g.Gen2( m_ObjID, "DeleteTargetGroupConfig", msg )
                    )
                    r, ""
                with
                | x ->
                    HLogger.Trace( LogID.W_FAILED_GET_DIR_INFO, fun g -> g.Gen2( m_ObjID, dirName, x.Message ) )
                    [], x.Message
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                [], "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Name = r;
                    ErrorMessage = msg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Process "Create LU work dir" control request.
    /// </summary>
    /// <param name="arg">
    ///  The parameters of LU working directory.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.CreateLUWorkDir ( arg : HarukaCtrlerCtrlReq.T_CreateLUWorkDir ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        let lunStr = lun_me.toString arg.LUN
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "CreateLUWorkDir(TD:%s,LUN:%s)" tdidStr lunStr ) )
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    LUN = arg.LUN;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let tdDirName = Functions.AppendPathName m_ConfPath tdidStr
                let dirName = Functions.AppendPathName tdDirName ( lun_me.WorkDirName arg.LUN )
                let luRegex = Constants.LU_WORK_DIR_NAME_REGOBJ

                if not ( Directory.Exists tdDirName ) then
                    let wmsg = "Target device directory missing."
                    HLogger.Trace( LogID.W_FAILED_CREATE_DIR, fun g -> g.Gen2( m_ObjID, dirName, wmsg ) )
                    return sendMsg false wmsg
                else
                    // count number of LU dir
                    let count =
                        Directory.GetDirectories tdDirName
                        |> Seq.filter ( Path.GetFileName >> luRegex.IsMatch )
                        |> Seq.length

                    if count >= Constants.MAX_LOGICALUNIT_COUNT_IN_TD then
                        HLogger.Trace( LogID.W_LOGICAL_UNIT_COUNT_OVER, fun g -> g.Gen2( m_ObjID, tdidStr, count.ToString() ) )
                        return sendMsg false "Number of LU exceeds limit."
                    else
                        let! wr = Functions.CreateDirectoryAsync dirName
                        let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                        if Result.isError wr then
                            HLogger.Trace( LogID.W_FAILED_CREATE_DIR, fun g -> g.Gen2( m_ObjID, dirName, wmsg ) )
                        else
                            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "CreateLUWorkDir", "" ) )

                        return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult


    /// <summary>
    ///  Process "Delete LU work dir" control request.
    /// </summary>
    /// <param name="arg">
    ///  The parameters representing LU working directory which will be deleted.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.DeleteLUWorkDir ( arg : HarukaCtrlerCtrlReq.T_DeleteLUWorkDir ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        let lunStr = lun_me.toString arg.LUN
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "DeleteLUWorkDir(TD:%s,LUN:%s)" tdidStr lunStr ) )
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    LUN = arg.LUN;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let tdDirName = Functions.AppendPathName m_ConfPath tdidStr
                let dirName = Functions.AppendPathName tdDirName ( lun_me.WorkDirName arg.LUN )

                if not ( Directory.Exists tdDirName ) then
                    let wmsg = "Target device directory missing."
                    HLogger.Trace( LogID.W_FAILED_DELETE_DIR, fun g -> g.Gen2( m_ObjID, dirName, wmsg ) )
                    return sendMsg false wmsg
                else
                    let! wr = Functions.DeleteDirectoryRecursvelyAsync dirName
                    let wresult, wmsg = Result.isOk wr, Functions.GetErrorValue "" wr
                    if Result.isError wr then
                        HLogger.Trace( LogID.W_FAILED_DELETE_DIR, fun g -> g.Gen2( m_ObjID, dirName, wmsg ) )
                    else
                        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "DeleteLUWorkDir", "" ) )
                    return sendMsg wresult wmsg
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult


    /// <summary>
    ///  Process "Get target device procs" control request.
    /// </summary>
    /// <param name="sessID">
    ///  Session ID.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.GetTargetDeviceProcs ( sessID : CtrlSessionID ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetTargetDeviceProcs" ) )
        let curStatus = m_MgrCliSessID

        let tdIDs, msg  =
            if Controller.CheckLoginStatus curStatus sessID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( sessID, DateTime.UtcNow )
                let resultList =
                    m_TargetDeviceProcs.Keys
                    |> Seq.sort
                    |> Seq.truncate Constants.MAX_TARGET_DEVICE_COUNT
                    |> Seq.toList
                HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                    let msg = sprintf "(ProcCnt=%d)" resultList.Length
                    g.Gen2( m_ObjID, "GetTargetDeviceProcs", msg )
                )
                ( resultList, "" )
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                [], "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( {
                    TargetDeviceID = tdIDs;
                    ErrorMessage = msg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Process "Kill target device procs" control request.
    /// </summary>
    /// <param name="arg">
    ///  Target device ID that will be killed.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.KillTargetDeviceProc ( arg : HarukaCtrlerCtrlReq.T_KillTargetDeviceProc ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "KillTargetDeviceProc(%s)" tdidStr ) )
        let curStatus = m_MgrCliSessID

        let sendMsg ( wresult : bool ) ( wmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            task {
                let r, pinfo = m_TargetDeviceProcs.TryGetValue( arg.TargetDeviceID )
                if not r then
                    // If specified process is not exist, it consider success killing child process.
                    HLogger.Trace( LogID.I_TARGET_DEVICE_PROC_MISSING, fun g -> g.Gen1( m_ObjID, tdidStr ) )
                    return sendMsg true ""
                else
                    // Remove process info. If this entry is removed, restart will not to do.
                    m_TargetDeviceProcs.Remove( arg.TargetDeviceID ) |> ignore

                    // terminate process
                    pinfo.m_Proc.Kill( true )
                    pinfo.m_Proc.Dispose()

                    HLogger.Trace( LogID.I_KILLED_TARGET_DEVICE_PROC, fun g ->
                        let pidStr = pinfo.m_Proc.Id |> sprintf "%d"
                        g.Gen2( m_ObjID, tdidStr, pidStr )
                    )
                    return sendMsg true ""
            }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendMsg false "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "Start target device procs" control request.
    /// </summary>
    /// <param name="arg">
    ///  Target device ID that will be started.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private this.StartTargetDeviceProc ( arg : HarukaCtrlerCtrlReq.T_StartTargetDeviceProc ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "StartTargetDeviceProc(%s)" tdidStr ) )
        let curStatus = m_MgrCliSessID

        let wresult, wmsg =
            if Controller.CheckLoginStatus curStatus arg.SessionID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
                match this.StartNewTDProcessAndAddEntry arg.TargetDeviceID with
                | Ok() ->
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetTargetDeviceProcs", "" ) )
                    true, ""
                | Error( x ) ->
                    // The error message is already written in StartNewTDProcessAndAddEntry function.
                    false, x
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                false, "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Transfer control request to specified target device process.
    /// </summary>
    /// <param name="arg">
    ///  The target device controll request that will be transfered to specified target device process.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private _.TargetDeviceCtrlRequest ( arg : HarukaCtrlerCtrlReq.T_TargetDeviceCtrlRequest ) : Task<string> =
        let tdid = arg.TargetDeviceID
        let tdidStr = tdid_me.toString tdid
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "TargetDeviceCtrlRequest(%s)" tdidStr ) )
        let curStatus = m_MgrCliSessID

        let sendResBytes ( wresult : string ) ( errmsg : string ) =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse( {
                    TargetDeviceID = arg.TargetDeviceID;
                    Response = wresult;
                    ErrorMessage = errmsg;
                })
            }

        if Controller.CheckLoginStatus curStatus arg.SessionID then
            m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
            let r, pinfo = m_TargetDeviceProcs.TryGetValue( arg.TargetDeviceID )
            if not r then
                // If specified process is not exist
                HLogger.Trace( LogID.W_TARGET_DEVICE_PROC_MISSING, fun g -> g.Gen1( m_ObjID, tdidStr ) )
                sendResBytes "" "Specified target device missing."
                |> Task.FromResult
            else
                // tranfer request string to specified process
                task {
                    let! resStr, wmsg =
                        task {
                            try
                                // Send request
                                do! pinfo.m_Proc.StandardInput.WriteLineAsync arg.Request
                                // receive response
                                let! resStr = pinfo.m_Proc.StandardOutput.ReadLineAsync()
                                HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "TargetDeviceCtrlRequest", "" ) )
                                return ( resStr, "" )
                            with
                            | _ as x ->
                                // If unexpected error occurred, terminate the target device process.
                                HLogger.Trace( LogID.E_FAILED_SEND_REQUEST_TO_TD, fun g ->
                                    let pidStr = pinfo.m_Proc.Id |> sprintf "%d"
                                    g.Gen3( m_ObjID, tdidStr, pidStr, x.Message )
                                )
                                pinfo.m_Proc.Kill( true )
                                return ( "", "Error:" + x.Message )
                        }
                    return sendResBytes resStr wmsg
                }
        else
            HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
            sendResBytes "" "Session ID mismatch"
            |> Task.FromResult

    /// <summary>
    ///  Process "Create Media File" control request.
    /// </summary>
    /// <param name="arg">
    ///  Media creation arguments.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private this.CreateMediaFile ( arg : HarukaCtrlerCtrlReq.T_CreateMediaFile ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "CreateMediaFile"  ) )
        let curStatus = m_MgrCliSessID

        let wresult, pid, wmsg =
            if Controller.CheckLoginStatus curStatus arg.SessionID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
                let now = DateTime.UtcNow

                // delete terminated media creation proc entry.
                m_InitMediaProcs
                |> Seq.filter ( fun itr ->
                    match itr.Value.Progress with
                    | NormalEnd _ -> true
                    | AbnormalEnd _ -> true
                    | _ -> ( ( now - itr.Value.CreatedTime ).TotalSeconds > Constants.INITMEDIA_MAX_REMAIN_TIME )
                )
                |> Seq.iter ( fun itr ->
                    HLogger.Trace( LogID.I_INITMEDIA_PROC_ENTRY_REMOVED, fun g -> g.Gen1( m_ObjID, sprintf "%d" itr.Key ) )
                    itr.Value.Kill()
                    m_InitMediaProcs.Remove itr.Key |> ignore
                )

                if m_InitMediaProcs.Count >= Constants.INITMEDIA_MAX_MULTIPLICITY then
                    HLogger.Trace( LogID.W_INITMEDIA_PROC_MULTIPLICITY_OV, fun g -> g.Gen0 m_ObjID )
                    false, 0UL, "Maximum multiplicity of media creation process exceeded."
                else
                    let p = new MediaCreateProc( arg.MediaType, m_ConfPath, m_IM_ExePath )
                    m_InitMediaProcs.Add( p.ProcIdentfier, p )
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "CreateMediaFile", "" ) )
                    true, p.ProcIdentfier, ""
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                false, 0UL, "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult( {
                    Result = wresult;
                    ProcID = pid;
                    ErrorMessage = wmsg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Process "Create Media File" control request.
    /// </summary>
    /// <param name="sessID">
    ///  Session ID.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private this.GetInitMediaStatus ( sessID : CtrlSessionID ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetInitMediaStatus"  ) )
        let curStatus = m_MgrCliSessID

        let rlist, wmsg =
            if Controller.CheckLoginStatus curStatus sessID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( sessID, DateTime.UtcNow )

                let rstat : HarukaCtrlerCtrlRes.T_Procs list =
                    m_InitMediaProcs
                    |> Seq.map ( fun itr ->
                        {
                            ProcID = itr.Key;
                            PathName = itr.Value.PathName;
                            FileType = itr.Value.FileTypeStr;
                            Status = 
                                match itr.Value.Progress with
                                | MC_PROGRESS.NotStarted ->
                                    HarukaCtrlerCtrlRes.U_NotStarted()
                                | MC_PROGRESS.ProgressCreation( x ) ->
                                    HarukaCtrlerCtrlRes.U_ProgressCreation( x )
                                | MC_PROGRESS.Recovery( x ) ->
                                    HarukaCtrlerCtrlRes.U_Recovery( x )
                                | MC_PROGRESS.NormalEnd( _ ) ->
                                    HarukaCtrlerCtrlRes.U_NormalEnd()
                                | MC_PROGRESS.AbnormalEnd( _ ) ->
                                    HarukaCtrlerCtrlRes.U_AbnormalEnd()
                            ErrorMessage =
                                itr.Value.ErrorMessages
                                |> Seq.toList
                        } : HarukaCtrlerCtrlRes.T_Procs
                    )
                    |> Seq.toList

                // delete terminated media creation proc entry.
                let now = DateTime.UtcNow
                m_InitMediaProcs
                |> Seq.filter ( fun itr ->
                    match itr.Value.Progress with
                    | NormalEnd _ -> true
                    | AbnormalEnd _ -> true
                    | _ -> ( ( now - itr.Value.CreatedTime ).TotalSeconds > Constants.INITMEDIA_MAX_REMAIN_TIME )
                )
                |> Seq.iter ( fun itr ->
                    HLogger.Trace( LogID.I_INITMEDIA_PROC_ENTRY_REMOVED, fun g -> g.Gen1( m_ObjID, sprintf "%d" itr.Key ) )
                    itr.Value.Kill()
                    m_InitMediaProcs.Remove itr.Key |> ignore
                )

                HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                    let msg = sprintf "(StatCount=%d)" rstat.Length
                    g.Gen2( m_ObjID, "GetInitMediaStatus", msg )
                )
                rstat, ""
            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                [], "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( {
                    Procs = rlist;
                    ErrorMessage = wmsg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Process "Cancel Media Creation" control request.
    /// </summary>
    /// <param name="arg">
    ///  arguments of a media creation process that will be terminated.
    /// </param>
    /// <returns>
    ///  Bytes array that will be sent to management client.
    /// </returns>
    member private this.KillInitMediaProc ( arg : HarukaCtrlerCtrlReq.T_KillInitMediaProc ) : Task<string> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "KillInitMediaProc"  ) )
        let curStatus = m_MgrCliSessID

        let wresult, wmsg =
            if Controller.CheckLoginStatus curStatus arg.SessionID then
                m_MgrCliSessID <- MgrCliSessionStatus.LoggedIn( arg.SessionID, DateTime.UtcNow )
                let now = DateTime.UtcNow

                // delete terminated media creation proc entry.
                m_InitMediaProcs
                |> Seq.filter ( fun itr ->
                    match itr.Value.Progress with
                    | NormalEnd _ -> true
                    | AbnormalEnd _ -> true
                    | _ -> ( ( now - itr.Value.CreatedTime ).TotalSeconds > Constants.INITMEDIA_MAX_REMAIN_TIME )
                )
                |> Seq.iter ( fun itr ->
                    HLogger.Trace( LogID.I_INITMEDIA_PROC_ENTRY_REMOVED, fun g -> g.Gen1( m_ObjID, sprintf "%d" itr.Key ) )
                    itr.Value.Kill()
                    m_InitMediaProcs.Remove itr.Key |> ignore
                )

                if not ( m_InitMediaProcs.ContainsKey arg.ProcID ) then
                    HLogger.Trace( LogID.W_INITMEDIA_PROC_MISSING, fun g ->
                        let msg = sprintf "%d" arg.ProcID
                        g.Gen1( m_ObjID, msg )
                    )
                    false, "Specified process is missing."
                else
                    let i = m_InitMediaProcs.[ arg.ProcID ]
                    i.Kill()
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "KillInitMediaProc", "" ) )
                    true, ""

            else
                HLogger.Trace( LogID.W_MGR_CLI_SESSION_ID_MISMATCH, fun g -> g.Gen0 m_ObjID )
                false, "Session ID mismatch"

        let rb =
            HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                Response = HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( {
                    Result = wresult;
                    ErrorMessage = wmsg;
                })
            }
        Task.FromResult rb

    /// <summary>
    ///  Start new target device process and add to process entry.
    /// </summary>
    /// <param name="tdid">
    ///  Target device ID.
    /// </param>
    /// <returns>
    ///  If succeed to start new process, returns Ok(), otherwise error message.
    /// </returns>
    member private this.StartNewTDProcessAndAddEntry ( tdid : TDID_T ) : Result<unit,string> =
        let tdidStr = tdid_me.toString tdid
        let dirName = Functions.AppendPathName m_ConfPath tdidStr

        let r, pinfo = m_TargetDeviceProcs.TryGetValue( tdid )
        let procCount = m_TargetDeviceProcs.Count
        if r then
            // If specified target device already started, it consider an error.
            HLogger.Trace( LogID.W_TD_PROC_ALREADY_STARTED, fun g ->
                let pidStr = pinfo.m_Proc.Id |> sprintf "%d"
                g.Gen2( m_ObjID, tdidStr, pidStr )
            )
            Error( "Target device already started." )
        elif not ( Directory.Exists dirName ) then
            // If target device working directory is not exist, it consider an error.
            HLogger.Trace( LogID.W_TD_WORK_DIR_MISSING, fun g -> g.Gen1( m_ObjID, tdidStr ) )
            Error( "Target device working directory missing." )
        elif procCount >= Constants.MAX_TARGET_DEVICE_COUNT then
            // If target device working directory is not exist, it consider an error.
            HLogger.Trace( LogID.W_TARGET_DEVICE_COUNT_OVER, fun g -> g.Gen1( m_ObjID, procCount.ToString() ) )
            Error( "Number of target devicees exceeds limit." )
        else
            // Create Process structure
            let p = new Process(
                StartInfo = ProcessStartInfo(
                    FileName = m_TD_ExePath,
                    Arguments = "\"" + tdidStr + "\"",
                    CreateNoWindow = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = m_ConfPath
                ),
                EnableRaisingEvents = true
            )

            // register OnExit function
            p.Exited.Add ( fun _ -> this.OnExitChildProc tdid )

            // Start
            try
                HLogger.Trace( LogID.I_TRY_START_TD_PROC, fun g -> g.Gen1( m_ObjID, tdidStr ) )
                if p.Start() then
                    HLogger.Trace( LogID.I_TARGET_DEVICE_PROC_STARTED, fun g ->
                        let pidStr = p.Id |> sprintf "%d"
                        g.Gen2( m_ObjID, tdidStr, pidStr )
                    )

                    let utcNow = DateTime.UtcNow
                    let pinfo = {
                        m_Proc = p;
                        m_LastStartTime = utcNow.Ticks / 600000000L;
                        m_RestartCount = 0;
                    }
                    m_TargetDeviceProcs.AddOrUpdate( tdid, pinfo, fun k o -> pinfo ) |> ignore
                    m_logAgr.AddChild( p.StandardError )
                    Ok()
                else
                    p.Dispose()
                    HLogger.Trace( LogID.W_FAILED_START_TARGET_DEVICE_PROC, fun g -> g.Gen1( m_ObjID, tdidStr ) )
                    Error( "Failed to start target device proc." )
            with
            | _ as x ->
                p.Dispose()
                HLogger.Trace( LogID.W_FAILED_START_TARGET_DEVICE_PROC, fun g -> g.Gen1( m_ObjID, tdidStr ) )
                Error( "Failed to start target device proc. Msaage=" + x.Message )

    //-------------------------------------------------------------------------
    // static method

    /// <summary>
    ///  Load controller configurations or create default configurations.
    /// </summary>
    /// <param name="objID">
    ///  Object ID that is used to log output.
    /// </param>
    /// <param name="confPath">
    ///  Directory name that store configuration files.
    /// </param>
    /// <returns>
    ///  Loaded or default configurations.
    /// </returns>
    /// <remarks>
    ///  If failed to load configuration file,
    ///  it raise an exception.
    /// </remarks>
    static member private LoadConfig ( objID : OBJIDX_T ) ( confPath : string ) : HarukaCtrlConf.T_HarukaCtrl =
        let fname = Functions.AppendPathName confPath Constants.CONTROLLER_CONF_FILE_NAME
        
        // If specified directory or controller configuration file is not exist,
        // create default configuration files.
        if not ( Directory.Exists confPath ) || not ( File.Exists fname ) then
            HLogger.Trace( LogID.F_MISSING_CONTROLLER_CONF_FILE, fun g -> g.Gen1( objID, fname ) )
            raise <| Exception( "Missing controller configuration file." )

        let conf = HarukaCtrlConf.ReaderWriter.LoadFile fname
        HLogger.Trace( LogID.I_CONTROLLER_CONF_FILE_LOADED, fun g -> g.Gen1( objID, fname ) )
        conf

    /// <summary>
    ///  Check if a session is valid.
    /// </summary>
    static member private CheckLoginStatus ( cur : MgrCliSessionStatus ) ( sessID : CtrlSessionID ) =
        match cur with
        | UnUsed ->
            false
        | LoggedIn( efID, _ ) ->
            efID = sessID
