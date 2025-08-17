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

type CtrlConnection_Test3() =

    static member ReceiveTargetDeviceCtrlRequest ( c : NetworkStream ) ( tdid : TDID_T ) ( sessID : CtrlSessionID ) =
        task {
            let! targetDeviceCtrlRequestStr = Functions.FramingReceiver c 
            let targetDeviceCtrlRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString targetDeviceCtrlRequestStr
            match targetDeviceCtrlRequest.Request with
            | HarukaCtrlerCtrlReq.U_TargetDeviceCtrlRequest( x ) ->
                Assert.True(( x.TargetDeviceID = tdid ))
                Assert.True(( x.SessionID = sessID ))
                return x.Request
            | _ ->
                Assert.Fail __LINE__
                return ""
        }

    [<Fact>]
    member _.SendTargetDeviceRequest_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SendTargetDeviceRequest_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetActiveTargetGroups( _ ) ->
                    ()
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse({
                            TargetDeviceID = tdid;
                            Response = "";
                            ErrorMessage = "qqqqqqqqqqq";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "qqqqqqqqqqq" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SendTargetDeviceRequest_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SendTargetDeviceRequest_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! _ = Functions.FramingReceiver c 

                // send response for TargetDeviceCtrlRequest
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Response = "";
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
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
    member _.SendTargetDeviceRequest_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SendTargetDeviceRequest_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! _ = Functions.FramingReceiver c 

                // send response for TargetDeviceCtrlRequest
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "qqwweerrffgg" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "qqwweerrffgg" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SendTargetDeviceRequest_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SendTargetDeviceRequest_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! _ = Functions.FramingReceiver c 

                // send response for TargetDeviceCtrlRequest
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult({
                            TargetDeviceID = tdid;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
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
    member _.SendTargetDeviceRequest_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SendTargetDeviceRequest_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! _ = Functions.FramingReceiver c 

                // send response for TargetDeviceCtrlRequest
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse({
                            TargetDeviceID = tdid;
                            Response = "aaaaaaaaaaaaaaaaaaaaaaaa";
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
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
    member _.SendTargetDeviceRequest_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SendTargetDeviceRequest_006"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let wlp : TargetDeviceConf.T_LogParameters = {
                    SoftLimit = Constants.LOGPARAM_MAX_SOFTLIMIT + 100u;
                    HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT + 100u;
                    LogLevel = LogLevel.LOGLEVEL_INFO;
                }
                try
                    do! cc1.SetLogParameters tdid wlp
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
    member _.GetActiveTargetGroups_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetActiveTargetGroups_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetActiveTargetGroups( _ ) ->
                    ()
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "hhhhhhhhhhhhh" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "hhhhhhhhhhhhh" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetActiveTargetGroups_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetActiveTargetGroups_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetActiveTargetGroups( _ ) ->
                    ()
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_DeviceName( "hhhhhhhhhhhhh" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetActiveTargetGroups tdid
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
    member _.GetActiveTargetGroups_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetActiveTargetGroups_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetActiveTargetGroups( _ ) ->
                    ()
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( {
                            ActiveTGInfo = [];
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetActiveTargetGroups tdid
                Assert.True(( r.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetActiveTargetGroups_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetActiveTargetGroups_004"
        let wList : TargetDeviceCtrlRes.T_ActiveTGInfo list = [
            {
                ID = GlbFunc.newTargetGroupID();
                Name = "aaa01";
            }
            {
                ID = GlbFunc.newTargetGroupID();
                Name = "aaa02";
            };
        ]
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetActiveTargetGroups( _ ) -> ()
                | _ ->  Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( {
                            ActiveTGInfo = wList;
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetActiveTargetGroups tdid
                Assert.True(( r = wList ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLoadedTargetGroups_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLoadedTargetGroups_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLoadedTargetGroups( _ ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "aaaaaa" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLoadedTargetGroups tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaa" ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLoadedTargetGroups_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLoadedTargetGroups_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLoadedTargetGroups( _ ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( {
                        ActiveTGInfo = [];
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLoadedTargetGroups tdid
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
    member _.GetLoadedTargetGroups_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLoadedTargetGroups_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLoadedTargetGroups( _ ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LoadedTargetGroups( {
                        LoadedTGInfo = [];
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetLoadedTargetGroups tdid
                Assert.True(( r.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLoadedTargetGroups_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLoadedTargetGroups_004"
        let tgids : TargetDeviceCtrlRes.T_LoadedTGInfo list = [
            for i = 0 to 10 do
                yield {
                    ID = GlbFunc.newTargetGroupID();
                    Name = sprintf "aaa%d" i;
                }
        ]
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLoadedTargetGroups( _ ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LoadedTargetGroups( {
                        LoadedTGInfo = tgids;
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetLoadedTargetGroups tdid
                Assert.True(( r = tgids ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.InactivateTargetGroup_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "InactivateTargetGroup_001"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_InactivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( {
                        ID = tgid;
                        Result = false;
                        ErrorMessage = "aaa";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.InactivateTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "Failed to inactivate target group" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.InactivateTargetGroup_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "InactivateTargetGroup_002"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_InactivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.InactivateTargetGroup tdid tgid
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
    member _.InactivateTargetGroup_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "InactivateTargetGroup_003"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_InactivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "ggggggggggggg" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.InactivateTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ggggggggggggg" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.InactivateTargetGroup_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "InactivateTargetGroup_004"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_InactivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups({
                        ActiveTGInfo = [];
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.InactivateTargetGroup tdid tgid
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
    member _.InactivateTargetGroup_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "InactivateTargetGroup_005"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_InactivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( {
                        ID = tgid;
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.InactivateTargetGroup tdid tgid
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.ActivateTargetGroup_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "ActivateTargetGroup_001"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_ActivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( {
                        ID = tgid;
                        Result = false;
                        ErrorMessage = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.ActivateTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.ActivateTargetGroup_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "ActivateTargetGroup_002"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_ActivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.ActivateTargetGroup tdid tgid
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
    member _.ActivateTargetGroup_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "ActivateTargetGroup_003"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_ActivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "aaAAssSFF" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.ActivateTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaAAssSFF" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.ActivateTargetGroup_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "ActivateTargetGroup_004"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_ActivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.ActivateTargetGroup tdid tgid
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
    member _.ActivateTargetGroup_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "ActivateTargetGroup_005"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_ActivateTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( {
                        ID = tgid;
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.ActivateTargetGroup tdid tgid
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UnloadTargetGroup_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "UnloadTargetGroup_001"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_UnloadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = tgid;
                        Result = false;
                        ErrorMessage = "11111111111111111";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.UnloadTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "11111111111111111" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UnloadTargetGroup_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "UnloadTargetGroup_002"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_UnloadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.UnloadTargetGroup tdid tgid
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
    member _.UnloadTargetGroup_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "UnloadTargetGroup_003"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_UnloadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "222222222222222" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.UnloadTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "222222222222222" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.UnloadTargetGroup_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "UnloadTargetGroup_004"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_UnloadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.UnloadTargetGroup tdid tgid
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
    member _.UnloadTargetGroup_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "UnloadTargetGroup_005"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_UnloadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = tgid;
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.UnloadTargetGroup tdid tgid
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetGroup_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LoadTargetGroup_001"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LoadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( {
                        ID = tgid;
                        Result = false;
                        ErrorMessage = "2222233322222222222222";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LoadTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "2222233322222222222222" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetGroup_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LoadTargetGroup_002"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LoadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LoadTargetGroup tdid tgid
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
    member _.LoadTargetGroup_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LoadTargetGroup_003"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LoadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "444444444444" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LoadTargetGroup tdid tgid
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "444444444444" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetGroup_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LoadTargetGroup_004"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LoadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LoadTargetGroup tdid tgid
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
    member _.LoadTargetGroup_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LoadTargetGroup_005"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LoadTargetGroup( x ) ->
                    Assert.True(( x = tgid ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( {
                        ID = tgid;
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.LoadTargetGroup tdid tgid
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetLogParameters_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SetLogParameters_001"
        let logParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
            HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_SetLogParameters( x ) ->
                    Assert.True(( x.SoftLimit = logParam.SoftLimit ))
                    Assert.True(( x.HardLimit = logParam.HardLimit ))
                    Assert.True(( x.LogLevel = logParam.LogLevel ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( false )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.SetLogParameters tdid logParam
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_FAILED_SET_LOGPARAM" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetLogParameters_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SetLogParameters_002"
        let logParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
            HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_SetLogParameters( x ) ->
                    Assert.True(( x.SoftLimit = logParam.SoftLimit ))
                    Assert.True(( x.HardLimit = logParam.HardLimit ))
                    Assert.True(( x.LogLevel = logParam.LogLevel ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "466644444444" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.SetLogParameters tdid logParam
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "466644444444" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetLogParameters_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SetLogParameters_003"
        let logParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
            HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_SetLogParameters( x ) ->
                    Assert.True(( x.SoftLimit = logParam.SoftLimit ))
                    Assert.True(( x.HardLimit = logParam.HardLimit ))
                    Assert.True(( x.LogLevel = logParam.LogLevel ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.SetLogParameters tdid logParam
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
    member _.SetLogParameters_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SetLogParameters_004"
        let logParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = Constants.LOGPARAM_MAX_SOFTLIMIT + 9999u;
            HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT + 8888u;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.SetLogParameters tdid logParam
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
    member _.SetLogParameters_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "SetLogParameters_005"
        let logParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
            HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_SetLogParameters( x ) ->
                    Assert.True(( x.SoftLimit = logParam.SoftLimit ))
                    Assert.True(( x.HardLimit = logParam.HardLimit ))
                    Assert.True(( x.LogLevel = logParam.LogLevel ))
                | _ -> 
                    Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( true )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.SetLogParameters tdid logParam
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLogParameters_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLogParameters_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLogParameters( x ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LogParameters( {
                        SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                        HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                        LogLevel = LogLevel.LOGLEVEL_INFO;
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetLogParameters tdid
                Assert.True(( r.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT ))
                Assert.True(( r.HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT ))
                Assert.True(( r.LogLevel = LogLevel.LOGLEVEL_INFO ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLogParameters_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLogParameters_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLogParameters( x ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "44444444444444444" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLogParameters tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "44444444444444444" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLogParameters_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLogParameters_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLogParameters( x ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLogParameters tdid
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
    member _.GetDeviceName_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetDeviceName_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetDeviceName( x ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_DeviceName( "aaawwwwwww" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetDeviceName tdid
                Assert.True(( r = "aaawwwwwww" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetDeviceName_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetDeviceName_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetDeviceName( x ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "66666666666666666" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetDeviceName tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "66666666666666666" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetDeviceName_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetDeviceName_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetDeviceName( x ) -> ()
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetDeviceName tdid
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
    member _.GetSession_InTargetDevice_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetDevice_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let sessList : TargetDeviceCtrlRes.T_Session list = [
                    {
                        TargetGroupID = tgid_me.fromPrim( 99u );
                        TargetNodeID = tnodeidx_me.fromPrim 1u;
                        TSIH = tsih_me.fromPrim 1us;
                        ITNexus = {
                            InitiatorName = "initiator001";
                            ISID = isid_me.fromElem 0uy 1uy 2us 3uy 4us;
                            TargetName = "target001";
                            TPGT = tpgt_me.zero;
                        };
                        SessionParameters = {
                            MaxConnections = Constants.NEGOPARAM_MaxConnections;
                            InitiatorAlias = "aaa";
                            InitialR2T = false;
                            ImmediateData = false;
                            MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
                            FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
                            DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                            DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                            MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
                            DataPDUInOrder = true;
                            DataSequenceInOrder = true;
                            ErrorRecoveryLevel = 0uy;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SessionList( { Session = sessList } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! sesList = cc1.GetSession_InTargetDevice tdid
                Assert.True(( sesList.Length = 1 ))
                Assert.True(( sesList.[0].TargetGroupID = tgid_me.fromPrim( 99u ) ))
                Assert.True(( sesList.[0].TargetNodeID = tnodeidx_me.fromPrim 1u ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTargetDevice_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetDevice_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SessionList( { Session = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                c.Socket.Disconnect false
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! sesList = cc1.GetSession_InTargetDevice tdid
                Assert.True(( sesList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTargetDevice_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetDevice_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "aassdd" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetSession_InTargetDevice tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aassdd" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTargetDevice_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetDevice_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetSession_InTargetDevice tdid
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
    member _.GetSession_InTargetGroup_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetGroup_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 3u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let sessList : TargetDeviceCtrlRes.T_Session list = [
                    {
                        TargetGroupID = tgid_me.fromPrim( 4u );        // Retrieved target group ID will not be checked.
                        TargetNodeID = tnodeidx_me.fromPrim 1u;
                        TSIH = tsih_me.fromPrim 1us;
                        ITNexus = {
                            InitiatorName = "initiator001";
                            ISID = isid_me.fromElem 0uy 1uy 2us 3uy 4us;
                            TargetName = "target001";
                            TPGT = tpgt_me.zero;
                        };
                        SessionParameters = {
                            MaxConnections = Constants.NEGOPARAM_MaxConnections;
                            InitiatorAlias = "aaa";
                            InitialR2T = false;
                            ImmediateData = false;
                            MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
                            FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
                            DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                            DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                            MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
                            DataPDUInOrder = true;
                            DataSequenceInOrder = true;
                            ErrorRecoveryLevel = 0uy;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SessionList( { Session = sessList } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! sesList = cc1.GetSession_InTargetGroup tdid ( tgid_me.fromPrim( 3u ) )
                Assert.True(( sesList.Length = 1 ))
                Assert.True(( sesList.[0].TargetGroupID = tgid_me.fromPrim( 4u ) ))    // not checked
                Assert.True(( sesList.[0].TargetNodeID = tnodeidx_me.fromPrim 1u ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTargetGroup_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetGroup_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 3u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SessionList( { Session = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! sesList = cc1.GetSession_InTargetGroup tdid ( tgid_me.fromPrim( 3u ) )
                Assert.True(( sesList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTargetGroup_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetGroup_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 3u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "aassdd001" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetSession_InTargetGroup tdid ( tgid_me.fromPrim( 3u ) )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aassdd001" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTargetGroup_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTargetGroup_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 3u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetSession_InTargetGroup tdid ( tgid_me.fromPrim( 3u ) )
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
    member _.GetSession_InTarget_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTarget_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTarget( tid ) ->
                        Assert.True(( tid = tnodeidx_me.fromPrim 4u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let sessList : TargetDeviceCtrlRes.T_Session list = [
                    {
                        TargetGroupID = tgid_me.fromPrim( 4u );
                        TargetNodeID = tnodeidx_me.fromPrim 5u; // Retrieved target node ID will not be checked.
                        TSIH = tsih_me.fromPrim 1us;
                        ITNexus = {
                            InitiatorName = "initiator001";
                            ISID = isid_me.fromElem 0uy 1uy 2us 3uy 4us;
                            TargetName = "target001";
                            TPGT = tpgt_me.zero;
                        };
                        SessionParameters = {
                            MaxConnections = Constants.NEGOPARAM_MaxConnections;
                            InitiatorAlias = "aaa";
                            InitialR2T = false;
                            ImmediateData = false;
                            MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
                            FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
                            DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                            DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                            MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
                            DataPDUInOrder = true;
                            DataSequenceInOrder = true;
                            ErrorRecoveryLevel = 0uy;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SessionList( { Session = sessList } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! sesList = cc1.GetSession_InTarget tdid ( tnodeidx_me.fromPrim 4u )
                Assert.True(( sesList.Length = 1 ))
                Assert.True(( sesList.[0].TargetGroupID = tgid_me.fromPrim( 4u ) ))
                Assert.True(( sesList.[0].TargetNodeID = tnodeidx_me.fromPrim 5u ))    // not checked
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTarget_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTarget_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTarget( tid ) ->
                        Assert.True(( tid = tnodeidx_me.fromPrim 4u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_SessionList( { Session = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! sesList = cc1.GetSession_InTarget tdid ( tnodeidx_me.fromPrim 4u )
                Assert.True(( sesList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTarget_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTarget_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTarget( tid ) ->
                        Assert.True(( tid = tnodeidx_me.fromPrim 4u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "aassdd002" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetSession_InTarget tdid ( tnodeidx_me.fromPrim 4u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aassdd002" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetSession_InTarget_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetSession_InTarget_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetSession( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_SessInTarget( tid ) ->
                        Assert.True(( tid = tnodeidx_me.fromPrim 4u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetSession_InTarget tdid ( tnodeidx_me.fromPrim 4u )
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
    member _.DestructSession_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DestructSession_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_DestructSession( x ) ->
                    Assert.True(( x = tsih_me.fromPrim 4us ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_DestructSessionResult({
                        TSIH = tsih_me.fromPrim 4us;
                        Result = true;
                        ErrorMessage = "";
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.DestructSession tdid ( tsih_me.fromPrim 4us )
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DestructSession_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DestructSession_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_DestructSession( x ) ->
                    Assert.True(( x = tsih_me.fromPrim 4us ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_DestructSessionResult({
                        TSIH = tsih_me.fromPrim 4us;
                        Result = false;
                        ErrorMessage = "aaa";
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DestructSession tdid ( tsih_me.fromPrim 4us )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaa" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DestructSession_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DestructSession_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_DestructSession( x ) ->
                    Assert.True(( x = tsih_me.fromPrim 4us ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_DestructSessionResult({
                        TSIH = tsih_me.fromPrim 5us;
                        Result = true;
                        ErrorMessage = "";
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DestructSession tdid ( tsih_me.fromPrim 4us )
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
    member _.DestructSession_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DestructSession_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_DestructSession( x ) ->
                    Assert.True(( x = tsih_me.fromPrim 4us ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "qwsde001" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DestructSession tdid ( tsih_me.fromPrim 4us )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "qwsde001" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DestructSession_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DestructSession_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_DestructSession( x ) ->
                    Assert.True(( x = tsih_me.fromPrim 4us ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DestructSession tdid ( tsih_me.fromPrim 4us )
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
    member _.GetConnection_InTargetDevice_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetDevice_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let conist : TargetDeviceCtrlRes.T_Connection list = [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ConnectionID = cid_me.fromPrim 2us;
                        ConnectionCount = concnt_me.fromPrim 3;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "CHAP";
                            HeaderDigest = "CRC32C";
                            DataDigest = "CRC32C";
                            MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                            MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = conist } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InTargetDevice tdid 
                Assert.True(( conList.Length = 1 ))
                Assert.True(( conList.[0].TSIH = tsih_me.fromPrim 1us ))
                Assert.True(( conList.[0].ConnectionID = cid_me.fromPrim 2us ))
                Assert.True(( conList.[0].ConnectionCount = concnt_me.fromPrim 3 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTargetDevice_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetDevice_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InTargetDevice tdid 
                Assert.True(( conList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTargetDevice_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetDevice_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt111" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InTargetDevice tdid 
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt111" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTargetDevice_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetDevice_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetDevice( _ ) ->
                        ()
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InTargetDevice tdid 
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
    member _.GetConnection_InNetworkPortal_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InNetworkPortal_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInNetworkPortal( npidx ) ->
                        Assert.True(( npidx = netportidx_me.fromPrim 5u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let conist : TargetDeviceCtrlRes.T_Connection list = [
                    {
                        TSIH = tsih_me.fromPrim 4us;
                        ConnectionID = cid_me.fromPrim 5us;
                        ConnectionCount = concnt_me.fromPrim 6;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "CHAP";
                            HeaderDigest = "CRC32C";
                            DataDigest = "CRC32C";
                            MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                            MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = conist } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InNetworkPortal tdid ( netportidx_me.fromPrim 5u )
                Assert.True(( conList.Length = 1 ))
                Assert.True(( conList.[0].TSIH = tsih_me.fromPrim 4us ))
                Assert.True(( conList.[0].ConnectionID = cid_me.fromPrim 5us ))
                Assert.True(( conList.[0].ConnectionCount = concnt_me.fromPrim 6 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InNetworkPortal_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InNetworkPortal_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInNetworkPortal( npidx ) ->
                        Assert.True(( npidx = netportidx_me.fromPrim 5u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InNetworkPortal tdid ( netportidx_me.fromPrim 5u )
                Assert.True(( conList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InNetworkPortal_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InNetworkPortal_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInNetworkPortal( npidx ) ->
                        Assert.True(( npidx = netportidx_me.fromPrim 5u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt222" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InNetworkPortal tdid ( netportidx_me.fromPrim 5u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt222" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InNetworkPortal_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InNetworkPortal_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInNetworkPortal( npidx ) ->
                        Assert.True(( npidx = netportidx_me.fromPrim 5u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InNetworkPortal tdid ( netportidx_me.fromPrim 5u )
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
    member _.GetConnection_InTargetGroup_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetGroup_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 6u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let conist : TargetDeviceCtrlRes.T_Connection list = [
                    {
                        TSIH = tsih_me.fromPrim 7us;
                        ConnectionID = cid_me.fromPrim 8us;
                        ConnectionCount = concnt_me.fromPrim 9;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "CHAP";
                            HeaderDigest = "CRC32C";
                            DataDigest = "CRC32C";
                            MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                            MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = conist } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InTargetGroup tdid ( tgid_me.fromPrim( 6u ) )
                Assert.True(( conList.Length = 1 ))
                Assert.True(( conList.[0].TSIH = tsih_me.fromPrim 7us ))
                Assert.True(( conList.[0].ConnectionID = cid_me.fromPrim 8us ))
                Assert.True(( conList.[0].ConnectionCount = concnt_me.fromPrim 9 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTargetGroup_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetGroup_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 7u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InTargetGroup tdid ( tgid_me.fromPrim( 7u ) )
                Assert.True(( conList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTargetGroup_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetGroup_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 7u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt333" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InTargetGroup tdid ( tgid_me.fromPrim( 7u ) )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt333" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTargetGroup_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTargetGroup_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTargetGroup( tgid ) ->
                        Assert.True(( tgid = tgid_me.fromPrim( 7u ) ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InTargetGroup tdid ( tgid_me.fromPrim( 7u ) )
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
    member _.GetConnection_InTarget_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTarget_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTarget( tnode ) ->
                        Assert.True(( tnode = tnodeidx_me.fromPrim 7u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let conist : TargetDeviceCtrlRes.T_Connection list = [
                    {
                        TSIH = tsih_me.fromPrim 10us;
                        ConnectionID = cid_me.fromPrim 11us;
                        ConnectionCount = concnt_me.fromPrim 12;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "CHAP";
                            HeaderDigest = "CRC32C";
                            DataDigest = "CRC32C";
                            MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                            MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = conist } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InTarget tdid ( tnodeidx_me.fromPrim 7u )
                Assert.True(( conList.Length = 1 ))
                Assert.True(( conList.[0].TSIH = tsih_me.fromPrim 10us ))
                Assert.True(( conList.[0].ConnectionID = cid_me.fromPrim 11us ))
                Assert.True(( conList.[0].ConnectionCount = concnt_me.fromPrim 12 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTarget_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTarget_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTarget( tnode ) ->
                        Assert.True(( tnode = tnodeidx_me.fromPrim 7u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InTarget tdid ( tnodeidx_me.fromPrim 7u )
                Assert.True(( conList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTarget_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTarget_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTarget( tnode ) ->
                        Assert.True(( tnode = tnodeidx_me.fromPrim 7u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt223" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InTarget tdid ( tnodeidx_me.fromPrim 7u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt223" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InTarget_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InTarget_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInTarget( tnode ) ->
                        Assert.True(( tnode = tnodeidx_me.fromPrim 7u ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InTarget tdid ( tnodeidx_me.fromPrim 7u )
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
    member _.GetConnection_InSession_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InSession_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInSession( tsih ) ->
                        Assert.True(( tsih = tsih_me.fromPrim 11us ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                let conist : TargetDeviceCtrlRes.T_Connection list = [
                    {
                        TSIH = tsih_me.fromPrim 13us;
                        ConnectionID = cid_me.fromPrim 14us;
                        ConnectionCount = concnt_me.fromPrim 15;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "CHAP";
                            HeaderDigest = "CRC32C";
                            DataDigest = "CRC32C";
                            MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                            MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                        };
                        EstablishTime = DateTime();
                    }
                ]

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = conist } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InSession tdid ( tsih_me.fromPrim 11us )
                Assert.True(( conList.Length = 1 ))
                Assert.True(( conList.[0].TSIH = tsih_me.fromPrim 13us ))
                Assert.True(( conList.[0].ConnectionID = cid_me.fromPrim 14us ))
                Assert.True(( conList.[0].ConnectionCount = concnt_me.fromPrim 15 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InSession_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InSession_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInSession( tsih ) ->
                        Assert.True(( tsih = tsih_me.fromPrim 11us ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_ConnectionList( { Connection = [] } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! conList = cc1.GetConnection_InSession tdid ( tsih_me.fromPrim 11us )
                Assert.True(( conList.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InSession_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InSession_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInSession( tsih ) ->
                        Assert.True(( tsih = tsih_me.fromPrim 11us ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt224" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InSession tdid ( tsih_me.fromPrim 11us )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt224" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetConnection_InSession_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetConnection_InSession_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetConnection( x ) ->
                    match x with
                    | TargetDeviceCtrlReq.U_ConInSession( tsih ) ->
                        Assert.True(( tsih = tsih_me.fromPrim 11us ))
                    | _ -> Assert.Fail __LINE__
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetConnection_InSession tdid ( tsih_me.fromPrim 11us )
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
    member _.GetLUStatus_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUStatus_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLUStatus( x ) ->
                    Assert.True(( x = lun_me.fromPrim 6UL ))
                | _ -> Assert.Fail __LINE__

                let stat : TargetDeviceCtrlRes.T_LUStatus =
                    {
                        LUN = lun_me.fromPrim 6UL;
                        ErrorMessage = "";
                        LUStatus_Success = Some( {
                            ReadBytesCount = [ { Time = DateTime(); Value = 0L; Count = 1L; } ];
                            WrittenBytesCount = [];
                            ReadTickCount = [ { Time = DateTime(); Value = 0L; Count = 1L; } ];
                            WriteTickCount = [];
                            ACAStatus = None;
                        })
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LUStatus( stat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! lustat = cc1.GetLUStatus tdid ( lun_me.fromPrim 6UL )
                Assert.True(( lustat.ReadBytesCount.Length = 1 ))
                Assert.True(( lustat.WrittenBytesCount.Length = 0 ))
                Assert.True(( lustat.ReadTickCount.Length = 1 ))
                Assert.True(( lustat.WriteTickCount.Length = 0 ))
                Assert.True(( lustat.ACAStatus.IsNone ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUStatus_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUStatus_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLUStatus( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                let stat : TargetDeviceCtrlRes.T_LUStatus =
                    {
                        LUN = lun_me.fromPrim 6UL;
                        ErrorMessage = "";
                        LUStatus_Success = Some( {
                            ReadBytesCount = [];
                            WrittenBytesCount = [];
                            ReadTickCount = [];
                            WriteTickCount = [];
                            ACAStatus = None;
                        })
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LUStatus( stat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetLUStatus tdid ( lun_me.fromPrim 7UL )
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
    member _.GetLUStatus_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUStatus_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLUStatus( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                let stat : TargetDeviceCtrlRes.T_LUStatus =
                    {
                        LUN = lun_me.fromPrim 7UL;
                        ErrorMessage = "aaaaa";
                        LUStatus_Success = None
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LUStatus( stat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetLUStatus tdid ( lun_me.fromPrim 7UL )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaa" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUStatus_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUStatus_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLUStatus( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt444" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetLUStatus tdid ( lun_me.fromPrim 7UL )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt444" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUStatus_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUStatus_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetLUStatus( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetLUStatus tdid ( lun_me.fromPrim 7UL )
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
    member _.LUReset_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LUReset_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LUResetResult({
                        LUN = lun_me.fromPrim 7UL;
                        Result = true;
                        ErrorMessage = "";
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.LUReset tdid ( lun_me.fromPrim 7UL )
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LUReset_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LUReset_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LUResetResult({
                        LUN = lun_me.fromPrim 8UL;
                        Result = true;
                        ErrorMessage = "";
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LUReset tdid ( lun_me.fromPrim 7UL )
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
    member _.LUReset_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LUReset_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_LUResetResult({
                        LUN = lun_me.fromPrim 7UL;
                        Result = false;
                        ErrorMessage = "ttyyuu333";
                    })
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LUReset tdid ( lun_me.fromPrim 7UL )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ttyyuu333" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LUReset_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LUReset_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt555" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LUReset tdid ( lun_me.fromPrim 7UL )
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
    member _.LUReset_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "LUReset_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = lun_me.fromPrim 7UL ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.LUReset tdid ( lun_me.fromPrim 7UL )
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
    member _.GetMediaStatus_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetMediaStatus_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 2u ))
                | _ -> Assert.Fail __LINE__

                let mediastat : TargetDeviceCtrlRes.T_MediaStatus =
                    {
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 2u;
                        ErrorMessage = "";
                        MediaStatus_Success = Some({
                            ReadBytesCount = [{ Time = DateTime(); Value = 0L; Count = 1L; }];
                            WrittenBytesCount = [{ Time = DateTime(); Value = 2L; Count = 3L; }];
                            ReadTickCount = [{ Time = DateTime(); Value = 4L; Count = 5L; }];
                            WriteTickCount = [{ Time = DateTime(); Value = 6L; Count = 7L; }];
                        })
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_MediaStatus( mediastat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! mdstat = cc1.GetMediaStatus tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                Assert.True(( mdstat.ReadBytesCount.Length = 1 ))
                Assert.True(( mdstat.ReadBytesCount.[0].Value = 0L ))
                Assert.True(( mdstat.WrittenBytesCount.Length = 1 ))
                Assert.True(( mdstat.WrittenBytesCount.[0].Value = 2L ))
                Assert.True(( mdstat.ReadTickCount.Length = 1 ))
                Assert.True(( mdstat.ReadTickCount.[0].Value = 4L ))
                Assert.True(( mdstat.WriteTickCount.Length = 1 ))
                Assert.True(( mdstat.WriteTickCount.[0].Value = 6L ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetMediaStatus_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetMediaStatus_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 2u ))
                | _ -> Assert.Fail __LINE__

                let mediastat : TargetDeviceCtrlRes.T_MediaStatus =
                    {
                        LUN = lun_me.fromPrim 99UL;
                        ID = mediaidx_me.fromPrim 2u;
                        ErrorMessage = "";
                        MediaStatus_Success = Some({
                            ReadBytesCount = [{ Time = DateTime(); Value = 0L; Count = 1L; }];
                            WrittenBytesCount = [{ Time = DateTime(); Value = 2L; Count = 3L; }];
                            ReadTickCount = [{ Time = DateTime(); Value = 4L; Count = 5L; }];
                            WriteTickCount = [{ Time = DateTime(); Value = 6L; Count = 7L; }];
                        })
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_MediaStatus( mediastat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetMediaStatus tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
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
    member _.GetMediaStatus_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetMediaStatus_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 2u ))
                | _ -> Assert.Fail __LINE__

                let mediastat : TargetDeviceCtrlRes.T_MediaStatus =
                    {
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 9999u;
                        ErrorMessage = "";
                        MediaStatus_Success = Some({
                            ReadBytesCount = [{ Time = DateTime(); Value = 0L; Count = 1L; }];
                            WrittenBytesCount = [{ Time = DateTime(); Value = 2L; Count = 3L; }];
                            ReadTickCount = [{ Time = DateTime(); Value = 4L; Count = 5L; }];
                            WriteTickCount = [{ Time = DateTime(); Value = 6L; Count = 7L; }];
                        })
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_MediaStatus( mediastat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetMediaStatus tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
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
    member _.GetMediaStatus_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetMediaStatus_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 2u ))
                | _ -> Assert.Fail __LINE__

                let mediastat : TargetDeviceCtrlRes.T_MediaStatus =
                    {
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 2u;
                        ErrorMessage = "qqwwertyuiui";
                        MediaStatus_Success = None;
                    }

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_MediaStatus( mediastat )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetMediaStatus tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "qqwwertyuiui" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetMediaStatus_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetMediaStatus_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 2u ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnexpectedError( "wweerrtt666" )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetMediaStatus tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wweerrtt666" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetMediaStatus_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetMediaStatus_006"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive TargetDeviceCtrlRequest request
                let! reqStr = CtrlConnection_Test3.ReceiveTargetDeviceCtrlRequest c tdid sessID
                let tdReq = TargetDeviceCtrlReq.ReaderWriter.LoadString reqStr
                match tdReq.Request with
                | TargetDeviceCtrlReq.U_GetMediaStatus( x ) ->
                    Assert.True(( x.LUN = lun_me.fromPrim 1UL ))
                    Assert.True(( x.ID = mediaidx_me.fromPrim 2u ))
                | _ -> Assert.Fail __LINE__

                // send response for TargetDeviceCtrlRequest
                do! TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( {
                        ID = GlbFunc.newTargetGroupID();
                        Result = true;
                        ErrorMessage = "";
                    } )
                    |> CtrlConnection_Test1.SendTargetDeviceCtrlResponse c tdid
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetMediaStatus tdid ( lun_me.fromPrim 1UL ) ( mediaidx_me.fromPrim 2u )
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