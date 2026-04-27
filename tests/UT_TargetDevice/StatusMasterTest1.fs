//=============================================================================
// Haruka Software Storage.
// StatusMasterTest1.fs : Test cases for StatusMaster class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Concurrent
open System.Collections.Immutable
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.BlockDeviceLU
open Haruka.Test

#nowarn "1240"

//=============================================================================
// Class implementation

type StatusMaster_Test1 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultTargetGroupConfStr idx eas =
        ( {
            TargetGroupID = tgid_me.fromPrim( uint32 idx );
            TargetGroupName = sprintf "a-%03d" idx;
            EnabledAtStart = eas;
            Target = 
                [{
                    IdentNumber = tnodeidx_me.fromPrim ( uint16 idx + 1us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = sprintf "target%03d" idx;
                    TargetAlias = sprintf "target%03d" idx;
                    LUN = [ lun_me.fromPrim ( uint64 idx + 1UL ) ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }];
            LogicalUnit =
                [{
                    LUN = lun_me.fromPrim ( uint64 idx + 1UL );
                    LUName = "luname";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                }];
        } : TargetGroupConf.T_TargetGroup )
        |> TargetGroupConf.ReaderWriter.ToString

    let defaultTargetDeviceConf : TargetDeviceConf.T_TargetDevice = {
        NetworkPortal = [];
        NegotiableParameters = None;
        LogParameters = None;
        DeviceName = "A";
        EnableStatSNAckChecker = false;
    }

    let tgid0 = tgid_me.Zero
    let tgid1 = tgid_me.fromPrim( 1u )
    let tgid99 = tgid_me.fromPrim( 99u )

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.ReleaseMutex() |> ignore

    static member defaultConParam =
        {
            AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; |];
            HeaderDigest = [| DigestType.DST_CRC32C; |];
            DataDigest = [| DigestType.DST_CRC32C; |];
            MaxRecvDataSegmentLength_I = 8192u;
            MaxRecvDataSegmentLength_T = 8192u;
        }

    static member defaultSessParam =
        {
            MaxConnections = Constants.NEGOPARAM_MaxConnections;
            TargetGroupID = tgid_me.Zero;
            TargetConf = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetName = "target001001";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            };
            InitiatorName = "initiator001";
            InitiatorAlias = ""
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            InitialR2T = false;
            ImmediateData = false;
            MaxBurstLength = 512u;
            FirstBurstLength = 512u;
            DefaultTime2Wait = 0us;
            DefaultTime2Retain = 0us;
            MaxOutstandingR2T = 1us;
            DataPDUInOrder = true;
            DataSequenceInOrder = true;
            ErrorRecoveryLevel = 0uy;
        }

    member _.GetTestDirName ( caseName : string ) =
        Functions.AppendPathName ( Path.GetTempPath() ) "StatusMaster_Test1_" + caseName

    static member CreateEmptyTDConf( dirPath : string ) =
        let fn = Functions.AppendPathName dirPath Constants.TARGET_DEVICE_CONF_FILE_NAME
        TargetDeviceConf.ReaderWriter.WriteFile fn {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        fn

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.Constructor_001() =
        let pDirName = this.GetTestDirName "Constructor_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0  )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr 0 true )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let np = sm.GetNetworkPortal()
        Assert.True(( np.Length = 1 ))
        Assert.True(( np.[0].IdentNumber = netportidx_me.fromPrim 0u ))

        Assert.True(( sm.GetActiveTarget().Length = 1 ))
        Assert.True(( sm.GetActiveTarget().[0].IdentNumber = tnodeidx_me.fromPrim 1us ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_002() =
        let pDirName = this.GetTestDirName "Constructor_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let np = sm.GetNetworkPortal()
        Assert.True(( np.Length = 1 ))
        Assert.True(( np.[0].IdentNumber = netportidx_me.fromPrim 0u ))

        Assert.True(( sm.GetActiveTarget().Length = 1 ))
        Assert.True(( sm.GetActiveTarget().[0].IdentNumber = tnodeidx_me.fromPrim 2us ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_003() =
        let pDirName = this.GetTestDirName "Constructor_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = Some {
                SoftLimit = 999u;
                HardLimit = 9999u;
                LogLevel = LogLevel.LOGLEVEL_FAILED;
            };
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        // set initial log parameters
        let lock = GlbFunc.LogParamUpdateLock()
        try
            HLogger.SetLogParameters( Constants.LOGPARAM_DEF_SOFTLIMIT, Constants.LOGPARAM_DEF_HARDLIMIT, 0u, LogLevel.LOGLEVEL_VERBOSE, stderr )

            let killer = new HKiller()
            let _ = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

            let s, h, l = HLogger.GetLogParameters()
            Assert.True(( s = 999u ))
            Assert.True(( h = 9999u ))
            Assert.True(( l = LogLevel.LOGLEVEL_FAILED ))

            // set initial log parameters
            HLogger.SetLogParameters( Constants.LOGPARAM_DEF_SOFTLIMIT, Constants.LOGPARAM_DEF_HARDLIMIT, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        finally
            lock.ReleaseMutex() |> ignore

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetNetworkPortal_001() =
        let pDirName = this.GetTestDirName "GetNetworkPortal_001"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let npl = sm.GetNetworkPortal()
        Assert.True(( npl.Length = 1 ))
        Assert.True(( npl.[0].IdentNumber = netportidx_me.zero ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetNetworkPortal_002() =
        let pDirName = this.GetTestDirName "GetNetworkPortal_002"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf =
            TargetDeviceConf.ReaderWriter.ToString {
                defaultTargetDeviceConf with
                    NetworkPortal = [
                        {
                            IdentNumber = netportidx_me.fromPrim 1u;
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            TargetAddress = "";
                            PortNumber = uint16 ( GlbFunc.nextTcpPortNo() );
                            DisableNagle = false;
                            ReceiveBufferSize = 1111;
                            SendBufferSize = 8192;
                            WhiteList = [];
                        }
                    ];
            }
        File.WriteAllText( targetDeviceConfName, tdConf )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus
        let npl = sm.GetNetworkPortal()
        Assert.True(( npl.Length = 1 ))
        Assert.True(( npl.[0].IdentNumber = netportidx_me.fromPrim 1u ))
        Assert.True(( npl.[0].ReceiveBufferSize = 1111 ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetNetworkPortal_003() =
        let pDirName = this.GetTestDirName "GetNetworkPortal_003"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            TargetDeviceConf.ReaderWriter.ToString {
                defaultTargetDeviceConf with
                    NetworkPortal = [
                        {
                            IdentNumber = netportidx_me.fromPrim 1u;
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            TargetAddress = "";
                            PortNumber = uint16 ( GlbFunc.nextTcpPortNo() );
                            DisableNagle = false;
                            ReceiveBufferSize = 2222;
                            SendBufferSize = 8192;
                            WhiteList = [];
                        };
                        {
                            IdentNumber = netportidx_me.fromPrim 2u;
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            TargetAddress = "";
                            PortNumber = uint16 ( GlbFunc.nextTcpPortNo() );
                            DisableNagle = false;
                            ReceiveBufferSize = 3333;
                            SendBufferSize = 8192;
                            WhiteList = [];
                        };
                    ];
            }
        File.WriteAllText( targetDeviceConfName, tdConf )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus
        let npl = sm.GetNetworkPortal()
        Assert.True(( npl.Length = 2 ))
        Assert.True(( npl.[0].IdentNumber = netportidx_me.fromPrim 1u ))
        Assert.True(( npl.[0].ReceiveBufferSize = 2222 ))
        Assert.True(( npl.[1].IdentNumber = netportidx_me.fromPrim 2u ))
        Assert.True(( npl.[1].ReceiveBufferSize = 3333 ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTargetGroup_001() =
        let pDirName = this.GetTestDirName "GetActiveTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let atg = sm.GetActiveTargetGroup()
        Assert.True(( atg.Length = 0 ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTargetGroup_002() =
        let pDirName = this.GetTestDirName "GetActiveTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let atg = sm.GetActiveTargetGroup()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].TargetGroupID = tgid_me.fromPrim( 99u ) ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTargetGroup_003() =
        let pDirName = this.GetTestDirName "GetActiveTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let pc = PrivateCaller( sm )
        let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
        m_ActiveTargetGroups.TryAdd( 1u, () ) |> ignore
        let atg = sm.GetActiveTargetGroup()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].TargetGroupID = tgid_me.fromPrim( 99u ) ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTarget_001() =
        let pDirName = this.GetTestDirName "GetActiveTarget_001"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let atg = sm.GetActiveTarget()
        Assert.True(( atg.Length = 0 ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTarget_002() =
        let pDirName = this.GetTestDirName "GetActiveTarget_002"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let atg = sm.GetActiveTarget()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].IdentNumber = tnodeidx_me.fromPrim 100us ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTarget_003() =
        let pDirName = this.GetTestDirName "GetActiveTarget_003"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let pc = PrivateCaller( sm )
        let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
        m_ActiveTargetGroups.TryAdd( 1u, () ) |> ignore
        let atg = sm.GetActiveTarget()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].IdentNumber = tnodeidx_me.fromPrim 100us ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetFromLUN_001() =
        let pDirName = this.GetTestDirName "GetTargetFromLUN_001"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 88 false )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let tgl = sm.GetTargetFromLUN ( lun_me.fromPrim 89UL )
        Assert.True(( tgl.Length = 1 ))
        Assert.True(( tgl.[0].IdentNumber = tnodeidx_me.fromPrim 89us ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetFromLUN_002() =
        let pDirName = this.GetTestDirName "GetTargetFromLUN_002"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 88 true )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let tgl = sm.GetTargetFromLUN ( lun_me.fromPrim 1UL )
        Assert.True(( tgl.Length = 0 ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetFromLUN_003() =
        let pDirName = this.GetTestDirName "GetTargetFromLUN_003"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let tgConf =
            ( {
                TargetGroupID = tgid_me.fromPrim( 1u );
                TargetGroupName = "TargetGroup001";
                EnabledAtStart = true;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 10us;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "target000";
                        TargetAlias = "Target000";
                        LUN = [ lun_me.fromPrim ( uint64 1UL ); lun_me.fromPrim ( uint64 2UL ); ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    };
                    {
                        IdentNumber = tnodeidx_me.fromPrim 11us;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "target001";
                        TargetAlias = "Target001";
                        LUN = [ lun_me.fromPrim ( uint64 2UL ); ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    };
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim ( uint64 1UL );
                        LUName = "LU001";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    };
                    {
                        LUN = lun_me.fromPrim ( uint64 2UL );
                        LUName = "LU002";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    };
                ];
            } : TargetGroupConf.T_TargetGroup )
            |> TargetGroupConf.ReaderWriter.ToString
        File.WriteAllText( targetGroupConfName0, tgConf )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let tgl1 = sm.GetTargetFromLUN ( lun_me.fromPrim 0UL )
        Assert.True(( tgl1.Length = 2 ))

        let tgl2 = sm.GetTargetFromLUN ( lun_me.fromPrim 1UL )
        Assert.True(( tgl2.Length = 1 ))
        Assert.True(( tgl2.[0].IdentNumber = tnodeidx_me.fromPrim 10us ))

        let tgl3 = sm.GetTargetFromLUN ( lun_me.fromPrim 2UL )
        Assert.True(( tgl3.Length = 2 ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLoadedTarget_001() =
        let pDirName = this.GetTestDirName "GetLoadedTarget_001"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let tc = sm.GetLoadedTarget()
        Assert.True(( tc.Length = 0 ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLoadedTarget_002() =
        let pDirName = this.GetTestDirName "GetLoadedTarget_002"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let tgConf = defaultTargetGroupConfStr 0 true
        File.WriteAllText( targetGroupConfName0, tgConf )
        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let tc = sm.GetLoadedTarget()
        Assert.True(( tc.Length = 1 ))
        Assert.True(( tc.[0].IdentNumber = tnodeidx_me.fromPrim 1us ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLoadedTarget_003() =
        let pDirName = this.GetTestDirName "GetLoadedTarget_003"
        GlbFunc.CreateDir pDirName |> ignore
        StatusMaster_Test1.CreateEmptyTDConf pDirName |> ignore

        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let tgConf0 = defaultTargetGroupConfStr 0 true
        File.WriteAllText( targetGroupConfName0, tgConf0 )

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let tgConf1 = defaultTargetGroupConfStr 1 false
        File.WriteAllText( targetGroupConfName1, tgConf1 )

        let sm = new StatusMaster( pDirName, new HKiller(), stdin, stdout ) :> IStatus
        let tc = sm.GetLoadedTarget()
        Assert.True(( tc.Length = 2 ))
        Assert.True(( tc |> List.exists ( fun itr -> itr.IdentNumber = tnodeidx_me.fromPrim 1us ) ))
        Assert.True(( tc |> List.exists ( fun itr -> itr.IdentNumber = tnodeidx_me.fromPrim 2us ) ))
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateLoginNegociator_001() =
        let pDirName = this.GetTestDirName "CreateLoginNegociator_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let sp, cp = GlbFunc.GetNetConn()
        let ln = sm.CreateLoginNegociator cp DateTime.UtcNow ( tpgt_me.fromPrim 1us ) ( netportidx_me.fromPrim 2u )
        let pc_ln = new PrivateCaller( ln )
        Assert.True(( pc_ln.GetField( "m_StatusMaster" ) :?> IStatus = sm ))
        Assert.True(( pc_ln.GetField( "m_TargetPortalGroupTag" )= 1us ))
        Assert.True(( pc_ln.GetField( "m_NetPortIdx" )= 2u ))

        GlbFunc.ClosePorts [| sp; cp |]

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTSIH_001() =
        let pDirName = this.GetTestDirName "GetTSIH_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let I_TNexus1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        Assert.True(( sm.GetTSIH( I_TNexus1 ) = tsih_me.zero ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTSIH_002() =
        let pDirName = this.GetTestDirName "GetTSIH_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn0 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        let m_Sessions =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, new CSession_Stub( p_GetI_TNexus = ( fun () -> itn0 ) ) );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, new CSession_Stub( p_GetI_TNexus = ( fun () -> itn1 ) ) );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, new CSession_Stub( p_GetI_TNexus = ( fun () -> itn2 ) ) );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions )

        Assert.True(( sm.GetTSIH( itn0 ) = tsih_me.fromPrim 1us ))
        Assert.True(( sm.GetTSIH( itn1 ) = tsih_me.fromPrim 2us ))
        Assert.True(( sm.GetTSIH( itn2 ) = tsih_me.fromPrim 3us ))
        Assert.True(( sm.GetTSIH( itn3 ) = tsih_me.zero ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GenNewTSIH_001() =
        let pDirName = this.GetTestDirName "GenNewTSIH_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let m_Sessions =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, new CSession_Stub() );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, new CSession_Stub() );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, new CSession_Stub() );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions )

        Assert.True(( sm.GenNewTSIH() = tsih_me.fromPrim 4us ))

        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GenNewTSIH_002() =
        let pDirName = this.GetTestDirName "GenNewTSIH_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        pc_sm.SetField( "m_newTSIHGen", 65535 )

        Assert.True(( sm.GenNewTSIH() = tsih_me.fromPrim 1us ))

        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GenNewTSIH_003() =
        let pDirName = this.GetTestDirName "GenNewTSIH_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let m_Sessions =
            [
                for i = 1 to 65534 do
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), new CSession_Stub() );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions )

        Assert.True(( sm.GenNewTSIH() = tsih_me.fromPrim 65535us ))

        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GenNewTSIH_004() =
        let pDirName = this.GetTestDirName "GenNewTSIH_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let m_Sessions =
            [
                for i = 1 to 65535 do
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), new CSession_Stub() );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions )

        Assert.True(( sm.GenNewTSIH() = tsih_me.fromPrim 0us ))

        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetSession_001() =
        let pDirName = this.GetTestDirName "GetSession_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let sess_stub1 = new CSession_Stub( p_IsAlive = ( fun () -> true ) )
        let sess_stub2 = new CSession_Stub( p_IsAlive = ( fun () -> false ) )
        let sess_stub3 = new CSession_Stub( p_IsAlive = ( fun () -> true ) )

        let m_Sessions =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, sess_stub1 );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, sess_stub2 );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, sess_stub3 );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions )

        let sess_1 = sm.GetSession( tsih_me.fromPrim 1us )
        Assert.True(( sess_1.Value = ( sess_stub1 :> ISession ) ))
        let sess_3 = sm.GetSession( tsih_me.fromPrim 3us )
        Assert.True(( sess_3.Value = ( sess_stub3 :> ISession ) ))
        let sess_4 = sm.GetSession( tsih_me.fromPrim 4us )
        Assert.True(( sess_4.IsNone ))
        let m_Sessions1 = pc_sm.GetField( "m_Sessions" ) :?> OptimisticLock<ImmutableDictionary< TSIH_T, ISession >>

        Assert.True(( m_Sessions1.obj.Count = 3 ) )

        let sess_2 = sm.GetSession( tsih_me.fromPrim 2us )
        Assert.True(( sess_2.IsNone ))
        let m_Sessions2 = pc_sm.GetField( "m_Sessions" ) :?> OptimisticLock<ImmutableDictionary< TSIH_T, ISession >>
        Assert.True(( m_Sessions2.obj.Count = 2 ) )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.GetITNexusFromLUN_001() =
        let pDirName = this.GetTestDirName "GetITNexusFromLUN_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 = new CSession_Stub(
            p_GetSCSITaskRouter = ( fun () ->
                new CProtocolService_Stub( p_GetLUNs = ( fun () ->
                    [| lun_me.fromPrim 0UL; lun_me.fromPrim 1UL; |]
                ) )
            ),
            p_GetI_TNexus = ( fun () -> itn1 )
        )

        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us );
        let sess_stub2 = new CSession_Stub(
            p_GetSCSITaskRouter = ( fun () ->
                new CProtocolService_Stub( p_GetLUNs = ( fun () ->
                    [| lun_me.fromPrim 0UL; lun_me.fromPrim 2UL; |]
                ) )
            ),
            p_GetI_TNexus = ( fun () -> itn2 )
        )

        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us );
        let sess_stub3 = new CSession_Stub(
            p_GetSCSITaskRouter = ( fun () ->
                new CProtocolService_Stub( p_GetLUNs = ( fun () ->
                    [| lun_me.fromPrim 0UL; lun_me.fromPrim 3UL; |]
                ) )
            ),
            p_GetI_TNexus = ( fun () -> itn3 )
        )

        let m_Sessions1 =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, sess_stub1 );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, sess_stub2 );
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, sess_stub3 );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions1 )

        Assert.True(( sm.GetITNexusFromLUN( lun_me.fromPrim 1UL ) = [| itn1 |] ))
        Assert.True(( sm.GetITNexusFromLUN( lun_me.fromPrim 2UL ) = [| itn2 |] ))
        Assert.True(( sm.GetITNexusFromLUN( lun_me.fromPrim 3UL ) = [| itn3 |] ))
        Assert.True(( sm.GetITNexusFromLUN( lun_me.fromPrim 4UL ) = Array.empty ))

        let wv = sm.GetITNexusFromLUN( lun_me.fromPrim 0UL )
        Assert.True(( wv.Length = 3 ))
        Assert.True(( wv |> Array.exists ( (=) itn1 ) ))
        Assert.True(( wv |> Array.exists ( (=) itn2 ) ))
        Assert.True(( wv |> Array.exists ( (=) itn3 ) ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_001() =
        let pDirName = this.GetTestDirName "CreateNewSession_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test1.defaultSessParam )
            )
        let m_Sessions1 =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, sess_stub1 );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions1 )

        // same I_T Nexus is already used.
        let r = sm.CreateNewSession itn1 ( tsih_me.fromPrim 2us ) StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_002() =
        let pDirName = this.GetTestDirName "CreateNewSession_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let m_sessions1 =
            [
                for i = 1 to Constants.MAX_SESSION_COUNT_IN_TD - 1 do
                    let sess_stub1 =
                        new CSession_Stub(
                            p_GetI_TNexus = ( fun () -> itn1 ),
                            p_GetSessionParameter = ( fun () -> {
                                StatusMaster_Test1.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test1.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint16 i );
                                            LUN = [ lun_me.fromPrim ( uint64 i ) ];
                                    }
                            } )
                        )
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), sess_stub1 )
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )

        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us );
        let tsih2 = tsih_me.fromPrim ( uint16 Constants.MAX_SESSION_COUNT_IN_TD )

        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsSome ))
        Assert.True(( r.Value.I_TNexus.Equals itn2 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_003() =
        let pDirName = this.GetTestDirName "CreateNewSession_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let m_sessions1 =
            [
                for i = 1 to Constants.MAX_SESSION_COUNT_IN_TD do
                    let sess_stub1 =
                        new CSession_Stub(
                            p_GetI_TNexus = ( fun () -> itn1 ),
                            p_GetSessionParameter = ( fun () -> {
                                StatusMaster_Test1.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test1.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint16 i );
                                            LUN = [ lun_me.fromPrim ( uint64 i ) ];
                                    }
                            } )
                        )
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), sess_stub1 )
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )

        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us );
        let tsih2 = tsih_me.fromPrim ( uint16 Constants.MAX_SESSION_COUNT_IN_TD + 1us )

        // Session count per target device exceeds limits.
        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_004() =
        let pDirName = this.GetTestDirName "CreateNewSession_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test1.defaultSessParam )
            )
        let m_sessions1 =
            [
                for i = 1 to Constants.MAX_SESSION_COUNT_IN_TARGET - 1 do
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), sess_stub1 )
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let tsih2 = tsih_me.fromPrim ( uint16 Constants.MAX_SESSION_COUNT_IN_TARGET )

        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsSome ))
        Assert.True(( r.Value.I_TNexus.Equals itn2 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_005() =
        let pDirName = this.GetTestDirName "CreateNewSession_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test1.defaultSessParam )
            )
        let m_sessions1 =
            [
                for i = 1 to Constants.MAX_SESSION_COUNT_IN_TARGET do
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), sess_stub1 )
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let tsih2 = tsih_me.fromPrim ( uint16 Constants.MAX_SESSION_COUNT_IN_TARGET + 1us )

        // Session count per target exceeds limits.
        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_006() =
        let pDirName = this.GetTestDirName "CreateNewSession_006"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let m_sessions1 =
            [
                for i = 1 to Constants.MAX_SESSION_COUNT_IN_LU - 1 do
                    let sess_stub1 =
                        new CSession_Stub(
                            p_GetI_TNexus = ( fun () -> itn1 ),
                            p_GetSessionParameter = ( fun () -> {
                                StatusMaster_Test1.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test1.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint16 i );
                                            LUN = [ lun_me.fromPrim 1UL ];
                                    }
                            } )
                        )
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), sess_stub1 )
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let tsih2 = tsih_me.fromPrim ( uint16 Constants.MAX_SESSION_COUNT_IN_LU )

        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsSome ))
        Assert.True(( r.Value.I_TNexus.Equals itn2 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_007() =
        let pDirName = this.GetTestDirName "CreateNewSession_007"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let m_sessions1 =
            [
                for i = 1 to Constants.MAX_SESSION_COUNT_IN_LU do
                    let sess_stub1 =
                        new CSession_Stub(
                            p_GetI_TNexus = ( fun () -> itn1 ),
                            p_GetSessionParameter = ( fun () -> {
                                StatusMaster_Test1.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test1.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint16 i );
                                            LUN = [ lun_me.fromPrim 1UL ];
                                    }
                            } )
                        )
                    yield KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim ( uint16 i ), sess_stub1 )
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let tsih2 = tsih_me.fromPrim ( uint16 Constants.MAX_SESSION_COUNT_IN_LU + 1us )

        // Session count per LU exceeds limits.
        // if MAX_SESSION_COUNT_IN_TD <= MAX_SESSION_COUNT_IN_LU, 
        // it fails because it violates the per Target device limit, not the per LU limit.
        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_008() =
        let pDirName = this.GetTestDirName "CreateNewSession_008"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test1.defaultSessParam ),
                p_IsAlive = ( fun () -> true )
            )
        let m_sessions1 =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, sess_stub1 );
            ]
            |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_sessions1 )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us );

        // TSIH duplicate
        let r = sm.CreateNewSession itn2 ( tsih_me.fromPrim 1us ) StatusMaster_Test1.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))
        let r2 = sm.GetSession ( tsih_me.fromPrim 1us )
        Assert.True(( r2.IsSome ))
        Assert.True(( Functions.IsSame r2.Value sess_stub1 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLU_001() =
        let pDirName = this.GetTestDirName "GetLU_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let lu1 = sm.GetLU( lun_me.fromPrim 0UL )
        match lu1.Value with
        | :? BlockDeviceLU as x ->
            let pc_bd = new PrivateCaller( x )
            Assert.True(( ( pc_bd.GetField( "m_DeviceType" ) :?> BlockDeviceType ) = BlockDeviceType.BDT_Dummy ))
        | _ ->
            Assert.Fail __LINE__

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLU_002() =
        let pDirName = this.GetTestDirName "GetLU_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let mediaFileName = Functions.AppendPathName pDirName "a.txt"
        let targetGroupConfStr =
            sprintf "
                <TargetGroup>
                  <TargetGroupID>TG_00000000</TargetGroupID>
                  <TargetGroupName>a-001</TargetGroupName>
                  <EnabledAtStart>true</EnabledAtStart>
                  <Target>
                    <IdentNumber>1</IdentNumber>
                    <TargetPortalGroupTag>0</TargetPortalGroupTag>
                    <TargetName>target001</TargetName>
                    <TargetAlias>target001</TargetAlias>
                    <LUN>1</LUN>
                    <Auth><None>0</None></Auth>
                  </Target>
                  <LogicalUnit>
                    <LUN>1</LUN>
                    <LUName>luname001</LUName>
                    <LUDevice>
                      <BlockDevice>
                       <Peripheral>
                         <PlainFile>
                           <IdentNumber>1</IdentNumber>
                           <MediaName></MediaName>
                           <FileName>%s</FileName>
                           <WriteProtect>false</WriteProtect>
                         </PlainFile>
                       </Peripheral>
                      </BlockDevice>
                    </LUDevice>
                  </LogicalUnit>
                </TargetGroup>
                "
                mediaFileName
        File.WriteAllText( targetGroupConfName0, targetGroupConfStr )
        File.WriteAllBytes( mediaFileName, Array.zeroCreate<byte> 1024 )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let lu1 = sm.GetLU( lun_me.fromPrim 1UL )
        match lu1.Value with
        | :? BlockDeviceLU as x ->
            let pc_bd = new PrivateCaller( x )
            Assert.True(( ( pc_bd.GetField( "m_DeviceType" ) :?> BlockDeviceType ) = BlockDeviceType.BDT_Normal ))
        | _ ->
            Assert.Fail __LINE__

        ( killer :> IKiller ).NoticeTerminate()
        GlbFunc.DeleteFile mediaFileName
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLU_003() =
        let pDirName = this.GetTestDirName "GetLU_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let lu1 = sm.GetLU( lun_me.fromPrim 2UL )
        Assert.True(( lu1.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLU_004() =
        let pDirName = this.GetTestDirName "GetLU_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let m_LU0 = pc_sm.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
        Assert.True(( m_LU0.obj.Count = 0 ))

        let lu1 = sm.GetLU( lun_me.fromPrim 0UL )
        let m_LU1 = pc_sm.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
        Assert.True(( lu1.IsSome ))
        Assert.True(( m_LU1.obj.Count = 1 ))

        let lu2 = sm.GetLU( lun_me.fromPrim 0UL )
        let m_LU2 = pc_sm.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
        Assert.True(( lu2.IsSome ))
        Assert.True(( m_LU2.obj.Count = 1 ))

        Assert.True(( Functions.IsSame lu1.Value lu2.Value ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetLU_005() =
        let pDirName = this.GetTestDirName "GetLU_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let mutable cnt = 0
        let lun_stub = new CLU_Stub(
            p_GetLUResetStatus = ( fun () ->
                cnt <- cnt + 1
                true
            )
        ) 

        let m_LU1 = pc_sm.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
        m_LU1.Update( fun o ->
            o.Add( lun_me.fromPrim 0UL, lazy( lun_stub :> ILU ) )
        ) |> ignore

        let lu1 = sm.GetLU( lun_me.fromPrim 0UL )

        Assert.True(( m_LU1.obj.Count = 1 ))
        Assert.False(( Functions.IsSame lu1.Value ( lun_stub :> ILU  ) ))
        Assert.True(( cnt = 1 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateMedia_001() =
        let pDirName = this.GetTestDirName "CreateMedia_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let mediaFName = Functions.AppendPathName pDirName "a.txt"
        let s = File.CreateText( mediaFName )
        s.Write( Array.zeroCreate<char>( int Constants.MEDIA_BLOCK_SIZE ) )
        s.Close()
        s.Dispose()
        let mconf : TargetGroupConf.T_MEDIA = TargetGroupConf.T_MEDIA.U_PlainFile( {
            IdentNumber = mediaidx_me.fromPrim 1u;
            MediaName = "";
            FileName = Functions.AppendPathName pDirName "a.txt";
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1;
            WriteProtect = false;
        } )

        let me = sm.CreateMedia  mconf ( lun_me.fromPrim 1UL ) killer 
        Assert.True( me.BlockCount = 1UL )

        killer.NoticeTerminate()
        GlbFunc.DeleteFile mediaFName
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateMedia_002() =
        let pDirName = this.GetTestDirName "CreateMedia_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let mconf : TargetGroupConf.T_MEDIA = TargetGroupConf.T_MEDIA.U_DummyMedia({
            IdentNumber = mediaidx_me.fromPrim 1u;
            MediaName = "";
        })
        let me = sm.CreateMedia  mconf ( lun_me.fromPrim 1UL ) killer 
        Assert.True( me.BlockCount = 0UL )

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.NotifyLUReset_001() =
        let pDirName = this.GetTestDirName "NotifyLUReset_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )
        let m_LU = pc_sm.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >

        let lun_stub = new CLU_Stub()
        m_LU.Update( fun o ->
            o.Add( lun_me.fromPrim 0UL, lazy( lun_stub :> ILU ) )
        ) |> ignore

        sm.NotifyLUReset ( lun_me.fromPrim 0UL ) lun_stub

        Assert.True(( m_LU.obj.Count = 0 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.NotifyLUReset_002() =
        let pDirName = this.GetTestDirName "NotifyLUReset_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )
        let m_LU = pc_sm.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >

        let lun_stub = new CLU_Stub()
        m_LU.Update( fun o ->
            o.Add( lun_me.fromPrim 0UL, lazy( lun_stub :> ILU ) )
        )
        |> ignore
        sm.NotifyLUReset ( lun_me.fromPrim 1UL ) ( new CLU_Stub()  )

        Assert.True(( m_LU.obj.Count = 1 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
