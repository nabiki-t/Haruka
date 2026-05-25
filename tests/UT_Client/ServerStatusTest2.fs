//=============================================================================
// Haruka Software Storage.
// ServerStatusTest2.fs : Test cases for ServerStatus class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Concurrent
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type ServerStatus_Test2() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Publish_001() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_001"

        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let _ = ss.AddTargetDeviceNode ( GlbFunc.newTargetDeviceID() ) "a" false ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP

                try
                    do! ss.Publish cc1
                    Assert.Fail __LINE__
                with
                | :? ConfigurationError as x ->
                    Assert.StartsWith( "ERRMSG_VALIDATION_FAILED", x.Message )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_002() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_002"
            ServerStatus_Test1.WriteCtrlConfig dname portNo

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                do! ss.Publish cc1

                let ss2 = new ServerStatus( st )
                do! ss2.LoadConfigure cc1 true

                Assert.StrictEqual( uint16 portNo, ss2.ControllerNode.RemoteCtrlValue.PortNum )
            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_003_UpdateCtrlConf() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_003_UpdateCtrlConf"
            let portNo2 = GlbFunc.nextTcpPortNo()
            ServerStatus_Test1.WriteCtrlConfig dname portNo

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                Assert.StrictEqual( ModifiedStatus.NotModified, ( ss.ControllerNode :> IConfigFileNode ).Modified )

                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = Some {
                        PortNum = uint16 portNo2;
                        Address = "::1";
                        WhiteList = [];
                    }
                    LogMaintenance = None;
                    LogParameters = None;
                }
                ss.UpdateControllerNode conf |> ignore

                Assert.StrictEqual( ModifiedStatus.Modified, ( ss.ControllerNode :> IConfigFileNode ).Modified )

                do! ss.Publish cc1

                let ctrlConfFName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
                let wroteConfFile = HarukaCtrlConf.ReaderWriter.LoadFile ctrlConfFName
                Assert.StrictEqual( uint16 portNo2, wroteConfFile.RemoteCtrl.Value.PortNum )

                Assert.StrictEqual( ModifiedStatus.NotModified, ( ss.ControllerNode :> IConfigFileNode ).Modified )

            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_004_AddTargetDeviceConf_001() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_004_AddTargetDeviceConf_001"
            ServerStatus_Test1.WriteCtrlConfig dname portNo

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.Empty( tdNodes1 )

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "aassddd" false ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
                Assert.StrictEqual( ModifiedStatus.Modified, ( tdNode :> IConfigFileNode ).Modified )

                let _ = ss.AddNetworkPortalNode tdNode ServerStatus_Test1.defaultNP 
                let tgNode = ss.AddTargetGroupNode tdNode ( GlbFunc.newTargetGroupID() ) "tg01" false
                let tNode = ss.AddTargetNode tgNode {
                            IdentNumber = tnodeidx_me.fromPrim 1us;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t001"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNode ( lun_me.fromPrim 1UL ) "luname001" Constants.LU_DEF_MULTIPLICITY

                do! ss.Publish cc1

                let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
                let tdConfFName = Functions.AppendPathName tdConfDName ( Constants.TARGET_DEVICE_CONF_FILE_NAME )

                Assert.True(( Directory.Exists tdConfDName ))

                let wroteConfFile = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName
                Assert.StrictEqual( "aassddd", wroteConfFile.DeviceName )

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 1, tdNodes2.Length )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes2.[0] :> IConfigFileNode ).Modified )
            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_004_AddTargetDeviceConf_002() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_004_AddTargetDeviceConf_002"
            let portalPortNo = GlbFunc.nextTcpPortNo()
            let tdid1 = GlbFunc.newTargetDeviceID()
            let tgid1 = GlbFunc.newTargetGroupID()
            let tdConfDName1 = Functions.AppendPathName dname ( tdid_me.toString tdid1 )
            let tdConfFName1 = Functions.AppendPathName tdConfDName1 ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
            ServerStatus_Test1.WriteCtrlConfig dname portNo
            ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid1
            ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid1 ( lun_me.fromPrim 1UL ) "b001" 1us

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 1, tdNodes1.Length )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes1.[0] :> IConfigFileNode ).Modified )

                let tdid2 = GlbFunc.newTargetDeviceID()
                let tdNode2 = ss.AddTargetDeviceNode tdid2 "aassddd" false ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
                Assert.StrictEqual( ModifiedStatus.Modified, ( tdNode2 :> IConfigFileNode ).Modified )

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 2, tdNodes2.Length )
                let tdNode1_2 = tdNodes2 |> Seq.find( fun itr -> itr.TargetDeviceID = tdid1 )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNode1_2 :> IConfigFileNode ).Modified )
                let tdNode2_2 = tdNodes2 |> Seq.find( fun itr -> itr.TargetDeviceID = tdid2 )
                Assert.StrictEqual( ModifiedStatus.Modified, ( tdNode2_2 :> IConfigFileNode ).Modified )

                let _ = ss.AddNetworkPortalNode tdNode2 ServerStatus_Test1.defaultNP 
                let tgNode = ss.AddTargetGroupNode tdNode2 ( GlbFunc.newTargetGroupID() ) "tg01" false
                let tNode = ss.AddTargetNode tgNode {
                            IdentNumber = tnodeidx_me.fromPrim 1us;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t001"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNode ( lun_me.fromPrim 1UL ) "luname001" Constants.LU_DEF_MULTIPLICITY

                do! ss.Publish cc1

                Assert.True(( Directory.Exists tdConfDName1 ))
                let wroteConfFile1 = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName1
                Assert.StrictEqual( "a001", wroteConfFile1.DeviceName )

                let tdConfDName2 = Functions.AppendPathName dname ( tdid_me.toString tdid2 )
                let tdConfFName2 = Functions.AppendPathName tdConfDName2 ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
                Assert.True(( Directory.Exists tdConfDName2 ))
                let wroteConfFile2 = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName2
                Assert.StrictEqual( "aassddd", wroteConfFile2.DeviceName )

                let tdNodes3 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 2, tdNodes3.Length )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes3.[0] :> IConfigFileNode ).Modified )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes3.[1] :> IConfigFileNode ).Modified )
            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_005_UpdateTargetDeviceConf() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_005_UpdateTargetDeviceConf"
            let portalPortNo = GlbFunc.nextTcpPortNo()
            let tdid = GlbFunc.newTargetDeviceID()
            let tgid = GlbFunc.newTargetGroupID()
            let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
            let tdConfFName = Functions.AppendPathName tdConfDName ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
            ServerStatus_Test1.WriteCtrlConfig dname portNo
            ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid
            ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid tgid ( lun_me.fromPrim 1UL ) "b001" 1us

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 1, tdNodes1.Length )
                Assert.StrictEqual( "a001", tdNodes1.[0].TargetDeviceName )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes1.[0] :> IConfigFileNode ).Modified )

                do! cc1.KillTargetDeviceProc tdid

                let tdNode2 = ss.UpdateTargetDeviceNode tdNodes1.[0] tdid "a002" false tdNodes1.[0].NegotiableParameters tdNodes1.[0].LogParameters
                Assert.StrictEqual( ModifiedStatus.Modified, ( tdNode2 :> IConfigFileNode ).Modified )
                let v = ss.Validate()
                Assert.Empty( v )

                do! ss.Publish cc1

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 1, tdNodes2.Length )
                Assert.StrictEqual( "a002", tdNodes2.[0].TargetDeviceName )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes2.[0] :> IConfigFileNode ).Modified )

                let wroteConfFile = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName
                Assert.StrictEqual( "a002", wroteConfFile.DeviceName )
            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_006_DeleteTargetDeviceConf() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_006_DeleteTargetDeviceConf"
            let portalPortNo = GlbFunc.nextTcpPortNo()
            let tdid = GlbFunc.newTargetDeviceID()
            let tgid = GlbFunc.newTargetGroupID()
            let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
            ServerStatus_Test1.WriteCtrlConfig dname portNo
            ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid
            ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid tgid ( lun_me.fromPrim 1UL ) "b001" 1us

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 1, tdNodes1.Length )
                Assert.StrictEqual( "a001", tdNodes1.[0].TargetDeviceName )
                Assert.StrictEqual( ModifiedStatus.NotModified, ( tdNodes1.[0] :> IConfigFileNode ).Modified )

                do! cc1.KillTargetDeviceProc tdid

                do ss.DeleteTargetDeviceNode tdNodes1.[0]
                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.Empty( tdNodes2 )

                let v = ss.Validate()
                Assert.Empty( v )

                do! ss.Publish cc1

                Assert.False(( Directory.Exists tdConfDName ))
            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_007_AddTargetGroupConf() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_007_AddTargetGroupConf"
            let portalPortNo1 = GlbFunc.nextTcpPortNo()
            let portalPortNo2 = GlbFunc.nextTcpPortNo()
            let tdid1 = GlbFunc.newTargetDeviceID()
            let tdid2 = GlbFunc.newTargetDeviceID()
            let tgid1 = GlbFunc.newTargetGroupID()
            let tgid2 = GlbFunc.newTargetGroupID()
            let tdConfDName1 = Functions.AppendPathName dname ( tdid_me.toString tdid1 )
            let tdConfDName2 = Functions.AppendPathName dname ( tdid_me.toString tdid2 )

            ServerStatus_Test1.WriteCtrlConfig dname portNo
            ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo1 tdid1
            ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo2 tdid2
            ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid1 ( lun_me.fromPrim 10UL ) "b001" 1us
            ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid2 tgid2 ( lun_me.fromPrim 20UL ) "b002" 2us

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.StrictEqual( 2, tdNodes1.Length )

                let tdNode1 = tdNodes1 |> Seq.find ( fun itr -> itr.TargetDeviceID = tdid1 )
                let tdNode2 = tdNodes1 |> Seq.find ( fun itr -> itr.TargetDeviceID = tdid2 )

                let tgNodes11 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.StrictEqual( 1, tgNodes11.Length )

                let tgNodes12 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.StrictEqual( 1, tgNodes12.Length )

                let tgidA = GlbFunc.newTargetGroupID()
                let tgNodeA = ss.AddTargetGroupNode tdNode1 tgidA "tg01" false
                let tNodeA = ss.AddTargetNode tgNodeA {
                            IdentNumber = tnodeidx_me.fromPrim 10us;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t001"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNodeA ( lun_me.fromPrim 1UL ) "luname001" Constants.LU_DEF_MULTIPLICITY
                Assert.StrictEqual( ModifiedStatus.Modified, ( tgNodeA :> IConfigFileNode ).Modified )

                let tgidB = GlbFunc.newTargetGroupID()
                let tgNodeB = ss.AddTargetGroupNode tdNode2 tgidB "tg11" false
                let tNodeB = ss.AddTargetNode tgNodeB {
                            IdentNumber = tnodeidx_me.fromPrim 101us;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t011"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNodeB ( lun_me.fromPrim 11UL ) "luname011" Constants.LU_DEF_MULTIPLICITY
                Assert.StrictEqual( ModifiedStatus.Modified, ( tgNodeB :> IConfigFileNode ).Modified )

                do! ss.Publish cc1

                let tgConfFName1_1 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid1 )
                let tgConfFName1_A = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgidA )
                let tgConfFName2_1 = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgid2 )
                let tgConfFName2_B = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgidB )

                let wroteConfFile1_1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName1_1
                Assert.StrictEqual( "b001", wroteConfFile1_1.TargetGroupName )
                let wroteConfFile1_A = TargetGroupConf.ReaderWriter.LoadFile tgConfFName1_A
                Assert.StrictEqual( "tg01", wroteConfFile1_A.TargetGroupName )

                let wroteConfFile2_1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName2_1
                Assert.StrictEqual( "b002", wroteConfFile2_1.TargetGroupName )
                let wroteConfFile2_B = TargetGroupConf.ReaderWriter.LoadFile tgConfFName2_B
                Assert.StrictEqual( "tg11", wroteConfFile2_B.TargetGroupName )

                let tgNodes21 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.StrictEqual( 2, tgNodes21.Length )
                for itr in tgNodes21 do
                    Assert.StrictEqual( ModifiedStatus.NotModified, ( itr :> IConfigFileNode ).Modified )

                let tgNodes22 = ( tdNode2 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.StrictEqual( 2, tgNodes22.Length )
                for itr in tgNodes22 do
                    Assert.StrictEqual( ModifiedStatus.NotModified, ( itr :> IConfigFileNode ).Modified )
            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.Publish_TGLoaded_001() =
        task {
            let portNo, dname = ServerStatus_Test1.Init "Publish_TGLoaded_001"
            let portalPortNo1 = GlbFunc.nextTcpPortNo()
            let tdid1 = GlbFunc.newTargetDeviceID()
            let tgid11 = GlbFunc.newTargetGroupID()
            let tgid12 = GlbFunc.newTargetGroupID()
            let tdConfDName1 = Functions.AppendPathName dname ( tdid_me.toString tdid1 )

            ServerStatus_Test1.WriteCtrlConfig dname portNo
            ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo1 tdid1
            ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid11 ( lun_me.fromPrim 1UL ) "b001" 1us

            let killer = new HKiller() :> IKiller
            let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
            ctrl.LoadInitialTargetDeviceProcs()
            ctrl.WaitRequest()

            try
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                let tdNode1 = tdNodes1 |> Seq.find ( fun itr -> itr.TargetDeviceID = tdid1 )
                let tgNodes11 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                let tgNode11 = tgNodes11 |> Seq.find ( fun itr -> itr.TargetGroupName = "b001" )

                do! cc1.InactivateTargetGroup tdid1 tgid11
                do! cc1.UnloadTargetGroup tdid1 tgid11
                let! activetgs1 = cc1.GetActiveTargetGroups tdid1
                let! loadedtgs1 = cc1.GetLoadedTargetGroups tdid1 
                Assert.StrictEqual( 0, activetgs1.Length )
                Assert.StrictEqual( 0, loadedtgs1.Length )

                ss.UpdateTargetGroupNode tgNode11 tgid12 tgNode11.TargetGroupName tgNode11.EnabledAtStart |> ignore

                // Kill the target device process.
                let pc = PrivateCaller( ctrl )
                let procs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
                procs.[ tdid_me.toPrim tdid1 ].m_Proc.Kill()
                do! Task.Delay 100

                // the target group had been loaded and activated.
                let! activetgs2 = cc1.GetActiveTargetGroups tdid1
                let! loadedtgs2 = cc1.GetLoadedTargetGroups tdid1 
                Assert.StrictEqual( 1, activetgs2.Length )
                Assert.StrictEqual( 1, loadedtgs2.Length )

                do! ss.Publish cc1

                // the target group was unloaded
                let! activetgs3 = cc1.GetActiveTargetGroups tdid1
                let! loadedtgs3 = cc1.GetLoadedTargetGroups tdid1 
                Assert.StrictEqual( 0, activetgs3.Length )
                Assert.StrictEqual( 0, loadedtgs3.Length )

                let tgConfFName11 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid11 )
                let tgConfFName12 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid12 )
                Assert.False(( File.Exists tgConfFName11 ))
                Assert.True(( File.Exists tgConfFName12 ))

            finally
                killer.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }
