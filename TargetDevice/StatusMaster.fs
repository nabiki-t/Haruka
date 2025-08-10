//=============================================================================
// Haruka Software Storage.
// StatusMaster.fs : Defines StatusMaster class
// StatusMaster class has the status that shred in all of Haruka process.
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
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.Immutable

open Haruka
open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.Media
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// <summary>
///   Implementation of StatusMaster component.
/// </summary>
/// <param name="m_WorkDirPath">
///   path name of working directory specified at main arguments.
/// </param>
/// <param name="m_Killer">
///   Killer object.
/// </param>
type StatusMaster(
    m_WorkDirPath : string,
    m_Killer : IKiller,
    m_CtrlReqSource : TextReader,
    m_CtrlReqSink : TextWriter
) as this = 

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// Instance of ConfigurationMaster
    /// In the Haruka process, only one instance of ConfigurationMaster cmoponent is exist.
    let m_config =
        new ConfigurationMaster( m_WorkDirPath, m_Killer ) :> IConfiguration

    /// Component instance of TCPPort.
    /// Lifetime of TCPPort component is managed by StatusMaster 
    let m_ports =
        StatusMaster.CreatePortObj m_config ( this :> IStatus ) m_Killer m_ObjID

    /// Collection of the session objects. Indexed by TSIH.
    let m_Sessions =
        OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty )

    /// LU objects.
    let m_LU = ConcurrentDictionary< LUN_T, Lazy<ILU> >()

    /// New TSIH generator.
    let mutable m_newTSIHGen = 0L

    /// currently enabled target group ID
    let m_ActiveTargetGroups =
        let m = new ConcurrentDictionary< TGID_T, unit >()
        for ( conf , _ ) in m_config.GetAllTargetGroupConf() do
            if conf.EnabledAtStart then
                let tgid = conf.TargetGroupID
                m.TryAdd( tgid, () ) |> ignore
                HLogger.Trace( LogID.I_TARGET_GROUP_ACTIVATED, fun g -> g.Gen1( m_ObjID, tgid_me.toString tgid ) )
        m

    /// Timer object
    let m_Timer = new Timer( this.OnTimer, (), 1000, 1000 )

    do
        // set default log parameters
        let struct( s, h, l ) = m_config.GetDefaultLogParameters()
        HLogger.SetLogParameters( s, h, Constants.DEFAULT_LOG_OUTPUT_CYCLE, l, stderr )

        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, "StatusMaster", "" ) )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IStatus with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit = 
            // clear active target groups list
            m_ActiveTargetGroups.Clear()

            let empObj = ImmutableDictionary< TSIH_T, ISession >.Empty
            let oldSess =
                m_Sessions.Update( fun oldSess ->
                    struct( empObj, oldSess )
                )
            // Terminate all of sessions
            for itr in oldSess do
                itr.Value.DestroySession()

            // Terminate all of object in target groups
            // *** The same applies to LU as above. ***
            for itr in m_config.GetTargetGroupID() do
                m_config.UnloadTargetGroup itr

            // Stop timer object
            m_Timer.Dispose()

        // --------------------------------------------------------------------
        // Implementation of IStatus.GetNetworkPortal
        override _.GetNetworkPortal() : TargetDeviceConf.T_NetworkPortal list =
            m_config.GetNetworkPortal()

        // ------------------------------------------------------------------------
        // Implementation of IStatus.GetActiveTargetGroup
        override _.GetActiveTargetGroup() : TargetGroupConf.T_TargetGroup list =
            m_ActiveTargetGroups.Keys
            |> Seq.choose m_config.GetTargetGroupConf
            |> Seq.toList

        // ------------------------------------------------------------------------
        // Implementation of IStatus.GetActiveTarget
        override this.GetActiveTarget() : TargetGroupConf.T_Target list =
            ( this :> IStatus ).GetActiveTargetGroup()
            |> Seq.map _.Target
            |> Seq.concat
            |> Seq.toList

        // ------------------------------------------------------------------------
        // Implementation of IStatus.GetTargetFromLUN
        override _.GetTargetFromLUN ( lun : LUN_T ) : TargetGroupConf.T_Target list =
            // Get all of loaded target configuration
            let alltarget =
                m_config.GetAllTargetGroupConf()
                |> Seq.map fst
                |> Seq.map _.Target
                |> Seq.concat

            if lun = lun_me.zero then
                // LUN 0 can be accessed from all of targets.
                alltarget
                |> Seq.toList
            else
                alltarget
                |> Seq.filter ( fun itr -> ( Seq.exists( (=) lun ) itr.LUN ) )
                |> Seq.toList

        // ------------------------------------------------------------------------
        // Implementation of IStatus.IscsiNegoParamCO
        override _.IscsiNegoParamCO : IscsiNegoParamCO =
            m_config.IscsiNegoParamCO

        // ------------------------------------------------------------------------
        // Implementation of IStatus.IscsiNegoParamCO
        override _.IscsiNegoParamSW : IscsiNegoParamSW =
            m_config.IscsiNegoParamSW

        // --------------------------------------------------------------------
        // Implementation of IStatus.CreateLoginNegociator
        override this.CreateLoginNegociator ( sock : System.Net.Sockets.NetworkStream ) ( conTime : DateTime ) ( targetPortalGroupTag : TPGT_T ) ( netPortIdx : NETPORTIDX_T ) : ILoginNegociator =
            new LoginNegociator( this, sock, conTime, targetPortalGroupTag, netPortIdx, new HKiller() :> IKiller ) :> ILoginNegociator

        // --------------------------------------------------------------------
        // Implementation of IStatus.GetTSIH
        override _.GetTSIH ( argI_TNexus : ITNexus ) : TSIH_T =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.GetTSIH" ) )

            // Search TSIH from I_T Nexus by sequential seach.
            let result = 
                let r = m_Sessions.obj
                r |> Seq.tryFind ( fun itr -> ITNexus.Equals( argI_TNexus, itr.Value.I_TNexus ) )
            if result.IsNone then
                tsih_me.zero
            else
                result |> Option.get |> _.Key

        // --------------------------------------------------------------------
        // Implementation of IStatus.GenNewTSIH
        override _.GenNewTSIH() : TSIH_T =
            let rSess = m_Sessions.obj
            let rec loop ( cnt : int ) =

                // Get next candidate value
                let next = 
                    Interlocked.Increment( &m_newTSIHGen )
                    |> uint16
                    |> tsih_me.fromPrim
            
                // Check for generated value is valid and not used.
                if next = tsih_me.zero || rSess.ContainsKey( next ) then
                    // If generated value is invalid or used, try to next value
                    if cnt < 65536 then
                        loop ( cnt + 1 )
                    else
                        // Failed to generate new TSIH
                        tsih_me.zero
                else
                    // Generated value is valid.
                    next
         
            // try to generate new TSIH
            loop 0

        // --------------------------------------------------------------------
        // Implementation of IStatus.GetSession
        override _.GetSession ( tsih : TSIH_T ) : ISession voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.GetSession" ) )

            let result =
                m_Sessions.Update( fun oldSess ->
                    // Search session component
                    match oldSess.TryGetValue( tsih ) with
                    | false, _ ->
                        struct( oldSess, ValueNone )
                    | true, session ->
                        // Check searched session is still in alive.
                        if session.IsAlive then
                            struct( oldSess, ValueSome( session ) )
                        else
                            // Unluckily, in between above check and following removing action,
                            // if an effective session and TSIH is (re-)created, that session is unfairly dropped.
                            // But this event is too rarely, I ignore this possibility.
                            let newSess = oldSess.Remove tsih
                            struct( newSess, ValueNone )
                )
            if result.IsNone then
                HLogger.Trace( LogID.E_MISSING_SESSION, fun g -> g.Gen0 m_ObjID )
            result

        // --------------------------------------------------------------------
        // Implementation of IStatus.GetITNexusFromLUN
        override _.GetITNexusFromLUN ( lun : LUN_T ) : ITNexus[] =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.GetITNexusFromLUN" ) )
            let r = m_Sessions.obj
            r
            |> Seq.choose ( fun itr ->
                let r =
                    itr.Value.SCSITaskRouter.GetLUNs()
                    |> Array.exists( (=) lun )
                if r then
                    Some itr.Value.I_TNexus
                else
                    None
            )
            |> Seq.toArray
           
        // --------------------------------------------------------------------
        // Implementation of IStatus.CreateNewSession
        override this.CreateNewSession ( argI_TNexus : ITNexus ) ( argTSIH : TSIH_T ) ( sessionParameter : IscsiNegoParamSW ) ( newCmdSN : CMDSN_T ) : ISession voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.CreateNewSession" ) )

            fun ( oldSess : ImmutableDictionary< TSIH_T, ISession > ) ( retryStat : IKiller ValueOption ) ->

                // Destroy created session object, and retry to add new session.
                // Normally, a session is constructed and destroyed multiple times,
                // so there is no problem in rebuilding the session object.
                if retryStat.IsSome then
                    retryStat.Value.NoticeTerminate()

                if this.CheckSessionCountUpperLimit oldSess argI_TNexus sessionParameter |> not then
                    struct( oldSess, ValueNone, ValueNone )

                // Check TSIH value duplicate
                elif oldSess.ContainsKey argTSIH then
                    HLogger.Trace( LogID.E_FAILED_CREATE_SESSION, fun g ->
                        let msg = sprintf "Specified TSIH(%d) is already exist." ( tsih_me.toPrim argTSIH )
                        g.Gen2( m_ObjID, argI_TNexus.I_TNexusStr, msg )
                    )
                    struct( oldSess, ValueNone, ValueNone )

                // Check for there is not exists the session having same I_T Nexus identifier.
                elif oldSess |> Seq.tryFind ( fun itr -> argI_TNexus.Equals( itr.Value.I_TNexus ) ) |> Option.isSome then
                    HLogger.Trace( LogID.E_FAILED_CREATE_SESSION, fun g -> g.Gen2( m_ObjID, argI_TNexus.I_TNexusStr, "Specified I_T Nexus is already exist." ) )
                    struct( oldSess, ValueNone, ValueNone )

                else
                    // Create new session object
                    let k = new HKiller() :> IKiller
                    let session =
                        new Session( this :> IStatus, DateTime.UtcNow, argI_TNexus, argTSIH, sessionParameter, newCmdSN, k ) :> ISession

                    let newSess = oldSess.Add( argTSIH, session )
                    HLogger.Trace(
                        LogID.I_CREATE_NEW_SESSION,
                        fun g -> g.Gen2( m_ObjID, ValueNone, ValueNone, ValueSome argTSIH, ValueNone, ValueNone, argI_TNexus.I_TNexusStr, argTSIH )
                    )
                    struct( newSess, ValueSome k, ValueSome session )

            |> m_Sessions.Update< IKiller, ISession voption >

