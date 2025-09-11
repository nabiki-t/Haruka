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
                let _ = ss.AddTargetDeviceNode ( GlbFunc.newTargetDeviceID() ) "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP

                try
                    do! ss.Publish cc1
                    Assert.Fail __LINE__
                with
                | :? ConfigurationError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_VALIDATION_FAILED" ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_002() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_002"
        ServerStatus_Test1.WriteCtrlConfig dname portNo

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                do! ss.Publish cc1

                let ss2 = new ServerStatus( st )
                do! ss2.LoadConfigure cc1 true

                Assert.True(( ss2.ControllerNode.RemoteCtrlValue.PortNum = uint16 portNo ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_003_UpdateCtrlConf() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_003_UpdateCtrlConf"
        let portNo2 = GlbFunc.nextTcpPortNo()
        ServerStatus_Test1.WriteCtrlConfig dname portNo

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                Assert.True(( ( ss.ControllerNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

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

                Assert.True(( ( ss.ControllerNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                do! ss.Publish cc1

                let ctrlConfFName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
                let wroteConfFile = HarukaCtrlConf.ReaderWriter.LoadFile ctrlConfFName
                Assert.True(( wroteConfFile.RemoteCtrl.Value.PortNum = uint16 portNo2 ))

                Assert.True(( ( ss.ControllerNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_004_AddTargetDeviceConf_001() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_004_AddTargetDeviceConf_001"
        ServerStatus_Test1.WriteCtrlConfig dname portNo

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "aassddd" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
                Assert.True(( ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let _ = ss.AddNetworkPortalNode tdNode ServerStatus_Test1.defaultNP 
                let tgNode = ss.AddTargetGroupNode tdNode ( GlbFunc.newTargetGroupID() ) "tg01" false
                let tNode = ss.AddTargetNode tgNode {
                            IdentNumber = tnodeidx_me.fromPrim 1u;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t001"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNode ( lun_me.fromPrim 1UL ) "luname001"

                do! ss.Publish cc1

                let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
                let tdConfFName = Functions.AppendPathName tdConfDName ( Constants.TARGET_DEVICE_CONF_FILE_NAME )

                Assert.True(( Directory.Exists tdConfDName ))

                let wroteConfFile = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName
                Assert.True(( wroteConfFile.DeviceName = "aassddd" ))

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))
                Assert.True(( ( tdNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_004_AddTargetDeviceConf_002() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_004_AddTargetDeviceConf_002"
        let portalPortNo = GlbFunc.nextTcpPortNo()
        let tdid1 = GlbFunc.newTargetDeviceID()
        let tgid1 = GlbFunc.newTargetGroupID()
        let tdConfDName1 = Functions.AppendPathName dname ( tdid_me.toString tdid1 )
        let tdConfFName1 = Functions.AppendPathName tdConfDName1 ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid1
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid1 ( lun_me.fromPrim 1UL ) "b001" 1u

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 1 ))
                Assert.True(( ( tdNodes1.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let tdid2 = GlbFunc.newTargetDeviceID()
                let tdNode2 = ss.AddTargetDeviceNode tdid2 "aassddd" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
                Assert.True(( ( tdNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 2 ))
                let tdNode1_2 = tdNodes2 |> Seq.find( fun itr -> itr.TargetDeviceID = tdid1 )
                Assert.True(( ( tdNode1_2 :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tdNode2_2 = tdNodes2 |> Seq.find( fun itr -> itr.TargetDeviceID = tdid2 )
                Assert.True(( ( tdNode2_2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let _ = ss.AddNetworkPortalNode tdNode2 ServerStatus_Test1.defaultNP 
                let tgNode = ss.AddTargetGroupNode tdNode2 ( GlbFunc.newTargetGroupID() ) "tg01" false
                let tNode = ss.AddTargetNode tgNode {
                            IdentNumber = tnodeidx_me.fromPrim 1u;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t001"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNode ( lun_me.fromPrim 1UL ) "luname001"

                do! ss.Publish cc1

                Assert.True(( Directory.Exists tdConfDName1 ))
                let wroteConfFile1 = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName1
                Assert.True(( wroteConfFile1.DeviceName = "a001" ))

                let tdConfDName2 = Functions.AppendPathName dname ( tdid_me.toString tdid2 )
                let tdConfFName2 = Functions.AppendPathName tdConfDName2 ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
                Assert.True(( Directory.Exists tdConfDName2 ))
                let wroteConfFile2 = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName2
                Assert.True(( wroteConfFile2.DeviceName = "aassddd" ))

                let tdNodes3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes3.Length = 2 ))
                Assert.True(( ( tdNodes3.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                Assert.True(( ( tdNodes3.[1] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_005_UpdateTargetDeviceConf() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_005_UpdateTargetDeviceConf"
        let portalPortNo = GlbFunc.nextTcpPortNo()
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let tdConfFName = Functions.AppendPathName tdConfDName ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid tgid ( lun_me.fromPrim 1UL ) "b001" 1u

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 1 ))
                Assert.True(( tdNodes1.[0].TargetDeviceName = "a001" ))
                Assert.True(( ( tdNodes1.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                do! cc1.KillTargetDeviceProc tdid

                let tdNode2 = ss.UpdateTargetDeviceNode tdNodes1.[0] tdid "a002" tdNodes1.[0].NegotiableParameters tdNodes1.[0].LogParameters
                Assert.True(( ( tdNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
                let v = ss.Validate()
                Assert.True(( v.Length = 0 ))

                do! ss.Publish cc1

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))
                Assert.True(( tdNodes2.[0].TargetDeviceName = "a002" ))
                Assert.True(( ( tdNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let wroteConfFile = TargetDeviceConf.ReaderWriter.LoadFile tdConfFName
                Assert.True(( wroteConfFile.DeviceName = "a002" ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_006_DeleteTargetDeviceConf() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_006_DeleteTargetDeviceConf"
        let portalPortNo = GlbFunc.nextTcpPortNo()
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid tgid ( lun_me.fromPrim 1UL ) "b001" 1u

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 1 ))
                Assert.True(( tdNodes1.[0].TargetDeviceName = "a001" ))
                Assert.True(( ( tdNodes1.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                do! cc1.KillTargetDeviceProc tdid

                do ss.DeleteTargetDeviceNode tdNodes1.[0]
                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 0 ))

                let v = ss.Validate()
                Assert.True(( v.Length = 0 ))

                do! ss.Publish cc1

                Assert.False(( Directory.Exists tdConfDName ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_007_AddTargetGroupConf() =
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
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid1 ( lun_me.fromPrim 10UL ) "b001" 1u
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid2 tgid2 ( lun_me.fromPrim 20UL ) "b002" 2u

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 2 ))

                let tdNode1 = tdNodes1 |> Seq.find ( fun itr -> itr.TargetDeviceID = tdid1 )
                let tdNode2 = tdNodes1 |> Seq.find ( fun itr -> itr.TargetDeviceID = tdid2 )

                let tgNodes11 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes11.Length = 1 ))

                let tgNodes12 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes12.Length = 1 ))

                let tgidA = GlbFunc.newTargetGroupID()
                let tgNodeA = ss.AddTargetGroupNode tdNode1 tgidA "tg01" false
                let tNodeA = ss.AddTargetNode tgNodeA {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t001"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNodeA ( lun_me.fromPrim 1UL ) "luname001"
                Assert.True(( ( tgNodeA :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let tgidB = GlbFunc.newTargetGroupID()
                let tgNodeB = ss.AddTargetGroupNode tdNode2 tgidB "tg11" false
                let tNodeB = ss.AddTargetNode tgNodeB {
                            IdentNumber = tnodeidx_me.fromPrim 101u;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "t011"
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                let _ = ss.AddDummyDeviceLUNode tNodeB ( lun_me.fromPrim 11UL ) "luname011"
                Assert.True(( ( tgNodeB :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                do! ss.Publish cc1

                let tgConfFName1_1 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid1 )
                let tgConfFName1_A = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgidA )
                let tgConfFName2_1 = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgid2 )
                let tgConfFName2_B = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgidB )

                let wroteConfFile1_1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName1_1
                Assert.True(( wroteConfFile1_1.TargetGroupName = "b001" ))
                let wroteConfFile1_A = TargetGroupConf.ReaderWriter.LoadFile tgConfFName1_A
                Assert.True(( wroteConfFile1_A.TargetGroupName = "tg01" ))

                let wroteConfFile2_1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName2_1
                Assert.True(( wroteConfFile2_1.TargetGroupName = "b002" ))
                let wroteConfFile2_B = TargetGroupConf.ReaderWriter.LoadFile tgConfFName2_B
                Assert.True(( wroteConfFile2_B.TargetGroupName = "tg11" ))

                let tgNodes21 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes21.Length = 2 ))
                for itr in tgNodes21 do
                    Assert.True(( ( itr :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let tgNodes22 = ( tdNode2 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes22.Length = 2 ))
                for itr in tgNodes22 do
                    Assert.True(( ( itr :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname


