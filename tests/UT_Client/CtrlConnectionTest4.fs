namespace Haruka.Test.UT.Client

open System
open System.IO
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

type CtrlConnection_Test4() =

    static member ReceiveMediaControlRequest ( c : NetworkStream ) ( tdid : TDID_T ) ( lun : LUN_T ) ( mid : MEDIAIDX_T ) ( sessID : CtrlSessionID ) =
        task {
            let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
            let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
            return 
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_MediaControlRequest( x ) ->
                    Assert.True(( x.LUN = lun ))
                    Assert.True(( x.ID = mid ))
                    let reqStr = MediaCtrlReq.ReaderWriter.LoadString x.Request
                    reqStr.Request
                | _ ->
                    Assert.Fail __LINE__
                    MediaCtrlReq.U_Debug( MediaCtrlReq.U_GetAllTraps() ) // dummy
        }

    static member SendMediaControlResponse ( c : NetworkStream ) ( tdid : TDID_T ) ( lun : LUN_T ) ( mid : MEDIAIDX_T ) ( res : MediaCtrlRes.T_Response ) =
        task {
            let resStr = MediaCtrlRes.ReaderWriter.ToString {
                Response = res;
            }
            do! TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( {
                    LUN = lun;
                    ID = mid;
                    ErrorMessage = "";
                    Response = resStr;
                } )
                |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
        }

    [<Fact>]
    member _.DebugMedia_GetAllTraps_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! reqData = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID
                match reqData with
                | MediaCtrlReq.U_Debug( y ) ->
                    match y with
                    | MediaCtrlReq.U_GetAllTraps( z ) ->
                        ()
                    | _ ->
                        Assert.Fail __LINE__

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AllTraps({
                            Trap = [];
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                Assert.True(( r.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response
                do! Functions.FramingSender c "aaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt555" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt555" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AllTraps({
                            Trap = [];
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 99UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AllTraps({
                            Trap = [];
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 22222u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_006"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                let resStr =
                    MediaCtrlRes.ReaderWriter.ToString {
                        Response = MediaCtrlRes.U_Debug(
                            MediaCtrlRes.U_AllTraps({
                                Trap = [];
                            })
                        );
                    }
                do! TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( {
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 2u;
                        ErrorMessage = "aaaaaaaaaaaaaaa";
                        Response = resStr;
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaaaaaaaa" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_007() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_007"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                let resStr =
                    MediaCtrlRes.ReaderWriter.ToString {
                        Response = MediaCtrlRes.U_Debug(
                            MediaCtrlRes.U_AllTraps({
                                Trap = [];
                            })
                        );
                    }
                do! TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( {
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 2u;
                        ErrorMessage = "";
                        Response = "rrrrrrrrrrrrrrrrrrrrrrrrrrrr";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_008() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_008"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Unexpected( "fffffffffffffffff" )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "fffffffffffffffff" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_009() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_009"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_CounterValue( 0 )
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetAllTraps_010() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetAllTraps_010"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AllTraps({
                            Trap = [
                                for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
                                    yield {
                                        Event = MediaCtrlRes.U_TestUnitReady();
                                        Action = MediaCtrlRes.U_Count( { Index = i; Value = i * 2; } )
                                    }
                            ];
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.DebugMedia_GetAllTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                Assert.True(( r.Length = Constants.DEBUG_MEDIA_MAX_TRAP_COUNT ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_AddTrap_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_AddTrap_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! reqData = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 
                match reqData with
                | MediaCtrlReq.U_Debug( x ) ->
                    match x with
                    | MediaCtrlReq.U_AddTrap( y ) ->
                        Assert.True(( y.Event.IsU_TestUnitReady ))
                        Assert.True(( y.Action.IsU_Count ))
                    | _ ->
                        Assert.Fail __LINE__

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AddTrapResult({
                            Result = true;
                            ErrorMessage = "";
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let at_event = MediaCtrlReq.U_TestUnitReady()
                let at_action = MediaCtrlReq.U_Count( 1 )
                do! cc1.DebugMedia_AddTrap tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) at_event at_action
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_AddTrap_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_AddTrap_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Unexpected( "eeeeeeeeeeeeeeee" )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let at_event = MediaCtrlReq.U_TestUnitReady()
                let at_action = MediaCtrlReq.U_Count( 1 )
                try
                    do! cc1.DebugMedia_AddTrap tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) at_event at_action
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "eeeeeeeeeeeeeeee" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_AddTrap_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_AddTrap_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_CounterValue( 0 )
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let at_event = MediaCtrlReq.U_TestUnitReady()
                let at_action = MediaCtrlReq.U_Count( 1 )
                try
                    do! cc1.DebugMedia_AddTrap tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) at_event at_action
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_AddTrap_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_AddTrap_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AddTrapResult({
                            Result = false;
                            ErrorMessage = "555555555555555555";
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let at_event = MediaCtrlReq.U_TestUnitReady()
                let at_action = MediaCtrlReq.U_Count( 1 )
                try
                    do! cc1.DebugMedia_AddTrap tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) at_event at_action
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "555555555555555555" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_AddTrap_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_AddTrap_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "888888888888888888888" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let at_event = MediaCtrlReq.U_TestUnitReady()
                let at_action = MediaCtrlReq.U_Count( 1 )
                try
                    do! cc1.DebugMedia_AddTrap tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) at_event at_action
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "888888888888888888888" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_ClearTraps_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_ClearTraps_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! reqData = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 
                match reqData with
                | MediaCtrlReq.U_Debug( x ) ->
                    Assert.True(( x.IsU_ClearTraps ))

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_ClearTrapsResult({
                            Result = true;
                            ErrorMessage = "";
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.DebugMedia_ClearTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_ClearTraps_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_ClearTraps_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_ClearTrapsResult({
                            Result = false;
                            ErrorMessage = "4444444444444444444444444";
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DebugMedia_ClearTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "4444444444444444444444444" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_ClearTraps_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_ClearTraps_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_CounterValue( 0 )
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DebugMedia_ClearTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                with
                | :? RequestError as x ->
                    ()
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_ClearTraps_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_ClearTraps_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Unexpected( "aaawwwrrrttt" )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DebugMedia_ClearTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaawwwrrrttt" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_ClearTraps_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_ClearTraps_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "gggthththththt" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DebugMedia_ClearTraps tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "gggthththththt" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetCounterValue_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetCounterValue_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! reqData = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 
                match reqData with
                | MediaCtrlReq.U_Debug( x ) ->
                    match x with
                    | MediaCtrlReq.U_GetCounterValue( y ) ->
                        Assert.True(( y = 99 ))
                    | _ ->
                        Assert.Fail __LINE__

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_CounterValue( 88 )
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! v = cc1.DebugMedia_GetCounterValue tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 99
                Assert.True(( v = 88 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetCounterValue_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetCounterValue_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Debug(
                        MediaCtrlRes.U_AddTrapResult({
                            Result = false;
                            ErrorMessage = "4444444444444444444444444";
                        })
                    )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetCounterValue tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 99
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetCounterValue_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetCounterValue_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response for MediaCtrlReq
                do! MediaCtrlRes.U_Unexpected( "aawqqeerrr" )
                    |> CtrlConnection_Test4.SendMediaControlResponse c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetCounterValue tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 99
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aawqqeerrr" ))
                
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DebugMedia_GetCounterValue_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DebugMedia_GetCounterValue_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive MediaCtrlReq request
                let! _ = CtrlConnection_Test4.ReceiveMediaControlRequest c tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) sessID 

                // send response
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "gggthththththt" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.DebugMedia_GetCounterValue tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u ) 99
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "gggthththththt" ))
                
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname
