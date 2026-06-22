//=============================================================================
// Haruka Software Storage.
// CommandRunnerTest4.fs : Test cases for CommandRunner class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Text
open System.Text.RegularExpressions

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type CommandRunner_Test4() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let GenCommandStream ( txt : string ) =
        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )
        ws.WriteLine( txt )
        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let rs = new StreamReader( ms )
        ms, ws, rs

    let GenOutputStream() =
        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )
        ms, ws

    let CheckOutputMessage ( ms : MemoryStream ) ( ws : StreamWriter ) ( pronpt : string ) ( msg : string ) : StreamReader =
        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( ms )
        let esc ( s : string ) =
            let sb = StringBuilder()
            for itr in s do
                if String.exists ( (=) itr ) "=$^{[(|)*+?\\" then
                    sb.Append '\\' |> ignore
                sb.Append itr |> ignore
            sb.ToString()
        let outline = out_rs.ReadLine()
        let reg = Regex( sprintf "^%s> *%s.*$" ( esc pronpt ) ( esc msg ) )
        Assert.True(( reg.IsMatch outline ))
        out_rs

    let CallCommandLoop ( cr : CommandRunner ) ( stat : ( ServerStatus * CtrlConnection * IConfigureNode ) option ) : ( bool * ( ServerStatus * CtrlConnection * IConfigureNode ) option ) =
        let pc = PrivateCaller( cr )
        let struct( r, stat ) =
            pc.Invoke( "CommandLoop", stat )
            :?> Task< struct( bool * ( ServerStatus * CtrlConnection * IConfigureNode ) option ) >
            |> Functions.RunTaskSynchronously
        ( r, stat )

    let GenStub ( cmdstr : string ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream cmdstr
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc 

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.MediaStatus_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.MediaStatus_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.MediaStatus_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> None )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.MediaStatus_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.MediaStatus_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.MediaStatus_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.MediaStatus_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.MediaStatus_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_GetMediaStatus <- ( fun tdid lun mdid ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                flg4 <- true
                return {
                    ReadBytesCount = [];
                    WrittenBytesCount = [];
                    ReadTickCount = [];
                    WriteTickCount = [];
                }
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Media Status"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                Assert.True(( event.IsU_TestUnitReady ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e ReadCapacity /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                Assert.True(( event.IsU_ReadCapacity ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e Read /slba 11 /elba 22 /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match event with
                | MediaCtrlReq.U_Read( x ) ->
                    Assert.StrictEqual( 11UL, x.StartLBA )
                    Assert.StrictEqual( 22UL, x.EndLBA )
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e Write /slba 33 /elba 44 /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match event with
                | MediaCtrlReq.U_Write( x ) ->
                    Assert.StrictEqual( 33UL, x.StartLBA )
                    Assert.StrictEqual( 44UL, x.EndLBA )
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e Format /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                Assert.True( event.IsU_Format )
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match action with
                | MediaCtrlReq.U_ACA( x ) ->
                    Assert.True(( x = "abc" ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a LUReset /msg xyz" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match action with
                | MediaCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = "xyz" ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Count /idx 75" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match action with
                | MediaCtrlReq.U_Count( x ) ->
                    Assert.True(( x = 75 ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_009 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match action with
                | MediaCtrlReq.U_Delay( x ) ->
                    Assert.True(( x = 45 ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_010 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Wait" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                match action with
                | MediaCtrlReq.U_Wait() ->
                    ()
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_011 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ ->  None )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.AddTrap_012 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ ->  Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.AddTrap_013 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )
        ss.p_GetAncestorLogicalUnit <- ( fun _ ->  Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.AddTrap_014 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return []  } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_015 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ]  } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


    [<Fact>]
    member _.AddTrap_016 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_017 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = 
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_ClearTraps <- ( fun tdid lun mdid ->
            task {
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
                Assert.StrictEqual( ( medianode :> IMediaNode ).IdentNumber, mdid )
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Traps cleared."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.ClearTrap_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> None )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.ClearTrap_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.ClearTrap_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_GetAllTraps <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                flg4 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Registered traps"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_GetAllTraps <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                flg4 <- true
                return [
                    {
                        Event = MediaCtrlRes.U_TestUnitReady();
                        Action = MediaCtrlRes.U_ACA( "abc" );
                    }
                ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Registered traps"
        let outline = ( out_rs.ReadLine() ).TrimStart()
        Assert.True(( outline.StartsWith "TestUnitReady" ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Traps_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Traps_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> None )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Traps_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_009 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskList_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_GetTaskWaitStatus <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                flg4 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Task wait status"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskList_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false
        
        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_GetTaskWaitStatus <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                flg4 <- true
                return [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ITT = itt_me.fromPrim 2u;
                        Description = "aaaa";
                    }
                ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Task wait status"
        let outline = ( out_rs.ReadLine() ).TrimStart()
        Assert.True(( outline.StartsWith "aaaa" ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskList_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.TaskList_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.TaskList_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> None )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.TaskList_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskList_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskList_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskList_009 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task list" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskResume_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        cc.p_DebugMedia_Resume <- ( fun tdid lun mdid tsih itt ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                Assert.True(( tsih = tsih_me.fromPrim 0us ))
                Assert.True(( itt = itt_me.fromPrim 1u ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True( flg4 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Task( TSIH=0, ITT=1 ) resumed."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskResume_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.TaskResume_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> None )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.TaskResume_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, medianode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.TaskResume_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskResume_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskResume_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.TaskResume_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "task resume 0 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        ss.p_GetAncestorLogicalUnit <- ( fun _ -> Some lunode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

