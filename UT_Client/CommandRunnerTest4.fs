namespace Haruka.Test.UT.Client

open System
open System.IO
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Net
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test


type CommandRunner_Test4() =

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

    let CheckOutputMessage ( ms : MemoryStream ) ( ws : StreamWriter ) ( expmsg : string ) =
        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( ms )
        let outline = out_rs.ReadLine()
        Assert.True(( outline.StartsWith expmsg ))
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



    [<Fact>]
    member _.MediaStatus_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            None
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.MediaStatus_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            None
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.MediaStatus_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg3 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.MediaStatus_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "mediastatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetMediaStatus <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
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
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Media Status"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                Assert.True(( event.IsU_TestUnitReady ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e ReadCapacity /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                Assert.True(( event.IsU_ReadCapacity ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e Read /slba 11 /elba 22 /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                match event with
                | MediaCtrlReq.U_Read( x ) ->
                    Assert.True(( x.StartLBA = 11UL ))
                    Assert.True(( x.EndLBA = 22UL ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e Write /slba 33 /elba 44 /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                match event with
                | MediaCtrlReq.U_Write( x ) ->
                    Assert.True(( x.StartLBA = 33UL ))
                    Assert.True(( x.EndLBA = 44UL ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e Format /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                Assert.True(( event.IsU_Format ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a ACA /msg abc" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                match action with
                | MediaCtrlReq.U_ACA( x ) ->
                    Assert.True(( x = "abc" ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a LUReset /msg xyz" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                match action with
                | MediaCtrlReq.U_LUReset( x ) ->
                    Assert.True(( x = "xyz" ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Count /idx 75" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                match action with
                | MediaCtrlReq.U_Count( x ) ->
                    Assert.True(( x = 75 ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_009 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid event action ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                match action with
                | MediaCtrlReq.U_Delay( x ) ->
                    Assert.True(( x = 45 ))
                | _ ->
                    Assert.Fail __LINE__
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Trap added."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddTrap_010 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            None
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.AddTrap_011 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            None
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.AddTrap_012 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "add trap /e TestUnitReady /a Delay /ms 45" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg3 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DebugMedia_ClearTraps <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdn.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( medianode :> IMediaNode ).IdentNumber ))
                flg4 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Traps cleared."
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.ClearTrap_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            None
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.ClearTrap_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            None
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.ClearTrap_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "clear trap" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
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

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Registered traps"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg4 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
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

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg4 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> Registered traps"
        let outline = out_rs.ReadLine()
        Assert.True(( outline.StartsWith "  TestUnitReady" ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Traps_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            None
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Traps_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            None
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        try
            let _ = CallCommandLoop cr ( Some ( ss, cc, medianode ) )
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Traps_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "traps" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let medianode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = medianode ))
            flg2 <- true
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg3 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, medianode ) )

        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, medianode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD> ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


