//=============================================================================
// Haruka Software Storage.
// ControllerTest2.fs : Test cases for Controller class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Controller

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Concurrent
open System.Threading
open System.Net
open System.Net.Sockets
open System.Diagnostics

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Controller
open Haruka.IODataTypes
open System.Collections.Generic

//=============================================================================
// Type definition

type MediaCreateProcStub() =
    inherit MediaCreateProc( HarukaCtrlerCtrlReq.T_MediaType.U_PlainFile({ FileName = "a"; FileSize = 1L; }), "", "" )

    let mutable m_Progress : MC_PROGRESS option = None
    let mutable m_CreatedTime : DateTime option = None
    let mutable m_Kill : ( unit -> unit ) option = None
    let mutable m_SubprocessHasTerminated : bool option = None
    let mutable m_ProcIdentfier : uint64 option = None
    let mutable m_ErrorMessages : ( string list ) option = None
    let mutable m_CreatedFile : ( string list ) option = None
    let mutable m_PathName : string option = None
    let mutable m_FileTypeStr : string option = None

    member _.p_Progress with set v = m_Progress <- Some( v )
    member _.p_CreatedTime with set v = m_CreatedTime <- Some( v )
    member _.p_Kill with set v = m_Kill <- Some( v )
    member _.p_SubprocessHasTerminated with set v = m_SubprocessHasTerminated <- Some( v )
    member _.p_ProcIdentfier with set v = m_ProcIdentfier <- Some( v )
    member _.p_ErrorMessages with set v = m_ErrorMessages <- Some( v )
    member _.p_CreatedFile with set v = m_CreatedFile <- Some( v )
    member _.p_PathName with set v = m_PathName <- Some( v )
    member _.p_FileTypeStr with set v = m_FileTypeStr <- Some( v )

    override _.Progress = m_Progress.Value
    override _.CreatedTime = m_CreatedTime.Value
    override _.Kill() = m_Kill.Value ()
    override _.SubprocessHasTerminated = m_SubprocessHasTerminated.Value
    override _.ProcIdentfier = m_ProcIdentfier.Value
    override _.ErrorMessages = m_ErrorMessages.Value
    override _.CreatedFile = m_CreatedFile.Value
    override _.PathName = m_PathName.Value
    override _.FileTypeStr = m_FileTypeStr.Value

//=============================================================================
// Class implementation

