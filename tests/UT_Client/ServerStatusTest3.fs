//=============================================================================
// Haruka Software Storage.
// ServerStatusTest3.fs : Test cases for ServerStatus class.
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

type ServerStatus_Test3() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    static member Init ( caseName : string ) =
        let portNo = GlbFunc.nextTcpPortNo()
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "ServerStatus_Test3_" + caseName
        if Directory.Exists dname then GlbFunc.DeleteDir dname
        GlbFunc.CreateDir dname |> ignore
        ( portNo, dname )

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Publish_008_UpdateTargetGroupConf() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_008_UpdateTargetGroupConf"
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
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid1 ( lun_me.fromPrim 1UL ) "b001" 1u
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid2 tgid2 ( lun_me.fromPrim 2UL ) "b002" 2u

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

                let tgNodes12 = ( tdNode2 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes12.Length = 1 ))

                do! cc1.InactivateTargetGroup tdid1 tgid1
                do! cc1.UnloadTargetGroup tdid1 tgid1
                do! cc1.InactivateTargetGroup tdid2 tgid2
                do! cc1.UnloadTargetGroup tdid2 tgid2

                let tgNode21 = ss.UpdateTargetGroupNode tgNodes11.[0] tgid1 "newtg01" true
                Assert.True(( ( tgNode21 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let tgNode22 = ss.UpdateTargetGroupNode tgNodes12.[0] tgid2 "newtg02" true
                Assert.True(( ( tgNode22 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                do! ss.Publish cc1

                let tgConfFName1 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid1 )
                let tgConfFName2 = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgid2 )

                let wroteConfFile1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName1
                Assert.True(( wroteConfFile1.TargetGroupName = "newtg01" ))

                let wroteConfFile2 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName2
                Assert.True(( wroteConfFile2.TargetGroupName = "newtg02" ))

                let tgNodes31 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes31.Length = 1 ))

                let tgNodes32 = ( tdNode2 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes32.Length = 1 ))

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_009_DeleteTargetGroupConf() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_009_DeleteTargetGroupConf"
        let portalPortNo1 = GlbFunc.nextTcpPortNo()
        let portalPortNo2 = GlbFunc.nextTcpPortNo()
        let tdid1 = GlbFunc.newTargetDeviceID()
        let tdid2 = GlbFunc.newTargetDeviceID()
        let tgid11 = GlbFunc.newTargetGroupID()
        let tgid12 = GlbFunc.newTargetGroupID()
        let tgid21 = GlbFunc.newTargetGroupID()
        let tgid22 = GlbFunc.newTargetGroupID()
        let tdConfDName1 = Functions.AppendPathName dname ( tdid_me.toString tdid1 )
        let tdConfDName2 = Functions.AppendPathName dname ( tdid_me.toString tdid2 )

        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo1 tdid1
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo2 tdid2
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid11 ( lun_me.fromPrim 1UL ) "b001" 1u
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid1 tgid12 ( lun_me.fromPrim 2UL ) "b002" 2u
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid2 tgid21 ( lun_me.fromPrim 3UL ) "b003" 3u
        ServerStatus_Test1.WriteTargetGroupConfig_WithDummyLU dname tdid2 tgid22 ( lun_me.fromPrim 4UL ) "b004" 4u

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
                Assert.True(( tgNodes11.Length = 2 ))
                let tgNode11 = tgNodes11 |> Seq.find ( fun itr -> itr.TargetGroupName = "b001" )

                let tgNodes12 = ( tdNode2 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes12.Length = 2 ))
                let tgNode21 = tgNodes12 |> Seq.find ( fun itr -> itr.TargetGroupName = "b003" )

                do! cc1.InactivateTargetGroup tdid1 tgid11
                do! cc1.UnloadTargetGroup tdid1 tgid11
                do! cc1.InactivateTargetGroup tdid2 tgid21
                do! cc1.UnloadTargetGroup tdid2 tgid21

                do ss.DeleteTargetGroupNode tgNode11
                do ss.DeleteTargetGroupNode tgNode21

                do! ss.Publish cc1

                let tgConfFName11 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid11 )
                let tgConfFName12 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid12 )
                let tgConfFName21 = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgid21 )
                let tgConfFName22 = Functions.AppendPathName tdConfDName2 ( tgid_me.toString tgid22 )

                Assert.False(( File.Exists tgConfFName11 ))
                let wroteConfFile12 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName12
                Assert.True(( wroteConfFile12.TargetGroupName = "b002" ))

                Assert.False(( File.Exists tgConfFName21 ))
                let wroteConfFile22 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName22
                Assert.True(( wroteConfFile22.TargetGroupName = "b004" ))

                let tgNodes31 = ( tdNode1 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes31.Length = 1 ))

                let tgNodes32 = ( tdNode2 :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes32.Length = 1 ))

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_010_AddLU() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_010_AddLU"
        let portalPortNo1 = GlbFunc.nextTcpPortNo()
        let tdid1 = GlbFunc.newTargetDeviceID()
        let tgid1 = GlbFunc.newTargetGroupID()
        let tdConfDName1 = Functions.AppendPathName dname ( tdid_me.toString tdid1 )

        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo1 tdid1
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

                let tgNodes1 = ( tdNodes1.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes1.Length = 1 ))

                let tNodes1 = ( tgNodes1.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes1.Length = 1 ))

                do! cc1.InactivateTargetGroup tdid1 tgid1
                do! cc1.UnloadTargetGroup tdid1 tgid1

                let _ = ss.AddDummyDeviceLUNode tNodes1.[0] ( lun_me.fromPrim 2UL ) "luname002" Constants.LU_DEF_MULTIPLICITY

                do! ss.Publish cc1

                let tgConfFName1 = Functions.AppendPathName tdConfDName1 ( tgid_me.toString tgid1 )
                let wroteConfFile1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName1
                Assert.True(( wroteConfFile1.LogicalUnit.Length = 2 ))

                let luDirName1 = Functions.AppendPathName tdConfDName1 ( lun_me.WorkDirName ( lun_me.fromPrim 1UL ) )
                Assert.True(( Directory.Exists luDirName1 ))
                let luDirName2 = Functions.AppendPathName tdConfDName1 ( lun_me.WorkDirName ( lun_me.fromPrim 2UL ) )
                Assert.True(( Directory.Exists luDirName2 ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Publish_011_DeleteLU() =
        let portNo, dname = ServerStatus_Test1.Init "Publish_011_DeleteLU"
        let portalPortNo1 = GlbFunc.nextTcpPortNo()
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )

        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo1 tdid

        let lun1 = lun_me.fromPrim 1UL
        let lun2 = lun_me.fromPrim 2UL
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "tg001";
            EnabledAtStart = true;
            Target = [{
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = "target001";
                TargetAlias = "";
                LUN = [ lun1; lun2; ];
                Auth = TargetGroupConf.U_None();
            }];
            LogicalUnit = [
                {
                    LUN = lun1;
                    LUName = "";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice( );
                };
                {
                    LUN = lun2;
                    LUName = "";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.U_DummyDevice();
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf

        let luDName1 = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName lun1 )
        GlbFunc.CreateDir luDName1 |> ignore

        let luDName2 = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName lun2 )
        GlbFunc.CreateDir luDName2 |> ignore

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

                let tgNodes1 = ( tdNodes1.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes1.Length = 1 ))

                let tNodes1 = ( tgNodes1.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes1.Length = 1 ))

                let luNodes = tgNodes1.[0].GetAccessibleLUNodes()
                Assert.True(( luNodes.Length = 2 ))
                let lu1Node = luNodes |> Seq.find( fun itr -> itr.LUN = lun1 )

                do! cc1.InactivateTargetGroup tdid tgid
                do! cc1.UnloadTargetGroup tdid tgid

                let _ = ss.DeleteNodeInTargetGroup lu1Node

                do! ss.Publish cc1

                let wroteConfFile1 = TargetGroupConf.ReaderWriter.LoadFile tgConfFName
                Assert.True(( wroteConfFile1.LogicalUnit.Length = 1 ))
                Assert.True(( wroteConfFile1.LogicalUnit.[0].LUN = lun2 ))

                Assert.False(( Directory.Exists luDName1 ))
                Assert.True(( Directory.Exists luDName2 ))
            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname


    [<Fact>]
    member _.LoadAllConfigure() =
        let portNo, dname = ServerStatus_Test3.Init "LoadAllConfigure"
        let tdcount = Constants.MAX_TARGET_DEVICE_COUNT
        let tgcount = Constants.MAX_TARGET_GROUP_COUNT_IN_TD
        let tdids = [
            for i = 0 to tdcount - 1 do
                yield GlbFunc.newTargetDeviceID()
        ]
        let tgids = [|
            for i = 0 to tdcount - 1 do
                yield [|
                    for i = 0 to tgcount - 1 do
                        yield GlbFunc.newTargetGroupID()
                |]
        |]
        let tgconfs : HarukaCtrlerCtrlRes.T_TargetGroup list array = [|
            for i = 0 to tdcount - 1 do
                let confs = [
                    for j = 0 to tgcount - 1 do
                        let conf : TargetGroupConf.T_TargetGroup = {
                            TargetGroupID = tgids.[i].[j];
                            TargetGroupName = ( sprintf "targetgroup_bbb_%03d" j );
                            EnabledAtStart = false;
                            Target = [{
                                IdentNumber = tnodeidx_me.fromPrim 0u;
                                TargetPortalGroupTag = tpgt_me.zero;
                                TargetName = ( sprintf "target%03d" j );
                                TargetAlias = "";
                                LUN = [ lun_me.fromPrim ( uint64 j + 1UL ) ];
                                Auth = TargetGroupConf.U_None();
                            }];
                            LogicalUnit = [{
                                LUN = lun_me.fromPrim ( uint64 j + 1UL );
                                LUName = "";
                                WorkPath = "";
                                MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                                LUDevice = TargetGroupConf.U_DummyDevice();
                            }];
                        }
                        yield conf
                ]
                let respconfs : HarukaCtrlerCtrlRes.T_TargetGroup list =
                    confs
                    |> List.map ( fun itr -> {
                        TargetGroupID = itr.TargetGroupID; Config = TargetGroupConf.ReaderWriter.ToString itr;
                    })
                yield respconfs
        |]

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo

                let! _ = Functions.FramingReceiver c
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = tdids;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2

                for i = 0 to tdcount - 1 do
                    let! _ = Functions.FramingReceiver c

                    let tdconf : TargetDeviceConf.T_TargetDevice = {
                        NetworkPortal = [ ServerStatus_Test1.defaultNP ];
                        NegotiableParameters = None;
                        LogParameters = None;
                        DeviceName = ( sprintf "targetdevice_aaa_%03d" i );
                        EnableStatSNAckChecker = false;
                    }
                    let rb3 =
                        HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                            Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                                TargetDeviceID = tdids.[i];
                                Config = TargetDeviceConf.ReaderWriter.ToString tdconf
                                ErrorMessage = "";
                            })
                        }
                    do! Functions.FramingSender c rb3

                    let! _ = Functions.FramingReceiver c 
                    let rb4 =
                        HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                            Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                                TargetDeviceID = tdids.[i];
                                TargetGroup = tgconfs.[i];
                                ErrorMessage = "";
                            })
                        }
                    do! Functions.FramingSender c rb4

                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let cn = ss.ControllerNode
                Assert.True(( cn.RemoteCtrlValue.PortNum = uint16 portNo ))

                let tdl =
                    ss.GetTargetDeviceNodes()
                    |> List.sortBy ( fun itr -> itr.TargetDeviceName )
                Assert.True(( tdl.Length = tdcount ))

                for i = 0 to tdcount - 1 do
                    Assert.True(( tdl.[i].TargetDeviceID = tdids.[i] ))
                    Assert.True(( tdl.[i].TargetDeviceName = ( sprintf "targetdevice_aaa_%03d" i ) ))

                    let tgl =
                        ( tdl.[i] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                        |> List.sortBy ( fun itr -> itr.TargetGroupName )
                    Assert.True(( tgl.Length = tgcount ))

                    for j = 0 to tgcount - 1 do
                        Assert.True(( tgl.[j].TargetGroupID = tgids.[i].[j] ))
                        Assert.True(( tgl.[j].TargetGroupName = ( sprintf "targetgroup_bbb_%03d" j ) ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname
