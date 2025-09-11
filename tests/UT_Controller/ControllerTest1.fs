//=============================================================================
// Haruka Software Storage.
// ControllerTest1.fs : Test cases for Controller class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Controller

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Text
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Controller
open Haruka.IODataTypes

//=============================================================================
// Class implementation

type Controller_Test1 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    static let portNum = GlbFunc.nextTcpPortNo()
    static let dname = Controller_Test1.CreateTestDir "Controller_Test1"
    static let k = new HKiller() :> IKiller
    static let tc =
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
        let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        c.LoadInitialTargetDeviceProcs()
        c.WaitRequest()
        c
    static let pc = PrivateCaller( tc )

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "Controller_Test_" + caseName
        if Directory.Exists w1 then GlbFunc.DeleteDir w1
        GlbFunc.CreateDir w1 |> ignore
        w1

    static member CreateDefaultTDConf ( p : string ) ( tdid : TDID_T ) =
        let dname = Functions.AppendPathName p ( tdid_me.toString tdid )
        Directory.CreateDirectory dname |> ignore

        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [ {
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetAddress = "localhost";
                PortNumber = uint16( GlbFunc.nextTcpPortNo() );
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                WhiteList = [];
            } ];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "abc";
        }
        let tdConfName = Functions.AppendPathName dname Constants.TARGET_DEVICE_CONF_FILE_NAME
        TargetDeviceConf.ReaderWriter.WriteFile tdConfName tdConf

        let tgid = GlbFunc.newTargetGroupID()
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "";
            EnabledAtStart = true;
            Target = [ {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = "target001";
                TargetAlias = "";
                LUN = [ lun_me.fromPrim 1UL; ]
                Auth = TargetGroupConf.T_Auth.U_None();
            }];
            LogicalUnit = [{
                LUN = lun_me.fromPrim 1UL;
                LUName = "";
                WorkPath = "";
                LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            }]
        }
        let tgConfName = Functions.AppendPathName dname ( tgid_me.toString tgid )
        TargetGroupConf.ReaderWriter.WriteFile tgConfName tgConf

        let luWorkDirName = Functions.AppendPathName dname ( lun_me.WorkDirName( lun_me.fromPrim 1UL ) )
        Directory.CreateDirectory luWorkDirName |> ignore

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

    static member FirstLogin ( con : NetworkStream ) : Task<( CtrlSessionID )> =
        task {
            // send first login request
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Login( true )
                }
            do! Functions.FramingSender con reqStr

            // receive first login response
            let! resStr = Functions.FramingReceiver con
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LoginResult( x ) ->
                Assert.True( x.Result )
                return x.SessionID
            | _ ->
                Assert.Fail __LINE__
                return CtrlSessionID()

        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Login_001() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID with
            | LoggedIn( id, _ ) ->
                Assert.True(( sessID = id ))
            | _ ->
                Assert.Fail __LINE__

            // send second login request
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Login( false )
                }
            do! Functions.FramingSender c1 reqStr

            // receive second login response
            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LoginResult( x ) ->
                Assert.False( x.Result )    // login failed
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.Login_002() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID with
            | LoggedIn( id, _ ) ->
                Assert.True(( sessID = id ))
                // update login status
                pc.SetField( "m_MgrCliSessID", MgrCliSessionStatus.LoggedIn( id, DateTime() ) )
            | _ ->
                Assert.Fail __LINE__

            // send second login request
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Login( false )
                }
            do! Functions.FramingSender c1 reqStr

            // receive second login response
            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LoginResult( x ) ->
                // login success
                Assert.True( x.Result )
                Assert.True(( x.SessionID <> sessID ))

                // Check current status
                let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
                match m_MgrCliSessID with
                | LoggedIn( id, _ ) ->
                    Assert.True(( x.SessionID = id ))
                | _ ->
                    Assert.Fail __LINE__

            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.Login_003() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID with
            | LoggedIn( id, _ ) ->
                Assert.True(( id = sessID ))
            | _ ->
                Assert.Fail __LINE__

            // send second login request
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Login( true )
                }
            do! Functions.FramingSender c1 reqStr

            // receive second login response
            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LoginResult( x ) ->
                Assert.True( x.Result )    // login success
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.Logout_001() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let dummySessID = CtrlSessionID.NewID()

            pc.SetField( "m_MgrCliSessID", MgrCliSessionStatus.UnUsed )

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Logout( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.SessionID = dummySessID ))
            | _ ->
                Assert.Fail __LINE__

            let m_MgrCliSessID2 = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            Assert.True(( m_MgrCliSessID2 = MgrCliSessionStatus.UnUsed ))

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.Logout_002() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            // update login status
            let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID with
            | LoggedIn( id, _ ) ->
                pc.SetField( "m_MgrCliSessID", MgrCliSessionStatus.LoggedIn( id, DateTime() ) )
            | _ ->
                Assert.Fail __LINE__

            // send logout request with dummy session id
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Logout( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.SessionID = dummySessID ))
            | _ ->
                Assert.Fail __LINE__

            let m_MgrCliSessID2 = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            Assert.True(( m_MgrCliSessID2 = MgrCliSessionStatus.UnUsed ))

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.Logout_003() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            // send logout request with dummy session id
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Logout( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.SessionID = dummySessID ))
            | _ ->
                Assert.Fail __LINE__

            let m_MgrCliSessID2 = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID2 with
            | LoggedIn( id, _ ) ->
                Assert.True(( id = sessID ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.Logout_004() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            // send logout request
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_Logout( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LogoutResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.SessionID = sessID ))
            | _ ->
                Assert.Fail __LINE__

            let m_MgrCliSessID2 = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            Assert.True(( m_MgrCliSessID2 = MgrCliSessionStatus.UnUsed ))

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.NoOperation_001() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_NoOperation( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.SessionID = dummySessID ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.NoOperation_002() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_NoOperation( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.SessionID = sessID ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.NoOperation_003() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_NoOperation( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.SessionID = dummySessID ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.NoOperation_004() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_NoOperation( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            // update login status
            let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID with
            | LoggedIn( id, _ ) ->
                pc.SetField( "m_MgrCliSessID", MgrCliSessionStatus.LoggedIn( id, DateTime() ) )
            | _ ->
                Assert.Fail __LINE__

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.SessionID = sessID ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.NoOperation_005() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_NoOperation( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            // update login status
            let m_MgrCliSessID = pc.GetField( "m_MgrCliSessID" ) :?> MgrCliSessionStatus
            match m_MgrCliSessID with
            | LoggedIn( id, _ ) ->
                pc.SetField( "m_MgrCliSessID", MgrCliSessionStatus.LoggedIn( id, DateTime() ) )
            | _ ->
                Assert.Fail __LINE__

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_NoOperationResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.SessionID = dummySessID ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetControllerConfig_001() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetControllerConfig( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig( x ) ->
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetControllerConfig_002() =
        task {
            let ctrlConfData =
                let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
                File.ReadAllText fname

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetControllerConfig( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig( x ) ->
                Assert.True(( x.Config = ctrlConfData ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetControllerConfig_003() =
        task {
            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let fname_bk = fname + "_bk"
            File.Move( fname, fname_bk )
            try
                use con1 = new TcpClient( "::1", portNum )
                use c1 = con1.GetStream()
                let! sessID = Controller_Test1.FirstLogin c1

                let reqStr =
                    HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                        Request = HarukaCtrlerCtrlReq.T_Request.U_GetControllerConfig( sessID )
                    }
                do! Functions.FramingSender c1 reqStr

                let! resStr = Functions.FramingReceiver c1
                let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

                match res.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig( x ) ->
                    Assert.True(( x.Config = "" ))
                    Assert.True(( x.ErrorMessage.Length > 0 ))
                | _ ->
                    Assert.Fail __LINE__

                c1.Close()
                con1.Close()

            finally
                File.Move( fname_bk, fname )
        }

    [<Fact>]
    member _.GetControllerConfig_004() =
        task {
            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let fname_bk = fname + "_bk"
            File.Move( fname, fname_bk )
            Directory.CreateDirectory fname |> ignore
            try
                use con1 = new TcpClient( "::1", portNum )
                use c1 = con1.GetStream()
                let! sessID = Controller_Test1.FirstLogin c1

                let reqStr =
                    HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                        Request = HarukaCtrlerCtrlReq.T_Request.U_GetControllerConfig( sessID )
                    }
                do! Functions.FramingSender c1 reqStr

                let! resStr = Functions.FramingReceiver c1
                let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

                match res.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig( x ) ->
                    Assert.True(( x.Config = "" ))
                    Assert.True(( x.ErrorMessage.Length > 0 ))
                | _ ->
                    Assert.Fail __LINE__

                c1.Close()
                con1.Close()
            finally
                Directory.Delete fname
                File.Move( fname_bk, fname )
        }

    [<Fact>]
    member _.SetControllerConfig_001() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_SetControllerConfig({
                        SessionID = dummySessID;
                        Config = "";
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.SetControllerConfig_002() =
        task {
            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let fname_bk = fname + "_bk"
            File.Copy( fname, fname_bk )

            try
                use con1 = new TcpClient( "::1", portNum )
                use c1 = con1.GetStream()
                let! sessID = Controller_Test1.FirstLogin c1

                let reqStr =
                    HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                        Request = HarukaCtrlerCtrlReq.T_Request.U_SetControllerConfig({
                            SessionID = sessID;
                            Config = "AAAAAAAAAAAAAAAAAAAAAAAA";
                        })
                    }
                do! Functions.FramingSender c1 reqStr

                let! resStr = Functions.FramingReceiver c1
                let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

                match res.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult( x ) ->
                    Assert.True(( x.Result ))
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                c1.Close()
                con1.Close()

                let wdata = File.ReadAllText fname
                Assert.True(( wdata = "AAAAAAAAAAAAAAAAAAAAAAAA" ))

            finally
                File.Delete fname
                File.Move( fname_bk, fname )
        }

    [<Fact>]
    member _.SetControllerConfig_003() =
        task {
            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let fname_bk = fname + "_bk"
            File.Move( fname, fname_bk )

            try
                use con1 = new TcpClient( "::1", portNum )
                use c1 = con1.GetStream()
                let! sessID = Controller_Test1.FirstLogin c1

                let reqStr =
                    HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                        Request = HarukaCtrlerCtrlReq.T_Request.U_SetControllerConfig({
                            SessionID = sessID;
                            Config = "BBBBBBBBBBBBBBBBBBBBBBBB";
                        })
                    }
                do! Functions.FramingSender c1 reqStr

                let! resStr = Functions.FramingReceiver c1
                let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

                match res.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult( x ) ->
                    Assert.True(( x.Result ))
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                c1.Close()
                con1.Close()

                let wdata = File.ReadAllText fname
                Assert.True(( wdata = "BBBBBBBBBBBBBBBBBBBBBBBB" ))
            finally
                File.Delete fname
                File.Move( fname_bk, fname )
        }

    [<Fact>]
    member _.SetControllerConfig_004() =
        task {
            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let fname_bk = fname + "_bk"
            File.Move( fname, fname_bk )
            Directory.CreateDirectory fname |> ignore

            try
                use con1 = new TcpClient( "::1", portNum )
                use c1 = con1.GetStream()
                let! sessID = Controller_Test1.FirstLogin c1

                let reqStr =
                    HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                        Request = HarukaCtrlerCtrlReq.T_Request.U_SetControllerConfig({
                            SessionID = sessID;
                            Config = "";
                        })
                    }
                do! Functions.FramingSender c1 reqStr

                let! resStr = Functions.FramingReceiver c1
                let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

                match res.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult( x ) ->
                    Assert.False(( x.Result ))
                    Assert.True(( x.ErrorMessage.Length > 0 ))
                | _ ->
                    Assert.Fail __LINE__

                c1.Close()
                con1.Close()
            finally
                Directory.Delete fname
                File.Move( fname_bk, fname )
        }

    [<Fact>]
    member _.GetTargetDeviceDir_001() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceDir( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs( x ) ->
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
                Assert.True(( x.TargetDeviceID.Length = 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetDeviceDir_002() =
        task {
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceDir( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs( x ) ->
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID.Length = 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetDeviceDir_003() =
        task {
            let vdirname = [|
                for i = 0 to ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) do
                    sprintf "%s%08d" Constants.TARGET_DEVICE_DIR_PREFIX i;
            |]
            vdirname
            |> Array.iter ( fun itr ->
                let tddname = Functions.AppendPathName dname itr
                Directory.CreateDirectory tddname |> ignore
            )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceDir( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs( x ) ->
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
                let l = x.TargetDeviceID |> List.sort
                for i = 0 to ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) do
                    Assert.True(( l.[i] = tdid_me.fromString vdirname.[i] ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            vdirname
            |> Array.iter ( fun itr ->
                let tddname = Functions.AppendPathName dname itr
                GlbFunc.DeleteDir tddname
            )
        }

    [<Fact>]
    member _.GetTargetDeviceDir_004() =
        task {
            let vdirname = [|
                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT + 10 do
                    sprintf "%s%08d" Constants.TARGET_DEVICE_DIR_PREFIX i;
            |]
            vdirname
            |> Array.iter ( fun itr ->
                let tddname = Functions.AppendPathName dname itr
                Directory.CreateDirectory tddname |> ignore
            )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceDir( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs( x ) ->
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
                let l = x.TargetDeviceID |> List.sort
                for i = 0 to ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) do
                    Assert.True(( l.[i] = tdid_me.fromString vdirname.[i] ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            vdirname
            |> Array.iter ( fun itr ->
                let tddname = Functions.AppendPathName dname itr
                GlbFunc.DeleteDir tddname
            )
        }

    [<Fact>]
    member _.GetTargetDeviceDir_005() =
        task {
            let wtdid = tdid_me.fromPrim( 11111111u )
            let tddname1 = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname1 |> ignore
            let tddname2 =
                Functions.AppendPathName dname "abcdefg"
            Directory.CreateDirectory tddname2 |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceDir( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs( x ) ->
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID.Length = 1 ))
                Assert.True(( x.TargetDeviceID.[0] = wtdid ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            GlbFunc.DeleteDir tddname1
            GlbFunc.DeleteDir tddname2
        }

    [<Fact>]
    member _.CreateTargetDeviceDir_001() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            if File.Exists tddname then File.Delete tddname

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.False( Directory.Exists tddname )
                Assert.False( File.Exists tddname )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetDeviceDir_002() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            if File.Exists tddname then File.Delete tddname

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True( Directory.Exists tddname )
                Assert.False( File.Exists tddname )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetDeviceDir_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            if File.Exists tddname then File.Delete tddname
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True( Directory.Exists tddname )
                Assert.False( File.Exists tddname )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetDeviceDir_004() =
        task {
            let vdirname = [|
                for i = 0 to ( Constants.MAX_TARGET_DEVICE_COUNT - 2 ) do
                    sprintf "%08d" i;
            |]
            vdirname
            |> Array.iter ( fun itr ->
                let tddname = Functions.AppendPathName dname ( Constants.TARGET_DEVICE_DIR_PREFIX + itr )
                Directory.CreateDirectory tddname |> ignore
            )

            let wtdid2 = tdid_me.fromPrim( 11110255u )
            let wtdid3 = tdid_me.fromPrim( 22220256u )
            let tddname2 = Functions.AppendPathName dname ( tdid_me.toString wtdid2 )
            let tddname3 = Functions.AppendPathName dname ( tdid_me.toString wtdid3 )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr1 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid2;
                    })
                }
            do! Functions.FramingSender c1 reqStr1

            let! resStr1 = Functions.FramingReceiver c1
            let res1 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr1

            match res1.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID = wtdid2 ))
                Assert.True( Directory.Exists tddname2 )
                Assert.False( File.Exists tddname2 )
            | _ ->
                Assert.Fail __LINE__

            let reqStr2 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid3;
                    })
                }
            do! Functions.FramingSender c1 reqStr2

            let! resStr2 = Functions.FramingReceiver c1
            let res2 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr2

            match res2.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.ErrorMessage.StartsWith "Number of target devicees exceeds limit" ))
                Assert.True(( x.TargetDeviceID = wtdid3 ))
                Assert.False( Directory.Exists tddname3 )
                Assert.False( File.Exists tddname3 )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            vdirname
            |> Array.iter ( fun itr ->
                let tddname = Functions.AppendPathName dname ( Constants.TARGET_DEVICE_DIR_PREFIX + itr )
                GlbFunc.DeleteDir tddname
            )
            GlbFunc.DeleteDir tddname2
            GlbFunc.DeleteDir tddname3
        }

    [<Fact>]
    member _.CreateTargetDeviceDir_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            File.WriteAllBytes( tddname, Array.empty )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr1 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr1

            let! resStr1 = Functions.FramingReceiver c1
            let res1 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr1

            match res1.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.False( Directory.Exists tddname )
                Assert.True( File.Exists tddname )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            File.Delete tddname
        }

    [<Fact>]
    member _.DeleteTargetDeviceDir_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetDeviceDir({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
                Assert.True(( x.TargetDeviceID = wtdid ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.DeleteTargetDeviceDir_002() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID = wtdid ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.DeleteTargetDeviceDir_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( x.TargetDeviceID = wtdid ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }


    [<Fact>]
    member _.DeleteTargetDeviceDir_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore
            let fname = Functions.AppendPathName tddname "a.txt"
            File.WriteAllBytes( fname, Array.empty )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.False(( Directory.Exists tddname ))
                Assert.False(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
            GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.DeleteTargetDeviceDir_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore
            let subDirName1 = Functions.AppendPathName tddname "d001"
            Directory.CreateDirectory subDirName1 |> ignore
            let fname1 = Functions.AppendPathName subDirName1 "a.txt"
            File.WriteAllBytes( fname1, Array.empty )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetDeviceDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetDeviceDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.False(( Directory.Exists tddname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }


    [<Fact>]
    member _.GetTargetDeviceConfig_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceConfig({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetDeviceConfig_002() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetDeviceConfig_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetDeviceConfig_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname Constants.TARGET_DEVICE_CONF_FILE_NAME
            Directory.CreateDirectory tddname |> ignore
            Directory.CreateDirectory fname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetDeviceConfig_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let fname = Functions.AppendPathName tddname Constants.TARGET_DEVICE_CONF_FILE_NAME
            let fdata = ( String.replicate 256 "a" ) + "<aaa><bbb>&\"\'&>><<"
            File.WriteAllText( fname, fdata )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Config = fdata ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetDeviceConfig_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceConfig({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                        Config = "abcdefg000"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.CreateTargetDeviceConfig_002() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname Constants.TARGET_DEVICE_CONF_FILE_NAME
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        Config = "abcdefg000"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
                Assert.False(( Directory.Exists tddname ))
                Assert.False(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.CreateTargetDeviceConfig_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname Constants.TARGET_DEVICE_CONF_FILE_NAME
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        Config = "abcdefg"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let wroteData = File.ReadAllText fname
            Assert.True(( wroteData = "abcdefg" ))
            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetDeviceConfig_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname Constants.TARGET_DEVICE_CONF_FILE_NAME
            Directory.CreateDirectory fname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        Config = "abcdefg"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( Directory.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetDeviceConfig_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname Constants.TARGET_DEVICE_CONF_FILE_NAME
            Directory.CreateDirectory tddname |> ignore
            File.WriteAllText( fname , "YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetDeviceConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        Config = "abcdefg000<abc>aa<bcd>b\'b\"b</bcd>&</abc>"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetDeviceConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let wroteData = File.ReadAllText fname
            Assert.True(( wroteData = "abcdefg000<abc>aa<bcd>b\'b\"b</bcd>&</abc>" ))

            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetGroupID_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = [] ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetGroupID_002() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetGroupID_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = [] ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetGroupID_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let vtgid =
                [
                    for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD do
                        yield GlbFunc.newTargetGroupID()
                ]
                |> List.sort

            for itr in vtgid do
                let fname = Functions.AppendPathName tddname ( tgid_me.toString itr )
                File.WriteAllText( fname, "" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = vtgid ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetGroupID_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let vtgid =
                [
                    for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD + 1 do
                        yield GlbFunc.newTargetGroupID()
                ]
                |> List.sort

            for itr in vtgid do
                let fname = Functions.AppendPathName tddname ( tgid_me.toString itr )
                File.WriteAllText( fname, "" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                let w = vtgid |> List.truncate ( int Constants.MAX_TARGET_GROUP_COUNT_IN_TD )
                Assert.True(( x.TargetGroupID = w ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetGroupID_006() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let wtgid1 = tgid_me.fromPrim( 11111111u )
            let wtgid2 = tgid_me.fromPrim( 22222222u )

            let fname1 = Functions.AppendPathName tddname ( tgid_me.toString wtgid1 )
            Directory.CreateDirectory fname1 |> ignore

            let fname2 = Functions.AppendPathName tddname ( tgid_me.toString wtgid2 )
            File.WriteAllText( fname2, "" )

            let fname3 = Functions.AppendPathName tddname "aaaaaaaa.txt"
            File.WriteAllText( fname3, "" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupID({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupID( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = [ wtgid2 ] ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetGroupConfig_001() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupConfig({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetGroupConfig_002() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetTargetGroupConfig_003() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.Config = "" ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetGroupConfig_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let wtgid = tgid_me.fromPrim( 11111111u )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            let fdata = ( String.replicate 256 "a" ) + "<aaa><bbb>&\"\'&>><<"
            File.WriteAllText( fname, fdata )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.Config  = fdata ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetTargetGroupConfig_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let wtgid = tgid_me.fromPrim( 11111111u )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            Directory.CreateDirectory fname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.Config  = "" ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup = [] ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_002() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup = [] ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup = [] ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let wtgid = tgid_me.fromPrim( 11111111u )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            let fdata = ( String.replicate 256 "a" ) + "<aaa><bbb>&\"\'&>><<"
            File.WriteAllText( fname, fdata )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup.[0].TargetGroupID = wtgid ))
                Assert.True(( x.TargetGroup.[0].Config = fdata ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let vtgid =
                [
                    for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD do
                        yield GlbFunc.newTargetGroupID()
                ]
                |> List.sort
            let tgconfdata =
                [
                    for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD do
                        yield sprintf "AAAAA %d BBBBB" i
                ]

            for i = 0 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD - 1 do
                let fname = Functions.AppendPathName tddname ( tgid_me.toString vtgid.[i] )
                File.WriteAllText( fname, tgconfdata.[i] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup.Length = Constants.MAX_TARGET_GROUP_COUNT_IN_TD ))
                for i = 0 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD - 1 do
                    Assert.True(( x.TargetGroup.[i].TargetGroupID = vtgid.[i] ))
                    Assert.True(( x.TargetGroup.[i].Config = tgconfdata.[i] ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_006() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let vtgid =
                [
                    for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD + 1 do
                        yield GlbFunc.newTargetGroupID()
                ]
                |> List.sort
            let tgconfdata =
                [
                    for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD + 1 do
                        yield sprintf "AAAAA %d BBBBB" i
                ]

            for i = 0 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD do
                let fname = Functions.AppendPathName tddname ( tgid_me.toString vtgid.[i] )
                File.WriteAllText( fname, tgconfdata.[i] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup.Length = Constants.MAX_TARGET_GROUP_COUNT_IN_TD ))
                for i = 0 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD - 1 do
                    Assert.True(( x.TargetGroup.[i].TargetGroupID = vtgid.[i] ))
                    Assert.True(( x.TargetGroup.[i].Config = tgconfdata.[i] ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetAllTargetGroupConfig_007() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let wtgid1 = tgid_me.fromPrim( 11111111u )
            let wtgid2 = tgid_me.fromPrim( 22222222u )

            let fname1 = Functions.AppendPathName tddname ( tgid_me.toString wtgid1 )
            Directory.CreateDirectory fname1 |> ignore

            let fname2 = Functions.AppendPathName tddname ( tgid_me.toString wtgid2 )
            File.WriteAllText( fname2, "aaaa" )

            let fname3 = Functions.AppendPathName tddname "aaaaaaaa.txt"
            File.WriteAllText( fname3, "bbbb" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetAllTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroup.Length = 1 ))
                Assert.True(( x.TargetGroup.[0].TargetGroupID = wtgid2 ))
                Assert.True(( x.TargetGroup.[0].Config = "aaaa" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetGroupConfig_001() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                        Config = "abcdefg000"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.CreateTargetGroupConfig_002() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                        Config = "abcdefg000"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
                Assert.False(( Directory.Exists tddname ))
                Assert.False(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.CreateTargetGroupConfig_003() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                        Config = "abcdefg"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let wroteData = File.ReadAllText fname
            Assert.True(( wroteData = "abcdefg" ))

            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetGroupConfig_004() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            Directory.CreateDirectory fname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                        Config = "abcdefg"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( Directory.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetGroupConfig_005() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            Directory.CreateDirectory tddname |> ignore
            File.WriteAllText( fname , "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                        Config = "abcdefg000<abc>aa<bcd>b\'b\"b</bcd>&</abc>"
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let wroteData = File.ReadAllText fname
            Assert.True(( wroteData = "abcdefg000<abc>aa<bcd>b\'b\"b</bcd>&</abc>" ))

            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateTargetGroupConfig_006() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid1 = tgid_me.fromPrim( 11111111u )
            let wtgid2 = tgid_me.fromPrim( 22222222u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let vfname = [|
                for i = 0 to ( Constants.MAX_TARGET_GROUP_COUNT_IN_TD - 2 ) do
                    tgid_me.fromPrim( uint32 i );
            |]
            vfname
            |> Array.iter ( fun itr ->
                let tgfname = Functions.AppendPathName tddname ( tgid_me.toString itr )
                File.WriteAllText( tgfname , "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" )
            )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr1 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid1;
                        Config = 
                            "abcdefg000"
                            |> Encoding.UTF8.GetBytes
                            |> Convert.ToBase64String
                    })
                }
            do! Functions.FramingSender c1 reqStr1

            let! resStr1 = Functions.FramingReceiver c1
            let res1 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr1

            match res1.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid1 ))
                Assert.True(( Directory.Exists tddname ))
                let tgfname = Functions.AppendPathName tddname ( tgid_me.toString wtgid1 )
                Assert.True(( File.Exists tgfname ))
            | _ ->
                Assert.Fail __LINE__

            let reqStr2 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid2;
                        Config = 
                            "abcdefg000"
                            |> Encoding.UTF8.GetBytes
                            |> Convert.ToBase64String
                    })
                }
            do! Functions.FramingSender c1 reqStr2

            let! resStr2 = Functions.FramingReceiver c1
            let res2 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr2

            match res2.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid2 ))
                Assert.True(( x.ErrorMessage.StartsWith "Number of target group exceeds limit" ))
                Assert.True(( Directory.Exists tddname ))
                let tgfname = Functions.AppendPathName tddname ( tgid_me.toString wtgid2 )
                Assert.False(( File.Exists tgfname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteTargetGroupConfig_001() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetGroupConfig({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.DeleteTargetGroupConfig_002() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.DeleteTargetGroupConfig_003() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteTargetGroupConfig_004() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            File.WriteAllText( fname , "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult( x ) ->
                Assert.True( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.False(( File.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteTargetGroupConfig_005() =
        task {
            let wtdid = tdid_me.Zero
            let wtgid = tgid_me.fromPrim( 11111111u )
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let fname = Functions.AppendPathName tddname ( tgid_me.toString wtgid )
            Directory.CreateDirectory fname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteTargetGroupConfig({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        TargetGroupID = wtgid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult( x ) ->
                Assert.False( x.Result )
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.TargetGroupID = wtgid ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( Directory.Exists fname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir fname
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetLUWorkDir_001() =
        task {
            let wtdid = tdid_me.Zero

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Name.Length = 0 ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetLUWorkDir_002() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Name.Length = 0 ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.GetLUWorkDir_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            File.WriteAllBytes( tddname, [| 0uy |] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Name.Length = 0 ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteFile tddname
        }

    [<Fact>]
    member _.GetLUWorkDir_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Name.Length = 0 ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetLUWorkDir_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let v = [|
                ( lun_me.WorkDirName ( 0UL |> lun_me.fromPrim ) )
                ( 0UL |> lun_me.fromPrim |> lun_me.toString )
                ( Constants.LU_WORK_DIR_PREFIX + "aaaaa" )
            |]
            for itr in v do
                File.WriteAllBytes( Functions.AppendPathName tddname itr, [| 0uy |] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Name.Length = 0 ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetLUWorkDir_006() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            let v = [|
                ( lun_me.WorkDirName ( 0UL |> lun_me.fromPrim ) )
                ( 0UL |> lun_me.fromPrim |> lun_me.toString )
                ( Constants.LU_WORK_DIR_PREFIX + "aaaaa" )
            |]
            for itr in v do
                itr
                |> Functions.AppendPathName tddname
                |> Directory.CreateDirectory
                |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.Name.Length = 1 ))
                Assert.True(( x.Name.[0] = lun_me.fromPrim 0UL ))
                Assert.True(( x.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.GetLUWorkDir_007() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            Directory.CreateDirectory tddname |> ignore

            for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 1 do
                let luwdirname1 = ( lun_me.WorkDirName ( i |> uint64 |> lun_me.fromPrim ) )
                let luwdirname2 = Functions.AppendPathName tddname luwdirname1
                Directory.CreateDirectory luwdirname2 |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            for i = 0 to 1 do
                let reqStr1 =
                    HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                        Request = HarukaCtrlerCtrlReq.T_Request.U_GetLUWorkDir({
                            SessionID = sessID;
                            TargetDeviceID = wtdid;
                        })
                    }
                do! Functions.FramingSender c1 reqStr1

                let! resStr1 = Functions.FramingReceiver c1
                let res1 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr1

                match res1.Response with
                | HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs( x ) ->
                    Assert.True(( x.TargetDeviceID = wtdid ))
                    Assert.True(( x.Name.Length = Constants.MAX_LOGICALUNIT_COUNT_IN_TD ))
                    x.Name
                    |> List.sort
                    |> List.iteri ( fun idx itr -> Assert.True(( itr = lun_me.fromPrim ( uint64 idx ) )) )
                    Assert.True(( x.ErrorMessage = "" ))
                | _ ->
                    Assert.Fail __LINE__

                let luwdirname3 = ( lun_me.WorkDirName ( Constants.MAX_LOGICALUNIT_COUNT_IN_TD |> uint64 |> lun_me.fromPrim ) )
                let luwdirname4 = Functions.AppendPathName tddname luwdirname3
                Directory.CreateDirectory luwdirname4 |> ignore

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateLUWorkDir_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.CreateLUWorkDir_002() =
       task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
                Assert.False(( Directory.Exists tddname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.CreateLUWorkDir_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            File.WriteAllLines( tddname, [| "a" |] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.fromPrim 0UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
                Assert.False(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteFile tddname
        }

    [<Fact>]
    member _.CreateLUWorkDir_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore
            File.WriteAllLines( ludname, [| "a" |] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.fromPrim 0UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
                Assert.True(( Directory.Exists tddname ))
                Assert.True(( File.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateLUWorkDir_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.fromPrim 0UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateLUWorkDir_006() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore
            Directory.CreateDirectory ludname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.fromPrim 0UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage = "" ))
                Assert.True(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.CreateLUWorkDir_007() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore

            for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 2 do
                let luwdirname1 = lun_me.WorkDirName( i |> uint64 |> lun_me.fromPrim )
                let luwdirname2 = Functions.AppendPathName tddname luwdirname1
                Directory.CreateDirectory luwdirname2 |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let wlun1 = lun_me.fromPrim ( uint64 Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 1UL )
            let reqStr1 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = wlun1;
                    })
                }
            do! Functions.FramingSender c1 reqStr1

            let! resStr1 = Functions.FramingReceiver c1
            let res1 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr1

            match res1.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = wlun1 ))
                Assert.True(( x.ErrorMessage = "" ))
                let ludname =
                    Functions.AppendPathName
                        tddname ( lun_me.WorkDirName wlun1 )
                Assert.True(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            let wlun2 = lun_me.fromPrim ( uint64 Constants.MAX_LOGICALUNIT_COUNT_IN_TD )
            let reqStr2 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = wlun2;
                    })
                }
            do! Functions.FramingSender c1 reqStr2

            let! resStr2 = Functions.FramingReceiver c1
            let res2 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr2

            match res2.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = wlun2 ))
                Assert.True(( x.ErrorMessage.StartsWith "Number of LU exceeds limit" ))
                let ludname =
                    Functions.AppendPathName
                        tddname ( lun_me.WorkDirName wlun2 )
                Assert.False(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteLUWorkDir_001() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = dummySessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage = "Session ID mismatch" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.DeleteLUWorkDir_002() =
        task {
            let wtdid = tdid_me.Zero
            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.StartsWith "Target device directory missing" ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
        }

    [<Fact>]
    member _.DeleteLUWorkDir_003() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists ludname then GlbFunc.DeleteDir ludname
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            File.WriteAllLines( tddname, [| "a" |] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteFile tddname
        }

    [<Fact>]
    member _.DeleteLUWorkDir_004() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists ludname then GlbFunc.DeleteDir ludname
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore
            File.WriteAllLines( ludname, [| "a" |] )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.False(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteLUWorkDir_005() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists ludname then GlbFunc.DeleteDir ludname
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length = 0 ))
                Assert.False(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteLUWorkDir_006() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists ludname then GlbFunc.DeleteDir ludname
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore
            Directory.CreateDirectory ludname |> ignore

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length = 0 ))
                Assert.False(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }

    [<Fact>]
    member _.DeleteLUWorkDir_007() =
        task {
            let wtdid = tdid_me.Zero
            let tddname = Functions.AppendPathName dname ( tdid_me.toString wtdid )
            let ludname = Functions.AppendPathName tddname ( lun_me.WorkDirName lun_me.zero )
            if Directory.Exists ludname then GlbFunc.DeleteDir ludname
            if Directory.Exists tddname then GlbFunc.DeleteDir tddname
            Directory.CreateDirectory tddname |> ignore
            Directory.CreateDirectory ludname |> ignore
            let fname1 = Functions.AppendPathName ludname "a.txt"
            let subdname = Functions.AppendPathName ludname "d001"
            let fname2 = Functions.AppendPathName subdname "b.txt"

            Directory.CreateDirectory subdname |> ignore
            File.WriteAllText( fname1, "a" )
            File.WriteAllText( fname2, "a" )

            use con1 = new TcpClient( "::1", portNum )
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_DeleteLUWorkDir({
                        SessionID = sessID;
                        TargetDeviceID = wtdid;
                        LUN = lun_me.zero;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult( x ) ->
                Assert.True(( x.Result ))
                Assert.True(( x.TargetDeviceID = wtdid ))
                Assert.True(( x.LUN = lun_me.zero ))
                Assert.True(( x.ErrorMessage.Length = 0 ))
                Assert.False(( Directory.Exists ludname ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            GlbFunc.DeleteDir tddname
        }