type Controller_Test2 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.ReleaseMutex() |> ignore

    static member DeleteDir ( dname : string ) =
        Functions.loopAsync ( fun () -> task {
            try
                GlbFunc.DeleteDir dname
                return false
            with
            |_ ->
                return true
        } )
        |> Functions.RunTaskSynchronously

    ///////////////////////////////////////////////////////////////////////////
    // Test cases
        
    [<Fact>]
    member _.Constractor_001() =
        let dname = Controller_Test1.CreateTestDir "Constractor_001"
        let k = new HKiller() :> IKiller
        Assert.ThrowsAny ( fun () ->
            new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath ) |> ignore
        ) |> ignore
        k.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Constractor_002() =
        let dname = Controller_Test1.CreateTestDir "Constractor_001"
        GlbFunc.DeleteDir dname
        let k = new HKiller() :> IKiller
        Assert.ThrowsAny ( fun () ->
            new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath ) |> ignore
        ) |> ignore
        k.NoticeTerminate()

    [<Fact>]
    member _.Constractor_003() =
        let dname = Controller_Test1.CreateTestDir "Constractor_003"
        let confFName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME

        let conf : HarukaCtrlConf.T_HarukaCtrl =
            {
                RemoteCtrl = Some {
                    PortNum = uint16( GlbFunc.nextTcpPortNo() )
                    Address = "localhost";
                    WhiteList = [];
                };
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile({
                        TotalLimit = 1234u;
                        MaxFileCount = 234u;
                        ForceSync = true;
                    })
                };
                LogParameters = Some {
                    SoftLimit = 3456u;
                    HardLimit = 4567u;
                    LogLevel = LogLevel.LOGLEVEL_INFO;
                };
            }
        HarukaCtrlConf.ReaderWriter.WriteFile confFName conf

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )

        Assert.True( Directory.Exists dname )
        Assert.True( File.Exists confFName )

        Assert.True( tc.CtrlConf.RemoteCtrl.IsSome )
        Assert.StrictEqual( conf.RemoteCtrl.Value.PortNum, tc.CtrlConf.RemoteCtrl.Value.PortNum )
        Assert.StrictEqual( "localhost", tc.CtrlConf.RemoteCtrl.Value.Address )
        Assert.True( tc.CtrlConf.LogMaintenance.IsSome )
        match tc.CtrlConf.LogMaintenance.Value.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.StrictEqual( 1234u, x.TotalLimit )
            Assert.StrictEqual( 234u, x.MaxFileCount )
            Assert.True( x.ForceSync )
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.Fail __LINE__
        Assert.True( tc.CtrlConf.LogParameters.IsSome )
        Assert.StrictEqual( 3456u, tc.CtrlConf.LogParameters.Value.SoftLimit )
        Assert.StrictEqual( 4567u, tc.CtrlConf.LogParameters.Value.HardLimit )
        Assert.StrictEqual( LogLevel.LOGLEVEL_INFO, tc.CtrlConf.LogParameters.Value.LogLevel )

        k.NoticeTerminate()
        GlbFunc.DeleteDir dname
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )

    [<Fact>]
    member _.Constractor_004() =
        let dname = Controller_Test1.CreateTestDir "Constractor_004"
        let confFName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME

        let conf : HarukaCtrlConf.T_HarukaCtrl =
            {
                RemoteCtrl = None;
                LogMaintenance = None;
                LogParameters = None
            }
        HarukaCtrlConf.ReaderWriter.WriteFile confFName conf

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )

        Assert.True( Directory.Exists dname )
        Assert.True( File.Exists confFName )

        Assert.True( tc.CtrlConf.RemoteCtrl.IsNone )
        Assert.True( tc.CtrlConf.LogMaintenance.IsNone )
        Assert.True( tc.CtrlConf.LogParameters.IsNone )

        k.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Constractor_005() =
        let dname = Controller_Test1.CreateTestDir "Constractor_005"
        let confFName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME

        let conf : HarukaCtrlConf.T_HarukaCtrl =
            {
                RemoteCtrl = None;
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToStdout( 99999u );
                };
                LogParameters = None
            }
        HarukaCtrlConf.ReaderWriter.WriteFile confFName conf

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )

        Assert.True( Directory.Exists dname )
        Assert.True( File.Exists confFName )

        Assert.True( tc.CtrlConf.RemoteCtrl.IsNone )
        Assert.True( tc.CtrlConf.LogMaintenance.IsSome )
        match tc.CtrlConf.LogMaintenance.Value.OutputDest with
        | HarukaCtrlConf.U_ToFile( _ ) ->
            Assert.Fail __LINE__
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.StrictEqual( 99999u, x )
        Assert.True( tc.CtrlConf.LogParameters.IsNone )

        k.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadInitialTargetDeviceProcs_001() =
        let dname = Controller_Test1.CreateTestDir "LoadInitialTargetDeviceProcs_001"
        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        let pc = PrivateCaller( tc )
        let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
        Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

        tc.LoadInitialTargetDeviceProcs()
        Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

        k.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadInitialTargetDeviceProcs_002() =
        let dname = Controller_Test1.CreateTestDir "LoadInitialTargetDeviceProcs_002"

        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
        Controller_Test1.CreateDefaultTDConf dname ( GlbFunc.newTargetDeviceID() )

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        let pc = PrivateCaller( tc )
        let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
        Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

        tc.LoadInitialTargetDeviceProcs()
        Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

        for i in m_TargetDeviceProcs do
            i.Value.m_Proc.Kill()

        k.NoticeTerminate()
        Controller_Test2.DeleteDir dname

    [<Fact>]
    member _.LoadInitialTargetDeviceProcs_003() =
        let dname = Controller_Test1.CreateTestDir "LoadInitialTargetDeviceProcs_003"
        Directory.CreateDirectory ( Functions.AppendPathName dname ( Constants.TARGET_DEVICE_DIR_PREFIX + "aaaa" ) ) |> ignore
        Directory.CreateDirectory ( Functions.AppendPathName dname ( let a = Guid.NewGuid() in a.ToString() )) |> ignore

        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
        Controller_Test1.CreateDefaultTDConf dname ( GlbFunc.newTargetDeviceID() )

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        let pc = PrivateCaller( tc )
        let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
        Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

        tc.LoadInitialTargetDeviceProcs()
        Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

        for i in m_TargetDeviceProcs do
            i.Value.m_Proc.Kill()

        k.NoticeTerminate()
        Controller_Test2.DeleteDir dname

    [<Fact>]
    member _.LoadInitialTargetDeviceProcs_004() =
        let dname = Controller_Test1.CreateTestDir "LoadInitialTargetDeviceProcs_004"
        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

        let tdids = [
            for _ = 0 to Constants.MAX_TARGET_DEVICE_COUNT do
                yield GlbFunc.newTargetDeviceID()
        ]
        for itr in tdids do
            Controller_Test1.CreateDefaultTDConf dname itr

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        let pc = PrivateCaller( tc )
        let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
        Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

        tc.LoadInitialTargetDeviceProcs()
        Assert.StrictEqual( Constants.MAX_TARGET_DEVICE_COUNT, m_TargetDeviceProcs.Count )

        let wlist1 = m_TargetDeviceProcs.Keys |> Seq.sort |> Seq.map tdid_me.fromPrim |> Seq.toList
        let wlist2 = tdids|> List.sort |> List.truncate Constants.MAX_TARGET_DEVICE_COUNT
        Assert.StrictEqual( wlist1, wlist2 )

        k.NoticeTerminate()
        Controller_Test2.DeleteDir dname

    [<Fact>]
    member _.WaitRequests_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "WaitRequests_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            for i = 0 to 5 do
                use con = GlbFunc.ConnectToServer portNum
                use c = con.GetStream()
                try
                    for j = 0 to 5 do
                        let reqStr =
                            let req : HarukaCtrlerCtrlReq.T_HarukaCtrlerCtrlReq = {
                                Request = HarukaCtrlerCtrlReq.T_Request.U_Login( true )
                            }
                            HarukaCtrlerCtrlReq.ReaderWriter.ToString req
                        do! Functions.FramingSender c reqStr

                        let! resStr = Functions.FramingReceiver c
                        let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

                        match res.Response with
                        | HarukaCtrlerCtrlRes.T_Response.U_LoginResult( x ) ->
                            Assert.True( x.Result )
                        | _ ->
                            Assert.Fail __LINE__

                with
                | :? Xunit.Sdk.FailException ->
                    Assert.Fail __LINE__
                | _ ->
                    ()
                c.Close()
                con.Close()

            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.WaitRequests_002() =
        let dname = Controller_Test1.CreateTestDir "WaitRequests_002"
        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "localhost" portNum

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        tc.LoadInitialTargetDeviceProcs()
        tc.WaitRequest()

        let r = Dns.GetHostEntry "localhost"
        for itr in r.AddressList do
            use con = new TcpClient( itr.ToString(), portNum )
            con.Close()

        k.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.WaitRequests_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "WaitRequests_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()

            let reqStr = "aaaaaaaaaaaaaa"
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( x ) ->
                Assert.True( x.Length > 0 )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.WaitRequests_004() =
        task {
            let dname = Controller_Test1.CreateTestDir "WaitRequests_004"
            let portNum = GlbFunc.nextTcpPortNo()

            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let conf : HarukaCtrlConf.T_HarukaCtrl = {
                RemoteCtrl = Some {
                    PortNum = uint16 portNum;
                    Address = "::1"
                    WhiteList = [ IPCondition.Any ];
                };
                LogMaintenance = None;
                LogParameters = None;
            }
            HarukaCtrlConf.ReaderWriter.WriteFile fname conf

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()

            let reqStr = "aaaaaaaaaaaaaa"
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( x ) ->
                Assert.True( x.Length > 0 )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.WaitRequests_005() =
        task {
            let dname = Controller_Test1.CreateTestDir "WaitRequests_005"
            let portNum = GlbFunc.nextTcpPortNo()

            let fname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
            let conf : HarukaCtrlConf.T_HarukaCtrl = {
                RemoteCtrl = Some {
                    PortNum = uint16 portNum;
                    Address = "::1"
                    WhiteList = [ IPCondition.IPv4Linklocal ];
                };
                LogMaintenance = None;
                LogParameters = None;
            }
            HarukaCtrlConf.ReaderWriter.WriteFile fname conf

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()

            try
                let! resStr = Functions.FramingReceiver c1
                Assert.Fail __LINE__
            with
            | _ as x ->
                ()

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetTargetDeviceProcs_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetTargetDeviceProcs_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceProcs( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( x ) ->
                Assert.StrictEqual( 0, x.TargetDeviceID.Length )
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetTargetDeviceProcs_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetTargetDeviceProcs_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceProcs( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( x ) ->
                Assert.StrictEqual( 1, x.TargetDeviceID.Length )
                Assert.StrictEqual( tdid, x.TargetDeviceID.[0] )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetTargetDeviceProcs_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetTargetDeviceProcs_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let tdid1 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid1

            let tdid2 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid2

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr1 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillTargetDeviceProc({
                        SessionID = sessID;
                        TargetDeviceID = tdid1;
                    })
                }
            do! Functions.FramingSender c1 reqStr1

            let! resStr1 = Functions.FramingReceiver c1
            let res1 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr1
            match res1.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult( x ) ->
                Assert.True( x.Result )
                Assert.StrictEqual( tdid1, x.TargetDeviceID )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            let reqStr2 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceProcs( sessID )
                }
            do! Functions.FramingSender c1 reqStr2

            let! resStr2 = Functions.FramingReceiver c1
            let res2 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr2

            match res2.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( x ) ->
                Assert.StrictEqual( 2, x.TargetDeviceID.Length )
                let wlist = [ tdid0; tdid2 ] |> List.sort
                Assert.StrictEqual( wlist, x.TargetDeviceID |> List.sort )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetTargetDeviceProcs_004() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetTargetDeviceProcs_004"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdids = [
                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 1 do
                    yield GlbFunc.newTargetDeviceID()
            ]
            for itr in tdids do
                Controller_Test1.CreateDefaultTDConf dname itr

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceProcs( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( x ) ->
                Assert.StrictEqual( Constants.MAX_TARGET_DEVICE_COUNT, x.TargetDeviceID.Length )
                Assert.StrictEqual( ( tdids |> List.sort ), x.TargetDeviceID |> List.sort )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetTargetDeviceProcs_005() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetTargetDeviceProcs_005"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            let tdids = [
                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT do
                    yield GlbFunc.newTargetDeviceID()
            ]
            for itr in tdids do
                let pi = {
                    m_Proc = new Process();
                    m_LastStartTime = 0L;
                    m_RestartCount = 0;
                }
                m_TargetDeviceProcs.AddOrUpdate( tdid_me.toPrim itr, pi, ( fun k o -> pi ) ) |> ignore

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr2 =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetTargetDeviceProcs( sessID )
                }
            do! Functions.FramingSender c1 reqStr2

            let! resStr2 = Functions.FramingReceiver c1
            let res2 = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr2

            match res2.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( x ) ->
                Assert.StrictEqual( Constants.MAX_TARGET_DEVICE_COUNT, x.TargetDeviceID.Length )
                let wlist = tdids |> List.sort |> List.truncate Constants.MAX_TARGET_DEVICE_COUNT
                Assert.StrictEqual( wlist, x.TargetDeviceID )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            m_TargetDeviceProcs.Clear()
            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.KillTargetDeviceProc_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillTargetDeviceProc_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()
            let dummyTDID = GlbFunc.newTargetDeviceID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_KillTargetDeviceProc( {
                            SessionID = dummySessID;
                            TargetDeviceID = dummyTDID;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StrictEqual( dummyTDID, x.TargetDeviceID )
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.KillTargetDeviceProc_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillTargetDeviceProc_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_KillTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult( x ) ->
                Assert.True(( x.Result ))
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.KillTargetDeviceProc_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillTargetDeviceProc_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummyTDID = GlbFunc.newTargetDeviceID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_KillTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = dummyTDID;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult( x ) ->
                Assert.True( x.Result )
                Assert.StrictEqual( dummyTDID, x.TargetDeviceID )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.StartTargetDeviceProc_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "StartTargetDeviceProc_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()
            let dummyTDID = GlbFunc.newTargetDeviceID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( {
                            SessionID = dummySessID;
                            TargetDeviceID = dummyTDID;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( x ) ->
                Assert.False( x.Result )
                Assert.StrictEqual( dummyTDID, x.TargetDeviceID )
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.StartTargetDeviceProc_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "StartTargetDeviceProc_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummyTDID = GlbFunc.newTargetDeviceID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = dummyTDID;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( x ) ->
                Assert.False( x.Result )
                Assert.StrictEqual( dummyTDID, x.TargetDeviceID )
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.StartTargetDeviceProc_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "StartTargetDeviceProc_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( x ) ->
                Assert.False( x.Result )
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.StartTargetDeviceProc_004() =
        task {
            let dname = Controller_Test1.CreateTestDir "StartTargetDeviceProc_004"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( x ) ->
                Assert.True( x.Result )
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.StrictEqual( 0, x.ErrorMessage.Length )
            | _ ->
                Assert.Fail __LINE__
            
            // Verify that the Target Group configuration has not been loaded.
            let reqtgstr =
                let wstr =
                    TargetDeviceCtrlReq.ReaderWriter.ToString {
                        Request =
                            TargetDeviceCtrlReq.T_Request.U_GetLoadedTargetGroups()
                    }
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_TargetDeviceCtrlRequest( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                            Request = wstr;
                    } )
                }
            do! Functions.FramingSender c1 reqtgstr
            let! restgstr = Functions.FramingReceiver c1
            let restg = HarukaCtrlerCtrlRes.ReaderWriter.LoadString restgstr
            match restg.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse( x ) ->
                let w = TargetDeviceCtrlRes.ReaderWriter.LoadString x.Response
                match w.Response with
                | TargetDeviceCtrlRes.U_LoadedTargetGroups( x ) ->
                    Assert.StrictEqual( 0, x.LoadedTGInfo.Length )
                | _ ->
                    Assert.Fail __LINE__
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.StartTargetDeviceProc_005() =
        task {
            let dname = Controller_Test1.CreateTestDir "StartTargetDeviceProc_005"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let tdids = [
                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 2 do
                    yield GlbFunc.newTargetDeviceID()
            ]
            for itr in tdids do
                let pi = {
                    m_Proc = new Process();
                    m_LastStartTime = 0L;
                    m_RestartCount = 0;
                }
                m_TargetDeviceProcs.AddOrUpdate( tdid_me.toPrim itr, pi, ( fun k o -> pi ) ) |> ignore

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( x ) ->
                Assert.True( x.Result )
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.StrictEqual( 0, x.ErrorMessage.Length )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            Assert.StrictEqual( Constants.MAX_TARGET_DEVICE_COUNT, m_TargetDeviceProcs.Count )
            let r, v = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
            Assert.True( r )
            v.m_Proc.Kill()

            m_TargetDeviceProcs.Clear()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.StartTargetDeviceProc_006() =
        task {
            let dname = Controller_Test1.CreateTestDir "StartTargetDeviceProc_006"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let tdids = [
                for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 1 do
                    yield GlbFunc.newTargetDeviceID()
            ]
            for itr in tdids do
                let pi = {
                    m_Proc = new Process();
                    m_LastStartTime = 0L;
                    m_RestartCount = 0;
                }
                m_TargetDeviceProcs.AddOrUpdate( tdid_me.toPrim itr, pi, ( fun k o -> pi ) ) |> ignore

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_StartTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult( x ) ->
                Assert.False( x.Result )
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.True(( x.ErrorMessage.Length > 0 ))
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            Assert.StrictEqual( Constants.MAX_TARGET_DEVICE_COUNT, m_TargetDeviceProcs.Count )
            m_TargetDeviceProcs.Clear()

            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.TargetDeviceCtrlRequest_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "TargetDeviceCtrlRequest_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()
            let dummyTDID = GlbFunc.newTargetDeviceID()

            let tdCtrlReq =
                TargetDeviceCtrlReq.ReaderWriter.ToString {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetDeviceName()
                }

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_TargetDeviceCtrlRequest( {
                            SessionID = dummySessID;
                            TargetDeviceID = dummyTDID;
                            Request = tdCtrlReq;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse( x ) ->
                Assert.StrictEqual( "", x.Response )
                Assert.StrictEqual( dummyTDID, x.TargetDeviceID )
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.TargetDeviceCtrlRequest_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "TargetDeviceCtrlRequest_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummyTDID = GlbFunc.newTargetDeviceID()

            let tdCtrlReq =
                TargetDeviceCtrlReq.ReaderWriter.ToString {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetDeviceName()
                }

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_TargetDeviceCtrlRequest( {
                            SessionID = sessID;
                            TargetDeviceID = dummyTDID;
                            Request = tdCtrlReq;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse( x ) ->
                Assert.StrictEqual( "", x.Response )
                Assert.StrictEqual( dummyTDID, x.TargetDeviceID )
                Assert.StartsWith( "Specified target device missing", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            GlbFunc.DeleteDir dname
        }

    [<Fact>]
    member _.TargetDeviceCtrlRequest_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "TargetDeviceCtrlRequest_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let tdCtrlReq =
                TargetDeviceCtrlReq.ReaderWriter.ToString {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetDeviceName()
                }

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_TargetDeviceCtrlRequest( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                            Request = tdCtrlReq;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse( x ) ->
                Assert.True(( x.Response.Length > 0 ))
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.StrictEqual( "", x.ErrorMessage )

                let tdCtrlRes =
                    TargetDeviceCtrlRes.ReaderWriter.LoadString x.Response
                match tdCtrlRes.Response with
                | TargetDeviceCtrlRes.T_Response.U_DeviceName( x ) ->
                    Assert.StrictEqual( "abc", x )
                | _ ->
                    Assert.Fail __LINE__
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.CreateMediaFile_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "CreateMediaFile_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateMediaFile( {
                        SessionID = dummySessID;
                        MediaType = HarukaCtrlerCtrlReq.U_PlainFile({
                            FileName = "a";
                            FileSize = 1L;
                        })
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult( x ) ->
                Assert.False( x.Result )
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.CreateMediaFile_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "CreateMediaFile_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub( p_Progress = MC_PROGRESS.NotStarted, p_CreatedTime = DateTime.UtcNow )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateMediaFile( {
                        SessionID = sessID;
                        MediaType = HarukaCtrlerCtrlReq.U_PlainFile({
                            FileName = "a";
                            FileSize = 1L;
                        })
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StartsWith( "Maximum multiplicity of", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.CreateMediaFile_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "CreateMediaFile_003"
            let portNum = GlbFunc.nextTcpPortNo()
            let mfilename = Functions.AppendPathName dname "a.txt"
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub( p_Progress = MC_PROGRESS.NotStarted, p_CreatedTime = DateTime.UtcNow )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateMediaFile( {
                        SessionID = sessID;
                        MediaType = HarukaCtrlerCtrlReq.U_PlainFile({
                            FileName = mfilename;
                            FileSize = 1L;
                        })
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult( x ) ->
                Assert.True( x.Result )
            | _ ->
                Assert.Fail __LINE__

            let mutable loopcnt = 0
            while not ( File.Exists mfilename ) && loopcnt < 100 do
                Thread.Sleep 5
                loopcnt <- loopcnt + 1
            Assert.True( loopcnt < 100 )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.CreateMediaFile_004() =
        task {
            let dname = Controller_Test1.CreateTestDir "CreateMediaFile_004"
            let portNum = GlbFunc.nextTcpPortNo()
            let mfilename = Functions.AppendPathName dname "a.txt"
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            m_InitMediaProcs.Add(
                0x1000000000000001UL,
                new MediaCreateProcStub(
                    p_Progress = MC_PROGRESS.NormalEnd( DateTime.UtcNow ),
                    p_CreatedTime = DateTime.UtcNow,
                    p_Kill = ( fun _ -> () )
                )
            )
            for i = 2 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub( p_Progress = MC_PROGRESS.NotStarted, p_CreatedTime = DateTime.UtcNow)
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateMediaFile( {
                        SessionID = sessID;
                        MediaType = HarukaCtrlerCtrlReq.U_PlainFile({
                            FileName = mfilename;
                            FileSize = 1L;
                        })
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult( x ) ->
                Assert.True( x.Result )
            | _ ->
                Assert.Fail __LINE__

            let mutable loopcnt = 0
            while not ( File.Exists mfilename ) && loopcnt < 100 do
                Thread.Sleep 5
                loopcnt <- loopcnt + 1
            Assert.True( loopcnt < 100 )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.CreateMediaFile_005() =
        task {
            let dname = Controller_Test1.CreateTestDir "CreateMediaFile_005"
            let portNum = GlbFunc.nextTcpPortNo()
            let mfilename = Functions.AppendPathName dname "a.txt"
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            m_InitMediaProcs.Add(
                0x1000000000000001UL,
                new MediaCreateProcStub(
                    p_Progress = MC_PROGRESS.NotStarted,
                    p_CreatedTime = DateTime.UtcNow - TimeSpan( 0, 0, Constants.INITMEDIA_MAX_REMAIN_TIME + 1 ),
                    p_Kill = ( fun _ -> () )
                )
            )
            for i = 2 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub( p_Progress = MC_PROGRESS.NotStarted, p_CreatedTime = DateTime.UtcNow)
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_CreateMediaFile( {
                        SessionID = sessID;
                        MediaType = HarukaCtrlerCtrlReq.U_PlainFile({
                            FileName = mfilename;
                            FileSize = 1L;
                        })
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult( x ) ->
                Assert.True( x.Result )
            | _ ->
                Assert.Fail __LINE__

            let mutable loopcnt = 0
            while not ( File.Exists mfilename ) && loopcnt < 100 do
                Thread.Sleep 5
                loopcnt <- loopcnt + 1
            Assert.True( loopcnt < 100 )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( dummySessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( 0, x.Procs.Length )
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( 0, x.Procs.Length )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub(
                        p_Progress = MC_PROGRESS.NotStarted,
                        p_CreatedTime = DateTime.UtcNow,
                        p_ErrorMessages = [ "a"; "b" ],
                        p_PathName = "ccc",
                        p_FileTypeStr = "ddd"
                    )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( "", x.ErrorMessage )
                Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, x.Procs.Length )
                for i = 0 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                    Assert.StrictEqual( 0x1000000000000001UL + ( uint64 i ), x.Procs.[i].ProcID )
                    Assert.StrictEqual( "ccc", x.Procs.[i].PathName )
                    Assert.StrictEqual( "ddd", x.Procs.[i].FileType )
                    Assert.StrictEqual( HarukaCtrlerCtrlRes.U_NotStarted(), x.Procs.[i].Status )
                    Assert.StrictEqual( [ "a"; "b" ], x.Procs.[i].ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_004() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_004"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub(
                        p_Progress = MC_PROGRESS.ProgressCreation( 2uy ),
                        p_CreatedTime = DateTime.UtcNow,
                        p_ErrorMessages = [ "a" ],
                        p_PathName = "ccc",
                        p_FileTypeStr = "ddd"
                    )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( "", x.ErrorMessage )
                Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, x.Procs.Length )
                for i = 0 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                    Assert.StrictEqual( 0x1000000000000001UL + ( uint64 i ), x.Procs.[i].ProcID )
                    Assert.StrictEqual( "ccc", x.Procs.[i].PathName )
                    Assert.StrictEqual( "ddd", x.Procs.[i].FileType )
                    Assert.StrictEqual( HarukaCtrlerCtrlRes.U_ProgressCreation( 2uy ), x.Procs.[i].Status )
                    Assert.StrictEqual( [ "a" ], x.Procs.[i].ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_005() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_005"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub(
                        p_Progress = MC_PROGRESS.Recovery( 3uy ),
                        p_CreatedTime = DateTime.UtcNow,
                        p_ErrorMessages = [ "x" ],
                        p_PathName = "ccc",
                        p_FileTypeStr = "ddd"
                    )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( "", x.ErrorMessage )
                Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, x.Procs.Length )
                for i = 0 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                    Assert.StrictEqual( 0x1000000000000001UL + ( uint64 i ), x.Procs.[i].ProcID )
                    Assert.StrictEqual( "ccc", x.Procs.[i].PathName )
                    Assert.StrictEqual( "ddd", x.Procs.[i].FileType )
                    Assert.StrictEqual( HarukaCtrlerCtrlRes.U_Recovery( 3uy ), x.Procs.[i].Status )
                    Assert.StrictEqual( [ "x" ], x.Procs.[i].ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_006() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_006"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub(
                        p_Progress = MC_PROGRESS.NormalEnd( DateTime.UtcNow ),
                        p_CreatedTime = DateTime.UtcNow,
                        p_ErrorMessages = [ "x" ],
                        p_PathName = "ccc1",
                        p_FileTypeStr = "ddd1",
                        p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                    )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( "", x.ErrorMessage )
                Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, x.Procs.Length )
                for i = 0 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                    Assert.StrictEqual( 0x1000000000000001UL + ( uint64 i ), x.Procs.[i].ProcID )
                    Assert.StrictEqual( "ccc1", x.Procs.[i].PathName )
                    Assert.StrictEqual( "ddd1", x.Procs.[i].FileType )
                    Assert.StrictEqual( HarukaCtrlerCtrlRes.U_NormalEnd(), x.Procs.[i].Status )
                    Assert.StrictEqual( [ "x" ], x.Procs.[i].ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 0, m_InitMediaProcs.Count )
            Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY,  flg1 )

            c1.Close()
            con1.Close()
            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_007() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_007"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub(
                        p_Progress = MC_PROGRESS.AbnormalEnd( DateTime.UtcNow ),
                        p_CreatedTime = DateTime.UtcNow,
                        p_ErrorMessages = [ "x" ],
                        p_PathName = "ccc1",
                        p_FileTypeStr = "ddd1",
                        p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                    )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( "", x.ErrorMessage )
                Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, x.Procs.Length )
                for i = 0 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                    Assert.StrictEqual( 0x1000000000000001UL + ( uint64 i ), x.Procs.[i].ProcID )
                    Assert.StrictEqual( "ccc1", x.Procs.[i].PathName )
                    Assert.StrictEqual( "ddd1", x.Procs.[i].FileType )
                    Assert.StrictEqual( HarukaCtrlerCtrlRes.U_AbnormalEnd(), x.Procs.[i].Status )
                    Assert.StrictEqual( [ "x" ], x.Procs.[i].ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 0, m_InitMediaProcs.Count )
            Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, flg1 )

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.GetInitMediaStatus_008() =
        task {
            let dname = Controller_Test1.CreateTestDir "GetInitMediaStatus_008"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            for i = 1 to Constants.INITMEDIA_MAX_MULTIPLICITY do
                m_InitMediaProcs.Add(
                    0x1000000000000000UL + ( uint64 i ),
                    new MediaCreateProcStub(
                        p_Progress = MC_PROGRESS.NotStarted,
                        p_CreatedTime = DateTime.UtcNow - TimeSpan( 0, 0, Constants.INITMEDIA_MAX_REMAIN_TIME + 1 ),
                        p_ErrorMessages = [ "x" ],
                        p_PathName = "ccc2",
                        p_FileTypeStr = "ddd2",
                        p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                    )
                )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_GetInitMediaStatus( sessID )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus( x ) ->
                Assert.StrictEqual( "", x.ErrorMessage )
                Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, x.Procs.Length )
                for i = 0 to Constants.INITMEDIA_MAX_MULTIPLICITY - 1 do
                    Assert.StrictEqual( 0x1000000000000001UL + ( uint64 i ), x.Procs.[i].ProcID )
                    Assert.StrictEqual( "ccc2", x.Procs.[i].PathName )
                    Assert.StrictEqual( "ddd2", x.Procs.[i].FileType )
                    Assert.StrictEqual( HarukaCtrlerCtrlRes.U_NotStarted(), x.Procs.[i].Status )
                    Assert.StrictEqual( [ "x" ], x.Procs.[i].ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 0, m_InitMediaProcs.Count )
            Assert.StrictEqual( Constants.INITMEDIA_MAX_MULTIPLICITY, flg1 )

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.KillInitMediaProc_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillInitMediaProc_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let dummySessID = CtrlSessionID.NewID()

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc({
                        SessionID = dummySessID;
                        ProcID = 0UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StrictEqual( "Session ID mismatch", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.KillInitMediaProc_002() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillInitMediaProc_002"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc({
                        SessionID = sessID;
                        ProcID = 0UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StartsWith( "Specified process is missing", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.KillInitMediaProc_003() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillInitMediaProc_003"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            m_InitMediaProcs.Add(
                0x1000000000000000UL,
                new MediaCreateProcStub(
                    p_Progress = MC_PROGRESS.NotStarted,
                    p_CreatedTime = DateTime.UtcNow,
                    p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                )
            )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc({
                        SessionID = sessID;
                        ProcID = 0x1000000000000000UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( x ) ->
                Assert.True( x.Result )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 1, flg1 )
            Assert.StrictEqual( 1, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.KillInitMediaProc_004() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillInitMediaProc_004"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            m_InitMediaProcs.Add(
                0x1000000000000000UL,
                new MediaCreateProcStub(
                    p_Progress = MC_PROGRESS.NormalEnd( DateTime.UtcNow ),
                    p_CreatedTime = DateTime.UtcNow,
                    p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                )
            )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc({
                        SessionID = sessID;
                        ProcID = 0x1000000000000000UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StartsWith( "Specified process is missing", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 1, flg1 )
            Assert.StrictEqual( 0, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.KillInitMediaProc_005() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillInitMediaProc_005"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            m_InitMediaProcs.Add(
                0x1000000000000000UL,
                new MediaCreateProcStub(
                    p_Progress = MC_PROGRESS.AbnormalEnd( DateTime.UtcNow ),
                    p_CreatedTime = DateTime.UtcNow,
                    p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                )
            )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc({
                        SessionID = sessID;
                        ProcID = 0x1000000000000000UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StartsWith( "Specified process is missing", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 1, flg1 )
            Assert.StrictEqual( 0, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.KillInitMediaProc_006() =
        task {
            let dname = Controller_Test1.CreateTestDir "KillInitMediaProc_006"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum
            let tdid = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            let pc = PrivateCaller( tc )
            let m_InitMediaProcs = pc.GetField( "m_InitMediaProcs" ) :?> Dictionary< uint64, MediaCreateProc >

            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let mutable flg1 = 0
            m_InitMediaProcs.Add(
                0x1000000000000000UL,
                new MediaCreateProcStub(
                    p_Progress = MC_PROGRESS.NotStarted,
                    p_CreatedTime = DateTime.UtcNow - TimeSpan( 0, 0, Constants.INITMEDIA_MAX_REMAIN_TIME + 1 ),
                    p_Kill = ( fun _ -> flg1 <- flg1 + 1; () )
                )
            )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1
            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request = HarukaCtrlerCtrlReq.T_Request.U_KillInitMediaProc({
                        SessionID = sessID;
                        ProcID = 0x1000000000000000UL;
                    })
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult( x ) ->
                Assert.False(( x.Result ))
                Assert.StartsWith( "Specified process is missing", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            Assert.StrictEqual( 1, flg1 )
            Assert.StrictEqual( 0, m_InitMediaProcs.Count )

            c1.Close()
            con1.Close()

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.OnExitChildProc_001() =
        task {
            let dname = Controller_Test1.CreateTestDir "OnExitChildProc_001"
            let portNum = GlbFunc.nextTcpPortNo()
            Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

            let tdid0 = GlbFunc.newTargetDeviceID()
            Controller_Test1.CreateDefaultTDConf dname tdid0

            let k = new HKiller() :> IKiller
            let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
            tc.LoadInitialTargetDeviceProcs()
            tc.WaitRequest()

            let pc = PrivateCaller( tc )
            let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
            Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

            use con1 = GlbFunc.ConnectToServer portNum
            use c1 = con1.GetStream()
            let! sessID = Controller_Test1.FirstLogin c1

            let reqStr =
                HarukaCtrlerCtrlReq.ReaderWriter.ToString {
                    Request =
                        HarukaCtrlerCtrlReq.T_Request.U_KillTargetDeviceProc( {
                            SessionID = sessID;
                            TargetDeviceID = tdid0;
                        } )
                }
            do! Functions.FramingSender c1 reqStr

            let! resStr = Functions.FramingReceiver c1
            let res = HarukaCtrlerCtrlRes.ReaderWriter.LoadString resStr

            match res.Response with
            | HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult( x ) ->
                Assert.True( x.Result )
                Assert.StrictEqual( tdid0, x.TargetDeviceID )
                Assert.StrictEqual( "", x.ErrorMessage )
            | _ ->
                Assert.Fail __LINE__

            c1.Close()
            con1.Close()

            for i = 1 to 10 do
                Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )
                if i <> 10 then Thread.Sleep 50

            k.NoticeTerminate()
            Controller_Test2.DeleteDir dname
        }

    [<Fact>]
    member _.OnExitChildProc_002() =
        let dname = Controller_Test1.CreateTestDir "OnExitChildProc_002"
        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

        let tdid0 = GlbFunc.newTargetDeviceID()
        Controller_Test1.CreateDefaultTDConf dname tdid0

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        tc.LoadInitialTargetDeviceProcs()
        tc.WaitRequest()

        let pc = PrivateCaller( tc )
        let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
        Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

        let m_Sema = pc.GetField( "m_Sema" ) :?> SemaphoreSlim

        let r, v = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
        Assert.True( r )
        Assert.StrictEqual( 0, v.m_RestartCount )

        v.m_Proc.Kill()
        Thread.Sleep 10

        let mutable cnt = 0
        while cnt < 10 do
            m_Sema.Wait()
            let r, v = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
            Assert.True( r )
            m_Sema.Release() |> ignore

            if v.m_RestartCount = 1 then
                cnt <- 99
            else
                cnt <- cnt + 1
                if cnt < 10 then
                    Thread.Sleep 50
        Assert.StrictEqual( 99, cnt )

        k.NoticeTerminate()
        Controller_Test2.DeleteDir dname

    [<Fact>]
    member _.OnExitChildProc_003() =
        let currentSec = ( let a = DateTime.UtcNow in a.Ticks % 600000000L ) / 10000000L |> float
        let maxWaitTime = 0.5 * ( float Constants.MAX_CHILD_PROC_RESTART_COUNT + 1.0 )
        if currentSec > 60.0 - maxWaitTime then
            Thread.Sleep( int32 ( 60.0 - currentSec ) * 1000 )

        let dname = Controller_Test1.CreateTestDir "OnExitChildProc_003"
        let portNum = GlbFunc.nextTcpPortNo()
        Controller_Test1.CreateDefaultCtrlConf dname "::1" portNum

        let tdid0 = GlbFunc.newTargetDeviceID()
        Controller_Test1.CreateDefaultTDConf dname tdid0

        let k = new HKiller() :> IKiller
        let tc = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
        tc.LoadInitialTargetDeviceProcs()
        tc.WaitRequest()

        let pc = PrivateCaller( tc )
        let m_TargetDeviceProcs = pc.GetField( "m_TargetDeviceProcs" ) :?> ConcurrentDictionary< uint32, TargetDeviceProcInfo >
        Assert.StrictEqual( 1, m_TargetDeviceProcs.Count )

        let m_Sema = pc.GetField( "m_Sema" ) :?> SemaphoreSlim

        let r, v = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
        Assert.True( r )
        Assert.StrictEqual( 0, v.m_RestartCount )

        for i = 1 to Constants.MAX_CHILD_PROC_RESTART_COUNT do
            m_Sema.Wait()
            let r, v = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
            Assert.True( r )
            m_Sema.Release() |> ignore

            let curRC = v.m_RestartCount
            v.m_Proc.Kill()
            Thread.Sleep 10

            let mutable cnt = 0
            while cnt < 10 do
                m_Sema.Wait()
                let r, v = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
                Assert.True( r )
                m_Sema.Release() |> ignore

                if v.m_RestartCount = curRC + 1 then
                    cnt <- 99
                else
                    cnt <- cnt + 1
                    if cnt < 10 then
                        Thread.Sleep 50
            Assert.StrictEqual( 99, cnt )

        m_Sema.Wait()
        let r2, v2 = m_TargetDeviceProcs.TryGetValue( tdid_me.toPrim tdid0 )
        Assert.True( r2 )
        m_Sema.Release() |> ignore

        v2.m_Proc.Kill()
        Thread.Sleep 10

        let mutable cnt = 0
        while cnt < 10 && m_TargetDeviceProcs.Count > 0 do
            cnt <- cnt + 1
            if cnt <> 9 then
                Thread.Sleep 50
        Assert.StrictEqual( 0, m_TargetDeviceProcs.Count )

        k.NoticeTerminate()
        Controller_Test2.DeleteDir dname