(*
        // --------------------------------------------------------------------
        // Implementation of IStatus.ReinstateSession
        override this.ReinstateSession ( argI_TNexus : ITNexus ) ( oldTsih : TSIH_T ) ( newTsih : TSIH_T ) ( sessionParameter : IscsiNegoParamSW ) ( newCmdSN : CMDSN_T ) : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.ReinstateSession" ) )

            // Remove old session object
            let r, wSess = m_Sessions.obj.TryGetValue( oldTsih )
            if r then wSess.DestroySession()

            // create new session
            ( this :> IStatus ).CreateNewSession argI_TNexus newTsih sessionParameter newCmdSN
*)
        // --------------------------------------------------------------------
        // Implementation of IStatus.ReinstateSession
        override _.RemoveSession ( tsih : TSIH_T ) : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.RemoveSession" ) )

            m_Sessions.Update( fun ( oldVal : ImmutableDictionary< TSIH_T, ISession > ) ->
                // This method is called after a session has been destroyed, but depending on the timing,
                // it may be called after ReinstateSession method with the same TSIH.
                let r, s = oldVal.TryGetValue tsih
                if not r then
                    HLogger.Trace( LogID.I_SESSION_REMOVED , fun g -> g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome tsih, ValueNone, ValueNone ) )
                    oldVal
                elif not s.IsAlive then
                    HLogger.Trace( LogID.I_SESSION_REMOVED , fun g -> g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome tsih, ValueNone, ValueNone ) )
                    oldVal.Remove tsih
                else
                    HLogger.Trace( LogID.I_SESSION_ALREADY_REINSTATED , fun g -> g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome tsih, ValueNone, ValueNone ) )
                    oldVal
            )
            |> ignore

        // ------------------------------------------------------------------------
        // Implementation of IStatus.GetLU
        override this.GetLU ( argLUN : LUN_T ) : ILU voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.GetLU" ) )

            let luConfigs =
                [
                    for ( conf, k ) in m_config.GetAllTargetGroupConf() do
                        for itr in conf.LogicalUnit do
                            yield struct ( itr, k )
                ]
            let tryGetLUObject() =
                try
                    m_LU.GetOrAdd(
                        argLUN,
                        ( fun lun ->
                            // If not exist, create new LU object and return created new object.
                            let struct ( luinfo, wKiller ) : struct ( TargetGroupConf.T_LogicalUnit * IKiller ) =
                                if lun = lun_me.zero then
                                    struct (
                                        {
                                            LUN = lun;
                                            LUName = "LUN_0 Logical unit";
                                            WorkPath = "";
                                            LUDevice = TargetGroupConf.U_DummyDevice()
                                        },
                                        // LUN 0 belongs to target device, so killer object of status master is used for LUN 0.
                                        m_Killer
                                    )
                                else
                                    luConfigs |> List.find ( fun struct ( conf, _ ) -> conf.LUN = lun )
                            assert( luinfo.LUN = argLUN )

                            match luinfo.LUDevice with
                            | TargetGroupConf.T_DEVICE.U_BlockDevice( x ) ->
                                lazy
                                    let o = new BlockDeviceLU( BlockDeviceType.BDT_Normal, this, luinfo.LUN, x, luinfo.WorkPath, wKiller ) :> ILU
                                    HLogger.Trace( LogID.I_CREATE_LU_COMPONENT, fun g -> g.Gen0 m_ObjID )
                                    o
                            | TargetGroupConf.T_DEVICE.U_DummyDevice( _ ) ->
                                lazy
                                    let dummyDeviceConf : TargetGroupConf.T_BlockDevice = {
                                        Peripheral = TargetGroupConf.T_MEDIA.U_DummyMedia({
                                            IdentNumber = mediaidx_me.zero;
                                            MediaName = "";
                                        })
                                    }
                                    let o = new BlockDeviceLU( BlockDeviceType.BDT_Dummy, this, luinfo.LUN, dummyDeviceConf, luinfo.WorkPath, wKiller ) :> ILU
                                    HLogger.Trace( LogID.I_CREATE_LU_COMPONENT, fun g -> g.Gen0 m_ObjID )
                                    o
                        )
                    )
                    |> ValueSome
                with
                | :? KeyNotFoundException as x ->
                    HLogger.Trace( LogID.E_FAILED_CREATE_LU, fun g -> g.Gen1( m_ObjID, "Specified LUN is unknown." ) )
                    ValueNone
                | _ as x ->
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                    ValueNone

            let rec loop cnt =
                if cnt < 100 then
                    match tryGetLUObject() with
                    | ValueNone ->
                        ValueNone
                    | ValueSome( rLU ) ->
                        if rLU.Value.LUResetStatus then
                            // Depending on the timing, it maybe LU is going to perform LU reset.
                            // In this case, wait for a time and retry to create LU object.
                            Thread.Sleep 10
                            loop ( cnt + 1 )
                        else
                            ValueSome( rLU.Value )
                else
                    HLogger.Trace( LogID.E_LU_CREATE_RETRY_OVER, fun g -> g.Gen0 m_ObjID )
                    ValueNone

            loop 0

        // ------------------------------------------------------------------------
        // Implementation of IStatus.CreateMedia
        override this.CreateMedia  ( confInfo : TargetGroupConf.T_MEDIA ) ( lun : LUN_T ) ( argKiller : IKiller ) : IMedia =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, "StatusMaster.CreateMedia" ) )

            let r =
                match confInfo with
                | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
                    new PlainFileMedia( this, x, argKiller, lun ) :> IMedia
                | TargetGroupConf.T_MEDIA.U_MemBuffer( x ) ->
                    new MemBufferMedia( this, x, argKiller, lun, Constants.MEMBUFFER_BUF_LINE_BLOCK_SIZE, Constants.MEDIA_BLOCK_SIZE ) :> IMedia
                | TargetGroupConf.T_MEDIA.U_DummyMedia( _ ) ->
                    new DummyMedia( argKiller, lun ) :> IMedia
                | TargetGroupConf.T_MEDIA.U_DebugMedia( x ) ->
                    new DebugMedia( this, x, argKiller, lun ) :> IMedia
            r.Initialize()
            r

        // ------------------------------------------------------------------------
        // Notify Logical Unit Reset.
        override _.NotifyLUReset ( lun : LUN_T ) ( lu : ILU ) : unit =
            // Remove resetted old LU
            let r, _ = m_LU.TryRemove( lun )
            if r then
                HLogger.Trace( LogID.I_LU_REMOVED, fun g -> g.Gen1( m_ObjID, lun_me.toString lun ) )
            else
                HLogger.Trace( LogID.I_LU_ALREADY_DELETED, fun g -> g.Gen1( m_ObjID, lun_me.toString lun ) )

        // ------------------------------------------------------------------------
        // Process parent control request.
        override this.ProcessControlRequest () : Task =
            task {
                HLogger.Trace( LogID.I_ENTER_CTRL_REQ_LOOP, fun g -> g.Gen0 m_ObjID )
                let mutable flg = true
                try
                    while flg && ( not m_Killer.IsNoticed ) do
                        let! lineStr = m_CtrlReqSource.ReadLineAsync()
                        if lineStr = null then
                            // If parent pipe is closed, tarminate child process
                            flg <- false
                            HLogger.Trace( LogID.W_DETECT_CTRL_PROC_TERMINATED, fun g -> g.Gen0 m_ObjID )
                        else
                            if not m_Killer.IsNoticed then
                                try
                                    let r = TargetDeviceCtrlReq.ReaderWriter.LoadString lineStr
                                    match r.Request with
                                    | TargetDeviceCtrlReq.U_GetActiveTargetGroups( _ ) ->
                                        do! this.ProcReq_GetActiveTargetGroups()
                                    | TargetDeviceCtrlReq.U_GetLoadedTargetGroups( _ ) ->
                                        do! this.ProcReq_GetLoadedTargetGroups()
                                    | TargetDeviceCtrlReq.U_InactivateTargetGroup( x ) ->
                                        do! this.ProcReq_InactivateTargetGroup x
                                    | TargetDeviceCtrlReq.U_ActivateTargetGroup( x ) ->
                                        do! this.ProcReq_ActivateTargetGroup x
                                    | TargetDeviceCtrlReq.U_UnloadTargetGroup( x ) ->
                                        do! this.ProcReq_UnloadTargetGroup x
                                    | TargetDeviceCtrlReq.U_LoadTargetGroup( x ) ->
                                        do! this.ProcReq_LoadTargetGroup x
                                    | TargetDeviceCtrlReq.U_SetLogParameters( x ) ->
                                        do! this.ProcReq_SetLogParameters x
                                    | TargetDeviceCtrlReq.U_GetLogParameters( _ ) ->
                                        do! this.ProcReq_GetLogParameters()
                                    | TargetDeviceCtrlReq.U_GetDeviceName( _ ) ->
                                        do! this.ProcReq_GetDeviceName()
                                    | TargetDeviceCtrlReq.U_GetSession( x ) ->
                                        do! this.ProcReq_GetSession x
                                    | TargetDeviceCtrlReq.U_DestructSession( x ) ->
                                        do! this.ProcReq_DestructSession x
                                    | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                                        do! this.ProcReq_GetConnection x
                                    | TargetDeviceCtrlReq.U_GetLUStatus( x ) ->
                                        do! this.ProcReq_GetLUStatus x
                                    | TargetDeviceCtrlReq.U_LUReset( x ) ->
                                        do! this.ProcReq_LUReset x
                                    | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                                        do! this.ProcReq_GetMediaStatus x.LUN x.ID
                                    | TargetDeviceCtrlReq.U_MediaControlRequest( x ) ->
                                        do! this.ProcReq_MediaControlRequest x
                                with
                                | _ as x ->
                                    let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = {
                                        Response = TargetDeviceCtrlRes.U_UnexpectedError( x.Message )
                                    }
                                    HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                                    do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
                                    do! m_CtrlReqSink.FlushAsync()
                with
                | _ as x ->
                    // If unexpected error occurred, terminated target device process.
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                    
                HLogger.Trace( LogID.I_EXIT_CTRL_REQ_LOOP, fun g -> g.Gen0 m_ObjID )
            }

        // ------------------------------------------------------------------------
        // Start iSCSI service
        override _.Start() : unit =
            m_ports
            |> List.iter ( fun ( _, itrPort ) ->
                ( itrPort :> IPort ).Start() |> ignore
            )

    
    //=========================================================================
    // Static method

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Create server port components.
    /// </summary>
    /// <param name="argConfig">
    ///   The instance of ConfigulationMaster component that exists only one in the Haruka process.
    /// </param>
    /// <param name="argStatus">
    ///   The instance of StatusMaster component that exists only one in the Haruka process.
    /// </param>
    /// <param name="killer">
    ///   The object that notices terminate request to created new IscsiTCPSvPort object.
    /// </param>
    /// <param name="objid">
    ///   Object identifier.
    /// </param>
    /// <returns>
    ///   List of following record.
    ///   <list type="bullet">
    ///     <item>
    ///       <term>NetworkPortalInfo</term>
    ///       <description>Information object that describes Port component.</description>
    ///     </item>
    ///     <item>
    ///       <term>IPort</term>
    ///       <description>Interface of created Port component.</description>
    ///     </item>
    ///   </list>
    /// </returns>
    static member private CreatePortObj
            ( argConfig : IConfiguration )
            ( argStatus : IStatus )
            ( killer : IKiller )
            ( objid : OBJIDX_T ) :
            ( TargetDeviceConf.T_NetworkPortal * IscsiTCPSvPort ) list =

        // Get the number of NetworkPortalInfo. if failed to access CConfigMaster component,
        // it must terminate process, so no check for exception.
        let wNPconf = argConfig.GetNetworkPortal()

        // Create new application domains and load the Port AppDomin assembry.
        let rec loop list idx =
            if idx < wNPconf.Length then
                let rNP = wNPconf.[idx]

                let rPort =
                    try
                        ValueSome( new IscsiTCPSvPort( argStatus, rNP, killer ) )
                    with
                    | _ as x ->
                        HLogger.Trace( LogID.E_FAILED_CREATE_PORTAD, fun g -> g.Gen1( objid, idx ) )
                        ValueNone

                // next one
                if rPort.IsSome then
                    loop ( ( rNP, rPort.Value ) :: list ) ( idx + 1 )
                else
                    loop list ( idx + 1 )
            else
                // complete
                list
        // start
        loop [] 0

    /// <summary>
    ///  Convert ResCountResult array to RESCOUNTER list.
    /// </summary>
    /// <param name="r">
    ///  ResCountResult data.
    /// </param>
    static member private ConvToRESCOUNTERList ( r : ResCountResult array ) : TargetDeviceCtrlRes.T_RESCOUNTER list =
        [
            for itr in r do
                {
                    Time = itr.Time;
                    Value = itr.Value;
                    Count = itr.Count;
                } : TargetDeviceCtrlRes.T_RESCOUNTER
        ]

    static member private GetMediaInLU ( rLU : ILU ) ( mediaID : MEDIAIDX_T ) : IMedia option =
        let rRootMedia = rLU.GetMedia()
        let rec loop ( v : IMedia list ) ( cont : IMedia list -> IMedia list ) ( acc : IMedia list ) : IMedia list =
            match v with
            | [] ->
                cont acc
            | h :: [] ->
                loop ( h.GetSubMedia() ) cont ( h :: acc )
            | h :: t ->
                let nc = ( fun acc2 -> loop t cont acc2 )
                loop ( h.GetSubMedia() ) nc ( h :: acc )

        let rl = loop ( rRootMedia.GetSubMedia() ) id [rRootMedia]
        rl |> List.tryFind ( fun itr -> itr.MediaIndex = mediaID )
        

    //=========================================================================
    // Private method

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Timer event. This method is called every second.
    /// </summary>
    /// <remarks>
    ///   This function may be called multiple times simultaneously.
    /// </remarks>
    member private _.OnTimer ( _ : obj ) =
        if not m_Killer.IsNoticed then
            // Flush log 
            HLogger.Flush()

    /// <summary>
    ///  Process "Get active target groups" control request.
    /// </summary>
    /// <remarks>
    ///   Results are written to stderr in XML string.
    /// </remarks>
    member private _.ProcReq_GetActiveTargetGroups() : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetActiveTargetGroups" ) )
        let vtgconf : TargetDeviceCtrlRes.T_ActiveTGInfo list = [
            for itr in m_ActiveTargetGroups do
                match m_config.GetTargetGroupConf itr.Key with
                | Some ( x ) ->
                    yield {
                        ID = itr.Key;
                        Name = x.TargetGroupName;
                    }
                | None -> ()
        ]
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = {
            Response = TargetDeviceCtrlRes.U_ActiveTargetGroups( {
                ActiveTGInfo = vtgconf;
            } )
        }
        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetActiveTargetGroups", "" ) )
        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get loaded target groups" control request.
    /// </summary>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetLoadedTargetGroups() : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetLoadedTargetGroups" ) )
        let vtgconf : TargetDeviceCtrlRes.T_LoadedTGInfo list = [
            for itr, _ in m_config.GetAllTargetGroupConf() do
                yield {
                    ID = itr.TargetGroupID;
                    Name = itr.TargetGroupName;
                }
        ]
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = {
            Response = TargetDeviceCtrlRes.U_LoadedTargetGroups( {
                LoadedTGInfo = vtgconf;
            } )
        }
        
        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
            let rmsg = sprintf "(Active target group count:%d)" vtgconf.Length
            g.Gen2( m_ObjID, "GetLoadedTargetGroups", rmsg )
        )
        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Inactivate target groups" control request.
    /// </summary>
    /// <param name="id">
    ///   Target group ID that will be inactivated.
    /// </param>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_InactivateTargetGroup( id : TGID_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "InactivateTargetGroup(%s)" ( tgid_me.toString id ) ) )
        let wresult = m_ActiveTargetGroups.TryRemove( id ) |> fst
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = {
            Response = TargetDeviceCtrlRes.U_InactivateTargetGroupResult( {
                ID = id;
                Result = true;
                ErrorMessage = "";
            } )
        }
        if wresult then
            HLogger.Trace( LogID.I_TARGET_GROUP_INACTIVATED, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
        else
            HLogger.Trace( LogID.I_TARGET_GROUP_ALREADY_INACTIVE, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )

        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Activate target groups" control request.
    /// </summary>
    /// <param name="id">
    ///   Target group ID that will be activated.
    /// </param>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_ActivateTargetGroup ( id : TGID_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "ActivateTargetGroup(%s)" ( tgid_me.toString id ) ) )
        let r, msg =
            if m_config.GetTargetGroupConf( id ).IsSome then
                m_ActiveTargetGroups.TryAdd( id, () ) |> ignore
                HLogger.Trace( LogID.I_TARGET_GROUP_ACTIVATED, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                true, ""
            else
                HLogger.Trace( LogID.I_TARGET_GROUP_MISSING, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                false, "Specified target group is missing."
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
            {
                Response = TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( {
                    ID = id;
                    Result = r;
                    ErrorMessage = msg;
                } )
            }
        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Unload target groups" control request.
    /// </summary>
    /// <param name="id">
    ///   Target group ID that will be unload.
    /// </param>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_UnloadTargetGroup ( id : TGID_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "UnloadTargetGroup(%s)" ( tgid_me.toString id ) ) )
        let r, msg =
            if m_ActiveTargetGroups.ContainsKey( id ) then
                HLogger.Trace( LogID.I_TARGET_GROUP_STILL_ACTIVE, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                false, "Specified target group is still active."
            else
                match m_config.GetTargetGroupConf( id ) with
                | None ->
                    // Returns true, if specified ID does not exist.
                    HLogger.Trace( LogID.I_TARGET_GROUP_MISSING, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                    true, ""
                | Some( x ) ->
                    let targetIDs = [| for itr in x.Target -> itr.IdentNumber |]
                    let oldSess = m_Sessions.obj
                    let r =
                        oldSess
                        |> Seq.exists( fun itr ->
                            let wid = itr.Value.SessionParameter.TargetConf.IdentNumber
                            targetIDs |> Array.exists( (=) wid )
                        )
                    if r then
                        // if target that is belonging in specified target group is still used,
                        // tha target group can not be unloade.
                        HLogger.Trace( LogID.I_TARGET_GROUP_STILL_USED, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                        false, "Specified target group is still used."
                    else
                        // remove already create LU object instance.
                        x.LogicalUnit
                        |> List.map _.LUN
                        |> List.iter ( fun itr -> m_LU.Remove itr |> ignore )

                        // All LUs are terminated when configuration is unloaded.
                        m_config.UnloadTargetGroup( id )
                        HLogger.Trace( LogID.I_TARGET_GROUP_UNLOADED, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                        true, ""

        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
            {
                Response = TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                    ID = id;
                    Result = r;
                    ErrorMessage = msg;
                } )
            }

        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Load target groups" control request.
    /// </summary>
    /// <param name="id">
    ///   Target group ID that will be unload.
    /// </param>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_LoadTargetGroup ( id : TGID_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "LoadTargetGroup(%s)" ( tgid_me.toString id ) ) )
        let r, msg =
            if m_ActiveTargetGroups.ContainsKey( id ) then
                HLogger.Trace( LogID.I_TARGET_GROUP_STILL_ACTIVE, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
                false, "Specified target group is still active."
            else
                if m_config.LoadTargetGroup( id ) then
                    HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "LoadTargetGroup", "" ) )
                    true, ""
                else
                    // log message for failed load configuration is witten in ConfigurationMaster.
                    HLogger.Trace( LogID.W_CTRL_REQ_ERROR_END, fun g -> g.Gen2( m_ObjID, "LoadTargetGroup", "" ) )
                    false , "Failed to load target group config."

        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
            {
                Response = TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( {
                    ID = id;
                    Result = r;
                    ErrorMessage = msg;
                } )
            }
        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Set log parameters" control request.
    /// </summary>
    /// <param name="lpara">
    ///   Requested log parameters.
    /// </param>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_SetLogParameters ( lpara : TargetDeviceCtrlReq.T_SetLogParameters ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g ->
            let msg =
                sprintf "SetLogParameters( SoftLimit:%d, HardLimit:%d, LogLevel:%s )"
                        lpara.SoftLimit lpara.HardLimit ( LogLevel.toString lpara.LogLevel )
            g.Gen1( m_ObjID, msg )
        )
        let r =
            let lv = lpara.LogLevel
            let softLimitStr = String.Format( "{0}", lpara.SoftLimit )
            let hardLimitStr = String.Format( "{0}", lpara.HardLimit )
            let logLevelStr = LogLevel.toString lv
            if lpara.SoftLimit > lpara.HardLimit then
                HLogger.Trace( LogID.W_INVALID_LOG_PARAM, fun g -> g.Gen3( m_ObjID, softLimitStr, hardLimitStr, logLevelStr ) )
                false
            else
                HLogger.SetLogParameters( lpara.SoftLimit, lpara.HardLimit, Constants.DEFAULT_LOG_OUTPUT_CYCLE, lv, stderr )
                HLogger.Trace( LogID.I_LOG_PARAM_UPDATED, fun g -> g.Gen3( m_ObjID, softLimitStr, hardLimitStr, logLevelStr ) )
                true
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = 
            {
                Response = TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( r )
            }

        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get log parameters" control request.
    /// </summary>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetLogParameters () : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetLogParameters" ) )
        let softLimit, hardLimit, level = HLogger.GetLogParameters()
        let levelStr = LogLevel.toString level
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = 
            {
                Response = TargetDeviceCtrlRes.T_Response.U_LogParameters( {
                    SoftLimit = softLimit;
                    HardLimit = hardLimit;
                    LogLevel = level;
                } )
            }
        let msg = sprintf "( SoftLimit:%d, HardLimit:%d, LogLevel:%s )" softLimit hardLimit levelStr
        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetLogParameters", msg ) )
        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get log parameters" control request.
    /// </summary>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetDeviceName () : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetDeviceName" ) )
        let dn = m_config.DeviceName
        let res : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes = 
            {
                Response = TargetDeviceCtrlRes.T_Response.U_DeviceName( dn )
            }
        HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetDeviceName", dn ) )
        task {
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get Session" control request.
    /// </summary>
    /// <param name="arg">
    ///   Specify where to get a list of sessions belonging to.
    /// </param>
    /// <returns>
    ///   Session list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetSession ( arg : TargetDeviceCtrlReq.T_GetSession ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetSession" ) )
        task {
            let rSess = m_Sessions.obj
            let retSessList =
                match arg with
                | TargetDeviceCtrlReq.U_SessInTargetDevice( _ ) ->
                    rSess
                    |> Seq.map ( fun kv -> kv.Value )
                | TargetDeviceCtrlReq.U_SessInTargetGroup( tgid ) ->
                    rSess
                    |> Seq.map ( fun kv -> kv.Value )
                    |> Seq.filter ( fun v -> v.SessionParameter.TargetGroupID = tgid )
                | TargetDeviceCtrlReq.U_SessInTarget( tnode ) ->
                    rSess
                    |> Seq.map ( fun kv -> kv.Value )
                    |> Seq.filter ( fun v -> v.SessionParameter.TargetConf.IdentNumber = tnode )
            let res1 : TargetDeviceCtrlRes.T_SessionList = {
                Session =
                    retSessList
                    |> Seq.map ( fun v ->
                        {
                            TargetGroupID = v.SessionParameter.TargetGroupID;
                            TargetNodeID = v.SessionParameter.TargetConf.IdentNumber;
                            TSIH = v.TSIH;
                            ITNexus = {
                                InitiatorName = v.I_TNexus.InitiatorName;
                                ISID = v.I_TNexus.ISID;
                                TargetName = v.I_TNexus.TargetName;
                                TPGT = v.I_TNexus.TPGT;
                            };
                            SessionParameters = {
                                MaxConnections = v.SessionParameter.MaxConnections;
                                InitiatorAlias = v.SessionParameter.InitiatorAlias;
                                InitialR2T = v.SessionParameter.InitialR2T;
                                ImmediateData = v.SessionParameter.ImmediateData;
                                MaxBurstLength = v.SessionParameter.MaxBurstLength;
                                FirstBurstLength = v.SessionParameter.FirstBurstLength;
                                DefaultTime2Wait = v.SessionParameter.DefaultTime2Wait;
                                DefaultTime2Retain = v.SessionParameter.DefaultTime2Retain;
                                MaxOutstandingR2T = v.SessionParameter.MaxOutstandingR2T;
                                DataPDUInOrder = v.SessionParameter.DataPDUInOrder;
                                DataSequenceInOrder = v.SessionParameter.DataSequenceInOrder;
                                ErrorRecoveryLevel = v.SessionParameter.ErrorRecoveryLevel;
                            };
                            EstablishTime = v.CreateDate;
                        } : TargetDeviceCtrlRes.T_Session
                    )
                    |> Seq.toList
            }
            let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                {
                    Response = TargetDeviceCtrlRes.T_Response.U_SessionList( res1 )
                }
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetSession", sprintf "(Session count = %d)" res1.Session.Length ) )
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Destruct Session" control request.
    /// </summary>
    /// <param name="sessID">
    ///   TSIH os the session which should be terminated.
    /// </param>
    /// <returns>
    ///   Session list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_DestructSession ( sessID : TSIH_T ) : Task<unit> =
        let tsihStr = sprintf "%d" ( tsih_me.toPrim sessID )
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "DestructSession(%d)" ( tsih_me.toPrim sessID ) ) )
        task {
            let rSess = m_Sessions.obj
            let r, sess = rSess.TryGetValue sessID
            let resultFlg, msg =
                if not r then
                    HLogger.Trace( LogID.W_MISSING_TSIH, fun g -> g.Gen1( m_ObjID, tsihStr ) )
                    false, "Unknown session."
                else
                    HLogger.Trace( LogID.I_SESSION_DESTRUCTING, fun g -> g.Gen2( m_ObjID, tsihStr, sess.I_TNexus.ToString() ) )
                    sess.DestroySession()
                    true, ""

            let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                {
                    Response = TargetDeviceCtrlRes.T_Response.U_DestructSessionResult({
                        TSIH = sessID;
                        Result = resultFlg;
                        ErrorMessage = msg;
                    })
                }
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get Connection" control request.
    /// </summary>
    /// <param name="arg">
    ///   Specify where to get a list of connections belonging to.
    /// </param>
    /// <returns>
    ///   Connections list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetConnection ( arg : TargetDeviceCtrlReq.T_GetConnection ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, "GetConnection" ) )
        task {
            let rSess = m_Sessions.obj
            let retConList =
                match arg with
                | TargetDeviceCtrlReq.U_ConInTargetDevice( _ ) ->
                    rSess
                    |> Seq.map ( fun itr -> itr.Value.GetAllConnections() )
                    |> Seq.concat
                | TargetDeviceCtrlReq.U_ConInNetworkPortal( npid ) ->
                    rSess
                    |> Seq.map ( fun itr -> itr.Value.GetAllConnections() )
                    |> Seq.concat
                    |> Seq.filter ( fun itr -> itr.NetPortIdx = npid )
                | TargetDeviceCtrlReq.U_ConInTargetGroup( tgid ) ->
                    rSess
                    |> Seq.filter ( fun itr -> itr.Value.SessionParameter.TargetGroupID = tgid )
                    |> Seq.map ( fun itr -> itr.Value.GetAllConnections() )
                    |> Seq.concat
                | TargetDeviceCtrlReq.U_ConInTarget( tid ) ->
                    rSess
                    |> Seq.filter ( fun itr -> itr.Value.SessionParameter.TargetConf.IdentNumber = tid )
                    |> Seq.map ( fun itr -> itr.Value.GetAllConnections() )
                    |> Seq.concat
                | TargetDeviceCtrlReq.U_ConInSession( tsih ) ->
                    let r, sess = rSess.TryGetValue tsih
                    if r then
                        sess.GetAllConnections()
                    else
                        Array.empty
            let res1 : TargetDeviceCtrlRes.T_ConnectionList = {
                Connection =
                    retConList
                    |> Seq.map ( fun v ->
                        {
                            TSIH = v.TSIH;
                            ConnectionID = v.CID;
                            ConnectionCount = v.ConCounter;
                            ReceiveBytesCount =
                                v.GetReceiveBytesCount()
                                |> StatusMaster.ConvToRESCOUNTERList;
                            SentBytesCount =
                                v.GetSentBytesCount()
                                |> StatusMaster.ConvToRESCOUNTERList;
                            ConnectionParameters = {
                                AuthMethod =
                                    if v.CurrentParams.AuthMethod.Length > 0 then
                                        AuthMethodCandidateValue.toStringName( v.CurrentParams.AuthMethod.[0] );
                                    else
                                        AuthMethodCandidateValue.toStringName( AuthMethodCandidateValue.AMC_None );
                                HeaderDigest =
                                    if v.CurrentParams.HeaderDigest.Length > 0 then
                                        DigestType.toStringName( v.CurrentParams.HeaderDigest.[0] );
                                    else
                                        DigestType.toStringName( DigestType.DST_None );
                                DataDigest =
                                    if v.CurrentParams.DataDigest.Length > 0 then
                                        DigestType.toStringName( v.CurrentParams.DataDigest.[0] );
                                    else
                                        DigestType.toStringName( DigestType.DST_None );
                                MaxRecvDataSegmentLength_I = v.CurrentParams.MaxRecvDataSegmentLength_I;
                                MaxRecvDataSegmentLength_T = v.CurrentParams.MaxRecvDataSegmentLength_T;
                            };
                            EstablishTime = v.ConnectedDate;
                        } : TargetDeviceCtrlRes.T_Connection
                    )
                    |> Seq.toList
            }
            let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                {
                    Response = TargetDeviceCtrlRes.T_Response.U_ConnectionList( res1 )
                }
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g ->
                let msg = sprintf "(Connection count = %d)" res1.Connection.Length
                g.Gen2( m_ObjID, "GetConnection", msg )
            )
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get LU status" control request.
    /// </summary>
    /// <param name="lun">
    ///   Specify LUN of the LU which status should be get.
    /// </param>
    /// <returns>
    ///   Connections list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetLUStatus ( lun : LUN_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g ->
            let msg = sprintf "GetLUStatus(%s)" ( lun_me.toString lun )
            g.Gen1( m_ObjID, msg )
        )
        task {
            let r, activeLU = m_LU.TryGetValue( lun )
            let res1 : TargetDeviceCtrlRes.T_LUStatus =
                if r && activeLU.IsValueCreated then
                    let rLU = activeLU.Value
                    // Specified LU is in active.
                    {
                        LUN = lun;
                        ErrorMessage = "";
                        LUStatus_Success = Some {
                            ReadBytesCount =
                                rLU.GetReadBytesCount()
                                |> StatusMaster.ConvToRESCOUNTERList;
                            WrittenBytesCount = 
                                rLU.GetWrittenBytesCount()
                                |> StatusMaster.ConvToRESCOUNTERList;
                            ReadTickCount =
                                rLU.GetReadTickCount()
                                |> StatusMaster.ConvToRESCOUNTERList;
                            WriteTickCount =
                                rLU.GetWriteTickCount()
                                |> StatusMaster.ConvToRESCOUNTERList;
                            ACAStatus =
                                match rLU.ACAStatus with
                                | ValueNone -> None
                                | ValueSome struct( itn, statusCode, senseKey, asc, isCurrent ) ->
                                    Some {
                                        ITNexus = {
                                            InitiatorName = itn.InitiatorName;
                                            ISID = itn.ISID;
                                            TargetName = itn.TargetName;
                                            TPGT = itn.TPGT;
                                        };
                                        StatusCode = uint8 statusCode;
                                        SenseKey = uint8 senseKey;
                                        AdditionalSenseCode = uint16 asc;
                                        IsCurrent = isCurrent;
                                    }
                        }
                    }
                else
                    let inactiveLUsCount=
                        m_config.GetAllTargetGroupConf()
                        |> Seq.map fst
                        |> Seq.map ( fun itr -> itr.LogicalUnit )
                        |> Seq.concat
                        |> Seq.filter ( fun itr -> itr.LUN = lun )
                        |> Seq.length

                    if inactiveLUsCount = 0 then
                        HLogger.Trace( LogID.I_MISSING_LU, fun g -> g.Gen1( m_ObjID, "" ) )
                        {
                            LUN = lun;
                            ErrorMessage = sprintf "Missing LU(%s)." ( lun_me.toString lun );
                            LUStatus_Success = None;
                        }
                    else
                        // Specified LUN is exist in the configuration. But, it is not created yet.
                        // It is considered that the LU is exist and no operational track record.
                        {
                            LUN = lun;
                            ErrorMessage = "";
                            LUStatus_Success = Some {
                                ReadBytesCount = [];
                                WrittenBytesCount =  [];
                                ReadTickCount = [];
                                WriteTickCount = [];
                                ACAStatus = None;
                            }
                        }
            let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                {
                    Response = TargetDeviceCtrlRes.T_Response.U_LUStatus( res1 )
                }
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetLUStatus", "" ) )
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
            do! m_CtrlReqSink.FlushAsync()

        }

    /// <summary>
    ///  Process "LU Reset" control request.
    /// </summary>
    /// <param name="lun">
    ///   Specify LUN of the LU which should be reseted.
    /// </param>
    /// <returns>
    ///   Connections list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_LUReset ( lun : LUN_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g -> g.Gen1( m_ObjID, sprintf "LUReset(%s)" ( lun_me.toString lun ) ) )
        task {
            let r, activeLU = m_LU.TryGetValue( lun )
            let f = ( r && activeLU.IsValueCreated )
            let extf =
                m_config.GetAllTargetGroupConf()
                |> Seq.map fst
                |> Seq.map _.LogicalUnit
                |> Seq.concat
                |> Seq.map _.LUN
                |> Seq.exists ( (=) lun )
            let emsg2 = "Specified LU is not configured."

            if f then
                let rLU = activeLU.Value
                HLogger.Trace( LogID.I_CTRL_REQ_LURESET, fun g -> g.Gen1( m_ObjID, lun_me.toString lun ) )
                rLU.LogicalUnitReset ValueNone ValueNone
            elif extf then
                // LU object is not created yet.
                // The request is ignored and it considered that the request is successed.
                HLogger.Trace( LogID.I_CTRL_REQ_LURESET_IGNORE, fun g -> g.Gen1( m_ObjID, lun_me.toString lun ) )
            else
                HLogger.Trace( LogID.I_MISSING_LU, fun g -> g.Gen1( m_ObjID, emsg2 ) )

            let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                {
                    Response = TargetDeviceCtrlRes.T_Response.U_LUResetResult( {
                        LUN = lun;
                        Result = ( f || extf );
                        ErrorMessage = if f || extf then "" else emsg2;
                    } )
                }

            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "LUReset", if f then "Reset requested" else "Ignored reset request" ) )
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get Media Status" control request.
    /// </summary>
    /// <param name="LUN">
    ///   Specify LUN of which the media belongs to.
    /// </param>
    /// <param name="mid">
    ///   Specify which to get status of the media.
    /// </param>
    /// <returns>
    ///   Connections list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private _.ProcReq_GetMediaStatus ( lun : LUN_T ) ( mid : MEDIAIDX_T ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g ->
            let msg = sprintf "GetMediaStatus( LUN=%s, MediaIdx=%d )" ( lun_me.toString lun ) ( mediaidx_me.toPrim mid  )
            g.Gen1( m_ObjID, msg )
        )
        task {
            let r, activeLU = m_LU.TryGetValue( lun )
            let f = ( r && activeLU.IsValueCreated )
            let extf =
                m_config.GetAllTargetGroupConf()
                |> Seq.map fst
                |> Seq.map _.LogicalUnit
                |> Seq.concat
                |> Seq.map _.LUN
                |> Seq.exists ( (=) lun )
            let res1 : TargetDeviceCtrlRes.T_MediaStatus = 
                if f then
                    let rMedia = StatusMaster.GetMediaInLU activeLU.Value mid
                    match rMedia with
                    | Some( x ) ->
                        {
                            LUN = lun;
                            ID = mid;
                            ErrorMessage = "";
                            MediaStatus_Success = Some {
                                ReadBytesCount = 
                                    x.GetReadBytesCount()
                                    |> StatusMaster.ConvToRESCOUNTERList;
                                WrittenBytesCount = 
                                    x.GetWrittenBytesCount()
                                    |> StatusMaster.ConvToRESCOUNTERList;
                                ReadTickCount =
                                    x.GetReadTickCount()
                                    |> StatusMaster.ConvToRESCOUNTERList;
                                WriteTickCount =
                                    x.GetWriteTickCount()
                                    |> StatusMaster.ConvToRESCOUNTERList;
                            }
                        }
                    | None ->
                        let msg = sprintf "Specified LU(LUN=%s) is configured, but media(ID=%d) missing." ( lun_me.toString lun ) ( mediaidx_me.toPrim mid  )
                        HLogger.Trace( LogID.I_MISSING_MEDIA, fun g -> g.Gen1( m_ObjID, msg ) )
                        {
                            LUN = lun;
                            ID = mid;
                            ErrorMessage = "Specified media missing.";
                            MediaStatus_Success = None;
                        }
                elif extf then
                    // Specified LUN is exist in the configuration. But, it is not created yet.
                    // It is considered that the LU is exist and no operational track record.
                    {
                        LUN = lun;
                        ID = mid;
                        ErrorMessage = "";
                        MediaStatus_Success = Some {
                            ReadBytesCount = [];
                            WrittenBytesCount =  [];
                            ReadTickCount = [];
                            WriteTickCount = [];
                        };
                    }
                else
                    let msg = "Specified LU is not configured."
                    HLogger.Trace( LogID.I_MISSING_LU, fun g -> g.Gen1( m_ObjID, msg ) )
                    {
                        LUN = lun;
                        ID = mid;
                        ErrorMessage = msg;
                        MediaStatus_Success = None;
                    }
            let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                {
                    Response = TargetDeviceCtrlRes.T_Response.U_MediaStatus( res1 )
                }
            HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "GetMediaStatus", "" ) )
            do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
            do! m_CtrlReqSink.FlushAsync()
        }

    /// <summary>
    ///  Process "Get Media Status" control request.
    /// </summary>
    /// <param name="LUN">
    ///   Specify LUN of which the media belongs to.
    /// </param>
    /// <param name="mid">
    ///   Specify which to get status of the media.
    /// </param>
    /// <returns>
    ///   Connections list.
    /// </returns>
    /// <remarks>
    ///   Results are written to stdout in XML string.
    /// </remarks>
    member private this.ProcReq_MediaControlRequest ( request : TargetDeviceCtrlReq.T_MediaControlRequest ) : Task<unit> =
        HLogger.Trace( LogID.V_CTRL_REQ_RECEIVED, fun g ->
            let lun = request.LUN |> lun_me.toString
            let mid = request.ID |> mediaidx_me.toPrim
            let msg = sprintf "MediaControlRequest( LUN=%s, MediaIdx=%d )" lun mid
            g.Gen1( m_ObjID, msg )
        )

        let doResponse ( res : TargetDeviceCtrlRes.T_MediaControlResponse ) =
            task {
                let res2 : TargetDeviceCtrlRes.T_TargetDeviceCtrlRes =
                    {
                        Response = TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( res )
                    }
                HLogger.Trace( LogID.V_CTRL_REQ_NORMAL_END, fun g -> g.Gen2( m_ObjID, "MediaControlRequest", "" ) )
                do! m_CtrlReqSink.WriteLineAsync( TargetDeviceCtrlRes.ReaderWriter.ToString res2 )
                do! m_CtrlReqSink.FlushAsync()
            }

        task {
            let lun = request.LUN
            let mid = request.ID
            let rLU = ( this :> IStatus ).GetLU lun

            match rLU with
            | ValueSome( rLU2 ) ->
                let rMedia = StatusMaster.GetMediaInLU rLU2 mid
                match rMedia with
                | Some ( x ) ->
                    let reqData = MediaCtrlReq.ReaderWriter.LoadString request.Request
                    let! resData = x.MediaControl reqData.Request
                    let resStr = MediaCtrlRes.ReaderWriter.ToString { Response = resData }
                    do! doResponse {
                        LUN = lun;
                        ID = mid;
                        ErrorMessage = "";
                        Response = resStr;
                    }
                | None ->
                    let msg = sprintf "Specified LU(LUN=%s) is configured, but media(ID=%d) missing." ( lun_me.toString lun ) ( mediaidx_me.toPrim mid  )
                    HLogger.Trace( LogID.I_MISSING_MEDIA, fun g -> g.Gen1( m_ObjID, msg ) )
                    do! doResponse {
                        LUN = lun;
                        ID = mid;
                        ErrorMessage = "Specified media missing.";
                        Response = "";
                    }
            | ValueNone ->
                let msg = "Specified LU is not configured."
                HLogger.Trace( LogID.I_MISSING_LU, fun g -> g.Gen1( m_ObjID, msg ) )
                do! doResponse {
                    LUN = lun;
                    ID = mid;
                    ErrorMessage = msg;
                    Response = "";
                }
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Get logged in session count for specified target.
    /// </summary>
    /// <param name="argSess">
    ///  Currentry effected session lists.
    /// </param>
    /// <param name="argI_TNexus">
    ///  I_T Nexus that is tring to login.
    /// </param>
    /// <param name="sessionParameter">
    ///  Target ID that will be logged in.
    /// </param>
    /// <returns>
    ///  Returns true if login is possible. 
    /// </returns>
    member private _.CheckSessionCountUpperLimit ( argSess : ImmutableDictionary< TSIH_T, ISession > ) ( argI_TNexus : ITNexus ) ( sessionParameter : IscsiNegoParamSW ) : bool =
        let tid = sessionParameter.TargetConf.IdentNumber
        let sessCntAtTarget =
            argSess
            |> Seq.filter ( fun itr -> itr.Value.SessionParameter.TargetConf.IdentNumber = tid )
            |> Seq.length

        let loginOverLUCnt =
            sessionParameter.TargetConf.LUN
            |> Seq.map ( fun lun ->
                argSess
                |> Seq.filter ( fun itr -> Seq.contains lun itr.Value.SessionParameter.TargetConf.LUN )
                |> Seq.length
            )
            |> Seq.filter ( fun itr -> itr >= Constants.MAX_SESSION_COUNT_IN_LU )
            |> Seq.length

        if argSess.Count >= Constants.MAX_SESSION_COUNT_IN_TD then
            let msg = sprintf "Number of sessions exceeds maximum value(%d)." Constants.MAX_SESSION_COUNT_IN_TD
            HLogger.Trace( LogID.E_FAILED_CREATE_SESSION, fun g -> g.Gen2( m_ObjID, argI_TNexus.I_TNexusStr, msg ) )
            false
        elif sessCntAtTarget >= Constants.MAX_SESSION_COUNT_IN_TARGET then
            let msg = sprintf "Number of sessions per target exceeds maximum value(%d)." Constants.MAX_SESSION_COUNT_IN_TARGET
            HLogger.Trace( LogID.E_FAILED_CREATE_SESSION, fun g -> g.Gen2( m_ObjID, argI_TNexus.I_TNexusStr, msg ) )
            false
        elif loginOverLUCnt >= 1 then
            let msg = sprintf "Number of sessions per LU exceeds maximum value(%d)." Constants.MAX_SESSION_COUNT_IN_LU
            HLogger.Trace( LogID.E_FAILED_CREATE_SESSION, fun g -> g.Gen2( m_ObjID, argI_TNexus.I_TNexusStr, msg ) )
            false
        else
            true

