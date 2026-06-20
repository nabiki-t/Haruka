//=============================================================================
// Haruka Software Storage.
// StatusMasterTest2.fs : Test cases for StatusMaster class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Concurrent
open System.Collections.Immutable
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.BlockDeviceLU
open Haruka.Test

#nowarn "1240"

//=============================================================================
// Class implementation

type StatusMaster_Test2 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultTargetGroupConfStr idx eas =
        ( {
            TargetGroupID = tgid_me.fromPrim( uint32 idx );
            TargetGroupName = sprintf "a-%03d" idx;
            EnabledAtStart = eas;
            Target = 
                [{
                    IdentNumber = tnodeidx_me.fromPrim ( uint16 idx + 1us );
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = sprintf "target%03d" idx;
                    TargetAlias = sprintf "target%03d" idx;
                    LUN = [ lun_me.fromPrim ( uint64 idx + 1UL ) ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }];
            LogicalUnit =
                [{
                    LUN = lun_me.fromPrim ( uint64 idx + 1UL );
                    LUName = "luname";
                    WorkPath = "";
                    MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                    LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                }];
        } : TargetGroupConf.T_TargetGroup )
        |> TargetGroupConf.ReaderWriter.ToString

    let tgid0 = tgid_me.Zero
    let tgid1 = tgid_me.fromPrim( 1u )
    let tgid99 = tgid_me.fromPrim( 99u )

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.ReleaseMutex() |> ignore

    member _.GetTestDirName ( caseName : string ) =
        Functions.AppendPathName ( Path.GetTempPath() ) "StatusMaster_Test2_" + caseName

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.ProcessControlRequest_001() =
        let pDirName = this.GetTestDirName "ProcessControlRequest_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let rq_in = new StreamReader( new MemoryStream() )
        let rq_out = new StreamWriter( new MemoryStream() )

        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, rq_in, rq_out ) :> IStatus

        killer.NoticeTerminate()

        sm.ProcessControlRequest().Wait()

        GlbFunc.AllDispose [ rq_in; rq_out; ]
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcessControlRequest_002() =
        let pDirName = this.GetTestDirName "ProcessControlRequest_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                rq_out.Dispose()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()

        GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcessControlRequest_003() =
        let pDirName = this.GetTestDirName "ProcessControlRequest_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        File.WriteAllText( targetDeviceConfName, "<TargetDevice></TargetDevice>" )
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                s.WriteLine( "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnexpectedError( x ) ->
                    Assert.True( x.Length > 0 )
                | _ ->
                    Assert.Fail __LINE__
                s.Close()
                o.Close()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetActiveTargetGroups_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetActiveTargetGroups_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetActiveTargetGroups()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( x ) ->
                    Assert.StrictEqual( 1, x.ActiveTGInfo.Length )
                    Assert.StrictEqual( tgid0, x.ActiveTGInfo.[0].ID )
                    Assert.StrictEqual( "a-000", x.ActiveTGInfo.[0].Name )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetActiveTargetGroups_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetActiveTargetGroups_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetActiveTargetGroups()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActiveTargetGroups( x ) ->
                    Assert.StrictEqual( 0, x.ActiveTGInfo.Length )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLoadedTargetGroups_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLoadedTargetGroups_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 false )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLoadedTargetGroups()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadedTargetGroups( x ) ->
                    Assert.StrictEqual( 2,       x.LoadedTGInfo.Length )
                    Assert.StrictEqual( tgid0,   x.LoadedTGInfo.[0].ID )
                    Assert.StrictEqual( "a-000", x.LoadedTGInfo.[0].Name )
                    Assert.StrictEqual( tgid1,   x.LoadedTGInfo.[1].ID )
                    Assert.StrictEqual( "a-001", x.LoadedTGInfo.[1].Name )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_InactivateTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_InactivateTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.True( x.Result )
                | _ ->
                    Assert.Fail __LINE__

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid1 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_InactivateTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_InactivateTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid99, x.ID )
                    Assert.False( x.Result )
                    Assert.StrictEqual( "Specified target group is missing.", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_InactivateTargetGroup_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_InactivateTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    // Transitions to the same state are ignored and treated as normal.
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.True( x.Result )
                | _ ->
                    Assert.Fail __LINE__

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >

                Assert.StrictEqual( 0, m_ActiveTargetGroups.Count )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_ActivateTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_ActivateTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 0, m_ActiveTargetGroups.Count )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_ActivateTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_ActivateTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_ActivateTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 0, m_ActiveTargetGroups.Count )

                let req2 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_ActivateTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req2 )
                s.Flush()

                let res2 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res2.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid99, x.ID )
                    Assert.False( x.Result )
                    Assert.StrictEqual( "Specified target group is missing.", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 0, m_ActiveTargetGroups.Count )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_ActivateTargetGroup_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_ActivateTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_ActivateTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ActivateTargetGroupResult( x ) ->
                    // Transitions to the same state are ignored and treated as normal.
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.False( x.Result )
                    Assert.StrictEqual( "Specified target group is still active.", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid99, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                let sessParam = {
                    StatusMaster_Test1.defaultSessParam with
                        TargetConf = {
                            StatusMaster_Test1.defaultSessParam.TargetConf with
                                IdentNumber = tnodeidx_me.fromPrim 1us;
                        }
                }

                let m_sessions1 =
                    let ss = new CSession_Stub( p_GetSessionParameter = ( fun _ -> sessParam ) )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.False( x.Result )
                    Assert.StrictEqual( "Specified target group is still used.", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid1 ) )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                // Target group 0 is still used.
                let m_sessions1 =
                    let ss = new CSession_Stub( p_GetSessionParameter = ( fun _ -> StatusMaster_Test1.defaultSessParam ) )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                pc.SetField( "m_Sessions", m_sessions1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.StrictEqual( 2, m_config.GetAllTargetGroupConf().Length )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid1, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.True( m_ActiveTargetGroups.ContainsKey( tgid_me.toPrim tgid0 ) )
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )
                Assert.StrictEqual( tgid0, ( fst ( m_config.GetAllTargetGroupConf().[0] ) ).TargetGroupID )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_UnloadTargetGroup_005() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_UnloadTargetGroup_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled
        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups1 = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 2, m_ActiveTargetGroups1.Count )

                // Target group 0 is still used.
                let m_sessions1 =
                    let ss = new CSession_Stub( p_GetSessionParameter = ( fun _ -> StatusMaster_Test1.defaultSessParam ) )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                pc.SetField( "m_Sessions", m_sessions1 )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.StrictEqual( 2, m_config.GetAllTargetGroupConf().Length )

                // create LU object in tgid1
                sm.GetLU ( lun_me.fromPrim 2UL ) |> ignore

                let m_LU1 = pc.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
                Assert.StrictEqual( 1, m_LU1.obj.Count )

                // inactivate target group tgid1
                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_InactivateTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_InactivateTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid1, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                let m_ActiveTargetGroups2 = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups2.Count )

                let m_LU2 = pc.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
                Assert.StrictEqual( 1, m_LU2.obj.Count )

                // Unload target group tgid1
                let req2 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_UnloadTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req2 )
                s.Flush()

                let res2 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res2.Response with
                | TargetDeviceCtrlRes.T_Response.U_UnloadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid1, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                let m_LU3 = pc.GetField( "m_LU" ) :?> OptimisticLock< ImmutableDictionary< LUN_T, Lazy<ILU> > >
                Assert.StrictEqual( 0, m_LU3.obj.Count  )

                let m_ActiveTargetGroups3 = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups3.Count )
                Assert.True( m_ActiveTargetGroups3.ContainsKey( tgid_me.toPrim tgid0 ) )
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )
                Assert.StrictEqual( tgid0, ( fst ( m_config.GetAllTargetGroupConf().[0] ) ).TargetGroupID )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count  )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.False( x.Result )
                    Assert.StrictEqual( "Specified target group is still active.",  x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid99 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid99, x.ID )
                    Assert.False( x.Result )
                    Assert.StrictEqual( "Failed to load target group config.", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )    // Default target group will be enabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        let targetGroupConfName1 = Functions.AppendPathName pDirName ( tgid_me.toString tgid1 )
        File.WriteAllText( targetGroupConfName1, defaultTargetGroupConfStr 1 true )

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid1 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    Assert.StrictEqual( tgid1, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 1, m_ActiveTargetGroups.Count )
                Assert.StrictEqual( 2, m_config.GetAllTargetGroupConf().Length )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteFile targetGroupConfName1
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_LoadTargetGroup_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LoadTargetGroup_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 false )    // Default target group will be disabled

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let pc = PrivateCaller( sm )
                let m_ActiveTargetGroups = pc.GetField( "m_ActiveTargetGroups" ) :?> ConcurrentDictionary< uint32, unit >
                Assert.StrictEqual( 0, m_ActiveTargetGroups.Count  )

                let m_config = pc.GetField( "m_config" ) :?> IConfiguration
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LoadTargetGroup( tgid0 )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LoadTargetGroupResult( x ) ->
                    // Transitions to the same state are ignored and treated as normal.
                    Assert.StrictEqual( tgid0, x.ID )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "",  x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                Assert.StrictEqual( 0, m_ActiveTargetGroups.Count )
                Assert.StrictEqual( 1, m_config.GetAllTargetGroupConf().Length )

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_SetLogParameters_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_SetLogParameters_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )


                let lock = GlbFunc.LogParamUpdateLock()
                try
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_VERBOSE, stderr )

                    let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                        Request = TargetDeviceCtrlReq.T_Request.U_SetLogParameters( {
                            SoftLimit = 9999u;
                            HardLimit = 999u;
                            LogLevel = LogLevel.LOGLEVEL_INFO;
                        })
                    }
                    s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                    s.Flush()

                    let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( x ) ->
                        Assert.False( x )
                    | _ ->
                        Assert.Fail __LINE__

