//=============================================================================
// Haruka Software Storage.
// ServerStatusTest4.fs : Test cases for ServerStatus class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type ServerStatus_Test4() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.UpdateControllerNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateControllerNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let ctrlNode1 = ss.ControllerNode
                let cnode1 = ( ctrlNode1 :> IConfigFileNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( cnode1.Length = 1 ))
                Assert.True(( ( ctrlNode1 :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = Some {
                        PortNum = 999us;
                        Address = "aaa";
                        WhiteList = [];
                    }
                    LogMaintenance = None;
                    LogParameters = None;
                }
                let ctrlNode2 = ss.UpdateControllerNode conf

                Assert.True(( ctrlNode2.GetConfigureData().RemoteCtrl.Value.PortNum = 999us ))
                Assert.True(( ctrlNode2.GetConfigureData().RemoteCtrl.Value.Address = "aaa" ))
                Assert.True(( ( ctrlNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let cnode2 = ( ctrlNode1 :> IConfigFileNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( cnode2.Length = 1 ))
                Assert.True(( cnode1 = cnode2 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetDeviceNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetDeviceNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let plist = tdNode.GetParentNodes<IConfigureNode>()
                Assert.True(( plist.Length = 1 ))
                Assert.True(( plist.[0] = ss.ControllerNode ))

                let clist = tdNode.GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 0 ))

                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                Assert.True(( tdlist.[0] = ( tdNode :?> ConfNode_TargetDevice ) ))

                Assert.True(( tdlist.[0].NegotiableParameters = ServerStatus_Test1.defaultNego ))
                Assert.True(( tdlist.[0].LogParameters = ServerStatus_Test1.defaultLP ))
                Assert.True(( tdlist.[0].TargetDeviceName = "a" ))
                Assert.True(( tdlist.[0].TargetDeviceID = tdid ))
                Assert.True(( tdNode.Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetDeviceNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetDeviceNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT + 1 do
                    let tdid = GlbFunc.newTargetDeviceID()
                    let tdname = sprintf "a%03d" i
                    let tdNode = ss.AddTargetDeviceNode tdid tdname ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                    let plist = tdNode.GetParentNodes<IConfigureNode>()
                    Assert.True(( plist.Length = 1 ))
                    Assert.True(( plist.[0] = ss.ControllerNode ))

                    let clist = tdNode.GetChildNodes<IConfigureNode>()
                    Assert.True(( clist.Length = 0 ))

                let tdlist =
                    ss.GetTargetDeviceNodes()
                    |> List.sortBy ( fun itr -> itr.TargetDeviceName )
                Assert.True(( tdlist.Length = Constants.MAX_TARGET_DEVICE_COUNT + 2 ))

                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT + 1 do
                    let wstr = sprintf "a%03d" i
                    Assert.True(( tdlist.[i].TargetDeviceName = wstr ))
                    Assert.True(( ( tdlist.[i] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteTargetDeviceNode_002"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let _ = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                ss.DeleteTargetDeviceNode tdlist2.[0]

                let tdlist3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist3.Length = 0 ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteTargetDeviceNode_003"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let _ = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                ss.DeleteTargetDeviceNode tdlist2.[0]

                let tdlist3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist3.Length = 0 ))

                try
                    ss.DeleteTargetDeviceNode tdlist2.[0]
                    Assert.Fail __LINE__
                with
                | :? KeyNotFoundException ->
                    ()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateTargetDeviceNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateTargetDeviceNode_002"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ) = tdNode ))
                Assert.True(( tdNode.Modified = ModifiedStatus.Modified ) )

                let newTdid = GlbFunc.newTargetDeviceID()
                let r = ss.UpdateTargetDeviceNode tdlist2.[0] newTdid "new" tdlist2.[0].NegotiableParameters tdlist2.[0].LogParameters
                Assert.True(( r.TargetDeviceID = newTdid ))
                Assert.True(( r.TargetDeviceName = "new" ))
                Assert.True(( r.NegotiableParameters = tdlist2.[0].NegotiableParameters ))
                Assert.True(( r.LogParameters = tdlist2.[0].LogParameters ))

                let tdlist3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist3.Length = 1 ))
                Assert.True(( ( tdlist3.[0] :> IConfigFileNode ) = r ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddNetworkPortalNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "AddNetworkPortalNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ) = tdNode ))
                Assert.True(( tdNode.Modified = ModifiedStatus.Modified ) )

                let r = ss.AddNetworkPortalNode tdlist2.[0] ServerStatus_Test1.defaultNP
                Assert.True(( r.NetworkPortal = ServerStatus_Test1.defaultNP ))
                    
                let nplist = ( tdlist2.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist.Length = 1 ))
                Assert.True(( nplist.[0] = r ))

                let tdlist3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist3.Length = 1 ))
                Assert.True(( ( tdlist3.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddNetworkPortalNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddNetworkPortalNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                Assert.True(( ( tdlist.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ) )

                let r = ss.AddNetworkPortalNode tdlist.[0] ServerStatus_Test1.defaultNP
                Assert.True(( r.NetworkPortal = ServerStatus_Test1.defaultNP ))
                    
                let nplist = ( tdlist.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist.Length = 2 ))

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteNetworkPortalNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteNetworkPortalNode_002"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
                let npnode = ss.AddNetworkPortalNode tdNode ServerStatus_Test1.defaultNP
                Assert.True(( ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                ss.DeleteNetworkPortalNode npnode

                let tdlist3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist3.Length = 1 ))
                Assert.True(( ( tdlist3.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let nplist2 = ( tdlist3.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist2.Length = 0 ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteNetworkPortalNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteNetworkPortalNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                Assert.True(( ( tdlist.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ) )

                let nplist = ( tdlist.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist.Length = 1 ))

                ss.DeleteNetworkPortalNode nplist.[0]

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let nplist2 = ( tdlist2.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist2.Length = 0 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateNetworkPortalNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateNetworkPortalNode_002"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let tdNode = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
                let npnode = ss.AddNetworkPortalNode tdNode ServerStatus_Test1.defaultNP

                Assert.True(( ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let conf = {
                    ServerStatus_Test1.defaultNP with
                        IdentNumber = netportidx_me.fromPrim 2u;
                        TargetAddress = "::2";
                        PortNumber = 456us;
                }
                let r = ss.UpdateNetworkPortalNode npnode conf
                Assert.True(( r.NetworkPortal = conf ))

                let tdlist3 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist3.Length = 1 ))
                Assert.True(( ( tdlist3.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let nplist2 = ( tdlist3.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist2.Length = 1 ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateNetworkPortalNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateNetworkPortalNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, _ = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                Assert.True(( ( tdlist.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ) )

                let nplist = ( tdlist.[0] :> IConfigFileNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist.Length = 1 ))

                let conf = {
                    ServerStatus_Test1.defaultNP with
                        IdentNumber = netportidx_me.fromPrim 2u;
                }
                let r = ss.UpdateNetworkPortalNode nplist.[0] conf
                Assert.True(( r.NetworkPortal = conf ))

                let tdlist2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist2.Length = 1 ))
                Assert.True(( ( tdlist2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let nplist2 = ( tdlist2.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist2.Length = 1 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetGroupNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetGroupNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "a" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))

                let plist = tgNode.GetParentNodes<IConfigureNode>()
                Assert.True(( plist.Length = 1 ))
                Assert.True(( plist.[0] = tdNodes.[0] ))

                let clist = tgNode.GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 0 ))

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetGroupNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetGroupNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))

                for i = 0 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD + 1 do
                    let tgid = GlbFunc.newTargetGroupID()
                    let tgname = sprintf "add%d" i
                    let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid tgname true :> IConfigFileNode

                    let plist = tgNode.GetParentNodes<IConfigureNode>()
                    Assert.True(( plist.Length = 1 ))
                    Assert.True(( plist.[0] = tdNodes.[0] ))

                    let clist = tgNode.GetChildNodes<IConfigureNode>()
                    Assert.True(( clist.Length = 0 ))

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))

                let tgNodes2 = ( tdNodes2.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = Constants.MAX_TARGET_GROUP_COUNT_IN_TD + 3 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteTargetGroupNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes1 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes1.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "a" true

                let tgNodes2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = 2 ))

                ss.DeleteTargetGroupNode tgNode

                let tgNodes3 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes3.Length = 1 ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateTargetGroupNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateTargetGroupNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                Assert.True(( ( tdNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "a" true
                Assert.True(( ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let tgNodes2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = 2 ))

                let tgid2 = GlbFunc.newTargetGroupID()
                let r = ss.UpdateTargetGroupNode tgNode tgid2 "add001" false
                Assert.True(( r.TargetGroupName = "add001" ))
                Assert.True(( ( r :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))
                Assert.True(( ( tdNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteNodeInTargetGroup_002() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteNodeInTargetGroup_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "a" true
                Assert.True(( ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let tNodes1 = ( tgNode :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes1.Length = 0 ))

                let tconf = ServerStatus_Test1.defTarget 1u "target000"
                let tNode = ss.AddTargetNode tgNode tconf

                let tNodes2 = ( tgNode :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes2.Length = 1 ))

                ss.DeleteNodeInTargetGroup tNode

                let tNodes3 = ( tgNode :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes3.Length = 0 ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteNodeInTargetGroup_003() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteNodeInTargetGroup_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
//                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes1 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes1.Length = 1 ))
                Assert.True(( ( tgNodes1.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes1.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                ss.DeleteNodeInTargetGroup tNodes.[0]

                let tgNodes2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = 1 ))
                Assert.True(( ( tgNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))

                let tconf = ServerStatus_Test1.defTarget 2u "target000"
                let tNode = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf

                let plist = ( tNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( plist.Length = 1 ))
                Assert.True(( plist.[0] = tgNode ))

                let clist = ( tNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 0 ))

                let tgNode2 =
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
//                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                let tconf = ServerStatus_Test1.defTarget 2u "target000"
                let tNode = ss.AddTargetNode tgNodes.[0] tconf

                let plist = ( tNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( plist.Length = 1 ))
                let clist = ( tNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 0 ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let tNodes2 = ( tgNode2.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes2.Length = 2 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateTargetNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateTargetNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1

                let tconf2 = ServerStatus_Test1.defTarget 33u "target999"
                let tNode2 = ss.UpdateTargetNode tNode1 tconf2
                Assert.True(( tNode2.Values = tconf2 ))

                let plist = ( tNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( plist.Length = 1 ))
                Assert.True(( plist.[0] = tgNode ))

                let clist = ( tNode2 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 0 ))

                let tgNode2 =
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateTargetNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateTargetNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                let tconf = ServerStatus_Test1.defTarget 2u "target999"
                let tNode = ss.UpdateTargetNode tNodes.[0] tconf
                Assert.True(( tNode.Values.TargetName = tconf.TargetName ))

                let plist = ( tNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( plist.Length = 1 ))
                let clist = ( tNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let tNodes2 = ( tgNode2.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes2.Length = 1 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetLURelation_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetLURelation_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1

                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 1UL ) "luname001"
                let pnodes1 = ( luNode :> ILUNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes1.Length = 1 ))
                Assert.True(( pnodes1.[0] = tNode1 ))

                let tconf2 : TargetGroupConf.T_Target = {
                    tconf1 with
                        IdentNumber = tnodeidx_me.fromPrim 3u;
                        TargetName = "target111";
                }
                let tNode2 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf2

                ss.AddTargetLURelation tNode2 ( luNode :> ILUNode )

                let pnodes2 = ( luNode :> ILUNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes2.Length = 2 ))
                Assert.True(( pnodes2 = [ tNode1; tNode2 ] || pnodes2 = [ tNode2; tNode1 ] ))

                let cnode1 = ( tNode2 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( cnode1.Length = 1 ))
                Assert.True(( cnode1.[0] = luNode ))

                let tgNode2 =
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddTargetLURelation_002() =
        let portNo, dname = ServerStatus_Test1.Init "AddTargetLURelation_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                let tdid = GlbFunc.newTargetDeviceID()
                let tgid = GlbFunc.newTargetGroupID()
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [
                            {
                                ( ServerStatus_Test1.defTarget 0u "target000" ) with
                                    LUN = [ lun_me.fromPrim 1UL ];
                            }
                            {
                                ( ServerStatus_Test1.defTarget 1u "target001" ) with
                                    LUN = [ lun_me.fromPrim 2UL ];
                            }
                        ];
                        LogicalUnit = [
                            {
                                LUN = lun_me.fromPrim 1UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_DummyDevice();
                            }
                            {
                                LUN = lun_me.fromPrim 2UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_DummyDevice();
                            };
                        ];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }
                //do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                //do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ) )

                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 2 ))
                let luNodes1 = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes1.Length = 1 ))
                let luNodes2 = ( tNodes.[1] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes2.Length = 1 ))

                ss.AddTargetLURelation tNodes.[0] ( luNodes2.[0] :?> ILUNode )

                let pnodes2 = ( luNodes2.[0] :?> ILUNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes2.Length = 2 ))

                let cnode1 = ( tNodes.[0] :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( cnode1.Length = 1 ))

                let tgNodes2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = 1 ))
                Assert.True(( ( tgNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetLURelation_001() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteTargetLURelation_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1

                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 1UL ) "luname001"
                let pnodes1 = ( luNode :> ILUNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes1.Length = 1 ))
                Assert.True(( pnodes1.[0] = tNode1 ))

                ss.DeleteTargetLURelation tNode1 ( luNode :> ILUNode )

                let cnode1 = ( tNode1 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( cnode1.Length = 0 ))

                let tgNode2 =
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetLURelation_003() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteTargetLURelation_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
//                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ) )
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))
                
                ss.DeleteTargetLURelation tNodes.[0] ( luNodes.[0] :?> ILUNode )

                let tgNodes2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = 1 ))
                Assert.True(( ( tgNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let tNodes2 = ( tgNodes2.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes2.Length = 1 ))
                let luNodes2 = ( tNodes2.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes2.Length = 0 ))

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetLURelation_004() =
        let portNo, dname = ServerStatus_Test1.Init "DeleteTargetLURelation_004"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ) )
                let tNodes = ( tgNodes.[0]  :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))
                
                let tconf2 = ServerStatus_Test1.defTarget 2u "target999"
                let tNode2 = ss.AddTargetNode tgNodes.[0] tconf2
                ss.AddTargetLURelation tNode2 ( luNodes.[0] :?> ILUNode )

                let pnodes1 = ( luNodes.[0] :?> ILUNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes1.Length = 2 ))
                Assert.True(( pnodes1 = [ tNodes.[0]; tNode2 ] || pnodes1 = [ tNode2; tNodes.[0] ] ))

                ss.DeleteTargetLURelation tNodes.[0] ( luNodes.[0] :?> ILUNode )

                let pnodes2 = ( luNodes.[0] :?> ILUNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes2.Length = 1 ))
                Assert.True(( pnodes2.[0] =tNode2 ))

                let cnodes2 = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( cnodes2.Length = 0 ))

                let tgNodes2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes2.Length = 1 ))
                Assert.True(( ( tgNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddBlockDeviceLUNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddBlockDeviceLUNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1

                let luNode = ss.AddBlockDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"
                let pnodes = ( luNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tNode1 ))

                let clist = ( tNode1 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddBlockDeviceLUNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddBlockDeviceLUNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
//                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                let luNode = ss.AddBlockDeviceLUNode tNodes.[0] ( lun_me.fromPrim 22UL ) "luname022"
                let pnodes = ( luNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tNodes.[0] ))

                let clist = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 2 ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddBlockDeviceLUNode_InTargetGroup_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddBlockDeviceLUNode_InTargetGroup_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true
                Assert.True(( ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
                
                let luNode = ss.AddBlockDeviceLUNode_InTargetGroup tgNode ( lun_me.fromPrim 22UL ) "luname022"
                let pnodes = ( luNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )

                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tgNode2 ))

                let clist = ( tgNode2 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddBlockDeviceLUNode_InTargetGroup_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddBlockDeviceLUNode_InTargetGroup_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
//                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let luNode = ss.AddBlockDeviceLUNode_InTargetGroup tgNodes.[0] ( lun_me.fromPrim 22UL ) "luname022"

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

                let pnodes = ( luNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tgNode2.[0] ))

                let clist1 = ( tgNode2.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist1.Length = 2 ))

                match clist1.[0] with
                | :? ConfNode_Target as x ->
                    ()
                | _ ->
                    Assert.Fail __LINE__

                match clist1.[1] with
                | :? ConfNode_BlockDeviceLU as x ->
                    ()
                | _ ->
                    Assert.Fail __LINE__

            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateBlockDeviceLUNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateBlockDeviceLUNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddBlockDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"

                let luNode2 = ss.UpdateBlockDeviceLUNode luNode ( lun_me.fromPrim 33UL ) "luname033"
                Assert.True(( ( luNode2 :> ILUNode ).LUN = lun_me.fromPrim 33UL ))
                Assert.True(( ( luNode2 :> ILUNode ).LUName = "luname033" ))

                let clist = ( tNode1 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))
                Assert.True(( clist.[0] = luNode2 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateBlockDeviceLUNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateBlockDeviceLUNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                let tdid = GlbFunc.newTargetDeviceID()
                let tgid = GlbFunc.newTargetGroupID()
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [{
                            ( ServerStatus_Test1.defTarget 0u "target000" ) with
                                LUN = [ lun_me.fromPrim 1UL ];
                        }];
                        LogicalUnit = [{
                                LUN = lun_me.fromPrim 1UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_BlockDevice({
                                    Peripheral = TargetGroupConf.U_DummyMedia({
                                        IdentNumber = mediaidx_me.fromPrim 1u;
                                        MediaName = "";
                                    })
                                });
                        }];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))

                let luNode2 = ss.UpdateBlockDeviceLUNode ( luNodes.[0] :?> ConfNode_BlockDeviceLU ) ( lun_me.fromPrim 33UL ) "luname033"
                Assert.True(( ( luNode2 :> ILUNode ).LUN = lun_me.fromPrim 33UL ))
                Assert.True(( ( luNode2 :> ILUNode ).LUName = "luname033" ))

                let pnodes = ( luNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddDummyDeviceLUNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddDummyDeviceLUNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1

                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"
                let pnodes = ( luNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tNode1 ))

                let clist = ( tNode1 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddDummyDeviceLUNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddDummyDeviceLUNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                let luNode = ss.AddDummyDeviceLUNode tNodes.[0] ( lun_me.fromPrim 22UL ) "luname022"
                let pnodes = ( luNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tNodes.[0] ))

                let clist = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 2 ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateDummyDeviceLUNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateDummyDeviceLUNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"

                let luNode2 = ss.UpdateDummyDeviceLUNode luNode ( lun_me.fromPrim 33UL ) "luname033"
                Assert.True(( ( luNode2 :> ILUNode ).LUN = lun_me.fromPrim 33UL ))
                Assert.True(( ( luNode2 :> ILUNode ).LUName = "luname033" ))

                let clist = ( tNode1 :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))
                Assert.True(( clist.[0] = luNode2 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateDummyDeviceLUNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateDummyDeviceLUNode_003"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))

                let luNode2 = ss.UpdateDummyDeviceLUNode ( luNodes.[0] :?> ConfNode_DummyDeviceLU ) ( lun_me.fromPrim 33UL ) "luname033"
                Assert.True(( ( luNode2 :> ILUNode ).LUN = lun_me.fromPrim 33UL ))
                Assert.True(( ( luNode2 :> ILUNode ).LUName = "luname033" ))

                let pnodes = ( luNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = tNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddPlainFileMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddPlainFileMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"

                let mNode = ss.AddPlainFileMediaNode luNode ServerStatus_Test1.defaultSF
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddPlainFileMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddPlainFileMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))

                let mNode = ss.AddPlainFileMediaNode luNodes.[0] ServerStatus_Test1.defaultSF
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdatePlainFileMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddPlainFileMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"
                let mNode = ss.AddPlainFileMediaNode luNode ServerStatus_Test1.defaultSF

                let conf = {
                    ServerStatus_Test1.defaultSF with
                        FileName = "aaaa";
                }
                let mNode2 = ss.UpdatePlainFileMediaNode mNode conf
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
                    Assert.True(( x.FileName = "aaaa" ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdatePlainFileMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdatePlainFileMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                let tdid = GlbFunc.newTargetDeviceID()
                let tgid = GlbFunc.newTargetGroupID()
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [{
                            ( ServerStatus_Test1.defTarget 0u "target000" ) with
                                LUN = [ lun_me.fromPrim 1UL ];
                        }];
                        LogicalUnit = [{
                                LUN = lun_me.fromPrim 1UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_BlockDevice({
                                    Peripheral = TargetGroupConf.U_PlainFile( ServerStatus_Test1.defaultSF )
                                });
                        }];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))
                let mNodes = luNodes.[0].GetDescendantNodes<IMediaNode>()
                Assert.True(( mNodes.Length = 1 ))

                let conf = {
                    ServerStatus_Test1.defaultSF with
                        FileName = "aaaa";
                }
                let mNode2 = ss.UpdatePlainFileMediaNode ( mNodes.[0] :?> ConfNode_PlainFileMedia )  conf
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
                    Assert.True(( x.FileName = "aaaa" ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddMemBufferMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddMemBufferMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"

                let mNode = ss.AddMemBufferMediaNode luNode {
                    IdentNumber = mediaidx_me.fromPrim 1u;
                    MediaName = "";
                    BytesCount = Constants.MEDIA_BLOCK_SIZE;
                }
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddMemBufferMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddMemBufferMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))

                let mNode = ss.AddMemBufferMediaNode luNodes.[0] {
                    IdentNumber = mediaidx_me.fromPrim 1u;
                    MediaName = "";
                    BytesCount = Constants.MEDIA_BLOCK_SIZE;
                }
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateMemBufferMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateMemBufferMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"
                let mNode = ss.AddMemBufferMediaNode luNode {
                    IdentNumber = mediaidx_me.fromPrim 1u;
                    MediaName = "";
                    BytesCount = Constants.MEDIA_BLOCK_SIZE;
                }

                let mNode2 = ss.UpdateMemBufferMediaNode mNode {
                    IdentNumber = mediaidx_me.fromPrim 2u;
                    MediaName = "";
                    BytesCount = Constants.MEDIA_BLOCK_SIZE * 2UL;
                }
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_MemBuffer( x ) ->
                    Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
                    Assert.True(( x.BytesCount = Constants.MEDIA_BLOCK_SIZE * 2UL ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateMemBufferMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateMemBufferMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                let tdid = GlbFunc.newTargetDeviceID()
                let tgid = GlbFunc.newTargetGroupID()
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [{
                            ( ServerStatus_Test1.defTarget 0u "target000" ) with
                                LUN = [ lun_me.fromPrim 1UL ];
                        }];
                        LogicalUnit = [{
                                LUN = lun_me.fromPrim 1UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_BlockDevice({
                                    Peripheral = TargetGroupConf.U_MemBuffer({
                                        IdentNumber = mediaidx_me.fromPrim 1u;
                                        MediaName = "";
                                        BytesCount = Constants.MEDIA_BLOCK_SIZE;
                                    })
                                });
                        }];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))
                let mNodes = luNodes.[0].GetDescendantNodes<IMediaNode>()
                Assert.True(( mNodes.Length = 1 ))

                let mNode2 = ss.UpdateMemBufferMediaNode ( mNodes.[0] :?> ConfNode_MemBufferMedia ) {
                    IdentNumber = mediaidx_me.fromPrim 2u;
                    MediaName = "";
                    BytesCount = Constants.MEDIA_BLOCK_SIZE * 2UL;
                }
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_MemBuffer( x ) ->
                    Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
                    Assert.True(( x.BytesCount = Constants.MEDIA_BLOCK_SIZE * 2UL ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddDummyMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddDummyMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"

                let mNode = ss.AddDummyMediaNode luNode ( mediaidx_me.fromPrim 1u ) ""
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddDummyMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddDummyMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))

                let mNode = ss.AddDummyMediaNode luNodes.[0] ( mediaidx_me.fromPrim 1u ) ""
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateDummyMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateDummyMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"
                let mNode = ss.AddDummyMediaNode luNode ( mediaidx_me.fromPrim 1u ) ""

                let mNode2 = ss.UpdateDummyMediaNode mNode ( mediaidx_me.fromPrim 2u ) "ggg"
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_DummyMedia( x ) ->
                    Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
                    Assert.True(( x.MediaName = "ggg" ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateDummyMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateDummyMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                let tdid = GlbFunc.newTargetDeviceID()
                let tgid = GlbFunc.newTargetGroupID()
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [{
                            ( ServerStatus_Test1.defTarget 0u "target000" ) with
                                LUN = [ lun_me.fromPrim 1UL ];
                        }];
                        LogicalUnit = [{
                                LUN = lun_me.fromPrim 1UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_BlockDevice({
                                    Peripheral = TargetGroupConf.U_DummyMedia({
                                        IdentNumber = mediaidx_me.fromPrim 1u;
                                        MediaName = "";
                                    })
                                });
                        }];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))
                let mNodes = luNodes.[0].GetDescendantNodes<IMediaNode>()
                Assert.True(( mNodes.Length = 1 ))

                let mNode2 = ss.UpdateDummyMediaNode ( mNodes.[0] :?> ConfNode_DummyMedia ) ( mediaidx_me.fromPrim 2u ) "fff"
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_DummyMedia( x ) ->
                    Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
                    Assert.True(( x.MediaName = "fff" ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddDebugMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "AddDebugMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"

                let mNode = ss.AddDebugMediaNode luNode ( mediaidx_me.fromPrim 1u ) ""
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddDebugMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "AddDebugMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))

                let mNode = ss.AddDebugMediaNode luNodes.[0] ( mediaidx_me.fromPrim 1u ) ""
                let pnodes = ( mNode :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateDebugMediaNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateDebugMediaNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))
                let tconf1 = ServerStatus_Test1.defTarget 2u "target000"
                let tNode1 = ss.AddTargetNode ( tgNode :?> ConfNode_TargetGroup ) tconf1
                let luNode = ss.AddDummyDeviceLUNode tNode1 ( lun_me.fromPrim 22UL ) "luname022"
                let mNode = ss.AddDebugMediaNode luNode ( mediaidx_me.fromPrim 1u ) ""

                let mNode2 = ss.UpdateDebugMediaNode mNode ( mediaidx_me.fromPrim 2u ) "ggg"
                let mDummyNode = ss.AddDummyMediaNode mNode2 ( mediaidx_me.fromPrim 99u ) ""

                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_DebugMedia( x ) ->
                    Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
                    Assert.True(( x.MediaName = "ggg" ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNode ))

                let clist = ( luNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( clist.Length = 1 ))

                let tgNode2 = 
                    ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.find ( fun itr -> itr.TargetGroupName = "xxyyzz" )
                Assert.True(( ( tgNode2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UpdateDebugMediaNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "UpdateDebugMediaNode_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                let tdid = GlbFunc.newTargetDeviceID()
                let tgid = GlbFunc.newTargetGroupID()
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [{
                            ( ServerStatus_Test1.defTarget 0u "target000" ) with
                                LUN = [ lun_me.fromPrim 1UL ];
                        }];
                        LogicalUnit = [{
                                LUN = lun_me.fromPrim 1UL;
                                LUName = "";
                                WorkPath = "";
                                LUDevice = TargetGroupConf.U_BlockDevice({
                                    Peripheral = TargetGroupConf.U_DebugMedia({
                                        IdentNumber = mediaidx_me.fromPrim 1u;
                                        MediaName = "";
                                        Peripheral = TargetGroupConf.U_DummyMedia({
                                            IdentNumber = mediaidx_me.fromPrim 99u;
                                            MediaName = "";
                                        });
                                    })
                                });
                        }];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))
                let luNodes = ( tNodes.[0] :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                Assert.True(( luNodes.Length = 1 ))
                let mNodes = luNodes.[0].GetChildNodes<IMediaNode>()
                Assert.True(( mNodes.Length = 1 ))

                let mNode2 = ss.UpdateDebugMediaNode ( mNodes.[0] :?> ConfNode_DebugMedia ) ( mediaidx_me.fromPrim 2u ) "fff"
                match ( mNode2 :> IMediaNode ).MediaConfData with
                | TargetGroupConf.T_MEDIA.U_DebugMedia( x ) ->
                    Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
                    Assert.True(( x.MediaName = "fff" ))
                | _ ->
                    Assert.Fail __LINE__

                let pnodes = ( mNode2 :> IConfigureNode ).GetParentNodes<IConfigureNode>()
                Assert.True(( pnodes.Length = 1 ))
                Assert.True(( pnodes.[0] = luNodes.[0] ))

                let tgNode2 = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNode2.Length = 1 ))
                Assert.True(( ( tgNode2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname


    [<Fact>]
    member _.IdentifyTargetDeviceNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "IdentifyTargetDeviceNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let pc = PrivateCaller( ss )
                try
                    let _ = pc.Invoke( "IdentifyTargetDeviceNode", ss.ControllerNode )
                    Assert.Fail __LINE__
                with
                | :? EditError as x ->
                    Assert.True( x.Message.StartsWith "ERRMSG_FAILED_IDENT_TARGET_DEVICE" )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.IdentifyTargetDeviceNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "IdentifyTargetDeviceNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))

                let pc = PrivateCaller( ss )
                let r = pc.Invoke( "IdentifyTargetDeviceNode", tgNodes.[0] )
                Assert.True(( r = tdNodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.IdentifyTargetDeviceNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "IdentifyTargetDeviceNode_003"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let pc = PrivateCaller( ss )
                try
                    let r = pc.Invoke( "IdentifyTargetDeviceNode", tdNodes.[0] )
                    Assert.Fail __LINE__
                with
                | :? EditError as x ->
                    Assert.True( x.Message.StartsWith "ERRMSG_FAILED_IDENT_TARGET_DEVICE" )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorTargetDevice_001() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorTargetDevice_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                match ss.GetAncestorTargetDevice ss.ControllerNode with
                | None ->
                    ()
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorTargetDevice_002() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorTargetDevice_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdnodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdnodes.Length > 0 ))
                let tgnodes = ( tdnodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgnodes.Length > 0 ))

                match ss.GetAncestorTargetDevice tgnodes.[0] with
                | None ->
                    Assert.Fail __LINE__
                | Some( x ) ->
                    Assert.True(( x = tdnodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorTargetDevice_003() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorTargetDevice_003"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdnodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdnodes.Length > 0 ))

                match ss.GetAncestorTargetDevice tdnodes.[0] with
                | None ->
                    Assert.Fail __LINE__
                | Some( x ) ->
                    Assert.True(( x = tdnodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.IdentifyTargetGroupNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "IdentifyTargetGroupNode_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo false
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let pc = PrivateCaller( ss )
                try
                    let _ = pc.Invoke( "IdentifyTargetGroupNode", ss.ControllerNode )
                    Assert.Fail __LINE__
                with
                | :? EditError as x ->
                    Assert.True( x.Message.StartsWith "ERRMSG_FAILED_IDENT_TARGET_GROUP" )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.IdentifyTargetGroupNode_002() =
        let portNo, dname = ServerStatus_Test1.Init "IdentifyTargetGroupNode_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                let pc = PrivateCaller( ss )
                let r = pc.Invoke( "IdentifyTargetGroupNode", tNodes.[0] )
                Assert.True(( r = tgNodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.IdentifyTargetGroupNode_003() =
        let portNo, dname = ServerStatus_Test1.Init "IdentifyTargetGroupNode_003"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))

                let pc = PrivateCaller( ss )
                try
                    let _ = pc.Invoke( "IdentifyTargetGroupNode", tgNodes.[0] )
                    Assert.Fail __LINE__
                with
                | :? EditError as x ->
                    Assert.True( x.Message.StartsWith "ERRMSG_FAILED_IDENT_TARGET_GROUP" )
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorTargetGroup_001() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorTargetGroup_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                match ss.GetAncestorTargetGroup ss.ControllerNode with
                | None ->
                    ()
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorTargetGroup_002() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorTargetGroup_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdnodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdnodes.Length > 0 ))
                let tgnodes = ( tdnodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgnodes.Length > 0 ))
                let lunodes = tgnodes.[0].GetAccessibleLUNodes()
                Assert.True(( lunodes.Length > 0 ))

                match ss.GetAncestorTargetGroup lunodes.[0] with
                | None ->
                    Assert.Fail __LINE__
                | Some( x ) ->
                    Assert.True(( x = tgnodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorTargetGroup_003() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorTargetGroup_003"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdnodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdnodes.Length > 0 ))
                let tgnodes = ( tdnodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgnodes.Length > 0 ))

                match ss.GetAncestorTargetGroup tgnodes.[0] with
                | None ->
                    Assert.Fail __LINE__
                | Some( x ) ->
                    Assert.True(( x = tgnodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorLogicalUnit_001() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorLogicalUnit_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                match ss.GetAncestorLogicalUnit ss.ControllerNode with
                | None ->
                    ()
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorLogicalUnit_002() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorLogicalUnit_002"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdnodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdnodes.Length > 0 ))
                let tgnodes = ( tdnodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgnodes.Length > 0 ))
                let lunodes = tgnodes.[0].GetAccessibleLUNodes()
                Assert.True(( lunodes.Length > 0 ))

                match ss.GetAncestorLogicalUnit lunodes.[0] with
                | None ->
                    Assert.Fail __LINE__
                | Some( x ) ->
                    Assert.True(( x = lunodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAncestorLogicalUnit_003() =
        let portNo, dname = ServerStatus_Test1.Init "GetAncestorLogicalUnit_003"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
//                do! ServerStatus_Test1.RespTargetDeviceProcs c []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdnodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdnodes.Length > 0 ))
                let tgnodes = ( tdnodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgnodes.Length > 0 ))
                let lunodes = tgnodes.[0].GetAccessibleLUNodes()
                Assert.True(( lunodes.Length > 0 ))
                let medianode = ss.AddDummyMediaNode lunodes.[0] ( mediaidx_me.fromPrim 99u ) ""

                match ss.GetAncestorLogicalUnit medianode with
                | None ->
                    Assert.Fail( __LINE__ )
                | Some( x ) ->
                    Assert.True(( x = lunodes.[0] ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetDeviceUnloaded_001() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetDeviceUnloaded_001"
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

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let _ = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))
                Assert.True(( ( tdNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                let! r = ss.TryCheckTargetDeviceUnloaded cc1 tdNodes2.[0]
                Assert.True r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetDeviceUnloaded_002() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetDeviceUnloaded_002"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                Assert.True(( ( tdNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let! r = ss.TryCheckTargetDeviceUnloaded cc1 tdNodes.[0]
                Assert.True r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetDeviceUnloaded_003() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetDeviceUnloaded_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                Assert.True(( ( tdNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let! r = ss.TryCheckTargetDeviceUnloaded cc1 tdNodes.[0]
                Assert.False r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetDeviceUnloaded_004() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetDeviceUnloaded_003"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                Assert.True(( ( tdNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let npNodes = ( tdNodes.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( npNodes.Length = 1 ))

                let! r = ss.TryCheckTargetDeviceUnloaded cc1 npNodes.[0]
                Assert.False r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CheckTargetDeviceUnloaded_001() =
        let portNo, dname = ServerStatus_Test1.Init "CheckTargetDeviceUnloaded_001"
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

                let tdNodes1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes1.Length = 0 ))

                let tdid = GlbFunc.newTargetDeviceID()
                let _ = ss.AddTargetDeviceNode tdid "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigFileNode

                let tdNodes2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes2.Length = 1 ))
                Assert.True(( ( tdNodes2.[0] :> IConfigFileNode ).Modified = ModifiedStatus.Modified ) )

                do! ss.CheckTargetDeviceUnloaded cc1 tdNodes2.[0]
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CheckTargetDeviceUnloaded_002() =
        let portNo, dname = ServerStatus_Test1.Init "CheckTargetDeviceUnloaded_002"
        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                Assert.True(( ( tdNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                try
                    do! ss.CheckTargetDeviceUnloaded cc1 tdNodes.[0]
                    Assert.Fail __LINE__
                with
                | :? EditError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_TARGET_DEVICE_RUNNING" ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetGroupUnloaded_001() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetGroupUnloaded_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))

                let! r = ss.TryCheckTargetGroupUnloaded cc1 tgNode
                Assert.True r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetGroupUnloaded_002() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetGroupUnloaded_002"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let! r = ss.TryCheckTargetGroupUnloaded cc1 tgNodes.[0]
                Assert.True r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetGroupUnloaded_003() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetGroupUnloaded_003"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid []
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let! r = ss.TryCheckTargetGroupUnloaded cc1 tgNodes.[0]
                Assert.True r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetGroupUnloaded_004() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetGroupUnloaded_004"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid [{
                        ID = tgid;
                        Name = "targetgroup000";
                    }]
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let! r = ss.TryCheckTargetGroupUnloaded cc1 tgNodes.[0]
                Assert.False r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.TryCheckTargetGroupUnloaded_005() =
        let portNo, dname = ServerStatus_Test1.Init "TryCheckTargetGroupUnloaded_004"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid [{
                        ID = tgid;
                        Name = "targetgroup000";
                    }]
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                let tNodes = ( tgNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tNodes.Length = 1 ))

                let! r = ss.TryCheckTargetGroupUnloaded cc1 tNodes.[0]
                Assert.False r
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CheckTargetGroupUnloaded_001() =
        let portNo, dname = ServerStatus_Test1.Init "CheckTargetGroupUnloaded_001"
        [|
            fun () -> task {
                let! sl, c, _, _, _ = ServerStatus_Test1.StubLoginAndInit portNo true
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))

                let tgid = GlbFunc.newTargetGroupID()
                let tgNode = ss.AddTargetGroupNode tdNodes.[0] tgid "xxyyzz" true :> IConfigFileNode
                Assert.True(( tgNode.Modified = ModifiedStatus.Modified ))

                do! ss.CheckTargetGroupUnloaded cc1 tgNode
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CheckTargetGroupUnloaded_002() =
        let portNo, dname = ServerStatus_Test1.Init "CheckTargetGroupUnloaded_002"
        [|
            fun () -> task {
                let! sl, c, _, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo true
                do! ServerStatus_Test1.RespTargetDeviceProcs c [ tdid ]
                do! ServerStatus_Test1.RespLoadedTargetGroup c tdid [{
                        ID = tgid;
                        Name = "targetgroup000";
                    }]
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let tdNodes = ss.GetTargetDeviceNodes()
                Assert.True(( tdNodes.Length = 1 ))
                let tgNodes = ( tdNodes.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tgNodes.Length = 1 ))
                Assert.True(( ( tgNodes.[0] :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

                try
                    do! ss.CheckTargetGroupUnloaded cc1 tgNodes.[0]
                    Assert.Fail __LINE__
                with
                | :? EditError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_TARGET_GROUP_LOADED" ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname