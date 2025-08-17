//=============================================================================
// Haruka Software Storage.
// ConfigurationMaster.fs : Defines ConfigurationMaster class
// ConfigurationMaster class has the configuration information that is used in
// Haruka project.

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Concurrent

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Class definition

/// <summary>
///   The main part of ConfigurationMaster component.
///   Notice that, this constractor is used to unit test only.
/// </summary>
/// <param name="m_WorkDirPath">
///   Path name of working directory.
/// </param>
/// <param name="killer">
///   An object that notices the terminate request to this object.
/// </param>
type ConfigurationMaster( m_WorkDirPath : string, killer : IKiller ) as this = 

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Killer object of this component
    /// In this class, there are not procedure that spending long time or creates threads,
    /// so, so special processing is performed for the end request.
    let m_Killer = killer

    /// Load configuration file.
    let (
        // Configuration of target device configuration.
        m_TargetDevice : TargetDeviceConf.T_TargetDevice,

        // Configuration of target group.
        m_TargetGroup :
            ConcurrentDictionary< TGID_T, {| conf : TargetGroupConf.T_TargetGroup; killer : IKiller |} >

    ) = ConfigurationMaster.LoadConfig m_WorkDirPath m_ObjID

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, "ConfigurationMaster", "" ) )


    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IConfiguration with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            ()  // Nothig to do

        // Imprementation of IConfiguration.GetNetworkPortal
        override _.GetNetworkPortal () : TargetDeviceConf.T_NetworkPortal list =
            if m_TargetDevice.NetworkPortal.Length > 0 then
                m_TargetDevice.NetworkPortal
            else
                [
                    {
                        IdentNumber = netportidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                        TargetAddress = "";
                        PortNumber = Constants.DEFAULT_ISCSI_PORT_NUM;
                        DisableNagle = Constants.DEF_DISABLE_NAGLE_IN_NP;
                        ReceiveBufferSize = Constants.DEF_RECEIVE_BUFFER_SIZE_IN_NP;
                        SendBufferSize = Constants.DEF_SEND_BUFFER_SIZE_IN_NP;
                        WhiteList = [];
                    }
                ]

        // Imprementation of IConfiguration.GetTargetGroupID
        override _.GetTargetGroupID() : TGID_T[] =
            [|
                for itr in m_TargetGroup do
                    itr.Value.conf.TargetGroupID
            |]

        // Imprementation of IConfiguration.GetTargetGroupConf
        override _.GetTargetGroupConf ( id : TGID_T ) : TargetGroupConf.T_TargetGroup option =
            let r, v = m_TargetGroup.TryGetValue id
            if r then
                Some v.conf
            else
                None

        // Imprementation of IConfiguration.GetAllTargetGroupConf
        override _.GetAllTargetGroupConf () : ( TargetGroupConf.T_TargetGroup * IKiller )[]=
            [|
                for itr in m_TargetGroup.Values -> ( itr.conf, itr.killer )
            |]

        // Imprementation of IConfiguration.UnloadTargetGroup
        override _.UnloadTargetGroup ( id : TGID_T ) : unit =
            HLogger.Trace( LogID.I_UNLOAD_TARGET_GROUP_CONFIG, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
            let result, v = m_TargetGroup.TryRemove id
            if not result then
                HLogger.Trace( LogID.W_FAILED_UNLOAD_TARGET_GROUP_CONF, fun g -> g.Gen1( m_ObjID, tgid_me.toString id ) )
            else
                // Notice termination to all of LU in unloaded target group
                v.killer.NoticeTerminate()

        // Imprementation of IConfiguration.LoadTargetGroup
        override _.LoadTargetGroup ( id : TGID_T ) : bool =
            // Get Target Group configuration file names.
            let tgConfNames = Functions.AppendPathName m_WorkDirPath ( tgid_me.toString id )
            HLogger.Trace( LogID.I_LOAD_TARGET_GROUP_CONFIG, fun g -> g.Gen1( m_ObjID, tgConfNames ) )

            try
                // Load Target Group configuration.
                let newTgConf =
                    let w = TargetGroupConf.ReaderWriter.LoadFile tgConfNames
                    {
                        w with
                            LogicalUnit = [
                                for itr2 in w.LogicalUnit -> {
                                    itr2 with
                                        WorkPath = Functions.AppendPathName m_WorkDirPath ( lun_me.WorkDirName itr2.LUN )
                                }
                            ]
                    }


                // Verify loaded configurations.
                let workTgConf =
                    [|
                        // Current loaded configurations
                        for itr in m_TargetGroup -> itr.Value.conf
                        yield newTgConf;
                    |]
                ConfigurationMaster.VerifyConfig m_TargetDevice workTgConf

                if m_TargetGroup.TryAdd ( newTgConf.TargetGroupID, {| conf = newTgConf; killer = new HKiller() |} ) then
                    true
                else
                    HLogger.Trace( LogID.W_FAILED_LOAD_TARGET_GROUP_CONF, fun g -> g.Gen2( m_ObjID, tgConfNames, "Unexpected duplicate target group ID." ) )
                    false
            with
            | _ as x ->
                HLogger.Trace( LogID.W_FAILED_LOAD_TARGET_GROUP_CONF, fun g -> g.Gen2( m_ObjID, tgConfNames, x.Message ) )
                false

        // Imprementation of IConfiguration.GetDefaultLogParameters
        override _.GetDefaultLogParameters() : struct ( uint32 * uint32 * LogLevel ) =
            match m_TargetDevice.LogParameters with
            | Some( x ) ->
                struct (
                    x.SoftLimit,
                    x.HardLimit,
                    x.LogLevel
                )
                
            | None ->
                struct ( Constants.LOGPARAM_DEF_SOFTLIMIT, Constants.LOGPARAM_DEF_HARDLIMIT, LogLevel.LOGLEVEL_INFO )

        // Imprementation of IConfiguration.IscsiNegoParamCO
        override _.IscsiNegoParamCO : IscsiNegoParamCO =
            {
                AuthMethod = Array.empty  // AuthMethod is decided when the target name is given by initiator.
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T =
                    match m_TargetDevice.NegotiableParameters with
                    | Some( x ) -> x.MaxRecvDataSegmentLength
                    | None -> Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
            }

        // Imprementation of IConfiguration.IscsiNegoParamSW
        override _.IscsiNegoParamSW : IscsiNegoParamSW =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;    // Harula does not support the function of 
                                                                        // constraining the maximum number of connections.
                TargetGroupID = tgid_me.Zero;                     // Target group ID is is decided when the target name is given by initiator.
                TargetConf = {                                          // Target node is decided when the target name is given by initiator.
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "";
                    TargetAlias = "";
                    LUN = [];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                InitiatorName = "";                     // In this time, initiator is unknown.
                InitiatorAlias = "";                    // In this time, initiator is unknown.
                TargetPortalGroupTag = tpgt_me.zero;    // Always 0
                InitialR2T = false;                     // In the default, Haruka is try to ommit the initial R2T.
                ImmediateData = true;
                MaxBurstLength =
                    match m_TargetDevice.NegotiableParameters with
                    | Some( x ) -> x.MaxBurstLength
                    | None -> Constants.NEGOPARAM_DEF_MaxBurstLength;
                FirstBurstLength =
                    match m_TargetDevice.NegotiableParameters with
                    | Some( x ) -> x.FirstBurstLength
                    | None -> Constants.NEGOPARAM_DEF_FirstBurstLength;
                DefaultTime2Wait =
                    match m_TargetDevice.NegotiableParameters with
                    | Some( x ) -> x.DefaultTime2Wait
                    | None -> Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                DefaultTime2Retain =
                    match m_TargetDevice.NegotiableParameters with
                    | Some( x ) -> x.DefaultTime2Retain
                    | None -> Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                MaxOutstandingR2T =
                    match m_TargetDevice.NegotiableParameters with
                    | Some( x ) -> x.MaxOutstandingR2T
                    | None -> Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
                DataPDUInOrder = false;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }

        // Imprementation of IConfiguration.DeviceName
        override _.DeviceName : string =
            m_TargetDevice.DeviceName

    //=========================================================================
    // static method

    // ------------------------------------------------------------------------
    /// <summary>
    ///   It Loads the system master configuration file,
    ///   and parses the XML file and returns internal config data structure.
    /// </summary>
    /// <param name="objId">
    ///   Identifier of configuration master instance.
    /// </param>
    /// <returns>
    ///   Configuration data structure. See ParseSystemConfigXMLFile function.
    /// </returns>
    static member private LoadConfig ( workDirPath : string ) ( objId : OBJIDX_T ) :
        TargetDeviceConf.T_TargetDevice * ConcurrentDictionary< TGID_T, {| conf : TargetGroupConf.T_TargetGroup; killer : IKiller |} > =
        try
            let sysConfName = Functions.AppendPathName workDirPath Constants.TARGET_DEVICE_CONF_FILE_NAME
            let rx = Constants.TARGET_GRP_CONFIG_FILE_NAME_REGOBJ

            HLogger.Trace( LogID.V_FALLBACK_CONFIG_VALUE, fun g -> g.Gen1( objId, workDirPath ) )

            // Load target device configuration
            let tdConf = TargetDeviceConf.ReaderWriter.LoadFile sysConfName

            // Get Target Group configuration file names.
            let tgConfNames =
                Directory.GetFiles workDirPath
                |> Array.filter ( Path.GetFileName >> rx.IsMatch )

            // Load Target Group configuration.
            let tgConf = [|
                for itr in tgConfNames -> 
                    let w = TargetGroupConf.ReaderWriter.LoadFile itr
                    {
                        w with
                            LogicalUnit = [
                                for itr2 in w.LogicalUnit -> {
                                    itr2 with
                                        WorkPath = Functions.AppendPathName workDirPath ( lun_me.WorkDirName itr2.LUN )
                                }
                            ]
                    }
            |]

            // Verify loaded configurations.
            ConfigurationMaster.VerifyConfig tdConf tgConf

            let tgConfBag = new ConcurrentDictionary< TGID_T, {| conf : TargetGroupConf.T_TargetGroup; killer : IKiller |} >()
            tgConf
            |> Array.iter ( fun itr ->
                tgConfBag.TryAdd( itr.TargetGroupID, {| conf = itr; killer = new HKiller(); |} ) |> ignore
            )

            ( tdConf, tgConfBag )
        with
        | _ as x ->
            HLogger.Trace( LogID.F_FAILED_LOAD_CONFIG_FILE, fun g -> g.Gen2( objId, workDirPath, x.Message ) )
            raise x // re-throw
        
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Verify loaded configuration
    /// </summary>
    /// <param name="tdConf">
    ///   Loaded target device configuration.
    /// </param>
    /// <param name="tgConf">
    ///   Loaded target group configurations.
    /// </param>
    /// <remarks>
    ///   If failed to check. it raise an exception.
    /// </remarks>
    static member private VerifyConfig ( tdConf : TargetDeviceConf.T_TargetDevice ) ( tgConf : TargetGroupConf.T_TargetGroup[] ) : unit =

        // Verify if a Network Portal's IdentNumber is unique
        let npv = tdConf.NetworkPortal
        if npv.Length <> ( npv |> Seq.distinctBy ( fun itr -> itr.IdentNumber ) |> Seq.length ) then
            raise <| ConfRWException( "Duplicate Network portal IdentNumber." )

        // Verify target group count
        if tgConf.Length > Constants.MAX_TARGET_GROUP_COUNT_IN_TD then
            raise <| ConfRWException( "Too many target groups." )

        // Target configuration sequence
        let targets =
            tgConf
            |> Seq.map _.Target
            |> Seq.concat
            |> Seq.toArray

        // Verify target count
        if targets.Length > Constants.MAX_TARGET_COUNT_IN_TD then
            raise <| ConfRWException( "Too many target." )

        targets
        |> Array.iter ( fun itr ->
            // Verify if a LUN is unique in Target
            let lunv = itr.LUN
            if lunv.Length <> ( lunv |> Seq.distinct |> Seq.length ) then
                raise <| ConfRWException( sprintf "Duplicate LUN in target(%s)." itr.TargetName )
            // Verify if a LUN 0 is not exist in Target
            if ( lunv |> List.tryFind ( (=) lun_me.zero ) ).IsSome then
                raise <| ConfRWException( sprintf "LUN 0 is exist in target(%s)." itr.TargetName )
        )

        let luns = [|
            for itr1 in tgConf do
                for itr2 in itr1.LogicalUnit do
                    yield itr2.LUN
        |]

        // Verify LUN count
        if luns.Length > Constants.MAX_LOGICALUNIT_COUNT_IN_TD then
            raise <| ConfRWException( "Too many LUs." )

        // Verify if a LUN is unique
        if luns.Length <> ( Array.distinct luns ).Length then
            raise <| ConfRWException( "Duplicate LUN." )

        // Verify if LUN 0 is exist or not.
        // (LUN 0 is always created by task router per target.)
        if ( luns |> Array.tryFind ( (=) lun_me.zero ) ).IsSome then
            raise <| ConfRWException( "LUN 0 is exist." )

        for tgitr in tgConf do
            let lunInLUs =
                [|
                    for itr in tgitr.LogicalUnit do
                        yield itr.LUN
                |]
            let lunRefs =
                [|
                    for itr in tgitr.Target do
                        for itr2 in itr.LUN -> itr2
                |]
                |> Array.distinct

            // specified LUN in target is exist or not.
            tgitr.Target
            |> List.iter ( fun itr1 ->
                itr1.LUN
                |> List.iter ( fun itr2 ->
                    if ( lunInLUs |> Array.tryFind ( (=) itr2 ) ).IsNone then
                        raise <| ConfRWException( sprintf "Missing LUN(%s) specified in target(%s)." ( lun_me.toString itr2 ) itr1.TargetName )
                )
            )
            // isolated LU
            lunInLUs
            |> Array.iter ( fun itr2 ->
                if ( lunRefs |> Array.tryFind ( (=) itr2 ) ).IsNone then
                    raise <| ConfRWException( sprintf "LU(%s) is not refferd by any target." ( lun_me.toString itr2 ) )
            )

        // Verify if a TargetName is unique
        if targets.Length <> ( targets |> Array.distinctBy ( fun itr -> itr.TargetName ) ).Length then
            raise <| ConfRWException( "Duplicate Target Name." )

        // Verify if a target's IdentNumber is unique
        if targets.Length <> ( targets |> Array.distinctBy ( fun itr -> itr.IdentNumber ) ).Length then
            raise <| ConfRWException( "Duplicate Target IdentNumber." )

        // Verify if a target group ID is unique
        if tgConf.Length <> ( tgConf |> Array.distinctBy ( fun itr -> itr.TargetGroupID ) ).Length then
            raise <| ConfRWException( "Duplicate Target group ID." )

        // Media ID list
        let mediaIDs =
            let rec loop ( itrm : TargetGroupConf.T_MEDIA ) ( cont : MEDIAIDX_T list -> MEDIAIDX_T list ) =
                match itrm with
                | TargetGroupConf.U_PlainFile( x ) ->
                    cont [ x.IdentNumber ]
                | TargetGroupConf.U_MemBuffer( x ) ->
                    cont [ x.IdentNumber ]
                | TargetGroupConf.U_DummyMedia( x ) ->
                    cont [ x.IdentNumber ]
                | TargetGroupConf.U_DebugMedia( x ) ->
                    loop x.Peripheral ( fun li -> cont ( x.IdentNumber :: li ) )
            [
                for itr1 in tgConf do
                    for itr2 in itr1.LogicalUnit do
                        match itr2.LUDevice with
                        | TargetGroupConf.U_BlockDevice( x ) ->
                            yield! loop x.Peripheral id
                        | _ -> ()
            ]

        // media ID
        // Verify if a target's IdentNumber is unique
        if mediaIDs.Length <> ( mediaIDs |> List.distinct |> List.length ) then
            raise <| ConfRWException( "Duplicate Media ID." )

        // Check LU work directory is exist or not.
        for tgitr in tgConf do
            for luitr in tgitr.LogicalUnit do
                if not ( Directory.Exists luitr.WorkPath ) then
                    Directory.CreateDirectory luitr.WorkPath |> ignore