//                    let softLimit, hardLimit, lv = HLogger.GetLogParameters()
//                    Assert.StrictEqual( 10000u, softLimit )
//                    Assert.StrictEqual( 10000u, hardLimit )
//                    Assert.StrictEqual( LogLevel.LOGLEVEL_VERBOSE, lv )
                finally
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
                    lock.ReleaseMutex() |> ignore

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_SetLogParameters_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_SetLogParameters_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let lock = GlbFunc.LogParamUpdateLock()
                try
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_VERBOSE, stderr )

                    let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                        Request = TargetDeviceCtrlReq.T_Request.U_SetLogParameters( {
                            SoftLimit = 9999u;
                            HardLimit = 99999u;
                            LogLevel = LogLevel.LOGLEVEL_INFO;
                        })
                    }
                    s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                    s.Flush()

                    let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_SetLogParametersResult( x ) ->
                        Assert.True( x )
                    | _ ->
                        Assert.Fail __LINE__

//                    let softLimit, hardLimit, lv = HLogger.GetLogParameters()
//                    Assert.StrictEqual( 9999u, softLimit )
//                    Assert.StrictEqual( 99999u, hardLimit )
//                    Assert.StrictEqual( LogLevel.LOGLEVEL_INFO, lv )
                finally
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
                    lock.ReleaseMutex() |> ignore

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLogParameters_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLogParameters_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let lock = GlbFunc.LogParamUpdateLock()
                try
                    HLogger.SetLogParameters( 1234u, 2345u, 0u, LogLevel.LOGLEVEL_WARNING, stderr )

                    let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                        Request = TargetDeviceCtrlReq.T_Request.U_GetLogParameters()
                    }
                    s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                    s.Flush()

                    let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_LogParameters( x ) ->
                        Assert.StrictEqual( 1234u, x.SoftLimit )
                        Assert.StrictEqual( 2345u, x.HardLimit )
                        Assert.StrictEqual( LogLevel.LOGLEVEL_WARNING, x.LogLevel )
                    | _ ->
                        Assert.Fail __LINE__
                finally
                    HLogger.SetLogParameters( 10000u, 10000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
                    lock.ReleaseMutex() |> ignore

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetDeviceName_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetDeviceName_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = Functions.AppendPathName pDirName Constants.TARGET_DEVICE_CONF_FILE_NAME
        TargetDeviceConf.ReaderWriter.WriteFile targetDeviceConfName {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "abcdefg";
            EnableStatSNAckChecker = false;
        }
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetDeviceName()
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_DeviceName( x ) ->
                    Assert.StrictEqual("abcdefg", x )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTargetDevice() )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.StrictEqual( 0, x.Session.Length )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let wCreateDate = DateTime.UtcNow
                let m_sessions1 =
                    let ss = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> StatusMaster_Test1.defaultSessParam ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "tn0", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> wCreateDate )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTargetDevice() )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.StrictEqual( 1, x.Session.Length )
                    Assert.StrictEqual( tgid0, x.Session.[0].TargetGroupID )
                    Assert.StrictEqual( tnodeidx_me.fromPrim 10us, x.Session.[0].TargetNodeID )
                    Assert.StrictEqual( wCreateDate, x.Session.[0].EstablishTime )
                    Assert.StrictEqual( tsih_me.fromPrim 1us, x.Session.[0].TSIH )
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 1us;
                                        TargetName = "target001001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target001001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss21 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 2u );
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 21us;
                                        TargetName = "target002001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss22 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 2u );
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 22us;
                                        TargetName = "target002002";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in0", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002002", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss21 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, ss22 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTargetGroup( tgid_me.fromPrim 2u ) )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.StrictEqual( 2, x.Session.Length )
                    let rss1, rss2 =
                        if x.Session.[0].TargetNodeID = tnodeidx_me.fromPrim 21us then
                            x.Session.[0], x.Session.[1]
                        else
                            x.Session.[1], x.Session.[2]
                    Assert.StrictEqual( tgid_me.fromPrim( 2u ),    rss1.TargetGroupID )
                    Assert.StrictEqual( tnodeidx_me.fromPrim 21us, rss1.TargetNodeID )
                    Assert.StrictEqual( tgid_me.fromPrim( 2u ),    rss2.TargetGroupID )
                    Assert.StrictEqual( tnodeidx_me.fromPrim 22us, rss2.TargetNodeID )
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetSession_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetSession_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 21us;
                                        TargetName = "target002001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in1", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 99us;
                                        TargetName = "target999999";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in2", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target999999", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    let ss3 = new CSession_Stub(
                        p_GetSessionParameter = ( fun _ -> {
                            StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid_me.fromPrim( 1u );
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 21us;
                                        TargetName = "target002001";
                                };
                        } ),
                        p_GetTSIH = ( fun _ -> tsih_me.fromPrim 1us ),
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in3", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_GetCreateDate = ( fun _ -> DateTime.UtcNow )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, ss3 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetSession( TargetDeviceCtrlReq.U_SessInTarget( tnodeidx_me.fromPrim 21us ) )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_SessionList( x ) ->
                    Assert.StrictEqual( 2, x.Session.Length )
                    Assert.StrictEqual( tgid_me.fromPrim( 1u ), x.Session.[0].TargetGroupID )
                    Assert.StrictEqual( tgid_me.fromPrim( 1u ), x.Session.[1].TargetGroupID )
                    Assert.StrictEqual( tnodeidx_me.fromPrim 21us, x.Session.[0].TargetNodeID )
                    Assert.StrictEqual( tnodeidx_me.fromPrim 21us, x.Session.[1].TargetNodeID )
                    let in0 = x.Session.[0].ITNexus.InitiatorName
                    let in1 = x.Session.[1].ITNexus.InitiatorName
                    Assert.True(( ( in0 = "in1" && in1 = "in3" ) || ( in0 = "in1" && in1 = "in3" ) ))
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_DestructSession_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_DestructSession_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_DestructSession( tsih_me.fromPrim 0us )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_DestructSessionResult( x ) ->
                    Assert.StrictEqual( tsih_me.fromPrim 0us, x.TSIH )
                    Assert.False(( x.Result ))
                    Assert.StartsWith( "Unknown session", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_DestructSession_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_DestructSession_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let mutable flg = 0

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetI_TNexus = ( fun _ -> new ITNexus( "in1", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "target002001", tpgt_me.zero ) ),
                        p_DestroySession = ( fun _ -> flg <- flg + 1 )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_DestructSession( tsih_me.fromPrim 1us )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_DestructSessionResult( x ) ->
                    Assert.StrictEqual( tsih_me.fromPrim 1us, x.TSIH )
                    Assert.True(( x.Result ))
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.StrictEqual( 1, flg )
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 3us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 3L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 4us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 4L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTargetDevice()
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let linestr = o.ReadLine()
                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( linestr )
                let wConList =
                    match res1.Response with
                    | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                        x.Connection
                    | _ ->
                        Assert.Fail __LINE__
                        []

                let wl = wConList |> List.sortBy ( fun itr -> itr.ConnectionID )
                Assert.StrictEqual( 5, wl.Length )

                Assert.StrictEqual( tsih_me.fromPrim 1us,             wl.[0].TSIH )
                Assert.StrictEqual( cid_me.fromPrim 0us,              wl.[0].ConnectionID )
                Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[0].ConnectionCount )
                Assert.StrictEqual( 0,                                wl.[0].ReceiveBytesCount.Length )
                Assert.StrictEqual( 0,                                wl.[0].SentBytesCount.Length )
                Assert.StrictEqual( DateTime( 0L, DateTimeKind.Utc ), wl.[0].EstablishTime )

                Assert.StrictEqual( tsih_me.fromPrim 1us,             wl.[1].TSIH )
                Assert.StrictEqual( cid_me.fromPrim 1us,              wl.[1].ConnectionID )
                Assert.StrictEqual( concnt_me.fromPrim 1,             wl.[1].ConnectionCount )
                Assert.StrictEqual( 1,                                wl.[1].ReceiveBytesCount.Length )
                Assert.StrictEqual( 0L,                               wl.[1].ReceiveBytesCount.[0].Value )
                Assert.StrictEqual( 1,                                wl.[1].SentBytesCount.Length )
                Assert.StrictEqual( 0L,                               wl.[1].SentBytesCount.[0].Value )
                Assert.StrictEqual( DateTime( 1L, DateTimeKind.Utc ), wl.[1].EstablishTime )

                Assert.StrictEqual( tsih_me.fromPrim 2us,             wl.[2].TSIH )
                Assert.StrictEqual( cid_me.fromPrim 2us,              wl.[2].ConnectionID )
                Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[2].ConnectionCount )
                Assert.StrictEqual( 1,                                wl.[2].ReceiveBytesCount.Length )
                Assert.StrictEqual( 1L,                               wl.[2].ReceiveBytesCount.[0].Value )
                Assert.StrictEqual( 1,                                wl.[2].SentBytesCount.Length )
                Assert.StrictEqual( 1L,                               wl.[2].SentBytesCount.[0].Value )
                Assert.StrictEqual( DateTime( 2L, DateTimeKind.Utc ), wl.[2].EstablishTime )

                Assert.StrictEqual( tsih_me.fromPrim 2us,             wl.[3].TSIH )
                Assert.StrictEqual( cid_me.fromPrim 3us,              wl.[3].ConnectionID )
                Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[3].ConnectionCount )
                Assert.StrictEqual( 1,                                wl.[3].ReceiveBytesCount.Length )
                Assert.StrictEqual( 2L,                               wl.[3].ReceiveBytesCount.[0].Value )
                Assert.StrictEqual( 1,                                wl.[3].SentBytesCount.Length )
                Assert.StrictEqual( 2L,                               wl.[3].SentBytesCount.[0].Value )
                Assert.StrictEqual( DateTime( 3L, DateTimeKind.Utc ), wl.[3].EstablishTime )

                Assert.StrictEqual( tsih_me.fromPrim 2us,             wl.[4].TSIH )
                Assert.StrictEqual( cid_me.fromPrim 4us,              wl.[4].ConnectionID )
                Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[4].ConnectionCount )
                Assert.StrictEqual( 1,                                wl.[4].ReceiveBytesCount.Length )
                Assert.StrictEqual( 3L,                               wl.[4].ReceiveBytesCount.[0].Value )
                Assert.StrictEqual( 1,                                wl.[4].SentBytesCount.Length )
                Assert.StrictEqual( 3L,                               wl.[4].SentBytesCount.[0].Value )
                Assert.StrictEqual( DateTime( 4L, DateTimeKind.Utc ), wl.[4].EstablishTime )

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) ),
                                    p_NetPortIdx = ( fun () -> netportidx_me.fromPrim 1u )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) ),
                                    p_NetPortIdx = ( fun () -> netportidx_me.fromPrim 2u )
                                ) :> IConnection;
                            |]
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) ),
                                    p_NetPortIdx = ( fun () -> netportidx_me.fromPrim 1u )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInNetworkPortal( netportidx_me.fromPrim 2u )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.StrictEqual( 1, wl.Length )

                    Assert.StrictEqual( tsih_me.fromPrim 1us,             wl.[0].TSIH )
                    Assert.StrictEqual( cid_me.fromPrim 1us,              wl.[0].ConnectionID )
                    Assert.StrictEqual( concnt_me.fromPrim 1,             wl.[0].ConnectionCount )
                    Assert.StrictEqual( 1,                                wl.[0].ReceiveBytesCount.Length )
                    Assert.StrictEqual( 0L,                               wl.[0].ReceiveBytesCount.[0].Value )
                    Assert.StrictEqual( 1,                                wl.[0].SentBytesCount.Length )
                    Assert.StrictEqual( 0L,                               wl.[0].SentBytesCount.[0].Value )
                    Assert.StrictEqual( DateTime( 1L, DateTimeKind.Utc ), wl.[0].EstablishTime )

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let tgid1 = GlbFunc.newTargetGroupID()
                let tgid2 = GlbFunc.newTargetGroupID()
                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid1 }
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test1.defaultSessParam with
                                TargetGroupID = tgid2 }
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTargetGroup( tgid2 )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.StrictEqual( 1, wl.Length )

                    Assert.StrictEqual( tsih_me.fromPrim 2us,             wl.[0].TSIH )
                    Assert.StrictEqual( cid_me.fromPrim 2us,              wl.[0].ConnectionID )
                    Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[0].ConnectionCount )
                    Assert.StrictEqual( 1,                                wl.[0].ReceiveBytesCount.Length )
                    Assert.StrictEqual( 1L,                               wl.[0].ReceiveBytesCount.[0].Value )
                    Assert.StrictEqual( 1,                                wl.[0].SentBytesCount.Length )
                    Assert.StrictEqual( 1L,                               wl.[0].SentBytesCount.[0].Value )
                    Assert.StrictEqual( DateTime( 2L, DateTimeKind.Utc ), wl.[0].EstablishTime )

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test1.defaultSessParam with
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 10us;
                                }
                            }
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test1.defaultSessParam with
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 10us;
                                }
                            }
                        )
                    )
                    let ss3 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 3us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        ),
                        p_GetSessionParameter = ( fun () ->
                            { StatusMaster_Test1.defaultSessParam with
                                TargetConf = {
                                    StatusMaster_Test1.defaultSessParam.TargetConf with
                                        IdentNumber = tnodeidx_me.fromPrim 1us;
                                }
                            }
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 3us, ss3 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTarget( tnodeidx_me.fromPrim 10us )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.StrictEqual( 2, wl.Length )

                    Assert.StrictEqual( tsih_me.fromPrim 1us,             wl.[0].TSIH )
                    Assert.StrictEqual( cid_me.fromPrim 0us,              wl.[0].ConnectionID )
                    Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[0].ConnectionCount )
                    Assert.StrictEqual( 0,                                wl.[0].ReceiveBytesCount.Length )
                    Assert.StrictEqual( 0,                                wl.[0].SentBytesCount.Length )
                    Assert.StrictEqual( DateTime( 0L, DateTimeKind.Utc ), wl.[0].EstablishTime )

                    Assert.StrictEqual( tsih_me.fromPrim 2us,             wl.[1].TSIH )
                    Assert.StrictEqual( cid_me.fromPrim 1us,              wl.[1].ConnectionID )
                    Assert.StrictEqual( concnt_me.fromPrim 1,             wl.[1].ConnectionCount )
                    Assert.StrictEqual( 1,                                wl.[1].ReceiveBytesCount.Length )
                    Assert.StrictEqual( 0L,                               wl.[1].ReceiveBytesCount.[0].Value )
                    Assert.StrictEqual( 1,                                wl.[1].SentBytesCount.Length )
                    Assert.StrictEqual( 0L,                               wl.[1].SentBytesCount.[0].Value )
                    Assert.StrictEqual( DateTime( 1L, DateTimeKind.Utc ), wl.[1].EstablishTime )

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_005() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 1us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 1 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 0L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 1L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    let ss2 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 2us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 2us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                                    p_GetSentBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 1L; } |] ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 2L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 2us, ss2 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInSession( tsih_me.fromPrim 1us )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    let wl = x.Connection |> List.sortBy ( fun itr -> itr.ConnectionID )
                    Assert.StrictEqual( 2, wl.Length )

                    Assert.StrictEqual( tsih_me.fromPrim 1us,             wl.[0].TSIH )
                    Assert.StrictEqual( cid_me.fromPrim 0us,              wl.[0].ConnectionID )
                    Assert.StrictEqual( concnt_me.fromPrim 0,             wl.[0].ConnectionCount )
                    Assert.StrictEqual( 0,                                wl.[0].ReceiveBytesCount.Length )
                    Assert.StrictEqual( 0,                                wl.[0].SentBytesCount.Length )
                    Assert.StrictEqual( DateTime( 0L, DateTimeKind.Utc ), wl.[0].EstablishTime )

                    Assert.StrictEqual( tsih_me.fromPrim 1us,             wl.[1].TSIH )
                    Assert.StrictEqual( cid_me.fromPrim 1us,              wl.[1].ConnectionID )
                    Assert.StrictEqual( concnt_me.fromPrim 1,             wl.[1].ConnectionCount )
                    Assert.StrictEqual( 1,                                wl.[1].ReceiveBytesCount.Length )
                    Assert.StrictEqual( 0L,                               wl.[1].ReceiveBytesCount.[0].Value )
                    Assert.StrictEqual( 1,                                wl.[1].SentBytesCount.Length )
                    Assert.StrictEqual( 0L,                               wl.[1].SentBytesCount.[0].Value )
                    Assert.StrictEqual( DateTime( 1L, DateTimeKind.Utc ), wl.[1].EstablishTime )

                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_006() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_006"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let m_sessions1 =
                    let ss1 = new CSession_Stub(
                        p_GetAllConnections = ( fun () ->
                            [| 
                                new CConnection_Stub(
                                    p_TSIH = ( fun () -> tsih_me.fromPrim 1us ),
                                    p_CID = ( fun () -> cid_me.fromPrim 0us ),
                                    p_ConCounter = ( fun () -> concnt_me.fromPrim 0 ),
                                    p_GetReceiveBytesCount = ( fun () -> Array.empty ),
                                    p_GetSentBytesCount = ( fun () -> Array.empty ),
                                    p_CurrentParams = ( fun () -> StatusMaster_Test1.defaultConParam ),
                                    p_ConnectedDate = ( fun () -> DateTime( 0L, DateTimeKind.Utc ) )
                                ) :> IConnection;
                            |]
                        )
                    )
                    [
                        KeyValuePair< TSIH_T, ISession >( tsih_me.fromPrim 1us, ss1 );
                    ]
                    |> ImmutableDictionary.CreateRange< TSIH_T, ISession >
                    |> OptimisticLock
                let pc = PrivateCaller( sm )
                pc.SetField( "m_Sessions", m_sessions1 )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInSession( tsih_me.fromPrim 9999us )
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    Assert.StrictEqual( 0, x.Connection.Length )
                | _ ->
                    Assert.Fail __LINE__

                pc.SetField( "m_Sessions", OptimisticLock( ImmutableDictionary< TSIH_T, ISession >.Empty ) )
                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetConnection_007() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetConnection_007"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetConnection(
                        TargetDeviceCtrlReq.U_ConInTargetDevice()
                    )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_ConnectionList( x ) ->
                    Assert.StrictEqual( 0, x.Connection.Length )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 0UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 0UL, x.LUN )
                    Assert.StartsWith( "Missing LU", x.ErrorMessage )
                    Assert.True(( x.LUStatus_Success.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True( x.LUStatus_Success.IsSome )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.ReadBytesCount.Length )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.WrittenBytesCount.Length )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.ReadTickCount.Length )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.WriteTickCount.Length )
                    Assert.True( x.LUStatus_Success.Value.ACAStatus.IsNone )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lazy( new CLU_Stub() :> ILU ) )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True( x.LUStatus_Success.IsSome )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.ReadBytesCount.Length )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.WrittenBytesCount.Length )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.ReadTickCount.Length )
                    Assert.StrictEqual( 0, x.LUStatus_Success.Value.WriteTickCount.Length )
                    Assert.True( x.LUStatus_Success.Value.ACAStatus.IsNone )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ProcCtrlReq_GetLUStatus_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetLUStatus_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let itn1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )

                let m_LUs1 =
                    let lu1 = lazy (
                        new CLU_Stub(
                            p_GetReadBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                            p_GetWrittenBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 1L; } |] ),
                            p_GetReadTickCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 1L; } |] ),
                            p_GetWriteTickCount = ( fun () -> [| { Time = DateTime(); Value = 4L; Count = 1L; } |] ),
                            p_ACAStatus = ( fun () ->  ValueSome ( itn1, ScsiCmdStatCd.CHECK_CONDITION, SenseKeyCd.NOT_READY, ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT, true ) ),
                            p_TaskDescStrings = ( fun () -> [||] )
                        ) :> ILU
                    )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetLUStatus( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                let res2 = res1.Response
                match res2 with
                | TargetDeviceCtrlRes.T_Response.U_LUStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True( x.LUStatus_Success.IsSome )
                    let sucVal = x.LUStatus_Success.Value
                    Assert.StrictEqual( 1,  sucVal.ReadBytesCount.Length )
                    Assert.StrictEqual( 1L, sucVal.ReadBytesCount.[0].Value )
                    Assert.StrictEqual( 1,  sucVal.WrittenBytesCount.Length )
                    Assert.StrictEqual( 2L, sucVal.WrittenBytesCount.[0].Value )
                    Assert.StrictEqual( 1,  sucVal.ReadTickCount.Length )
                    Assert.StrictEqual( 3L, sucVal.ReadTickCount.[0].Value )
                    Assert.StrictEqual( 1,  sucVal.WriteTickCount.Length )
                    Assert.StrictEqual( 4L, sucVal.WriteTickCount.[0].Value )
                    Assert.True( sucVal.ACAStatus.IsSome )
                    let acaVal = sucVal.ACAStatus.Value
                    Assert.StrictEqual( "initiator000",                              acaVal.ITNexus.InitiatorName )
                    Assert.StrictEqual( uint8 ScsiCmdStatCd.CHECK_CONDITION,         acaVal.StatusCode )
                    Assert.StrictEqual( uint16 ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT, acaVal.AdditionalSenseCode )
                    Assert.True( acaVal.IsCurrent )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 99UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 99UL, x.LUN )
                    Assert.False( x.Result )
                    Assert.StartsWith( "Specified LU is not configured", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lazy( new CLU_Stub() :> ILU ) )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.True( x.Result )
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_LUReset_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_LUReset_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )
                let mutable flg = false

                let m_LUs1 =
                    let lu1 = lazy (
                        new CLU_Stub(
                            p_LogicalUnitReset = ( fun s itt needResp ->
                                Assert.True(( s.IsNone ))
                                Assert.True(( itt.IsNone ))
                                Assert.False needResp
                                flg <- true
                            )
                        ) :> ILU
                    )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_LUReset( lun_me.fromPrim 1UL )
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_LUResetResult( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.True(( x.Result ))
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True(( flg ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 99UL;
                        ID = mediaidx_me.fromPrim 99u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 99UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 99u, x.ID )
                    Assert.StartsWith( "Specified LU is not configured",  x.ErrorMessage )
                    Assert.True(( x.MediaStatus_Success.IsNone ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 99u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 99u, x.ID )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True(( x.MediaStatus_Success.IsSome ))
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lazy( new CLU_Stub() :> ILU ) )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 99u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 99u, x.ID )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True( x.MediaStatus_Success.IsSome )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    let media1 =
                        new CMedia_Stub(
                            p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 1u ),
                            p_GetSubMedia = ( fun () -> [] ),
                            p_GetReadBytesCount = ( fun () -> Array.empty ),
                            p_GetWrittenBytesCount = ( fun () -> Array.empty ),
                            p_GetReadTickCount = ( fun () -> Array.empty ),
                            p_GetWriteTickCount = ( fun () -> Array.empty )
                        )
                    let lu1 = lazy ( new CLU_Stub( p_GetMedia = ( fun () -> media1 ) ) :> ILU )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 1u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 1u, x.ID )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True( x.MediaStatus_Success.IsSome )
                    Assert.StrictEqual( 0, x.MediaStatus_Success.Value.ReadBytesCount.Length )
                    Assert.StrictEqual( 0, x.MediaStatus_Success.Value.WrittenBytesCount.Length )
                    Assert.StrictEqual( 0, x.MediaStatus_Success.Value.ReadTickCount.Length )
                    Assert.StrictEqual( 0, x.MediaStatus_Success.Value.WriteTickCount.Length )

                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_GetMediaStatus_005() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_GetMediaStatus_005"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    let media4 =
                        new CMedia_Stub(
                            p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 4u ),
                            p_GetSubMedia = ( fun () -> [] ),
                            p_GetReadBytesCount = ( fun () -> [| { Time = DateTime(); Value = 1L; Count = 0L; } |] ),
                            p_GetWrittenBytesCount = ( fun () -> [| { Time = DateTime(); Value = 2L; Count = 0L; } |] ),
                            p_GetReadTickCount = ( fun () -> [| { Time = DateTime(); Value = 3L; Count = 0L; } |] ),
                            p_GetWriteTickCount = ( fun () -> [| { Time = DateTime(); Value = 4L; Count = 0L; } |] )
                        )
                    let media3 = new CMedia_Stub(
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 3u ),
                        p_GetSubMedia = ( fun () -> [ media4 ] )
                    )
                    let media2 = new CMedia_Stub( 
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 2u ),
                        p_GetSubMedia = ( fun () -> [] )
                    )
                    let media1 = new CMedia_Stub( 
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 1u ),
                        p_GetSubMedia = ( fun () -> [ media2; media3; ] )
                    )
                    let lu1 = lazy ( new CLU_Stub( p_GetMedia = ( fun () -> media1 ) ) :> ILU )
                    lu1.Force() |> ignore
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_GetMediaStatus( { 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 4u
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaStatus( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 4u, x.ID )
                    Assert.StrictEqual( "", x.ErrorMessage )
                    Assert.True( x.MediaStatus_Success.IsSome )
                    Assert.StrictEqual( 1,  x.MediaStatus_Success.Value.ReadBytesCount.Length )
                    Assert.StrictEqual( 1L, x.MediaStatus_Success.Value.ReadBytesCount.[0].Value )
                    Assert.StrictEqual( 1,  x.MediaStatus_Success.Value.WrittenBytesCount.Length )
                    Assert.StrictEqual( 2L, x.MediaStatus_Success.Value.WrittenBytesCount.[0].Value )
                    Assert.StrictEqual( 1,  x.MediaStatus_Success.Value.ReadTickCount.Length )
                    Assert.StrictEqual( 3L, x.MediaStatus_Success.Value.ReadTickCount.[0].Value )
                    Assert.StrictEqual( 1,  x.MediaStatus_Success.Value.WriteTickCount.Length )
                    Assert.StrictEqual( 4L, x.MediaStatus_Success.Value.WriteTickCount.[0].Value )

                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_001() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_001"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 99UL;
                        ID = mediaidx_me.fromPrim 4u;
                        Request = "aaaaaaa";
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 99UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 4u, x.ID )
                    Assert.StartsWith( "Specified LU is not configured", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_002() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_002"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 4u;
                        Request = "aaaaaaa";
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 4u, x.ID )
                    Assert.StartsWith( "Specified media missing", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_003() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_003"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )

        let tgConfStr =
            ( {
                TargetGroupID = tgid_me.fromPrim 0u;
                TargetGroupName = "a-000";
                EnabledAtStart = true;
                Target = 
                    [{
                        IdentNumber = tnodeidx_me.fromPrim 10us;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "target000";
                        TargetAlias = "target000";
                        LUN = [ lun_me.fromPrim 1UL ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }];
                LogicalUnit =
                    [{
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "luname";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_BlockDevice({
                            Peripheral = TargetGroupConf.U_DebugMedia({
                                IdentNumber = mediaidx_me.fromPrim 1u;
                                MediaName = "debugmedia";
                                Peripheral = TargetGroupConf.U_DummyMedia({
                                    IdentNumber = mediaidx_me.fromPrim 2u;
                                    MediaName = "dummymedia";
                                })
                            })
                            OptimalTransferLength = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH;
                        });
                    }];
            } : TargetGroupConf.T_TargetGroup )
            |> TargetGroupConf.ReaderWriter.ToString
        File.WriteAllText( targetGroupConfName0, tgConfStr )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let mediaCtrlReqStr =
                    MediaCtrlReq.ReaderWriter.ToString {
                        Request = MediaCtrlReq.U_Debug(
                            MediaCtrlReq.U_ClearTraps()
                        )
                    }

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 1u;
                        Request = mediaCtrlReqStr;
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 1u, x.ID )
                    let resData = MediaCtrlRes.ReaderWriter.LoadString x.Response
                    match resData.Response with
                    | MediaCtrlRes.U_Debug( x ) ->
                        match x with
                        | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                            Assert.True(( y.Result ))
                            Assert.StrictEqual( "", y.ErrorMessage )
                        | _ ->
                            Assert.Fail __LINE__
                    | _ ->
                        Assert.Fail __LINE__
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.ProcCtrlReq_MediaControlRequest_004() =
        let pDirName = this.GetTestDirName "ProcCtrlReq_MediaControlRequest_004"
        GlbFunc.CreateDir pDirName |> ignore

        let targetDeviceConfName = StatusMaster_Test1.CreateEmptyTDConf pDirName
        let targetGroupConfName0 = Functions.AppendPathName pDirName ( tgid_me.toString tgid0 )
        File.WriteAllText( targetGroupConfName0, defaultTargetGroupConfStr 0 true )

        let rq_out, rq_in = GlbFunc.CreateAnonymousPipe()
        let rs_out, rs_in = GlbFunc.CreateAnonymousPipe()
        let killer = new HKiller() :> IKiller
        let sm = new StatusMaster( pDirName, true, killer, new StreamReader( rq_in ), new StreamWriter( rs_out ) ) :> IStatus

        [|
            fun () -> task {
                do! sm.ProcessControlRequest()
            };
            fun () -> task {
                let s = new StreamWriter( rq_out )
                let o = new StreamReader( rs_in )

                let m_LUs1 =
                    let media1 = new CMedia_Stub( 
                        p_GetMediaIndex = ( fun () -> mediaidx_me.fromPrim 1u ),
                        p_GetSubMedia = ( fun () -> [] ),
                        p_MediaControl = ( fun request -> task {
                            return MediaCtrlRes.U_Debug(
                                MediaCtrlRes.U_ClearTrapsResult({
                                    Result = true;
                                    ErrorMessage = "ggggg";
                                })
                            )
                        })
                    )
                    let lu1 = lazy (
                        new CLU_Stub(
                            p_GetMedia = ( fun () -> media1 ),
                            p_GetLUResetStatus = ( fun () -> false )
                        ) :> ILU
                    )
                    [
                        KeyValuePair< LUN_T, Lazy<ILU> >( lun_me.fromPrim 1UL, lu1 )
                    ]
                let pc = PrivateCaller( sm )
                pc.SetField( "m_LU", OptimisticLock( m_LUs1.ToImmutableDictionary() ) )

                let mediaCtrlReqStr =
                    MediaCtrlReq.ReaderWriter.ToString {
                        Request = MediaCtrlReq.U_Debug(
                            MediaCtrlReq.U_ClearTraps()
                        )
                    }

                let req1 : TargetDeviceCtrlReq.T_TargetDeviceCtrlReq = {
                    Request = TargetDeviceCtrlReq.T_Request.U_MediaControlRequest({ 
                        LUN = lun_me.fromPrim 1UL;
                        ID = mediaidx_me.fromPrim 1u;
                        Request = mediaCtrlReqStr;
                    })
                }
                s.WriteLine( TargetDeviceCtrlReq.ReaderWriter.ToString req1 )
                s.Flush()

                let res1 = TargetDeviceCtrlRes.ReaderWriter.LoadString( o.ReadLine() )
                match res1.Response with
                | TargetDeviceCtrlRes.T_Response.U_MediaControlResponse( x ) ->
                    Assert.StrictEqual( lun_me.fromPrim 1UL, x.LUN )
                    Assert.StrictEqual( mediaidx_me.fromPrim 1u, x.ID )
                    let resData = MediaCtrlRes.ReaderWriter.LoadString x.Response
                    match resData.Response with
                    | MediaCtrlRes.U_Debug( x ) ->
                        match x with
                        | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                            Assert.True(( y.Result ))
                            Assert.StrictEqual( "ggggg", y.ErrorMessage )
                        | _ ->
                            Assert.Fail __LINE__
                    | _ ->
                        Assert.Fail __LINE__
                    Assert.StrictEqual( "", x.ErrorMessage )
                | _ ->
                    Assert.Fail __LINE__

                GlbFunc.AllDispose [ rq_out; rq_in; rs_out; rs_in; ]
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        killer.NoticeTerminate()
        GlbFunc.DeleteFile targetDeviceConfName
        GlbFunc.DeleteFile targetGroupConfName0
        GlbFunc.DeleteDir pDirName
