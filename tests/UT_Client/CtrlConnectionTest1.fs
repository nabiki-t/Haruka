//=============================================================================
// Haruka Software Storage.
// CtrlConnectionTest1.fs : Test cases for CtrlConnection class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.Threading
open System.IO
open System.Net
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type CtrlConnection_Test1() =


    ///////////////////////////////////////////////////////////////////////////
    // Common definition
    
    static member CreateDefaultCtrlConf ( p : string ) ( adr : string ) ( portNum : int ) =
        let fname = Functions.AppendPathName p Constants.CONTROLLER_CONF_FILE_NAME
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = uint16 portNum;
                Address = adr
                WhiteList = [];
            };
            LogMaintenance = None;
            LogParameters = None;
        }
        HarukaCtrlConf.ReaderWriter.WriteFile fname conf

    static member CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "CtrlConnection_Test_" + caseName
        if Directory.Exists w1 then GlbFunc.DeleteDir w1
        GlbFunc.CreateDir w1 |> ignore
        w1

    static member Init ( caseName : string ) =
        let portNo = GlbFunc.nextTcpPortNo()
        let dname = CtrlConnection_Test1.CreateTestDir caseName
        let k = new HKiller() :> IKiller
        let st = new StringTable( "" )
        let tdid = GlbFunc.newTargetDeviceID()
        ( portNo, dname, k, st, tdid )


    static member StubLogin ( portNo : int ) =
        task {
            let sl = new TcpListener( IPAddress.Parse "::1", portNo )
            sl.Start ()
            let! s = sl.AcceptSocketAsync()
            let c = new NetworkStream( s )

            // Receive Login request
            let! loginRequestStr = Functions.FramingReceiver c
            let loginRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString loginRequestStr
            match loginRequest.Request with
            | HarukaCtrlerCtrlReq.U_Login( x ) ->
                ()
            | _ ->
                Assert.Fail __LINE__

            // send login response
            let sessID = CtrlSessionID.NewID()
            let rb =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_LoginResult( {
                            Result = true;
                            SessionID = sessID;
                    })
                }
            do! Functions.FramingSender c rb

            return ( sl, c, sessID )
        }

    static member SendTargetDeviceCtrlResponse ( c : NetworkStream ) ( tdid : TDID_T ) ( res : TargetDeviceCtrlRes.T_Response ) =
        task {
            let rb1 =
                TargetDeviceCtrlRes.ReaderWriter.ToString {
                    Response = res
                }
            let rb2 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse({
                        TargetDeviceID = tdid;
                        Response = rb1;
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb2
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        task {
            try
                let! cc = CtrlConnection.Connect st "***" 1 false
                Assert.Fail __LINE__
            with
            | :? SocketException ->
                ()
        }
        |> Functions.RunTaskSynchronously

    [<Fact>]
    member _.Constractor_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Constractor_002"
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                try
                    let! cc = CtrlConnection.Connect st "::1" portNo false
                    ()
                with
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname
       
    [<Fact>]
    member _.Constractor_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Constractor_003"
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                try
                    let! cc1 = CtrlConnection.Connect st "::1" portNo false
                    let! cc2 = CtrlConnection.Connect st "::1" portNo false
                    Assert.Fail __LINE__
                with
                | :? RequestError ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Constractor_004() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Constractor_004"
        [|
            fun () -> task {
                let sl = new TcpListener( IPAddress.Parse "::1", portNo )
                sl.Start ()
                let! s = sl.AcceptSocketAsync()
                use c = new NetworkStream( s )
                let rb =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "errorabc" )
                    }
                do! Functions.FramingSender c rb
                GlbFunc.ClosePorts [|c|]
                sl.Stop()
            };
            fun () -> task {
                try
                    let! cc = CtrlConnection.Connect st "::1" portNo false
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message = "errorabc" ))
                    ()
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname
       
    [<Fact>]
    member _.Constractor_005() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Constractor_005"
        [|
            fun () -> task {
                let sl = new TcpListener( IPAddress.Parse "::1", portNo )
                sl.Start ()
                let! s = sl.AcceptSocketAsync()
                use c = new NetworkStream( s )
                let rb =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig( {
                                Config = "";
                                ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb
                GlbFunc.ClosePorts [|c|]
                sl.Stop()
            };
            fun () -> task {
                try
                    let! _ = CtrlConnection.Connect st "::1" portNo false
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Constractor_006() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Constractor_006"
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! cc2 = CtrlConnection.Connect st "::1" portNo true

                try
                    do! cc1.Logout()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()

                do! cc2.Logout()

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SendRequest_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "SendRequest_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive logout request
                let! logoutRequestStr = Functions.FramingReceiver c
                let logoutRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString logoutRequestStr
                match logoutRequest.Request with
                | HarukaCtrlerCtrlReq.U_Logout( x ) ->
                    ()
                | _ ->
                    Assert.Fail __LINE__

                // send error data
                do! Functions.FramingSender c "aaaaaaaaaaaaaaaaaaaaaa"
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.Logout()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Logout_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Logout_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive logout request
                let! logoutRequestStr = Functions.FramingReceiver c
                let logoutRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString logoutRequestStr
                match logoutRequest.Request with
                | HarukaCtrlerCtrlReq.U_Logout( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send logout response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( {
                                Result = false;
                                SessionID = CtrlSessionID();
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.Logout()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_FAILED_LOGOUT" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Logout_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Logout_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive logout request
                let! logoutRequestStr = Functions.FramingReceiver c
                let logoutRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString logoutRequestStr
                match logoutRequest.Request with
                | HarukaCtrlerCtrlReq.U_Logout( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send logout response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( {
                                Result = true;
                                SessionID = CtrlSessionID.NewID();
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.Logout()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Logout_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Logout_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive logout request
                let! logoutRequestStr = Functions.FramingReceiver c
                let logoutRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString logoutRequestStr
                match logoutRequest.Request with
                | HarukaCtrlerCtrlReq.U_Logout( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send unexpected error
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "aaaaaaaaa" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.Logout()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Logout_004() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Logout_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive logout request
                let! logoutRequestStr = Functions.FramingReceiver c
                let logoutRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString logoutRequestStr
                match logoutRequest.Request with
                | HarukaCtrlerCtrlReq.U_Logout( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send unexpected error
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = true;
                            ErrorMessage = "aaaaaaaa";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.Logout()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Logout_005() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "Logout_005"
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.Logout()
                with
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.NoOperation_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "NoOperation_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive NoOperation request
                let! noOperationRequestStr = Functions.FramingReceiver c
                let noOperationRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString noOperationRequestStr
                match noOperationRequest.Request with
                | HarukaCtrlerCtrlReq.U_NoOperation( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send NoOperation response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( {
                                Result = false;
                                SessionID = sessID;
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.NoOperation()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_FAILED_NO_OPERATION" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.NoOperation_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "NoOperation_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive NoOperation request
                let! noOperationRequestStr = Functions.FramingReceiver c
                let noOperationRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString noOperationRequestStr
                match noOperationRequest.Request with
                | HarukaCtrlerCtrlReq.U_NoOperation( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send NoOperation response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( {
                                Result = true;
                                SessionID = CtrlSessionID.NewID();
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.NoOperation()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.NoOperation_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "NoOperation_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive NoOperation request
                let! noOperationRequestStr = Functions.FramingReceiver c
                let noOperationRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString noOperationRequestStr
                match noOperationRequest.Request with
                | HarukaCtrlerCtrlReq.U_NoOperation( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send NoOperation response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "aaaaaaaaa" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.NoOperation()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.NoOperation_004() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "NoOperation_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive NoOperation request
                let! noOperationRequestStr = Functions.FramingReceiver c 
                let noOperationRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString noOperationRequestStr
                match noOperationRequest.Request with
                | HarukaCtrlerCtrlReq.U_NoOperation( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send NoOperation response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = true;
                            ErrorMessage = "aaaaaaaa";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.NoOperation()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.NoOperation_005() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "NoOperation_005"
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    do! cc1.NoOperation()
                with
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetControllerConfig_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetControllerConfig_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetControllerConfig request
                let! getControllerConfigRequestStr = Functions.FramingReceiver c 
                let getControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getControllerConfigRequestStr
                match getControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetControllerConfig( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig({
                            Config = "";
                            ErrorMessage = "aaaaaaaa";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    let! r = cc1.GetControllerConfig()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaa" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetControllerConfig_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetControllerConfig_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetControllerConfig request
                let! getControllerConfigRequestStr = Functions.FramingReceiver c 
                let getControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getControllerConfigRequestStr
                match getControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetControllerConfig( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig({
                            Config = "aaaaaaaaaaaaaa";
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
                    let! r = cc1.GetControllerConfig()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetControllerConfig_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetControllerConfig_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetControllerConfig request
                let! getControllerConfigRequestStr = Functions.FramingReceiver c 
                let getControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getControllerConfigRequestStr
                match getControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetControllerConfig( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "bbbbbbbbbbbbbbb" );
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    let! r = cc1.GetControllerConfig()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "bbbbbbbbbbbbbbb" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetControllerConfig_004() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetControllerConfig_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetControllerConfig request
                let! getControllerConfigRequestStr = Functions.FramingReceiver c 
                let getControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getControllerConfigRequestStr
                match getControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetControllerConfig( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = true;
                            ErrorMessage = "aaaaaaaa";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                try
                    let! r = cc1.GetControllerConfig()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetControllerConfig_005() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetControllerConfig_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetControllerConfig request
                let! getControllerConfigRequestStr = Functions.FramingReceiver c 
                let getControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getControllerConfigRequestStr
                match getControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetControllerConfig( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetControllerConfig response
                let ctrlConf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = Some {
                        PortNum = 100us;
                        Address = "abcdefg";
                        WhiteList = [];
                    };
                    LogMaintenance = Some {
                        OutputDest = HarukaCtrlConf.U_ToFile( {
                            TotalLimit = 9999u;
                            MaxFileCount = 88u;
                            ForceSync = false;
                        })
                    };
                    LogParameters = Some {
                        SoftLimit = 2222u;
                        HardLimit = 1111u;
                        LogLevel = LogLevel.LOGLEVEL_INFO;
                    };
                }
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig({
                            Config = HarukaCtrlConf.ReaderWriter.ToString ctrlConf
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
                    let! r = cc1.GetControllerConfig()
                    Assert.True(( r.RemoteCtrl.IsSome ))
                    Assert.True(( r.RemoteCtrl.Value.PortNum = 100us ))
                    Assert.True(( r.RemoteCtrl.Value.Address = "abcdefg" ))
                    Assert.True(( r.LogMaintenance.IsSome ))
                    match r.LogMaintenance.Value.OutputDest with
                    | HarukaCtrlConf.U_ToFile( x ) ->
                        Assert.True(( x.TotalLimit = 9999u ))
                        Assert.True(( x.MaxFileCount = 88u ))
                        Assert.True(( x.ForceSync = false ))
                    | _ ->
                        Assert.Fail __LINE__
                    Assert.True(( r.LogParameters.IsSome ))
                    Assert.True(( r.LogParameters.Value.SoftLimit = 2222u ))
                    Assert.True(( r.LogParameters.Value.HardLimit = 1111u ))
                    Assert.True(( r.LogParameters.Value.LogLevel = LogLevel.LOGLEVEL_INFO ))
                with
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetControllerConfig_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "SetControllerConfig_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive SetControllerConfig request
                let! setControllerConfigRequestStr = Functions.FramingReceiver c 
                let setControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString setControllerConfigRequestStr
                match setControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_SetControllerConfig( x ) ->
                    Assert.True(( sessID = x.SessionID ))
                    let wc = HarukaCtrlConf.ReaderWriter.LoadString x.Config
                    Assert.True(( wc.RemoteCtrl.IsNone ))
                    Assert.True(( wc.LogMaintenance.IsNone ))
                    Assert.True(( wc.LogParameters.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                // send SetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = false;
                            ErrorMessage = "aaaabbbb";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = None;
                    LogMaintenance = None;
                    LogParameters = None;
                }
                try
                    let! r = cc1.SetControllerConfig conf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaabbbb" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetControllerConfig_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "SetControllerConfig_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive SetControllerConfig request
                let! setControllerConfigRequestStr = Functions.FramingReceiver c 
                let setControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString setControllerConfigRequestStr
                match setControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_SetControllerConfig( x ) ->
                    Assert.True(( sessID = x.SessionID ))
                    let wc = HarukaCtrlConf.ReaderWriter.LoadString x.Config
                    Assert.True(( wc.RemoteCtrl.IsSome ))
                    Assert.True(( wc.RemoteCtrl.Value.PortNum = 123us ))
                    Assert.True(( wc.RemoteCtrl.Value.Address = "address001" ))
                    Assert.True(( wc.LogMaintenance.IsNone ))
                    Assert.True(( wc.LogParameters.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                // send SetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "bbbbbbbbbbbbbbb" );
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = Some {
                        PortNum = 123us;
                        Address = "address001";
                        WhiteList = [];
                    };
                    LogMaintenance = None;
                    LogParameters = None;
                }
                try
                    let! r = cc1.SetControllerConfig conf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "bbbbbbbbbbbbbbb" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetControllerConfig_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "SetControllerConfig_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive SetControllerConfig request
                let! setControllerConfigRequestStr = Functions.FramingReceiver c 
                let setControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString setControllerConfigRequestStr
                match setControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_SetControllerConfig( x ) ->
                    ()
                | _ ->
                    Assert.Fail __LINE__

                // send SetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult({
                            Result = true;
                            SessionID = sessID;
                        });
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = None;
                    LogMaintenance = None;
                    LogParameters = None;
                }
                try
                    let! r = cc1.SetControllerConfig conf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.SetControllerConfig_004() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "SetControllerConfig_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive SetControllerConfig request
                let! setControllerConfigRequestStr = Functions.FramingReceiver c 
                let setControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString setControllerConfigRequestStr
                match setControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_SetControllerConfig( x ) ->
                    ()
                | _ ->
                    Assert.Fail __LINE__

                // send SetControllerConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
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

                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = None;
                    LogMaintenance = None;
                    LogParameters = None;
                }
                do! cc1.SetControllerConfig conf

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceDir_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceDir_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceDir request
                let! getTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceDirRequestStr
                match getTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
                            ErrorMessage = "aaabbb";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceDir()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaabbb" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceDir_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceDir_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceDir request
                let! getTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceDirRequestStr
                match getTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "aaabbbcccddd" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceDir()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaabbbcccddd" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceDir_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceDir_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceDir request
                let! getTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceDirRequestStr
                match getTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
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
                    let! r = cc1.GetTargetDeviceDir()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceDir_004() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceDir_004"
        let tdids = [
            for i = 0 to 10 do
                yield GlbFunc.newTargetDeviceID()
        ]

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceDir request
                let! getTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceDirRequestStr
                match getTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = tdids;
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
                    let! r = cc1.GetTargetDeviceDir()
                    Assert.True(( r = tdids ))
                with
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceDir_005() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceDir_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceDir request
                let! getTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceDirRequestStr
                match getTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
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
                    let! r = cc1.GetTargetDeviceDir()
                    Assert.True(( r = [] ))
                with
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceDir_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceDir_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceDir request
                let! createTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceDirRequestStr
                match createTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult({
                            Result = false;
                            TargetDeviceID = tdid;
                            ErrorMessage = "aaaaaaaaa";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceDir_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceDir_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceDir request
                let! createTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceDirRequestStr
                match createTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult({
                            Result = true;
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
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
                    do! cc1.CreateTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceDir_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceDir_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceDir request
                let! createTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceDirRequestStr
                match createTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "abcdefghijkl" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "abcdefghijkl" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceDir_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceDir_004"
        use barr = new Barrier( 2 )
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceDir request
                let! createTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceDirRequestStr
                match createTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = false;
                            ErrorMessage = "aaaabbbb";
                        })
                    }
                do! Functions.FramingSender c rb2
                barr.SignalAndWait()
                GlbFunc.ClosePorts [|c|]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                barr.SignalAndWait()
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceDir_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceDir_005"
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.CreateTargetDeviceDir tdid

                let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
                Assert.True(( Directory.Exists wdname ))

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceDir_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetDeviceDir_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetDeviceDir request
                let! deleteTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let deleteTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetDeviceDirRequestStr
                match deleteTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult({
                            Result = false;
                            TargetDeviceID = tdid;
                            ErrorMessage = "aaaaaaaaa1";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa1" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceDir_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetDeviceDir_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetDeviceDir request
                let! deleteTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let deleteTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetDeviceDirRequestStr
                match deleteTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult({
                            Result = true;
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
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
                    do! cc1.DeleteTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceDir_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetDeviceDir_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetDeviceDir request
                let! deleteTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let deleteTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetDeviceDirRequestStr
                match deleteTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "xxyyzz" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "xxyyzz" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceDir_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetDeviceDir_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetDeviceDir request
                let! deleteTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let deleteTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetDeviceDirRequestStr
                match deleteTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetDeviceDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = false;
                            ErrorMessage = "aaaabbbb";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetDeviceDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetDeviceDir_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetDeviceDir_005"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.DeleteTargetDeviceDir tdid
                Assert.False(( Directory.Exists wdname ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceConfig_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceConfig_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceConfig request
                let! getTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceConfigRequestStr
                match getTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                            TargetDeviceID = tdid;
                            Config = "";
                            ErrorMessage = "aaaaaaaaa2";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa2" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceConfig_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceConfig_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceConfig request
                let! getTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceConfigRequestStr
                match getTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Config = "";
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
                    let! r = cc1.GetTargetDeviceConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceConfig_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceConfig_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceConfig request
                let! getTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceConfigRequestStr
                match getTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                            TargetDeviceID = tdid;
                            Config = "aaaaaaaaaaaaaaaaaaaaaaaaaaa";
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
                    let! r = cc1.GetTargetDeviceConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceConfig_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceConfig_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceConfig request
                let! getTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceConfigRequestStr
                match getTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "qqqqqqqqqqqqqqqq" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "qqqqqqqqqqqqqqqq" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceConfig_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceConfig_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceConfig request
                let! getTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceConfigRequestStr
                match getTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
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
                    let! r = cc1.GetTargetDeviceConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceConfig_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceConfig_006"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let confName = Functions.AppendPathName wdname Constants.TARGET_DEVICE_CONF_FILE_NAME
        Directory.CreateDirectory wdname |> ignore

        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "abc";
                PortNumber = 123us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 1;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaa";
        }
        TargetDeviceConf.ReaderWriter.WriteFile confName tdConf

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetTargetDeviceConfig tdid
                Assert.True(( r.NetworkPortal.Length = 1 ))
                Assert.True(( r.NetworkPortal.Item(0).TargetAddress = "abc" ))
                Assert.True(( r.NegotiableParameters.IsNone ))
                Assert.True(( r.LogParameters.IsNone ))
                Assert.True(( r.DeviceName = "aaa" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceConfig_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceConfig_001"
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "abc";
                PortNumber = 123us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 1;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaa";
        }
        let tdConfStr = TargetDeviceConf.ReaderWriter.ToString tdConf

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceConfig request
                let! createTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceConfigRequestStr
                match createTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.Config = tdConfStr ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult({
                            Result = false;
                            TargetDeviceID = tdid;
                            ErrorMessage = "aaaaaaaaa3";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetDeviceConfig tdid tdConf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa3" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceConfig_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceConfig_002"
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "abc";
                PortNumber = 123us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 1;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaa";
        }
        let tdConfStr = TargetDeviceConf.ReaderWriter.ToString tdConf

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceConfig request
                let! createTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceConfigRequestStr
                match createTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.Config = tdConfStr ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult({
                            Result = true;
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
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
                    do! cc1.CreateTargetDeviceConfig tdid tdConf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceConfig_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceConfig_003"
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "abc";
                PortNumber = 123us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 1;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaa";
        }
        let tdConfStr = TargetDeviceConf.ReaderWriter.ToString tdConf

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceConfig request
                let! createTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceConfigRequestStr
                match createTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.Config = tdConfStr ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "wwwwwwwwwwwwwww" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetDeviceConfig tdid tdConf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "wwwwwwwwwwwwwww" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceConfig_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceConfig_004"
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "abc";
                PortNumber = 123us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 1;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaa";
        }
        let tdConfStr = TargetDeviceConf.ReaderWriter.ToString tdConf

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetDeviceConfig request
                let! createTargetDeviceConfigRequestStr = Functions.FramingReceiver c 
                let createTargetDeviceConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetDeviceConfigRequestStr
                match createTargetDeviceConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetDeviceConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.Config = tdConfStr ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetDeviceConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
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
                    do! cc1.CreateTargetDeviceConfig tdid tdConf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname



    [<Fact>]
    member _.CreateTargetDeviceConfig_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceConfig_005"
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 999us;
                TargetAddress = "****";
                PortNumber = 0us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 0;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "*****";
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
                    do! cc1.CreateTargetDeviceConfig tdid tdConf
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetDeviceConfig_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetDeviceConfig_006"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let confName = Functions.AppendPathName wdname Constants.TARGET_DEVICE_CONF_FILE_NAME
        Directory.CreateDirectory wdname |> ignore
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "abc";
                PortNumber = 123us;
                DisableNagle = true;
                ReceiveBufferSize = 0;
                SendBufferSize = 1;
                WhiteList = [];
            }]
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "aaa";
        }
        let tdConfStr = TargetDeviceConf.ReaderWriter.ToString tdConf

        Assert.False(( File.Exists confName ))

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.CreateTargetDeviceConfig tdid tdConf
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        Assert.True(( File.Exists confName ))
        let resultStr = File.ReadAllText( confName )
        Assert.True(( resultStr = tdConfStr ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupID_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupID_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupID request
                let! getTargetGroupIDRequestStr = Functions.FramingReceiver c 
                let getTargetGroupIDRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupIDRequestStr
                match getTargetGroupIDRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupID( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupID response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID({
                            TargetDeviceID = tdid;
                            TargetGroupID = [];
                            ErrorMessage = "aaaaaaaaa4";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetGroupID tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa4" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupID_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupID_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupID request
                let! getTargetGroupIDRequestStr = Functions.FramingReceiver c 
                let getTargetGroupIDRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupIDRequestStr
                match getTargetGroupIDRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupID( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupID response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroupID = [];
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
                    let! r = cc1.GetTargetGroupID tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupID_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupID_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupID request
                let! getTargetGroupIDRequestStr = Functions.FramingReceiver c 
                let getTargetGroupIDRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupIDRequestStr
                match getTargetGroupIDRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupID( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupID response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "eeeeeeeeeeeee" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetGroupID tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "eeeeeeeeeeeee" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupID_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupID_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupID request
                let! getTargetGroupIDRequestStr = Functions.FramingReceiver c 
                let getTargetGroupIDRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupIDRequestStr
                match getTargetGroupIDRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupID( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupID response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
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
                    let! r = cc1.GetTargetGroupID tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupID_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupID_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupID request
                let! getTargetGroupIDRequestStr = Functions.FramingReceiver c 
                let getTargetGroupIDRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupIDRequestStr
                match getTargetGroupIDRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupID( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupID response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID({
                            TargetDeviceID = tdid;
                            TargetGroupID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetTargetGroupID tdid
                Assert.True( r.Length = 0 )

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupID_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupID_006"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        let wtgids =
            [
                for i = 0 to 10 do
                    let tgid = GlbFunc.newTargetGroupID()
                    let tgidPath = Functions.AppendPathName wdname ( tgid_me.toString tgid )
                    File.WriteAllText( tgidPath, "" )
                    yield tgid
            ]
            |> List.sort

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetTargetGroupID tdid
                Assert.True( r.Length = wtgids.Length )
                Assert.True(( ( List.sort r ) = wtgids ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_001"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupConfig request
                let! getTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupConfigRequestStr
                match getTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig({
                            TargetDeviceID = tdid;
                            TargetGroupID = tgid;
                            Config = "";
                            ErrorMessage = "aaaaaaaaa4";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa4" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_002"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupConfig request
                let! getTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupConfigRequestStr
                match getTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroupID = tgid;
                            Config = "";
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
                    let! r = cc1.GetTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_003"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupConfig request
                let! getTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupConfigRequestStr
                match getTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig({
                            TargetDeviceID = tdid;
                            TargetGroupID = GlbFunc.newTargetGroupID();
                            Config = "";
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
                    let! r = cc1.GetTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_004"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupConfig request
                let! getTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupConfigRequestStr
                match getTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig({
                            TargetDeviceID = tdid;
                            TargetGroupID = tgid;
                            Config = "aaaaaaaaaaaaaaaaaaaaaa";
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
                    let! r = cc1.GetTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_005"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupConfig request
                let! getTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupConfigRequestStr
                match getTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "rrrrrrrrrrrrrrr" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "rrrrrrrrrrrrrrr" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_006"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetGroupConfig request
                let! getTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetGroupConfigRequestStr
                match getTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult({
                            Result = true;
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
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
                    let! r = cc1.GetTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetGroupConfig_007() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetGroupConfig_007"
        let tgid = GlbFunc.newTargetGroupID()
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let tgconfName = Functions.AppendPathName wdname ( tgid_me.toString tgid )
        Directory.CreateDirectory wdname |> ignore

        TargetGroupConf.ReaderWriter.WriteFile tgconfName {
            TargetGroupID = tgid;
            TargetGroupName = "aaaaaaaaaa";
            EnabledAtStart = false;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "abcd";
                    TargetAlias = "eee";
                    LUN = [ lun_me.fromPrim 1UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                }
            ];
        }

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetTargetGroupConfig tdid tgid
                Assert.True(( r.TargetGroupID = tgid ))
                Assert.True(( r.TargetGroupName = "aaaaaaaaaa" ))
                Assert.True(( r.EnabledAtStart = false ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                            TargetDeviceID = tdid;
                            TargetGroup = [];
                            ErrorMessage = "aaaaaaaaa4";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa4" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroup = [];
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
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroup = [];
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
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroup = [
                                {
                                    TargetGroupID = GlbFunc.newTargetGroupID();
                                    Config = "aaaaaaaaaaaaa";
                                }
                            ];
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
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_005"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = GlbFunc.newTargetGroupID();
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroup = [
                                {
                                    TargetGroupID = wtgid1;
                                    Config = TargetGroupConf.ReaderWriter.ToString wtgconf1;
                                };
                            ];
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
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_006"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "aasdfgh" )
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aasdfgh" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_007() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_007"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigRequestStr
                match getAllTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
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
                    let! r = cc1.GetAllTargetGroupConfig tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_008() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_008"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetAllTargetGroupConfig tdid
                Assert.True(( r.Length = 0 ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetAllTargetGroupConfig_009() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetAllTargetGroupConfig_008"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }
        let tgconfName = Functions.AppendPathName wdname ( tgid_me.toString wtgid1 )
        TargetGroupConf.ReaderWriter.WriteFile tgconfName wtgconf1

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetAllTargetGroupConfig tdid
                Assert.True(( r.Length = 1 ))
                Assert.True(( r.[0].TargetGroupID = wtgid1 ))
                Assert.True(( r.[0].TargetGroupName = "aaaaaaaaaa" ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

