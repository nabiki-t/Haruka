//=============================================================================
// Haruka Software Storage.
// StatusMasterTest.fs : Test cases for StatusMaster class.
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

type StatusMaster_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultTargetGroupConfStr idx eas =
        ( {
            TargetGroupID = tgid_me.fromPrim( uint32 idx );
            TargetGroupName = sprintf "a-%03d" idx;
            EnabledAtStart = eas;
            Target = 
                [{
                    IdentNumber = tnodeidx_me.fromPrim ( uint32 idx );
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

    let tgid0 = tgid_me.Zero
    let tgid1 = tgid_me.fromPrim( 1u )
    let tgid99 = tgid_me.fromPrim( 99u )

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    member _.GetTestFileName( fn : string ) =
        Functions.AppendPathName ( Path.GetTempPath() ) fn

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
                IdentNumber = tnodeidx_me.fromPrim 0u;
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
        Functions.AppendPathName ( Path.GetTempPath() ) "StatusMaster_Test_" + caseName

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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0  )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr 0 true )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus

        let np = sm.GetNetworkPortal()
        Assert.True(( np.Length = 1 ))
        Assert.True(( np.[0].IdentNumber = netportidx_me.fromPrim 0u ))

        Assert.True(( sm.GetActiveTarget().Length = 1 ))
        Assert.True(( sm.GetActiveTarget().[0].IdentNumber = tnodeidx_me.fromPrim 0u ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_002() =
        let pDirName = this.GetTestDirName "Constructor_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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
        Assert.True(( sm.GetActiveTarget().[0].IdentNumber = tnodeidx_me.fromPrim 1u ))

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
            lock.Release() |> ignore

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetNetworkPortal_001() =
        let pDirName = this.GetTestDirName "GetNetworkPortal_001"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus
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
            ( {
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
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
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
            ( {
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
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
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
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let atg = sm.GetActiveTargetGroup()
        Assert.True(( atg.Length = 0 ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTargetGroup_002() =
        let pDirName = this.GetTestDirName "GetActiveTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let atg = sm.GetActiveTargetGroup()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].TargetGroupID = tgid_me.fromPrim( 99u ) ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTargetGroup_003() =
        let pDirName = this.GetTestDirName "GetActiveTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus
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
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let atg = sm.GetActiveTarget()
        Assert.True(( atg.Length = 0 ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTarget_002() =
        let pDirName = this.GetTestDirName "GetActiveTarget_002"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let atg = sm.GetActiveTarget()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].IdentNumber = tnodeidx_me.fromPrim 99u ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetActiveTarget_003() =
        let pDirName = this.GetTestDirName "GetActiveTarget_003"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 99 true )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus
        let pc = PrivateCaller( sm )
        let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
        m_ActiveTargetGroups.TryAdd( 1u, () ) |> ignore

        let atg = sm.GetActiveTarget()
        Assert.True(( atg.Length = 1 ))
        Assert.True(( atg.[0].IdentNumber = tnodeidx_me.fromPrim 99u ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetFromLUN_001() =
        let pDirName = this.GetTestDirName "GetTargetFromLUN_001"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 88 false )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let tgl = sm.GetTargetFromLUN ( lun_me.fromPrim 89UL )
        Assert.True(( tgl.Length = 1 ))
        Assert.True(( tgl.[0].IdentNumber = tnodeidx_me.fromPrim 88u ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetFromLUN_002() =
        let pDirName = this.GetTestDirName "GetTargetFromLUN_002"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 88 true )

        let k = new HKiller()
        let sm = new StatusMaster( pDirName, k, stdin, stdout ) :> IStatus

        let tgl = sm.GetTargetFromLUN ( lun_me.fromPrim 1UL )
        Assert.True(( tgl.Length = 0 ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetFromLUN_003() =
        let pDirName = this.GetTestDirName "GetTargetFromLUN_003"
        GlbFunc.CreateDir pDirName |> ignore
        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let tdConf = 
            ( {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "A";
                EnableStatSNAckChecker = false;
            } : TargetDeviceConf.T_TargetDevice )
            |> TargetDeviceConf.ReaderWriter.ToString
        File.WriteAllText( targetDeviceConfName, tdConf )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let tgConf =
            ( {
                TargetGroupID = tgid_me.fromPrim( 1u );
                TargetGroupName = "TargetGroup001";
                EnabledAtStart = true;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "target000";
                        TargetAlias = "Target000";
                        LUN = [ lun_me.fromPrim ( uint64 1UL ); lun_me.fromPrim ( uint64 2UL ); ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    };
                    {
                        IdentNumber = tnodeidx_me.fromPrim 1u;
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
        Assert.True(( tgl2.[0].IdentNumber = tnodeidx_me.fromPrim 0u ))

        let tgl3 = sm.GetTargetFromLUN ( lun_me.fromPrim 2UL )
        Assert.True(( tgl3.Length = 2 ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateLoginNegociator_001() =
        let pDirName = this.GetTestDirName "CreateLoginNegociator_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test.defaultSessParam )
            )
        let m_Sessions1 =
            [
                KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, sess_stub1 );
            ]
            |> ImmutableDictionary.CreateRange
            |> OptimisticLock
        pc_sm.SetField( "m_Sessions", m_Sessions1 )

        // same I_T Nexus is already used.
        let r = sm.CreateNewSession itn1 ( tsih_me.fromPrim 2us ) StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_002() =
        let pDirName = this.GetTestDirName "CreateNewSession_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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
                                StatusMaster_Test.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
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

        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsSome ))
        Assert.True(( r.Value.I_TNexus.Equals itn2 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_003() =
        let pDirName = this.GetTestDirName "CreateNewSession_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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
                                StatusMaster_Test.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
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
        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_004() =
        let pDirName = this.GetTestDirName "CreateNewSession_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test.defaultSessParam )
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

        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsSome ))
        Assert.True(( r.Value.I_TNexus.Equals itn2 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_005() =
        let pDirName = this.GetTestDirName "CreateNewSession_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test.defaultSessParam )
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
        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_006() =
        let pDirName = this.GetTestDirName "CreateNewSession_006"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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
                                StatusMaster_Test.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
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

        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsSome ))
        Assert.True(( r.Value.I_TNexus.Equals itn2 ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_007() =
        let pDirName = this.GetTestDirName "CreateNewSession_007"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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
                                StatusMaster_Test.defaultSessParam with
                                    TargetConf = {
                                        StatusMaster_Test.defaultSessParam.TargetConf with
                                            IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
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
        let r = sm.CreateNewSession itn2 tsih2 StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
        Assert.True(( r.IsNone ))

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.CreateNewSession_008() =
        let pDirName = this.GetTestDirName "CreateNewSession_008"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let killer = new HKiller()
        let sm = new StatusMaster( pDirName, killer, stdin, stdout ) :> IStatus
        let pc_sm = new PrivateCaller( sm )

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us );
        let sess_stub1 =
            new CSession_Stub(
                p_GetI_TNexus = ( fun () -> itn1 ),
                p_GetSessionParameter = ( fun () -> StatusMaster_Test.defaultSessParam ),
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
        let r = sm.CreateNewSession itn2 ( tsih_me.fromPrim 1us ) StatusMaster_Test.defaultSessParam ( cmdsn_me.zero )
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
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

    [<Fact>]
    member this.ProcessControlRequest_001() =
        let pDirName = this.GetTestDirName "ProcessControlRequest_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let rq_in = new StreamReader( new MemoryStream() )
        let rq_out = new StreamWriter( new MemoryStream() )

        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, rq_in, rq_out ) :> IStatus

        killer.NoticeTerminate()

        sm.ProcessControlRequest().Wait()

        GlbFunc.AllDispose [ rq_in; rq_out; ]
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcessControlRequest_002() =
        let pDirName = this.GetTestDirName "ProcessControlRequest_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                rq_out.Dispose()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()

        GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcessControlRequest_003() =
        let pDirName = this.GetTestDirName "ProcessControlRequest_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                s.WriteLine( "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnexpectedError( x ) ->
                    Assert.True( x.Length > 0 )
                | _ ->
                    Assert.Fail __LINE__
                s.Close()
                o.Close()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetActiveTargetGroups_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetActiveTargetGroups_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetActiveTargetGroups()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( x ) ->
                    Assert.True( x.ActiveTGInfo.Length = 1 )
                    Assert.True( x.ActiveTGInfo.[0].ID = tgid0 )
                    Assert.True( x.ActiveTGInfo.[0].Name = "a-000" )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetActiveTargetGroups_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetActiveTargetGroups_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetActiveTargetGroups()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( x ) ->
                    Assert.True( x.ActiveTGInfo.Length = 0 )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLoadedTargetGroups_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLoadedTargetGroups_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 false )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLoadedTargetGroups()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadedTargetGroups( x ) ->
                    Assert.True( x.LoadedTGInfo.Length = 2 )
                    Assert.True( x.LoadedTGInfo.[0].ID = tgid0 )
                    Assert.True( x.LoadedTGInfo.[0].Name = "a-000" )
                    Assert.True( x.LoadedTGInfo.[1].ID = tgid1 )
                    Assert.True( x.LoadedTGInfo.[1].Name = "a-001" )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_InactivateTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_InactivateTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid0 )
                    Assert.True( x.Result )
                | _ ->
                    Assert.Fail __LINE__

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid1 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_InactivateTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_InactivateTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid99 )
                    Assert.True( x.Result )
                | _ ->
                    Assert.Fail __LINE__

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_ActivateTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_ActivateTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 0 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_ActivateTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid0 )
                    Assert.True( x.Result )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                let req2 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_ActivateTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req2 )
                s.Flush()

                let res2 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res2.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid99 )
                    Assert.False( x.Result )
                    Assert.True(( x.ErrorMessage = "Specified target group is missing." ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid0 )
                    Assert.False( x.Result )
                    Assert.True(( x.ErrorMessage = "Specified target group is still active." ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid99 )
                    Assert.True( x.Result )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                let m_sessions1 =
                    let ss = new CSession_Stub( p_GetSessionParameter = ( fun _ -> StatusMaster_Test.defaultSessParam ) )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid0 )
                    Assert.False( x.Result )
                    Assert.True(( x.ErrorMessage = "Specified target group is still used." ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid1 ) )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                // Target group 0 is still used.
                let m_sessions1 =
                    let ss = new CSession_Stub( p_GetSessionParameter = ( fun _ -> StatusMaster_Test.defaultSessParam ) )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                pc.SetField( "m_Sessions", m_sessions1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.True( m_config.GetAllTargetGroupConf().Length = 2 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid1 )
                    Assert.True( x.Result )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )
                Assert.True( ( fst ( m_config.GetAllTargetGroupConf().[0] ) ).TargetGroupID = tgid0 )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_005() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups1 = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups1.Count = 2 )

                // Target group 0 is still used.
                let m_sessions1 =
                    let ss = new CSession_Stub( p_GetSessionParameter = ( fun _ -> StatusMaster_Test.defaultSessParam ) )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                pc.SetField( "m_Sessions", m_sessions1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.True( m_config.GetAllTargetGroupConf().Length = 2 )

                // create LU object in tgid1
                sm.GetLU ( lun_me.fromPrim 2UL ) |> ignore

                let m_LU1 = pc.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
                Assert.True( m_LU1.obj.Count = 1 )

                // inactivate target group tgid1
                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid1 )
                    Assert.True( x.Result )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                let m_ActiveTargetGroups2 = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups2.Count = 1 )

                let m_LU2 = pc.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
                Assert.True( m_LU2.obj.Count = 1 )

                // Unload target group tgid1
                let req2 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req2 )
                s.Flush()

                let res2 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res2.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid1 )
                    Assert.True( x.Result )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                let m_LU3 = pc.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
                Assert.True( m_LU3.obj.Count = 0 )

                let m_ActiveTargetGroups3 = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups3.Count = 1 )
                Assert.True( m_ActiveTargetGroups3.ContainsKey( tgid_me.toPrim tgid0 ) )
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )
                Assert.True( ( fst ( m_config.GetAllTargetGroupConf().[0] ) ).TargetGroupID = tgid0 )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid0 )
                    Assert.False( x.Result )
                    Assert.True(( x.ErrorMessage = "Specified target group is still active." ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid99 )
                    Assert.False( x.Result )
                    Assert.True(( x.ErrorMessage = "Failed to load target group config." ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.True( m_ActiveTargetGroups.Count = 1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.True( m_config.GetAllTargetGroupConf().Length = 1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    Assert.True( x.ID = tgid1 )
                    Assert.True( x.Result )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                Assert.True( m_ActiveTargetGroups.Count = 1 )
                Assert.True( m_config.GetAllTargetGroupConf().Length = 2 )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_SetLogParameters_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_SetLogParameters_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )


                let lock = GlbFunc.LogParamUpdateLock()
                try
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_VERBOSE, stderr )

                    let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                        Request = TargetDeviceCtrlReq.T_Request.U_SetLogParameters( {
                            SoftLimit = 9999u;
                            HardLimit = 999u;
                            LogLevel = LogLevel.LOGLEVEL_INFO;
                        })
                    }
                    s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                    s.Flush()

                    let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( x ) ->
                        Assert.False( x )
                    | _ ->
                        Assert.Fail __LINE__

                    let softLimit, hardLimit, lv = HLogger.GetLogParameters()
                    Assert.True(( softLimit = 10000u ))
                    Assert.True(( hardLimit = 10000u ))
                    Assert.True(( lv = LogLevel.LOGLEVEL_VERBOSE ))
                finally
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
                    lock.Release() |> ignore

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_SetLogParameters_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_SetLogParameters_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let lock = GlbFunc.LogParamUpdateLock()
                try
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_VERBOSE, stderr )

                    let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                        Request = TargetDeviceCtrlReq.T_Request.U_SetLogParameters( {
                            SoftLimit = 9999u;
                            HardLimit = 99999u;
                            LogLevel = LogLevel.LOGLEVEL_INFO;
                        })
                    }
                    s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                    s.Flush()

                    let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( x ) ->
                        Assert.True( x )
                    | _ ->
                        Assert.Fail __LINE__

                    let softLimit, hardLimit, lv = HLogger.GetLogParameters()
                    Assert.True(( softLimit = 9999u ))
                    Assert.True(( hardLimit = 99999u ))
                    Assert.True(( lv = LogLevel.LOGLEVEL_INFO ))
                finally
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
                    lock.Release() |> ignore

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLogParameters_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLogParameters_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let lock = GlbFunc.LogParamUpdateLock()
                try
                    HLogger.SetLogParameters( 1234u, 2345u, 0u, LogLevel.LOGLEVEL_WARNING, stderr )

                    let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                        Request = TargetDeviceCtrlReq.T_Request.U_GetLogParameters()
                    }
                    s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                    s.Flush()

                    let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_LogParameters( x ) ->
                        Assert.True(( x.SoftLimit = 1234u ))
                        Assert.True(( x.HardLimit = 2345u ))
                        Assert.True(( x.LogLevel = LogLevel.LOGLEVEL_WARNING ))
                    | _ ->
                        Assert.Fail __LINE__
                finally
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
                    lock.Release() |> ignore

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetDeviceName_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetDeviceName_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "abcdefg";
            EnableStatSNAckChecker = false;
        }
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetDeviceName()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_DeviceName( x ) ->
                    Assert.True(( x = "abcdefg" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTargetDevice() )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.True(( x.Session.Length = 0 ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let wCreateDate = DateTime.UtcNow
                let m_sessions1 =
                    let ss = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> StatusMaster_Test.defaultSessParam ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "tn0", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> wCreateDate )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTargetDevice() )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.True(( x.Session.Length = 1 ))
                    Assert.True(( x.Session.[0].TargetGroupID = tgid0 ))
                    Assert.True(( x.Session.[0].TargetNodeID = tnodeidx_me.fromPrim 0u ))
                    Assert.True(( x.Session.[0].EstablishTime = wCreateDate ))
                    Assert.True(( x.Session.[0].TSIH = tsih_me.fromPrim 1us ))
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 1u;
                                        TargetName = "target001001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target001001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss21 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 2u );
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 21u;
                                        TargetName = "target002001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss22 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 2u );
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 22u;
                                        TargetName = "target002002";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002002", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss21 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, ss22 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTargetGroup( tgid_me.fromPrim 2u ) )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.True(( x.Session.Length = 2 ))
                    let rss1, rss2 =
                        if x.Session.[0].TargetNodeID = tnodeidx_me.fromPrim 21u then
                            x.Session.[0], x.Session.[1]
                        else
                            x.Session.[1], x.Session.[2]
                    Assert.True(( rss1.TargetGroupID = tgid_me.fromPrim( 2u ) ))
                    Assert.True(( rss1.TargetNodeID = tnodeidx_me.fromPrim 21u ))
                    Assert.True(( rss2.TargetGroupID = tgid_me.fromPrim( 2u ) ))
                    Assert.True(( rss2.TargetNodeID = tnodeidx_me.fromPrim 22u ))
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 21u;
                                        TargetName = "target002001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in1", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 99u;
                                        TargetName = "target999999";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in2", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target999999", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss3 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 21u;
                                        TargetName = "target002001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in3", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, ss3 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTarget( tnodeidx_me.fromPrim 21u ) )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.True(( x.Session.Length = 2 ))
                    Assert.True(( x.Session.[0].TargetGroupID = tgid_me.fromPrim( 1u ) ))
                    Assert.True(( x.Session.[1].TargetGroupID = tgid_me.fromPrim( 1u ) ))
                    Assert.True(( x.Session.[0].TargetNodeID = tnodeidx_me.fromPrim 21u ))
                    Assert.True(( x.Session.[1].TargetNodeID = tnodeidx_me.fromPrim 21u ))
                    let in0 = x.Session.[0].ITNexus.InitiatorName
                    let in1 = x.Session.[1].ITNexus.InitiatorName
                    Assert.True(( ( in0 = "in1" && in1 = "in3" ) || ( in0 = "in1" && in1 = "in3" ) ))
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_DestructSession_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_DestructSession_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_DestructSession( tsih_me.fromPrim 0us )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_DestructSessionResult( x ) ->
                    Assert.True(( x.TSIH = tsih_me.fromPrim 0us ))
                    Assert.False(( x.Result ))
                    Assert.True(( x.ErrorMessage.StartsWith "Unknown session" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_DestructSession_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_DestructSession_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let mutable flg = 0

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in1", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_DestroySession = ( fun _ -> flg <- flg + 1 )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_DestructSession( tsih_me.fromPrim 1us )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_DestructSessionResult( x ) ->
                    Assert.True(( x.TSIH = tsih_me.fromPrim 1us ))
                    Assert.True(( x.Result ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( flg = 1 ))
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 3us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 3L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 4us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 4L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTargetDevice()
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let linestr = o.ReadLine()
                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( linestr )
                let wConList =
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                        x.Connection
                    | _ ->
                        Assert.Fail __LINE__
                        []

                let wl = wConList |> List.sortBy ( fun itr -> itr.ConnectionID )
                Assert.True(( wl.Length = 5 ))

                Assert.True(( wl.[0].TSIH = tsih_me.fromPrim 1us ))
                Assert.True(( wl.[0].ConnectionID = cid_me.fromPrim 0us ))
                Assert.True(( wl.[0].ConnectionCount = concnt_me.fromPrim 0 ))
                Assert.True(( wl.[0].ReceiveBytesCount.Length = 0 ))
                Assert.True(( wl.[0].SentBytesCount.Length = 0 ))
                Assert.True(( wl.[0].EstablishTime = DateTime( 0L, DateTimeKind.Utc ) ))

                Assert.True(( wl.[1].TSIH = tsih_me.fromPrim 1us ))
                Assert.True(( wl.[1].ConnectionID = cid_me.fromPrim 1us ))
                Assert.True(( wl.[1].ConnectionCount = concnt_me.fromPrim 1 ))
                Assert.True(( wl.[1].ReceiveBytesCount.Length = 1 ))
                Assert.True(( wl.[1].ReceiveBytesCount.[0].Value = 0L ))
                Assert.True(( wl.[1].SentBytesCount.Length = 1 ))
                Assert.True(( wl.[1].SentBytesCount.[0].Value = 0L ))
                Assert.True(( wl.[1].EstablishTime = DateTime( 1L, DateTimeKind.Utc ) ))

                Assert.True(( wl.[2].TSIH = tsih_me.fromPrim 2us ))
                Assert.True(( wl.[2].ConnectionID = cid_me.fromPrim 2us ))
                Assert.True(( wl.[2].ConnectionCount = concnt_me.fromPrim 0 ))
                Assert.True(( wl.[2].ReceiveBytesCount.Length = 1 ))
                Assert.True(( wl.[2].ReceiveBytesCount.[0].Value = 1L ))
                Assert.True(( wl.[2].SentBytesCount.Length = 1 ))
                Assert.True(( wl.[2].SentBytesCount.[0].Value = 1L ))
                Assert.True(( wl.[2].EstablishTime = DateTime( 2L, DateTimeKind.Utc ) ))

                Assert.True(( wl.[3].TSIH = tsih_me.fromPrim 2us ))
                Assert.True(( wl.[3].ConnectionID = cid_me.fromPrim 3us ))
                Assert.True(( wl.[3].ConnectionCount = concnt_me.fromPrim 0 ))
                Assert.True(( wl.[3].ReceiveBytesCount.Length = 1 ))
                Assert.True(( wl.[3].ReceiveBytesCount.[0].Value = 2L ))
                Assert.True(( wl.[3].SentBytesCount.Length = 1 ))
                Assert.True(( wl.[3].SentBytesCount.[0].Value = 2L ))
                Assert.True(( wl.[3].EstablishTime = DateTime( 3L, DateTimeKind.Utc ) ))

                Assert.True(( wl.[4].TSIH = tsih_me.fromPrim 2us ))
                Assert.True(( wl.[4].ConnectionID = cid_me.fromPrim 4us ))
                Assert.True(( wl.[4].ConnectionCount = concnt_me.fromPrim 0 ))
                Assert.True(( wl.[4].ReceiveBytesCount.Length = 1 ))
                Assert.True(( wl.[4].ReceiveBytesCount.[0].Value = 3L ))
                Assert.True(( wl.[4].SentBytesCount.Length = 1 ))
                Assert.True(( wl.[4].SentBytesCount.[0].Value = 3L ))
                Assert.True(( wl.[4].EstablishTime = DateTime( 4L, DateTimeKind.Utc ) ))

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) ),
                                    p_NetPortIdx = ( fun () -> netportidx_me.fromPrim 1u )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) ),
                                    p_NetPortIdx = ( fun () -> netportidx_me.fromPrim 2u )
                                ) :> IConnection;
                            |]
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) ),
                                    p_NetPortIdx = ( fun () -> netportidx_me.fromPrim 1u )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInNetworkPortal( netportidx_me.fromPrim 2u )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.True(( wl.Length = 1 ))

                    Assert.True(( wl.[0].TSIH = tsih_me.fromPrim 1us ))
                    Assert.True(( wl.[0].ConnectionID = cid_me.fromPrim 1us ))
                    Assert.True(( wl.[0].ConnectionCount = concnt_me.fromPrim 1 ))
                    Assert.True(( wl.[0].ReceiveBytesCount.Length = 1 ))
                    Assert.True(( wl.[0].ReceiveBytesCount.[0].Value = 0L ))
                    Assert.True(( wl.[0].SentBytesCount.Length = 1 ))
                    Assert.True(( wl.[0].SentBytesCount.[0].Value = 0L ))
                    Assert.True(( wl.[0].EstablishTime = DateTime( 1L, DateTimeKind.Utc ) ))

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let tgid1 = GlbFunc.newTargetGroupID()
                let tgid2 = GlbFunc.newTargetGroupID()
                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid1 }
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test.defaultSessParam with
                                TargetGroupID = tgid2 }
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTargetGroup( tgid2 )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.True(( wl.Length = 1 ))

                    Assert.True(( wl.[0].TSIH = tsih_me.fromPrim 2us ))
                    Assert.True(( wl.[0].ConnectionID = cid_me.fromPrim 2us ))
                    Assert.True(( wl.[0].ConnectionCount = concnt_me.fromPrim 0 ))
                    Assert.True(( wl.[0].ReceiveBytesCount.Length = 1 ))
                    Assert.True(( wl.[0].ReceiveBytesCount.[0].Value = 1L ))
                    Assert.True(( wl.[0].SentBytesCount.Length = 1 ))
                    Assert.True(( wl.[0].SentBytesCount.[0].Value = 1L ))
                    Assert.True(( wl.[0].EstablishTime = DateTime( 2L, DateTimeKind.Utc ) ))

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test.defaultSessParam with
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 0u;
                                }
                            }
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test.defaultSessParam with
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 0u;
                                }
                            }
                        )
                    )
                    let ss3 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 3us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test.defaultSessParam with
                                TargetConf = {
                                    StatusMaster_Test.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 1u;
                                }
                            }
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, ss3 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTarget( tnodeidx_me.fromPrim 0u )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.True(( wl.Length = 2 ))

                    Assert.True(( wl.[0].TSIH = tsih_me.fromPrim 1us ))
                    Assert.True(( wl.[0].ConnectionID = cid_me.fromPrim 0us ))
                    Assert.True(( wl.[0].ConnectionCount = concnt_me.fromPrim 0 ))
                    Assert.True(( wl.[0].ReceiveBytesCount.Length = 0 ))
                    Assert.True(( wl.[0].SentBytesCount.Length = 0 ))
                    Assert.True(( wl.[0].EstablishTime = DateTime( 0L, DateTimeKind.Utc ) ))

                    Assert.True(( wl.[1].TSIH = tsih_me.fromPrim 2us ))
                    Assert.True(( wl.[1].ConnectionID = cid_me.fromPrim 1us ))
                    Assert.True(( wl.[1].ConnectionCount = concnt_me.fromPrim 1 ))
                    Assert.True(( wl.[1].ReceiveBytesCount.Length = 1 ))
                    Assert.True(( wl.[1].ReceiveBytesCount.[0].Value = 0L ))
                    Assert.True(( wl.[1].SentBytesCount.Length = 1 ))
                    Assert.True(( wl.[1].SentBytesCount.[0].Value = 0L ))
                    Assert.True(( wl.[1].EstablishTime = DateTime( 1L, DateTimeKind.Utc ) ))

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_005() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInSession( tsih_me.fromPrim 1us )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.True(( wl.Length = 2 ))

                    Assert.True(( wl.[0].TSIH = tsih_me.fromPrim 1us ))
                    Assert.True(( wl.[0].ConnectionID = cid_me.fromPrim 0us ))
                    Assert.True(( wl.[0].ConnectionCount = concnt_me.fromPrim 0 ))
                    Assert.True(( wl.[0].ReceiveBytesCount.Length = 0 ))
                    Assert.True(( wl.[0].SentBytesCount.Length = 0 ))
                    Assert.True(( wl.[0].EstablishTime = DateTime( 0L, DateTimeKind.Utc ) ))

                    Assert.True(( wl.[1].TSIH = tsih_me.fromPrim 1us ))
                    Assert.True(( wl.[1].ConnectionID = cid_me.fromPrim 1us ))
                    Assert.True(( wl.[1].ConnectionCount = concnt_me.fromPrim 1 ))
                    Assert.True(( wl.[1].ReceiveBytesCount.Length = 1 ))
                    Assert.True(( wl.[1].ReceiveBytesCount.[0].Value = 0L ))
                    Assert.True(( wl.[1].SentBytesCount.Length = 1 ))
                    Assert.True(( wl.[1].SentBytesCount.[0].Value = 0L ))
                    Assert.True(( wl.[1].EstablishTime = DateTime( 1L, DateTimeKind.Utc ) ))

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_006() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_006"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInSession( tsih_me.fromPrim 9999us )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    Assert.True(( x.Connection.Length = 0 ))
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_007() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_007"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTargetDevice()
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    Assert.True(( x.Connection.Length = 0 ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 0UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 0UL ))
                    Assert.True(( x.ErrorMessage.StartsWith "Missing LU" ))
                    Assert.True(( x.LUStatus_Success.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.LUStatus_Success.IsSome ))
                    Assert.True(( x.LUStatus_Success.Value.ReadBytesCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.WrittenBytesCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.ReadTickCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.WriteTickCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.ACAStatus.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lazy( new CLU_Stub() :> ILU ) )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.LUStatus_Success.IsSome ))
                    Assert.True(( x.LUStatus_Success.Value.ReadBytesCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.WrittenBytesCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.ReadTickCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.WriteTickCount.Length = 0 ))
                    Assert.True(( x.LUStatus_Success.Value.ACAStatus.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let itn1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )

                let m_LUs1 =
                    let lu1 = lazy (
                        new CLU_Stub(
                            p_GetReadBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                            p_GetWrittenBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 1L; } |] ),
                            p_GetReadTickCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 1L; } |] ),
                            p_GetWriteTickCount = ( fun () -> [| { Time = DateTime(); Value = 4L; Count = 1L; } |] ),
                            p_ACAStatus = ( fun () ->  ValueSome ( itn1, ScsiCmdStatCd.CHECK_CONDITION, SenseKeyCd.NOT_READY, ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT, true ) )
                        ) :> ILU
                    )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                let res2 = res1.Response
                match res2 with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.LUStatus_Success.IsSome ))
                    let sucVal = x.LUStatus_Success.Value
                    Assert.True(( sucVal.ReadBytesCount.Length = 1 ))
                    Assert.True(( sucVal.ReadBytesCount.[0].Value = 1L ))
                    Assert.True(( sucVal.WrittenBytesCount.Length = 1 ))
                    Assert.True(( sucVal.WrittenBytesCount.[0].Value = 2L ))
                    Assert.True(( sucVal.ReadTickCount.Length = 1 ))
                    Assert.True(( sucVal.ReadTickCount.[0].Value = 3L ))
                    Assert.True(( sucVal.WriteTickCount.Length = 1 ))
                    Assert.True(( sucVal.WriteTickCount.[0].Value = 4L ))
                    Assert.True(( sucVal.ACAStatus.IsSome ))
                    let acaVal = sucVal.ACAStatus.Value
                    Assert.True(( acaVal.ITNexus.InitiatorName = "initiator000" ))
                    Assert.True(( acaVal.StatusCode = uint8 ScsiCmdStatCd.CHECK_CONDITION ))
                    Assert.True(( acaVal.AdditionalSenseCode = uint16 ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
                    Assert.True(( acaVal.IsCurrent ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 99UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 99UL ))
                    Assert.True(( x.Result = false ))
                    Assert.True(( x.ErrorMessage.StartsWith "Specified LU is not configured" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.Result = true ))
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lazy( new CLU_Stub() :> ILU ) )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.Result = true ))
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let mutable flg = false

                let m_LUs1 =
                    let lu1 = lazy (
                        new CLU_Stub(
                            p_LogicalUnitReset = ( fun s itt needResp ->
                                Assert.True(( s.IsNone ))
                                Assert.True(( itt.IsNone ))
                                Assert.False needResp
                                flg <- true
                            )
                        ) :> ILU
                    )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.Result ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( flg ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 99UL;
                        ID = mediaidx_me.fromPrim 99u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 99UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 99u ))
                    Assert.True(( x.ErrorMessage.StartsWith "Specified LU is not configured" ))
                    Assert.True(( x.MediaStatus_Success.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 99u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 99u ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.MediaStatus_Success.IsSome ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lazy( new CLU_Stub() :> ILU ) )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 99u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 99u ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.MediaStatus_Success.IsSome ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    let media1 =
                        new CMedia_Stub(
                            p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 1u ),
                            p_GetSubMedia = ( fun () -> [] ),
                            p_GetReadBytesCount = ( fun () -> Array.empty ),
                            p_GetWrittenBytesCount = ( fun () -> Array.empty ),
                            p_GetReadTickCount = ( fun () -> Array.empty ),
                            p_GetWriteTickCount = ( fun () -> Array.empty )
                        )
                    let lu1 = lazy ( new CLU_Stub( p_GetMedia = ( fun () -> media1 ) ) :> ILU )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 1u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 1u ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.MediaStatus_Success.IsSome ))
                    Assert.True(( x.MediaStatus_Success.Value.ReadBytesCount.Length = 0 ))
                    Assert.True(( x.MediaStatus_Success.Value.WrittenBytesCount.Length = 0 ))
                    Assert.True(( x.MediaStatus_Success.Value.ReadTickCount.Length = 0 ))
                    Assert.True(( x.MediaStatus_Success.Value.WriteTickCount.Length = 0 ))

                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_005() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    let media4 =
                        new CMedia_Stub(
                            p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 4u ),
                            p_GetSubMedia = ( fun () -> [] ),
                            p_GetReadBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                            p_GetWrittenBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 0L; } |] ),
                            p_GetReadTickCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 0L; } |] ),
                            p_GetWriteTickCount = ( fun () -> [| { Time = DateTime(); Value = 4L; Count = 0L; } |] )
                        )
                    let media3 = new CMedia_Stub(
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 3u ),
                        p_GetSubMedia = ( fun () -> [ media4 ] )
                    )
                    let media2 = new CMedia_Stub( 
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 2u ),
                        p_GetSubMedia = ( fun () -> [] )
                    )
                    let media1 = new CMedia_Stub( 
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 1u ),
                        p_GetSubMedia = ( fun () -> [ media2; media3; ] )
                    )
                    let lu1 = lazy ( new CLU_Stub( p_GetMedia = ( fun () -> media1 ) ) :> ILU )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 4u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 4u ))
                    Assert.True(( x.ErrorMessage = "" ))
                    Assert.True(( x.MediaStatus_Success.IsSome ))
                    Assert.True(( x.MediaStatus_Success.Value.ReadBytesCount.Length = 1 ))
                    Assert.True(( x.MediaStatus_Success.Value.ReadBytesCount.[0].Value = 1L ))
                    Assert.True(( x.MediaStatus_Success.Value.WrittenBytesCount.Length = 1 ))
                    Assert.True(( x.MediaStatus_Success.Value.WrittenBytesCount.[0].Value = 2L ))
                    Assert.True(( x.MediaStatus_Success.Value.ReadTickCount.Length = 1 ))
                    Assert.True(( x.MediaStatus_Success.Value.ReadTickCount.[0].Value = 3L ))
                    Assert.True(( x.MediaStatus_Success.Value.WriteTickCount.Length = 1 ))
                    Assert.True(( x.MediaStatus_Success.Value.WriteTickCount.[0].Value = 4L ))

                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 99UL;
                        ID = mediaidx_me.fromPrim 4u;
                        Request = "aaaaaaa";
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 99UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 4u ))
                    Assert.True(( x.ErrorMessage.StartsWith "Specified LU is not configured" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 4u;
                        Request = "aaaaaaa";
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 4u ))
                    Assert.True(( x.ErrorMessage.StartsWith "Specified media missing" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )

        let tgConfStr =
            ( {
                TargetGroupID = tgid_me.fromPrim 0u;
                TargetGroupName = "a-000";
                EnabledAtStart = true;
                Target = 
                    [{
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "target000";
                        TargetAlias = "target000";
                        LUN = [ lun_me.fromPrim 1UL ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }];
                LogicalUnit =
                    [{
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "luname";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_BlockDevice({
                            Peripheral = TargetGroupConf.U_DebugMedia({
                                IdentNumber = mediaidx_me.fromPrim 1u;
                                MediaName = "debugmedia";
                                Peripheral = TargetGroupConf.U_DummyMedia({
                                    IdentNumber = mediaidx_me.fromPrim 2u;
                                    MediaName = "dummymedia";
                                })
                            })
                            FallbackBlockSize = Blocksize.BS_512;
                            OptimalTransferLength = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH;
                        });
                    }];
            } : TargetGroupConf.T_TargetGroup )
            |> TargetGroupConf.ReaderWriter.ToString
        File.WriteAllText( targetGroupConfName0, tgConfStr )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let mediaCtrlReqStr =
                    MediaCtrlReq.ReaderWriter.ToString {
                        Request = MediaCtrlReq.U_Debug(
                            MediaCtrlReq.U_ClearTraps()
                        )
                    }

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 1u;
                        Request = mediaCtrlReqStr;
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 1u ))
                    let resData = MediaCtrlRes.ReaderWriter.LoadString x.Response
                    match resData.Response with
                    | MediaCtrlRes.U_Debug( x ) ->
                        match x with
                        | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                            Assert.True(( y.Result ))
                            Assert.True(( y.ErrorMessage = "" ))
                        | _ ->
                            Assert.Fail __LINE__
                    | _ ->
                        Assert.Fail __LINE__
                    Assert.True(( x.ErrorMessage ="" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    let media1 = new CMedia_Stub( 
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 1u ),
                        p_GetSubMedia = ( fun () -> [] ),
                        p_MediaControl = ( fun request -> task {
                            return MediaCtrlRes.U_Debug(
                                MediaCtrlRes.U_ClearTrapsResult({
                                    Result = true;
                                    ErrorMessage = "ggggg";
                                })
                            )
                        })
                    )
                    let lu1 = lazy (
                        new CLU_Stub(
                            p_GetMedia = ( fun () -> media1 ),
                            p_GetLUResetStatus = ( fun () -> false )
                        ) :> ILU
                    )
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let mediaCtrlReqStr =
                    MediaCtrlReq.ReaderWriter.ToString {
                        Request = MediaCtrlReq.U_Debug(
                            MediaCtrlReq.U_ClearTraps()
                        )
                    }

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 1u;
                        Request = mediaCtrlReqStr;
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 1u ))
                    let resData = MediaCtrlRes.ReaderWriter.LoadString x.Response
                    match resData.Response with
                    | MediaCtrlRes.U_Debug( x ) ->
                        match x with
                        | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                            Assert.True(( y.Result ))
                            Assert.True(( y.ErrorMessage = "ggggg" ))
                        | _ ->
                            Assert.Fail __LINE__
                    | _ ->
                        Assert.Fail __LINE__
                    Assert.True(( x.ErrorMessage ="" ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
