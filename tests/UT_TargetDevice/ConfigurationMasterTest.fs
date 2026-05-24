//=============================================================================
// Haruka Software Storage.
// ConfigurationMasterTest.fs : Test cases for ConfigurationMaster class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test

//=============================================================================
// Class implementation

type ConfigurationMaster_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let getAllTagetConf ( tgc : ( TargetGroupConf.T_TargetGroup * IKiller ) [] ) =
        [
            for ( i, _ ) in tgc do
                yield! i.Target
        ]
        |> List.sortBy ( fun itr -> itr.IdentNumber )
        |> List.toArray

    let getAllLUConf ( tgc : ( TargetGroupConf.T_TargetGroup * IKiller ) [] ) =
        [
            for ( i, _ ) in tgc do
                yield! i.LogicalUnit
        ]
        |> List.sortBy ( fun itr -> itr.LUN )
        |> List.toArray

    let tgid0 = tgid_me.Zero
    let tgid1 = tgid_me.fromPrim( 1u )
    let tgid2 = tgid_me.fromPrim( 2u )
    let tgid98 = tgid_me.fromPrim( 98u )
    let tgid99 = tgid_me.fromPrim( 99u )

    let defaultTargetGroupConfStr =
        let targetGroupConfStr2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.Zero;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [ {
                    IdentNumber = tnodeidx_me.fromPrim 1us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 1UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.ToString targetGroupConfStr2
       
    let genTargetGroupConfStr ( tgid : TGID_T ) ( tidx : TNODEIDX_T ) ( targetName : string ) ( lun: LUN_T ) =
        let targetGroupConfStr2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [ {
                    IdentNumber = tidx;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = targetName;
                    TargetAlias = "target001";
                    LUN = [ lun ];
                    Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [{
                    LUN = lun;
                    LUName = "lunametest";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.ToString targetGroupConfStr2

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.ReleaseMutex() |> ignore

    member _.GetTestDirName ( caseName : string ) =
        Functions.AppendPathName ( Path.GetTempPath() ) "ConfigurationMaster_Test_" + caseName

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.LoadConfig_001() =
        let pDirName = this.GetTestDirName "LoadConfig_001"
        GlbFunc.CreateDir pDirName |> ignore
        Assert.ThrowsAny( fun () ->
            new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
        ) |> ignore
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_002() =
        let pDirName = this.GetTestDirName "LoadConfig_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        GlbFunc.CreateDir targetDeviceConfName |> ignore

        Assert.ThrowsAny( fun () ->
            new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
        ) |> ignore

        GlbFunc.DeleteDir targetDeviceConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_003() =
        let pDirName = this.GetTestDirName "LoadConfig_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConfStr = "aaaa"
        File.WriteAllText( targetDeviceConfName, targetDeviceConfStr )

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr )

        Assert.ThrowsAny( fun () ->
            new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
        ) |> ignore

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_004() =
        let pDirName = this.GetTestDirName "LoadConfig_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aassddff";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.StrictEqual( "aassddff", cm.DeviceName )
        Assert.False( cm.EnableStatSNAckChecker )
        Assert.StrictEqual( 1, cm.GetNetworkPortal().Length )
        Assert.StrictEqual( 3260us, cm.GetNetworkPortal().[0].PortNumber )
        Assert.StrictEqual( 1, cm.GetAllTargetGroupConf().Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_005() =
        let pDirName = this.GetTestDirName "LoadConfig_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [
                {
                    IdentNumber = netportidx_me.fromPrim 0u;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetAddress = "";
                    PortNumber = 2360us;
                    DisableNagle = false;
                    ReceiveBufferSize = 8192;
                    SendBufferSize = 8192;
                    WhiteList = [];
                };
                {
                    IdentNumber = netportidx_me.fromPrim 1u;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetAddress = "";
                    PortNumber = 2361us;
                    DisableNagle = false;
                    ReceiveBufferSize = 8192;
                    SendBufferSize = 8192;
                    WhiteList = [ IPCondition.Any; ];
                };
                {
                    IdentNumber = netportidx_me.fromPrim 2u;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetAddress = "";
                    PortNumber = 2362us;
                    DisableNagle = false;
                    ReceiveBufferSize = 8192;
                    SendBufferSize = 8192;
                    WhiteList = [ IPCondition.Global; IPCondition.IPv4Linklocal; ];
                };
            ];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "rrffgghh";
            EnableStatSNAckChecker = true;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.StrictEqual( "rrffgghh", cm.DeviceName )
        Assert.True( cm.EnableStatSNAckChecker )
        Assert.StrictEqual( 3, cm.GetNetworkPortal().Length )
        Assert.StrictEqual( 2360us, cm.GetNetworkPortal().[0].PortNumber )
        Assert.StrictEqual( [], cm.GetNetworkPortal().[0].WhiteList )
        Assert.StrictEqual( 2361us, cm.GetNetworkPortal().[1].PortNumber )
        Assert.StrictEqual( [ IPCondition.Any; ], cm.GetNetworkPortal().[1].WhiteList )
        Assert.StrictEqual( 2362us, cm.GetNetworkPortal().[2].PortNumber )
        Assert.StrictEqual( [ IPCondition.Global; IPCondition.IPv4Linklocal; ], cm.GetNetworkPortal().[2].WhiteList )
        Assert.StrictEqual( 1, cm.GetAllTargetGroupConf().Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_006() =
        let pDirName = this.GetTestDirName "LoadConfig_006"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = Some {
                MaxRecvDataSegmentLength = 512u;
                MaxBurstLength = 512u;
                FirstBurstLength = 512u;
                DefaultTime2Wait = 99us;
                DefaultTime2Retain = 99us;
                MaxOutstandingR2T = 3us;
            };
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.StrictEqual( 1, cm.GetNetworkPortal().Length )
        Assert.StrictEqual( 512u, cm.IscsiNegoParamCO.MaxRecvDataSegmentLength_T )
        Assert.StrictEqual( 512u, cm.IscsiNegoParamSW.MaxBurstLength )
        Assert.StrictEqual( 512u, cm.IscsiNegoParamSW.FirstBurstLength )
        Assert.StrictEqual( 99us, cm.IscsiNegoParamSW.DefaultTime2Wait )
        Assert.StrictEqual( 99us, cm.IscsiNegoParamSW.DefaultTime2Retain )
        Assert.StrictEqual( 3us, cm.IscsiNegoParamSW.MaxOutstandingR2T )
        Assert.StrictEqual( 1, cm.GetAllTargetGroupConf().Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_007() =
        let pDirName = this.GetTestDirName "LoadConfig_007"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 1us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target000";
                    TargetAlias = "target000";
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                {
                    IdentNumber = tnodeidx_me.fromPrim 2us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
                }
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
                }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName targetGroupConf

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        let tgc = cm.GetAllTargetGroupConf()
        let targets = getAllTagetConf tgc
        let lus = getAllLUConf tgc
        Assert.StrictEqual( 2, targets.Length )
        Assert.StrictEqual( tnodeidx_me.fromPrim 1us , targets.[0].IdentNumber )
        Assert.StrictEqual( tpgt_me.fromPrim 0us, targets.[0].TargetPortalGroupTag )
        Assert.StrictEqual( "target000", targets.[0].TargetName )
        Assert.StrictEqual( "target000", targets.[0].TargetAlias )
        Assert.StrictEqual( 2, targets.[0].LUN.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, targets.[0].LUN.[0] )
        Assert.StrictEqual( lun_me.fromPrim 2UL, targets.[0].LUN.[1] )
        Assert.StrictEqual( TargetGroupConf.T_Auth.U_None(), targets.[0].Auth )
        Assert.StrictEqual( tnodeidx_me.fromPrim 2us, targets.[1].IdentNumber )
        Assert.StrictEqual( tpgt_me.fromPrim 0us, targets.[1].TargetPortalGroupTag )
        Assert.StrictEqual( "target001", targets.[1].TargetName )
        Assert.StrictEqual( "target001", targets.[1].TargetAlias )
        Assert.StrictEqual( 2, targets.[1].LUN.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, targets.[1].LUN.[0] )
        Assert.StrictEqual( lun_me.fromPrim 2UL, targets.[1].LUN.[1] )
        Assert.StrictEqual( TargetGroupConf.T_Auth.U_None(), targets.[1].Auth )

        Assert.StrictEqual( 2, lus.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus.[0].LUN )
        Assert.StrictEqual( "luname001", lus.[0].LUName )
        Assert.StrictEqual( TargetGroupConf.T_DEVICE.U_DummyDevice(), lus.[0].LUDevice )
        Assert.StrictEqual( Functions.AppendPathName pDirName "LU_1", lus.[0].WorkPath )
        Assert.StrictEqual( lun_me.fromPrim 2UL, lus.[1].LUN )
        Assert.StrictEqual( "luname002", lus.[1].LUName )
        Assert.StrictEqual( TargetGroupConf.T_DEVICE.U_DummyDevice(), lus.[1].LUDevice )
        Assert.StrictEqual( Functions.AppendPathName pDirName "LU_2", lus.[1].WorkPath )

        Assert.StrictEqual( 1, tgc.Length )
        Assert.StrictEqual( tgid_me.Zero, ( fst tgc.[0] ).TargetGroupID )
        Assert.StrictEqual( "a", ( fst tgc.[0] ).TargetGroupName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_008() =
        let pDirName = this.GetTestDirName "LoadConfig_008"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 2us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
                }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        let tgc = cm.GetAllTargetGroupConf()
        let targets = getAllTagetConf tgc
        let lus = getAllLUConf tgc
        Assert.StrictEqual( 2, targets.Length )
        Assert.StrictEqual( tnodeidx_me.fromPrim 1us, targets.[0].IdentNumber )
        Assert.StrictEqual( tpgt_me.fromPrim 0us, targets.[0].TargetPortalGroupTag )
        Assert.StrictEqual( "target001", targets.[0].TargetName )
        Assert.StrictEqual( "target001", targets.[0].TargetAlias )
        Assert.StrictEqual( 1, targets.[0].LUN.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, targets.[0].LUN.[0] )
        Assert.StrictEqual( TargetGroupConf.T_Auth.U_None(), targets.[0].Auth )
        Assert.StrictEqual( tnodeidx_me.fromPrim 2us, targets.[1].IdentNumber )
        Assert.StrictEqual( tpgt_me.fromPrim 0us, targets.[1].TargetPortalGroupTag )
        Assert.StrictEqual( "target002", targets.[1].TargetName )
        Assert.StrictEqual( "target002", targets.[1].TargetAlias )
        Assert.StrictEqual( 1, targets.[1].LUN.Length )
        Assert.StrictEqual( lun_me.fromPrim 2UL, targets.[1].LUN.[0] )
        Assert.StrictEqual( TargetGroupConf.T_Auth.U_None(), targets.[1].Auth )

        Assert.StrictEqual( 2, lus.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus.[0].LUN )
        Assert.StrictEqual( "luname001", lus.[0].LUName )
        Assert.StrictEqual( TargetGroupConf.T_DEVICE.U_DummyDevice(), lus.[0].LUDevice )
        Assert.StrictEqual( Functions.AppendPathName pDirName "LU_1", lus.[0].WorkPath )
        Assert.StrictEqual( lun_me.fromPrim 2UL, lus.[1].LUN )
        Assert.StrictEqual( "luname002", lus.[1].LUName )
        Assert.StrictEqual( TargetGroupConf.T_DEVICE.U_DummyDevice(), lus.[1].LUDevice )
        Assert.StrictEqual( Functions.AppendPathName pDirName "LU_2", lus.[1].WorkPath )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_009() =
        let pDirName = this.GetTestDirName "LoadConfig_009"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        GlbFunc.CreateDir targetGroupConfName |> ignore

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        let tgc = cm.GetAllTargetGroupConf()
        Assert.StrictEqual( 0, tgc.Length ) // directory is ignored, it's considered that there are no target groups.

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteDir targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_010() =
        let pDirName = this.GetTestDirName "LoadConfig_010"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConfStr = "<TargetGroup></TargetGroup>"
        File.WriteAllText( targetGroupConfName, targetGroupConfStr )
        Assert.ThrowsAny( fun () ->
            new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
        ) |> ignore

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadConfig_011() =
        let pDirName = this.GetTestDirName "LoadConfig_011"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, false, new HKiller() ) :> IConfiguration
        let tgc = cm.GetAllTargetGroupConf()
        Assert.StrictEqual( 0, tgc.Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName
    [<Fact>]
    member this.VerifyConfig_001() =
        let pDirName = this.GetTestDirName "VerifyConfig_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [
                for i = 1 to Constants.MAX_NETWORK_PORTAL_COUNT do
                yield {
                    IdentNumber =
                        if i = Constants.MAX_NETWORK_PORTAL_COUNT then
                            netportidx_me.fromPrim ( uint32 i - 1u );
                        else
                            netportidx_me.fromPrim ( uint32 i );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetAddress = "";
                    PortNumber = 2360us;
                    DisableNagle = false;
                    ReceiveBufferSize = 8192;
                    SendBufferSize = 8192;
                    WhiteList = [];
                };
            ];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr )

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Network portal IdentNumber", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_002() =
        let pDirName = this.GetTestDirName "VerifyConfig_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [
                for i = 1 to Constants.MAX_NETWORK_PORTAL_COUNT do
                yield {
                    IdentNumber = netportidx_me.fromPrim ( uint32 i );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetAddress = "";
                    PortNumber = 2360us;
                    DisableNagle = false;
                    ReceiveBufferSize = 8192;
                    SendBufferSize = 8192;
                    WhiteList = [];
                };
            ];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.StrictEqual( Constants.MAX_NETWORK_PORTAL_COUNT, cm.GetNetworkPortal().Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_003() =
        let pDirName = this.GetTestDirName "VerifyConfig_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD + 1 do
            let tgid = tgid_me.fromPrim( uint32 i )
            let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid )
            let targetGroupConfStr1 =
                genTargetGroupConfStr tgid ( tnodeidx_me.fromPrim ( uint16 i ) ) ( sprintf "target%03d" i ) ( lun_me.fromPrim 1UL )
            File.WriteAllText( targetGroupConfName1, targetGroupConfStr1 )

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Too many target groups", e.Message )

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_004() =
        let pDirName = this.GetTestDirName "VerifyConfig_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let tgids = [|
            for i = 0 to Constants.MAX_TARGET_COUNT_IN_TD - 1 do
                yield tgid_me.fromPrim( uint32 i )
        |]

        for i = 0 to Constants.MAX_TARGET_COUNT_IN_TD - 1 do
            let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgids.[i] )
            let targetGroupConfStr1 =
                genTargetGroupConfStr tgids.[i] ( tnodeidx_me.fromPrim ( uint16 i + 1us ) ) ( sprintf "target%03d" i ) ( lun_me.fromPrim ( uint64 i + 1UL ) )
            File.WriteAllText( targetGroupConfName1, targetGroupConfStr1 )

        let c = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        for i = 0 to Constants.MAX_TARGET_COUNT_IN_TD - 1 do
            let conf = c.GetTargetGroupConf tgids.[i]
            Assert.True( conf.IsSome )

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_005() =
        let pDirName = this.GetTestDirName "VerifyConfig_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.Zero;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD - 1 do
                    yield {
                        IdentNumber = tnodeidx_me.fromPrim( uint16 i );
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = sprintf "target%03d" i;
                        TargetAlias = "target001";
                        LUN = [ lun_me.fromPrim 1UL ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.fromPrim( 1u );
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim( 999us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target999";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname999";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let _ = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_006() =
        let pDirName = this.GetTestDirName "VerifyConfig_006"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf


        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.Zero;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD - 1 do
                    yield {
                        IdentNumber = tnodeidx_me.fromPrim( uint16 i );
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = sprintf "target%03d" i;
                        TargetAlias = "target001";
                        LUN = [ lun_me.fromPrim 1UL ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.fromPrim( 1u );
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim( 998us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target998";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
                {
                    IdentNumber = tnodeidx_me.fromPrim( 999us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target999";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname999";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Too many target", e.Message )

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_007() =
        let pDirName = this.GetTestDirName "VerifyConfig_007"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.fromPrim( 1u );
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [{
                    IdentNumber = tnodeidx_me.fromPrim( 1us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 0UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "LUN 0 is exist in target", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_008() =
        let pDirName = this.GetTestDirName "VerifyConfig_008"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [{
                    IdentNumber = tnodeidx_me.fromPrim( 1us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [
                        for i = 1 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD do
                            if i = Constants.MAX_LOGICALUNIT_COUNT_IN_TD then
                                lun_me.fromPrim ( uint64 i - 1UL )
                            else
                                lun_me.fromPrim ( uint64 i )
                    ];
                    Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [
                for i = 1 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 1 do
                    yield {
                        LUN = lun_me.fromPrim ( uint64 i );
                        LUName = sprintf "luname%d" i;
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.U_DummyDevice();
                    }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate LUN in target", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_009() =
        let pDirName = this.GetTestDirName "VerifyConfig_009"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [{
                    IdentNumber = tnodeidx_me.fromPrim( 1us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [
                        for i = 1 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD do
                            lun_me.fromPrim ( uint64 i )
                    ];
                    Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [
                for i = 1 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD do
                    yield {
                        LUN = lun_me.fromPrim ( uint64 i );
                        LUName = sprintf "luname%d" i;
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.U_DummyDevice();
                    }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        let gconf = cm.GetTargetGroupConf( tgid1 )
        Assert.True(( gconf.IsSome ))
        Assert.StrictEqual( Constants.MAX_LOGICALUNIT_COUNT_IN_TD, gconf.Value.LogicalUnit.Length )
        Assert.StrictEqual( 1, gconf.Value.Target.Length )
        Assert.StrictEqual( Constants.MAX_LOGICALUNIT_COUNT_IN_TD, gconf.Value.Target.[0].LUN.Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.VerifyConfig_010() =
        let pDirName = this.GetTestDirName "VerifyConfig_010"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                yield {
                    IdentNumber = tnodeidx_me.fromPrim 3us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target000";
                    TargetAlias = "";
                    LUN = [
                        for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 2 do
                            yield ( lun_me.fromPrim ( uint64 i + 1UL ) )
                    ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 2 do
                    yield {
                        LUN = lun_me.fromPrim ( uint64 i + 1UL );
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                let i = Constants.MAX_LUN_VALUE
                yield {
                    IdentNumber = tnodeidx_me.fromPrim 999us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target999";
                    TargetAlias = "";
                    LUN = [
                        yield ( lun_me.fromPrim i )
                    ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                let i = Constants.MAX_LUN_VALUE
                yield {
                    LUN = lun_me.fromPrim i;
                    LUName = "";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let _ = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_011() =
        let pDirName = this.GetTestDirName "VerifyConfig_011"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 3us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target000";
                    TargetAlias = "";
                    LUN = [
                        for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 2 do
                            yield ( lun_me.fromPrim ( uint64 i + 1UL ) )
                    ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 2 do
                    yield {
                        LUN = lun_me.fromPrim ( uint64 i + 1UL );
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 999us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target999";
                    TargetAlias = "";
                    LUN = [
                        for i = 1 to 2 do
                            yield ( lun_me.fromPrim ( uint64 i ) )
                    ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                for i = 1 to 2 do
                    yield {
                        LUN = lun_me.fromPrim ( uint64 i );
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Too many LUs", e.Message )

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_012() =
        let pDirName = this.GetTestDirName "VerifyConfig_012"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf
        
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 10us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target000";
                    TargetAlias = "target000";
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 11us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 2UL; lun_me.fromPrim 3UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
                {
                    LUN = lun_me.fromPrim 3UL;
                    LUName = "luname003";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate LUN", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_013() =
        let pDirName = this.GetTestDirName "VerifyConfig_013"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 11us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 3UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 0UL;
                    LUName = "luname000";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
                {
                    LUN = lun_me.fromPrim 3UL;
                    LUName = "luname003";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "LUN 0 is exist", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_014() =
        let pDirName = this.GetTestDirName "VerifyConfig_014"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 10us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 3UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Missing LUN(3", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_015() =
        let pDirName = this.GetTestDirName "VerifyConfig_015"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 10us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 1UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "LU(2) is not refferd by any target", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_016() =
        let pDirName = this.GetTestDirName "VerifyConfig_016"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD do
                    yield {
                        IdentNumber = tnodeidx_me.fromPrim ( uint16 i );
                        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                        TargetName = 
                            if i = Constants.MAX_TARGET_COUNT_IN_TD then
                                sprintf "target%d" ( i - 1 );
                            else
                                sprintf "target%d" i;
                        TargetAlias = "target001";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Target Name", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_017() =
        let pDirName = this.GetTestDirName "VerifyConfig_017"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim ( 10us );
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 1UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 11us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Target Name", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_018() =
        let pDirName = this.GetTestDirName "VerifyConfig_018"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD do
                    yield {
                        IdentNumber =
                            if i = Constants.MAX_TARGET_COUNT_IN_TD then
                                tnodeidx_me.fromPrim ( uint16 i - 1us )
                            else
                                tnodeidx_me.fromPrim ( uint16 i );
                        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                        TargetName = sprintf "target%d" i;
                        TargetAlias = "target001";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Target IdentNumber", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName



    [<Fact>]
    member this.VerifyConfig_019() =
        let pDirName = this.GetTestDirName "VerifyConfig_019"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        let targetGroupConf1 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;
            TargetGroupName = "a";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 1us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target001";
                    TargetAlias = "target001";
                    LUN = [ lun_me.fromPrim 1UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "luname001";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName1 targetGroupConf1

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 1us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Target IdentNumber", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_020() =
        let pDirName = this.GetTestDirName "VerifyConfig_020"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid0;  // duplicate
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 2us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Target group ID", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.VerifyConfig_021() =
        let pDirName = this.GetTestDirName "VerifyConfig_021"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConfStr2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid_me.Zero;
            TargetGroupName = "target001";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 1us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target001";
                    TargetAlias = "";
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "lu001";
                    WorkPath = "c:\\";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_BlockDevice( {
                        Peripheral = TargetGroupConf.U_DummyMedia( {
                            IdentNumber = mediaidx_me.fromPrim 1u;
                            MediaName = "";
                        })
                        OptimalTransferLength = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH;
                    } );
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "lu002";
                    WorkPath = "c:\\";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_BlockDevice( {
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 1u;
                            MediaName = "";
                        })
                        OptimalTransferLength = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH;
                    } );
                }

            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConfStr2

        let e =
            Assert.Throws<ConfRWException>( fun () ->
                new ConfigurationMaster( pDirName, true, new HKiller() ) |> ignore
            )
        Assert.StartsWith( "Duplicate Media ID", e.Message )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetTargetGroupID_001() =
        let pDirName = this.GetTestDirName "GetTargetGroupID_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConfStr2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [{
                    IdentNumber = tnodeidx_me.fromPrim 11us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 2UL ];
                    Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConfStr2

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let vtgid = cm.GetTargetGroupID() |> Array.sort
        let tgc = cm.GetAllTargetGroupConf() |> Array.sortBy ( fun ( itr, _ ) -> itr.TargetGroupID )
        Assert.StrictEqual( 2, vtgid.Length )
        Assert.StrictEqual( tgid0, vtgid.[0] )
        Assert.StrictEqual( tgid1, vtgid.[1] )

        Assert.StrictEqual( "a", ( fst tgc.[0] ).TargetGroupName )
        Assert.StrictEqual( "b", ( fst tgc.[1] ).TargetGroupName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.UnloadTargetGroup_001() =
        let pDirName = this.GetTestDirName "UnloadTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 11us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let tgid2 = tgid_me.fromPrim( 2u )
        let targetGroupConfName3 = Functions.AppendPathName pDirName ( tgid_me.toString tgid2 )
        let targetGroupConf3 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid2;
            TargetGroupName = "c";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 12us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target003";
                    TargetAlias = "target003";
                    LUN = [ lun_me.fromPrim 3UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 3UL;
                    LUName = "luname003";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName3 targetGroupConf3

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let vtgid1 = cm.GetTargetGroupID() |> Array.sort
        let tgc1 = cm.GetAllTargetGroupConf()
        let targets1 = getAllTagetConf tgc1
        let lus1 = getAllLUConf tgc1
        Assert.StrictEqual( 3, vtgid1.Length )
        Assert.StrictEqual( tgid0, vtgid1.[0] )
        Assert.StrictEqual( tgid1, vtgid1.[1] )
        Assert.StrictEqual( tgid2, vtgid1.[2] )

        Assert.StrictEqual( 3, lus1.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus1.[0].LUN )
        Assert.StrictEqual( lun_me.fromPrim 2UL, lus1.[1].LUN )
        Assert.StrictEqual( lun_me.fromPrim 3UL, lus1.[2].LUN )

        Assert.StrictEqual( 3, targets1.Length )
        Assert.StrictEqual( "target001", targets1.[0].TargetName )
        Assert.StrictEqual( "target002", targets1.[1].TargetName )
        Assert.StrictEqual( "target003", targets1.[2].TargetName )

        cm.UnloadTargetGroup( tgid1 )

        let vtgid2 = cm.GetTargetGroupID() |> Array.sort
        let tgc2 = cm.GetAllTargetGroupConf()
        let targets2 = getAllTagetConf tgc2
        let lus2 = getAllLUConf tgc2
        Assert.StrictEqual( 2, vtgid2.Length )
        Assert.StrictEqual( tgid0, vtgid2.[0] )
        Assert.StrictEqual( tgid2, vtgid2.[1] )

        Assert.StrictEqual( 2, lus2.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus2.[0].LUN )
        Assert.StrictEqual( lun_me.fromPrim 3UL, lus2.[1].LUN )

        Assert.StrictEqual( 2, targets2.Length )
        Assert.StrictEqual( "target001", targets2.[0].TargetName )
        Assert.StrictEqual( "target003", targets2.[1].TargetName )

        cm.UnloadTargetGroup( tgid99 )

        let vtgid3 = cm.GetTargetGroupID() |> Array.sort
        let tgc3 = cm.GetAllTargetGroupConf()
        let targets3 = getAllTagetConf tgc3
        let lus3 = getAllLUConf tgc3
        Assert.StrictEqual( 2, vtgid3.Length )
        Assert.StrictEqual( tgid0, vtgid3.[0] )
        Assert.StrictEqual( tgid2, vtgid3.[1] )

        Assert.StrictEqual( 2, lus3.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus3.[0].LUN )
        Assert.StrictEqual( lun_me.fromPrim 3UL, lus3.[1].LUN )

        Assert.StrictEqual( 2, targets3.Length )
        Assert.StrictEqual( "target001", targets3.[0].TargetName )
        Assert.StrictEqual( "target003", targets3.[1].TargetName )

        cm.UnloadTargetGroup( tgid2 )

        let vtgid4 = cm.GetTargetGroupID() |> Array.sort
        let tgc4 = cm.GetAllTargetGroupConf()
        let targets4 = getAllTagetConf tgc4
        let lus4 = getAllLUConf tgc4
        Assert.StrictEqual( 1, vtgid4.Length )
        Assert.StrictEqual( tgid0, vtgid4.[0] )

        Assert.StrictEqual( 1, lus4.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus4.[0].LUN )

        Assert.StrictEqual( 1, targets4.Length )
        Assert.StrictEqual( "target001", targets4.[0].TargetName )

        cm.UnloadTargetGroup( tgid0 )

        let vtgid5 = cm.GetTargetGroupID() |> Array.sort
        let tgc5 = cm.GetAllTargetGroupConf()
        let targets5 = getAllTagetConf tgc5
        let lus5 = getAllLUConf tgc5
        Assert.StrictEqual( 0, vtgid5.Length )
        Assert.StrictEqual( 0, lus5.Length )
        Assert.StrictEqual( 0, targets5.Length )

        cm.UnloadTargetGroup( tgid98 )

        let vtgid6 = cm.GetTargetGroupID() |> Array.sort
        let tgc6 = cm.GetAllTargetGroupConf()
        let targets6 = getAllTagetConf tgc6
        let lus6 = getAllLUConf tgc6
        Assert.StrictEqual( 0, vtgid6.Length )
        Assert.StrictEqual( 0, lus6.Length )
        Assert.StrictEqual( 0, targets6.Length )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteFile targetGroupConfName3
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadTargetGroup_001() =
        let pDirName = this.GetTestDirName "LoadTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let vtgid1 = cm.GetTargetGroupID() |> Array.sort
        let tgc1 = cm.GetAllTargetGroupConf()
        let targets1 = getAllTagetConf tgc1
        let lus1 = getAllLUConf tgc1
        Assert.StrictEqual( 1, vtgid1.Length )
        Assert.StrictEqual( tgid0, vtgid1.[0])
        Assert.StrictEqual( 1, lus1.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus1.[0].LUN )
        Assert.StrictEqual( 1, targets1.Length )
        Assert.StrictEqual( "target001", targets1.[0].TargetName )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        GlbFunc.CreateDir targetGroupConfName2 |> ignore

        Assert.False( cm.LoadTargetGroup( tgid1 ) )

        let vtgid2 = cm.GetTargetGroupID() |> Array.sort
        let tgc2 = cm.GetAllTargetGroupConf()
        let targets2 = getAllTagetConf tgc2
        let lus2 = getAllLUConf tgc2
        Assert.StrictEqual( 1, vtgid2.Length )
        Assert.StrictEqual( tgid0, vtgid2.[0] )
        Assert.StrictEqual( 1, lus2.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus2.[0].LUN )
        Assert.StrictEqual( 1, targets2.Length )
        Assert.StrictEqual( "target001", targets2.[0].TargetName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadTargetGroup_002() =
        let pDirName = this.GetTestDirName "LoadTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let vtgid1 = cm.GetTargetGroupID() |> Array.sort
        let tgc1 = cm.GetAllTargetGroupConf()
        let targets1 = getAllTagetConf tgc1
        let lus1 = getAllLUConf tgc1
        Assert.StrictEqual( 1, vtgid1.Length )
        Assert.StrictEqual( tgid0, vtgid1.[0] )
        Assert.StrictEqual( 1, lus1.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus1.[0].LUN )
        Assert.StrictEqual( 1, targets1.Length )
        Assert.StrictEqual( "target001", targets1.[0].TargetName )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName2, defaultTargetGroupConfStr )

        Assert.False( cm.LoadTargetGroup( tgid1 ) )

        let vtgid2 = cm.GetTargetGroupID() |> Array.sort
        let tgc2 = cm.GetAllTargetGroupConf()
        let targets2 = getAllTagetConf tgc2
        let lus2 = getAllLUConf tgc2
        Assert.StrictEqual( 1, vtgid2.Length )
        Assert.StrictEqual( tgid0, vtgid2.[0] )
        Assert.StrictEqual( 1, lus2.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus2.[0].LUN )
        Assert.StrictEqual( 1, targets2.Length )
        Assert.StrictEqual( "target001", targets2.[0].TargetName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadTargetGroup_003() =
        let pDirName = this.GetTestDirName "LoadTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let vtgid1 = cm.GetTargetGroupID() |> Array.sort
        let tgc1 = cm.GetAllTargetGroupConf()
        let targets1 = getAllTagetConf tgc1
        let lus1 = getAllLUConf tgc1
        Assert.StrictEqual( 1, vtgid1.Length )
        Assert.StrictEqual( tgid0, vtgid1.[0] )
        Assert.StrictEqual( 1, lus1.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus1.[0].LUN )
        Assert.StrictEqual( 1, targets1.Length )
        Assert.StrictEqual( "target001", targets1.[0].TargetName )

        let targetGroupConfName2 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        let targetGroupConf2 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid1;
            TargetGroupName = "b";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 11us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target002";
                    TargetAlias = "target002";
                    LUN = [ lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "luname002";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName2 targetGroupConf2

        let targetGroupConfName3 = Functions.AppendPathName pDirName ( tgid_me.toString tgid2 )
        let targetGroupConf3 : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid2;
            TargetGroupName = "c";
            EnabledAtStart = true;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 12us;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "target003";
                    TargetAlias = "target003";
                    LUN = [ lun_me.fromPrim 3UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [{
                    LUN = lun_me.fromPrim 3UL;
                    LUName = "luname003";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            };];
        }
        TargetGroupConf.ReaderWriter.WriteFile targetGroupConfName3 targetGroupConf3

        Assert.True( cm.LoadTargetGroup( tgid1 ) )

        let vtgid2 = cm.GetTargetGroupID() |> Array.sort
        let tgc2 = cm.GetAllTargetGroupConf()
        let targets2 = getAllTagetConf tgc2
        let lus2 = getAllLUConf tgc2
        Assert.StrictEqual( 2, vtgid2.Length )
        Assert.StrictEqual( tgid0, vtgid2.[0] )
        Assert.StrictEqual( tgid1, vtgid2.[1] )
        Assert.StrictEqual( 2, lus2.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus2.[0].LUN )
        Assert.StrictEqual( lun_me.fromPrim 2UL, lus2.[1].LUN )
        Assert.StrictEqual( 2, targets2.Length )
        Assert.StrictEqual( "target001", targets2.[0].TargetName )
        Assert.StrictEqual( "target002", targets2.[1].TargetName )

        Assert.True( cm.LoadTargetGroup( tgid2 ) )

        let vtgid3 = cm.GetTargetGroupID() |> Array.sort
        let tgc3 = cm.GetAllTargetGroupConf()
        let targets3 = getAllTagetConf tgc3
        let lus3 = getAllLUConf tgc3
        Assert.StrictEqual( 3, vtgid3.Length )
        Assert.StrictEqual( tgid0, vtgid3.[0] )
        Assert.StrictEqual( tgid1, vtgid3.[1] )
        Assert.StrictEqual( tgid2, vtgid3.[2] )
        Assert.StrictEqual( 3, lus3.Length )
        Assert.StrictEqual( lun_me.fromPrim 1UL, lus3.[0].LUN )
        Assert.StrictEqual( lun_me.fromPrim 2UL, lus3.[1].LUN )
        Assert.StrictEqual( lun_me.fromPrim 3UL, lus3.[2].LUN )
        Assert.StrictEqual( 3, targets3.Length )
        Assert.StrictEqual( "target001", targets3.[0].TargetName )
        Assert.StrictEqual( "target002", targets3.[1].TargetName )
        Assert.StrictEqual( "target003", targets3.[2].TargetName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteFile targetGroupConfName2
        GlbFunc.DeleteFile targetGroupConfName3
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IscsiNegoParamCO_001() =
        let pDirName = this.GetTestDirName "IscsiNegoParamCO_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        Assert.StrictEqual( Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength, cm.IscsiNegoParamCO.MaxRecvDataSegmentLength_T )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IscsiNegoParamCO_002() =
        let pDirName = this.GetTestDirName "IscsiNegoParamCO_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = Some {
                MaxRecvDataSegmentLength = 512u;
                MaxBurstLength = 512u;
                FirstBurstLength = 512u;
                DefaultTime2Wait = 99us;
                DefaultTime2Retain = 99us;
                MaxOutstandingR2T = 3us;
            };
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        Assert.StrictEqual( 512u, cm.IscsiNegoParamCO.MaxRecvDataSegmentLength_T )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IscsiNegoParamSW_001() =
        let pDirName = this.GetTestDirName "IscsiNegoParamSW_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        Assert.StrictEqual( 262144u, cm.IscsiNegoParamSW.MaxBurstLength )
        Assert.StrictEqual( 65536u, cm.IscsiNegoParamSW.FirstBurstLength )
        Assert.StrictEqual( 2us, cm.IscsiNegoParamSW.DefaultTime2Wait )
        Assert.StrictEqual( 20us, cm.IscsiNegoParamSW.DefaultTime2Retain )
        Assert.StrictEqual( 65535us, cm.IscsiNegoParamSW.MaxOutstandingR2T )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IscsiNegoParamSW_002() =
        let pDirName = this.GetTestDirName "IscsiNegoParamSW_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = Some {
                MaxRecvDataSegmentLength = 512u;
                MaxBurstLength = 9999u;
                FirstBurstLength = 9999u;
                DefaultTime2Wait = 99us;
                DefaultTime2Retain = 99us;
                MaxOutstandingR2T = 9999us;
            };
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        Assert.StrictEqual( 9999u, cm.IscsiNegoParamSW.MaxBurstLength )
        Assert.StrictEqual( 9999u, cm.IscsiNegoParamSW.FirstBurstLength )
        Assert.StrictEqual( 99us, cm.IscsiNegoParamSW.DefaultTime2Wait )
        Assert.StrictEqual( 99us, cm.IscsiNegoParamSW.DefaultTime2Retain )
        Assert.StrictEqual( 9999us, cm.IscsiNegoParamSW.MaxOutstandingR2T )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetDefaultLogParameters_001() =
        let pDirName = this.GetTestDirName "GetLogParameter_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let struct( s, h, l ) = cm.GetDefaultLogParameters()
        Assert.StrictEqual( Constants.LOGPARAM_DEF_SOFTLIMIT, s )
        Assert.StrictEqual( Constants.LOGPARAM_DEF_HARDLIMIT, h )
        Assert.StrictEqual( LogLevel.LOGLEVEL_INFO, l )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetDefaultLogParameters_002() =
        let pDirName = this.GetTestDirName "GetLogParameter_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = Some {
                SoftLimit = 999u;
                HardLimit = 9999u;
                LogLevel = LogLevel.LOGLEVEL_WARNING;
            };
            DeviceName = "aaaabbbb";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration

        let struct( s, h, l ) = cm.GetDefaultLogParameters()
        Assert.StrictEqual( 999u, s )
        Assert.StrictEqual( 9999u, h )
        Assert.StrictEqual( LogLevel.LOGLEVEL_WARNING, l )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetDeviceName_001() =
        let pDirName = this.GetTestDirName "GetDeviceName_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.StrictEqual( "", cm.DeviceName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.GetDeviceName_002() =
        let pDirName = this.GetTestDirName "GetDeviceName_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaaabbbb";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.StrictEqual( "aaaabbbb", cm.DeviceName )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.EnableStatSNAckChecker_001() =
        let pDirName = this.GetTestDirName "EnableStatSNAckChecker_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = true;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.True( cm.EnableStatSNAckChecker )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.EnableStatSNAckChecker_002() =
        let pDirName = this.GetTestDirName "EnableStatSNAckChecker_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        let targetDeviceConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName targetDeviceConf

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr )

        let cm = new ConfigurationMaster( pDirName, true, new HKiller() ) :> IConfiguration
        Assert.False( cm.EnableStatSNAckChecker )

        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName
