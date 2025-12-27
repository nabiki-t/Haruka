//=============================================================================
// Haruka Software Storage.
// CommandRunnerTest2.fs : Test cases for CommandRunner class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Net.Sockets
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

type CommandRunner_Test2() =

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

    let defTDNegoParam : TargetDeviceConf.T_NegotiableParameters = {
            MaxRecvDataSegmentLength = 0u;
            MaxBurstLength = 0u;
            FirstBurstLength = 0u;
            DefaultTime2Wait = 0us;
            DefaultTime2Retain = 0us;
            MaxOutstandingR2T = 0us;
        }
    let defTDLogParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = 0u;
            HardLimit = 0u;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.exit_001() =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "exit" )
        let tnode = CommandRunner_Test1.m_ControllerNode
        let mutable flg1 = false

        ss.p_IsModified <- false
        cc.p_Logout <- ( fun () ->
            task{
                flg1 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.False(( r ))
        Assert.True(( stat.IsNone ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.exit_002() =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "exit" )
        let tnode = CommandRunner_Test1.m_ControllerNode

        ss.p_IsModified <- true

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tnode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_CONFIG_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.exit_003() =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "exit /y" )
        let tnode = CommandRunner_Test1.m_ControllerNode
        let mutable flg1 = false

        ss.p_IsModified <- true
        cc.p_Logout <- ( fun () ->
            task{
                flg1 <- true
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.False(( r ))
        Assert.True(( stat.IsNone ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.exit_004() =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "exit" )
        let tnode = CommandRunner_Test1.m_ControllerNode
        let mutable flg1 = false

        ss.p_IsModified <- false
        cc.p_Logout <- ( fun () ->
            task{
                flg1 <- true
                raise <| RequestError( "" )
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.False(( r ))
        Assert.True(( stat.IsNone ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDERR_UNEXPECTED_REQUEST_ERROR"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.exit_005() =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "exit" )
        let tnode = CommandRunner_Test1.m_ControllerNode
        let mutable flg1 = false

        ss.p_IsModified <- false
        cc.p_Logout <- ( fun () ->
            task{
                flg1 <- true
                raise <| SocketException( 0 )
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.False(( r ))
        Assert.True(( stat.IsNone ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDERR_CONNECTION_ERROR"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "reload", false )>]
    [<InlineData( "reload /y", true )>]
    member _.reload_001 ( cmdstr: string ) ( modifiedFlg : bool ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let tnode = CommandRunner_Test1.m_ControllerNode
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_IsModified <- modifiedFlg
        cc.p_GetControllerConfig <- ( fun () ->
            task {
                flg1 <- true
                let conf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = None;
                    LogMaintenance = None;
                    LogParameters = None;
                }
                return conf
            }
        )
        cc.p_GetTargetDeviceDir <- ( fun () ->
            task {
                flg2 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus <> r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( tnode <> r_cn ))
        match r_cn with
        | :? ConfNode_Controller -> ()
        | _ -> Assert.Fail __LINE__

        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.reload_002() =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "reload" )
        let tnode = CommandRunner_Test1.m_ControllerNode

        ss.p_IsModified <- true

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tnode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_CONFIG_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.select_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "select 0" )
        let tnode = CommandRunner_Test1.m_ControllerNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tnode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_MISSING_NODE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.select_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "select 0" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let dm = new ConfNode_DummyMedia( new StringTable( "" ), cnr, cnr.NextID, mediaidx_me.zero, "" ) :> IConfigureNode
        cnr.AddNode cn
        cnr.AddNode dm
        cnr.AddRelation cn.NodeID dm.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, dm ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.unselect_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unselect" )
        let tnode = CommandRunner_Test1.m_ControllerNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tnode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.unselect_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unselect" )
        let cnr = new ConfNodeRelation()
        let p1 = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let p2 = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let cn = new ConfNode_DummyMedia( new StringTable( "" ), cnr, cnr.NextID, mediaidx_me.zero, "" ) :> IConfigureNode
        cnr.AddNode p1
        cnr.AddNode p2
        cnr.AddNode cn
        cnr.AddRelation p1.NodeID cn.NodeID
        cnr.AddRelation p2.NodeID cn.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, p1 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.unselect_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unselect /p 1" )
        let cnr = new ConfNodeRelation()
        let p1 = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let p2 = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let cn = new ConfNode_DummyMedia( new StringTable( "" ), cnr, cnr.NextID, mediaidx_me.zero, "" ) :> IConfigureNode
        cnr.AddNode p1
        cnr.AddNode p2
        cnr.AddNode cn
        cnr.AddRelation p1.NodeID cn.NodeID
        cnr.AddRelation p2.NodeID cn.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, p2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.unselect_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unselect /p 1" )

        let cnr = new ConfNodeRelation()
        let p1 = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let cn = new ConfNode_DummyMedia( new StringTable( "" ), cnr, cnr.NextID, mediaidx_me.zero, "" ) :> IConfigureNode
        cnr.AddNode p1
        cnr.AddNode cn
        cnr.AddRelation p1.NodeID cn.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_MISSING_NODE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.list_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "list" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let c1 = new ConfNode_DummyMedia( new StringTable( "" ), cnr, cnr.NextID, mediaidx_me.zero, "" ) :> IConfigureNode
        cnr.AddNode cn
        cnr.AddNode c1
        cnr.AddRelation cn.NodeID c1.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ( "0 : " + c1.ShortDescriptString )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.list_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "list" )
        let tnode = CommandRunner_Test1.m_ControllerNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tnode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_MISSING_CHILD_NODE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.llistparent_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "listparent" )
        let cnr = new ConfNodeRelation()
        let p1 = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        let cn = new ConfNode_DummyMedia( new StringTable( "" ), cnr, cnr.NextID, mediaidx_me.zero, "" ) :> IConfigureNode
        cnr.AddNode p1
        cnr.AddNode cn
        cnr.AddRelation p1.NodeID cn.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ( "0 : " + p1.ShortDescriptString )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.llistparent_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "listparent" )
        let tnode = CommandRunner_Test1.m_ControllerNode
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tnode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_MISSING_PARENT_NODE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set REMOTECTRL.PORTNUMBER 999" )>]
    [<InlineData( "set remotectrl.portnumber 999" )>]
    [<InlineData( "set REMOTECTRL:PORTNUMBER 999" )>]
    [<InlineData( "set REMOTECTRL_PORTNUMBER 999" )>]
    [<InlineData( "set REMOTECTRL-PORTNUMBER 999" )>]
    [<InlineData( "set REMOTECTRL/PORTNUMBER 999" )>]
    [<InlineData( "set REMOTECTRL;PORTNUMBER 999" )>]
    [<InlineData( "set PORTNUMBER 999" )>]
    member _.set_Controller_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = Some {
                PortNum = 0us
                Address = "";
                WhiteList = [];
            }
            LogMaintenance = None;
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            Assert.True(( newConf.RemoteCtrl.Value.PortNum = 999us ))
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue.PortNum = 999us ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue.Address = "" ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue = initnode.LogMaintenanceValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set REMOTECTRL.PORTNUMBER -1" )>]
    [<InlineData( "set REMOTECTRL.PORTNUMBER 65536" )>]
    [<InlineData( "set REMOTECTRL.PORTNUMBER ***" )>]
    member _.set_Controller_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set REMOTECTRL.ADDRESS aaa" )>]
    [<InlineData( "set ADDRESS aaa" )>]
    member _.set_Controller_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = Some {
                PortNum = 0us
                Address = "";
                WhiteList = [];
            }
            LogMaintenance = None;
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            Assert.True(( newConf.RemoteCtrl.Value.Address = "aaa" ))
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue.PortNum = 0us ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue.Address = "aaa" ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue = initnode.LogMaintenanceValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.OUTPUTSTDOUT true" )>]
    [<InlineData( "set OUTPUTSTDOUT true" )>]
    member _.set_Controller_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToFile( {
                    TotalLimit = 987u;
                    MaxFileCount = 0u;
                    ForceSync = true;
                })
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToStdout( x ) ->
                Assert.True(( x = 987u ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( x = 987u ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.OUTPUTSTDOUT false" )>]
    [<InlineData( "set OUTPUTSTDOUT false" )>]
    member _.set_Controller_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToFile( {
                    TotalLimit = 987u;
                    MaxFileCount = 123u;
                    ForceSync = true;
                })
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 987u ))
                Assert.True(( x.MaxFileCount = 123u ))
                Assert.True(( x.ForceSync = true ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 987u ))
            Assert.True(( x.MaxFileCount = 123u ))
            Assert.True(( x.ForceSync = true ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.OUTPUTSTDOUT true" )>]
    [<InlineData( "set OUTPUTSTDOUT true" )>]
    member _.set_Controller_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToStdout( 876u );
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToStdout( x ) ->
                Assert.True(( x = 876u ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( x = 876u ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.OUTPUTSTDOUT false" )>]
    [<InlineData( "set OUTPUTSTDOUT false" )>]
    member _.set_Controller_007 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToStdout( 876u );
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 876u ))
                Assert.True(( x.MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT ))
                Assert.True(( x.ForceSync = Constants.LOGMNT_DEF_FORCESYNC ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 876u ))
            Assert.True(( x.MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT ))
            Assert.True(( x.ForceSync = Constants.LOGMNT_DEF_FORCESYNC ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set OUTPUTSTDOUT -1" )>]
    [<InlineData( "set OUTPUTSTDOUT 4294967296" )>]
    [<InlineData( "set OUTPUTSTDOUT aaa" )>]
    member _.set_Controller_008 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.TOTALLIMIT 123" )>]
    [<InlineData( "set TOTALLIMIT 123" )>]
    member _.set_Controller_009 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToFile( {
                    TotalLimit = 987u;
                    MaxFileCount = 222u;
                    ForceSync = true;
                })
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 123u ))
                Assert.True(( x.MaxFileCount = 222u ))
                Assert.True(( x.ForceSync = true ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 123u ))
            Assert.True(( x.MaxFileCount = 222u ))
            Assert.True(( x.ForceSync = true ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.TOTALLIMIT 123" )>]
    [<InlineData( "set TOTALLIMIT 123" )>]
    member _.set_Controller_010 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToStdout( 876u );
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToStdout( x ) ->
                Assert.True(( x = 123u ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( x = 123u ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set TOTALLIMIT -1" )>]
    [<InlineData( "set TOTALLIMIT 4294967296" )>]
    [<InlineData( "set TOTALLIMIT aaa" )>]
    member _.set_Controller_011 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.MAXFILECOUNT 456" )>]
    [<InlineData( "set MAXFILECOUNT 456" )>]
    member _.set_Controller_012 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToFile( {
                    TotalLimit = 987u;
                    MaxFileCount = 222u;
                    ForceSync = true;
                })
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 987u ))
                Assert.True(( x.MaxFileCount = 456u ))
                Assert.True(( x.ForceSync = true ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 987u ))
            Assert.True(( x.MaxFileCount = 456u ))
            Assert.True(( x.ForceSync = true ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.MAXFILECOUNT 456" )>]
    [<InlineData( "set MAXFILECOUNT 456" )>]
    member _.set_Controller_013 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToStdout( 876u );
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 876u ))
                Assert.True(( x.MaxFileCount = 456u ))
                Assert.True(( x.ForceSync = Constants.LOGMNT_DEF_FORCESYNC ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 876u ))
            Assert.True(( x.MaxFileCount = 456u ))
            Assert.True(( x.ForceSync = Constants.LOGMNT_DEF_FORCESYNC ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXFILECOUNT -1" )>]
    [<InlineData( "set MAXFILECOUNT 4294967296" )>]
    [<InlineData( "set MAXFILECOUNT aaa" )>]
    member _.set_Controller_014 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.FORCESYNC false" )>]
    [<InlineData( "set FORCESYNC false" )>]
    member _.set_Controller_015 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToFile( {
                    TotalLimit = 987u;
                    MaxFileCount = 222u;
                    ForceSync = true;
                })
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 987u ))
                Assert.True(( x.MaxFileCount = 222u ))
                Assert.True(( x.ForceSync = false ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 987u ))
            Assert.True(( x.MaxFileCount = 222u ))
            Assert.True(( x.ForceSync = false ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGMAINTENANCE.FORCESYNC true" )>]
    [<InlineData( "set FORCESYNC true" )>]
    member _.set_Controller_016 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToStdout( 876u );
            };
            LogParameters = None;
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            match newConf.LogMaintenance.Value.OutputDest with
            | HarukaCtrlConf.U_ToFile( x ) ->
                Assert.True(( x.TotalLimit = 876u ))
                Assert.True(( x.MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT ))
                Assert.True(( x.ForceSync = true ))
            | _ ->
                Assert.Fail __LINE__
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        match ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 876u ))
            Assert.True(( x.MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT ))
            Assert.True(( x.ForceSync = true ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue = initnode.LogParametersValue ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set FORCESYNC -1" )>]
    [<InlineData( "set FORCESYNC aaa" )>]
    member _.set_Controller_017 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGPARAMETERS.SOFTLIMIT 444" )>]
    [<InlineData( "set SOFTLIMIT 444" )>]
    member _.set_Controller_018 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = None;
            LogParameters = Some {
                SoftLimit = 0u;
                HardLimit = 0u;
                LogLevel = LogLevel.LOGLEVEL_INFO;
            };
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            Assert.True(( newConf.LogParameters.Value.SoftLimit = 444u ))
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue = initnode.LogMaintenanceValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.SoftLimit = 444u ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.HardLimit = 0u ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.LogLevel = LogLevel.LOGLEVEL_INFO ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set SOFTLIMIT -1" )>]
    [<InlineData( "set SOFTLIMIT 4294967296" )>]
    [<InlineData( "set SOFTLIMIT aaa" )>]
    member _.set_Controller_019 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGPARAMETERS.HARDLIMIT 555" )>]
    [<InlineData( "set HARDLIMIT 555" )>]
    member _.set_Controller_020 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = None;
            LogParameters = Some {
                SoftLimit = 0u;
                HardLimit = 0u;
                LogLevel = LogLevel.LOGLEVEL_INFO;
            };
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            Assert.True(( newConf.LogParameters.Value.HardLimit = 555u ))
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue = initnode.LogMaintenanceValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.SoftLimit = 0u ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.HardLimit = 555u ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.LogLevel = LogLevel.LOGLEVEL_INFO ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set SOFTLIMIT -1" )>]
    [<InlineData( "set SOFTLIMIT 4294967296" )>]
    [<InlineData( "set SOFTLIMIT aaa" )>]
    member _.set_Controller_021 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGPARAMETERS.LOGLEVEL VERBOSE" )>]
    [<InlineData( "set LOGLEVEL VERBOSE" )>]
    member _.set_Controller_022 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let initnode = worknode.CreateUpdatedNode {
            RemoteCtrl = None;
            LogMaintenance = None;
            LogParameters = Some {
                SoftLimit = 0u;
                HardLimit = 0u;
                LogLevel = LogLevel.LOGLEVEL_INFO;
            };
        }
        let mutable flg1 = false
        ss.p_UpdateControllerNode <- ( fun newConf ->
            flg1 <- true
            Assert.True(( newConf.LogParameters.Value.LogLevel = LogLevel.LOGLEVEL_VERBOSE ))
            initnode.CreateUpdatedNode newConf
        )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).RemoteCtrlValue = initnode.RemoteCtrlValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogMaintenanceValue = initnode.LogMaintenanceValue ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.SoftLimit = 0u ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.HardLimit = 0u ))
        Assert.True(( ( r_cn :?> ConfNode_Controller ).LogParametersValue.LogLevel = LogLevel.LOGLEVEL_VERBOSE ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGLEVEL -1" )>]
    [<InlineData( "set LOGLEVEL 0" )>]
    [<InlineData( "set LOGLEVEL aaa" )>]
    member _.set_Controller_023 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_Controller_024 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID TD_11223344" )>]
    member _.set_TargetDevice_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnode = initnode ))
            Assert.True(( argtdid = tdid_me.fromString( "TD_11223344" ) ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.fromString( "TD_11223344" ) ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = defTDNegoParam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID TD_XXX" )>]
    member _.set_TargetDevice_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NAME aaaa" )>]
    member _.set_TargetDevice_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnode = initnode ))
            Assert.True(( argname = "aaaa" ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "aaaa" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = defTDNegoParam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NEGOTIABLEPARAMETERS.MAXRECVDATASEGMENTLENGTH 111" )>]
    [<InlineData( "set MAXRECVDATASEGMENTLENGTH 111" )>]
    member _.set_TargetDevice_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnego.MaxRecvDataSegmentLength = 111u ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        let expparam = {
            defTDNegoParam with
                MaxRecvDataSegmentLength = 111u;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = expparam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXRECVDATASEGMENTLENGTH -1" )>]
    [<InlineData( "set MAXRECVDATASEGMENTLENGTH 4294967296" )>]
    [<InlineData( "set MAXRECVDATASEGMENTLENGTH aaa" )>]
    member _.set_TargetDevice_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NEGOTIABLEPARAMETERS.MAXBURSTLENGTH 112" )>]
    [<InlineData( "set MAXBURSTLENGTH 112" )>]
    member _.set_TargetDevice_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnego.MaxBurstLength = 112u ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        let expparam = {
            defTDNegoParam with
                MaxBurstLength = 112u;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = expparam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXBURSTLENGTH -1" )>]
    [<InlineData( "set MAXBURSTLENGTH 4294967296" )>]
    [<InlineData( "set MAXBURSTLENGTH aaa" )>]
    member _.set_TargetDevice_007 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NEGOTIABLEPARAMETERS.FIRSTBURSTLENGTH 113" )>]
    [<InlineData( "set FIRSTBURSTLENGTH 113" )>]
    member _.set_TargetDevice_008 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnego.FirstBurstLength = 113u ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        let expparam = {
            defTDNegoParam with
                FirstBurstLength = 113u;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = expparam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set FIRSTBURSTLENGTH -1" )>]
    [<InlineData( "set FIRSTBURSTLENGTH 4294967296" )>]
    [<InlineData( "set FIRSTBURSTLENGTH aaa" )>]
    member _.set_TargetDevice_009 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NEGOTIABLEPARAMETERS.DEFAULTTIME2WAIT 114" )>]
    [<InlineData( "set DEFAULTTIME2WAIT 114" )>]
    member _.set_TargetDevice_010 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnego.DefaultTime2Wait = 114us ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        let expparam = {
            defTDNegoParam with
                DefaultTime2Wait = 114us;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = expparam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set DEFAULTTIME2WAIT -1" )>]
    [<InlineData( "set DEFAULTTIME2WAIT 65536" )>]
    [<InlineData( "set DEFAULTTIME2WAIT aaa" )>]
    member _.set_TargetDevice_011 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NEGOTIABLEPARAMETERS.DEFAULTTIME2RETAIN 115" )>]
    [<InlineData( "set DEFAULTTIME2RETAIN 115" )>]
    member _.set_TargetDevice_012 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnego.DefaultTime2Retain = 115us ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        let expparam = {
            defTDNegoParam with
                DefaultTime2Retain = 115us;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = expparam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set DEFAULTTIME2RETAIN -1" )>]
    [<InlineData( "set DEFAULTTIME2RETAIN 65536" )>]
    [<InlineData( "set DEFAULTTIME2RETAIN aaa" )>]
    member _.set_TargetDevice_013 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NEGOTIABLEPARAMETERS.MAXOUTSTANDINGR2T 116" )>]
    [<InlineData( "set MAXOUTSTANDINGR2T 116" )>]
    member _.set_TargetDevice_014 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( argnego.MaxOutstandingR2T = 116us ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        let expparam = {
            defTDNegoParam with
                MaxOutstandingR2T = 116us;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = expparam ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = defTDLogParam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXOUTSTANDINGR2T -1" )>]
    [<InlineData( "set MAXOUTSTANDINGR2T 65536" )>]
    [<InlineData( "set MAXOUTSTANDINGR2T aaa" )>]
    member _.set_TargetDevice_015 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGPARAMETERS.SOFTLIMIT 117" )>]
    [<InlineData( "set SOFTLIMIT 117" )>]
    member _.set_TargetDevice_016 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( arglog.SoftLimit = 117u ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = defTDNegoParam ))
        let expparam = {
            defTDLogParam with
                SoftLimit = 117u;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = expparam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set SOFTLIMIT -1" )>]
    [<InlineData( "set SOFTLIMIT 4294967296" )>]
    [<InlineData( "set SOFTLIMIT aaa" )>]
    member _.set_TargetDevice_017 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGPARAMETERS.HARDLIMIT 118" )>]
    [<InlineData( "set HARDLIMIT 118" )>]
    member _.set_TargetDevice_018 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( arglog.HardLimit = 118u ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = defTDNegoParam ))
        let expparam = {
            defTDLogParam with
                HardLimit = 118u;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = expparam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set HARDLIMIT -1" )>]
    [<InlineData( "set HARDLIMIT 4294967296" )>]
    [<InlineData( "set HARDLIMIT aaa" )>]
    member _.set_TargetDevice_019 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGPARAMETERS.LOGLEVEL WARNING" )>]
    [<InlineData( "set LOGLEVEL WARNING" )>]
    member _.set_TargetDevice_020 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let worknode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let initnode = worknode.CreateUpdatedNode ( tdid_me.Zero ) "" defTDNegoParam defTDLogParam
        let mutable flg1 = false

        ss.p_UpdateTargetDeviceNode <- ( fun argnode argtdid argname argnego arglog ->
            flg1 <- true
            Assert.True(( arglog.LogLevel = LogLevel.LOGLEVEL_WARNING ))
            initnode.CreateUpdatedNode argtdid argname argnego arglog
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceID = tdid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).TargetDeviceName = "" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).NegotiableParameters = defTDNegoParam ))
        let expparam = {
            defTDLogParam with
                LogLevel = LogLevel.LOGLEVEL_WARNING;
        }
        Assert.True(( ( r_cn :?> ConfNode_TargetDevice ).LogParameters = expparam ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LOGLEVEL -1" )>]
    [<InlineData( "set LOGLEVEL aaaa" )>]
    member _.set_TargetDevice_021 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set AAA -1" )>]
    member _.set_TargetDevice_022 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LUN 5" )>]
    [<InlineData( "set LUN 0x0000000000000005" )>]
    [<InlineData( "set LUN 0X0000000000000005" )>]
    member _.set_DummyDeviceLU_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyDeviceLUNode :?> ConfNode_DummyDeviceLU
        let mutable flg1 = false

        ss.p_UpdateDummyDeviceLUNode <- ( fun argnode arglun argname mm ->
            flg1 <- true
            Assert.True(( arglun = lun_me.fromPrim 5UL ))
            initnode.CreateUpdatedNode arglun argname mm
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ILUNode ).LUN = lun_me.fromPrim 5UL ))
        Assert.True(( ( r_cn :?> ILUNode ).LUName = "" ))
        Assert.True(( ( r_cn :?> ILUNode ).MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LUN -1" )>]
    [<InlineData( "set LUN aaaaa" )>]
    [<InlineData( "set LUN 18446744073709551616" )>]
    member _.set_DummyDeviceLU_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyDeviceLUNode :?> ConfNode_DummyDeviceLU
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NAME aaa" )>]
    member _.set_DummyDeviceLU_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyDeviceLUNode :?> ConfNode_DummyDeviceLU
        let mutable flg1 = false

        ss.p_UpdateDummyDeviceLUNode <- ( fun argnode arglun argname argmm ->
            flg1 <- true
            Assert.True(( argname = "aaa" ))
            initnode.CreateUpdatedNode arglun argname argmm
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ILUNode ).LUN = lun_me.zero ))
        Assert.True(( ( r_cn :?> ILUNode ).LUName = "aaa" ))
        Assert.True(( ( r_cn :?> ILUNode ).MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXMULTIPLICITY 2" )>]
    member _.set_DummyDeviceLU_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyDeviceLUNode :?> ConfNode_DummyDeviceLU
        let mutable flg1 = false

        ss.p_UpdateDummyDeviceLUNode <- ( fun argnode arglun argname argmm ->
            flg1 <- true
            Assert.True(( argmm = 2u ))
            initnode.CreateUpdatedNode arglun argname argmm
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ILUNode ).LUN = lun_me.zero ))
        Assert.True(( ( r_cn :?> ILUNode ).LUName = "" ))
        Assert.True(( ( r_cn :?> ILUNode ).MaxMultiplicity = 2u ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXMULTIPLICITY -1" )>]
    [<InlineData( "set MAXMULTIPLICITY aaaaa" )>]
    [<InlineData( "set MAXMULTIPLICITY 4294967296" )>]
    member _.set_DummyDeviceLU_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyDeviceLUNode :?> ConfNode_DummyDeviceLU
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaaa -1" )>]
    member _.set_DummyDeviceLU_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyDeviceLUNode :?> ConfNode_DummyDeviceLU
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LUN 1234605616436508552" )>]
    [<InlineData( "set LUN 0x1122334455667788" )>]
    [<InlineData( "set LUN 0X1122334455667788" )>]
    member _.set_BlockDeviceLU_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        ss.p_UpdateBlockDeviceLUNode <- ( fun argnode arglun argname argmm ->
            flg1 <- true
            Assert.True(( arglun = lun_me.fromPrim 1234605616436508552UL ))
            initnode.CreateUpdatedNode arglun argname argmm
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ILUNode ).LUN = lun_me.fromPrim 1234605616436508552UL ))
        Assert.True(( ( r_cn :?> ILUNode ).LUName = "" ))
        Assert.True(( ( r_cn :?> ILUNode ).MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set LUN -1" )>]
    [<InlineData( "set LUN aaaaa" )>]
    [<InlineData( "set LUN 18446744073709551616" )>]
    member _.set_BlockDeviceLU_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NAME bbb" )>]
    member _.set_BlockDeviceLU_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        ss.p_UpdateBlockDeviceLUNode <- ( fun argnode arglun argname argmm ->
            flg1 <- true
            Assert.True(( argname = "bbb" ))
            initnode.CreateUpdatedNode arglun argname argmm
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ILUNode ).LUN = lun_me.zero ))
        Assert.True(( ( r_cn :?> ILUNode ).LUName = "bbb" ))
        Assert.True(( ( r_cn :?> ILUNode ).MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXMULTIPLICITY 2" )>]
    member _.set_BlockDeviceLU_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        ss.p_UpdateBlockDeviceLUNode <- ( fun argnode arglun argname argmm ->
            flg1 <- true
            Assert.True(( argmm = 2u ))
            initnode.CreateUpdatedNode arglun argname argmm
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ILUNode ).LUN = lun_me.zero ))
        Assert.True(( ( r_cn :?> ILUNode ).LUName = "" ))
        Assert.True(( ( r_cn :?> ILUNode ).MaxMultiplicity = 2u ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXMULTIPLICITY -1" )>]
    [<InlineData( "set MAXMULTIPLICITY aaaaa" )>]
    [<InlineData( "set MAXMULTIPLICITY 4294967296" )>]
    member _.set_BlockDeviceLU_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaaa -1" )>]
    member _.set_BlockDeviceLU_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID TG_11223344" )>]
    member _.set_TargetGroup_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let initnode = wnode.CreateUpdatedNode tgid_me.Zero "" false
        let mutable flg1 = false

        ss.p_UpdateTargetGroupNode <- ( fun argcn argid argname argeas ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argid = tgid_me.fromString( "TG_11223344" ) ))
            initnode.CreateUpdatedNode argid argname argeas
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).TargetGroupID = tgid_me.fromString( "TG_11223344" ) ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).TargetGroupName = "" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).EnabledAtStart = false ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID TG_YYY" )>]
    member _.set_TargetGroup_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NAME ccc" )>]
    member _.set_TargetGroup_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let initnode = wnode.CreateUpdatedNode tgid_me.Zero "" false
        let mutable flg1 = false

        ss.p_UpdateTargetGroupNode <- ( fun argcn argid argname argeas ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argname = "ccc" ))
            initnode.CreateUpdatedNode argid argname argeas
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).TargetGroupID = tgid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).TargetGroupName = "ccc" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).EnabledAtStart = false ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ENABLEDATSTART true" )>]
    member _.set_TargetGroup_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let initnode = wnode.CreateUpdatedNode tgid_me.Zero "" false
        let mutable flg1 = false

        ss.p_UpdateTargetGroupNode <- ( fun argcn argid argname argeas ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argeas = true ))
            initnode.CreateUpdatedNode argid argname argeas
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).TargetGroupID = tgid_me.Zero ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).TargetGroupName = "" ))
        Assert.True(( ( r_cn :?> ConfNode_TargetGroup ).EnabledAtStart = true ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ENABLEDATSTART -1" )>]
    [<InlineData( "set ENABLEDATSTART aaa" )>]
    member _.set_TargetGroup_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa 1" )>]
    member _.set_TargetGroup_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID 333" )>]
    member _.set_Target_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetName = "";
            TargetAlias = "";
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.IdentNumber = tnodeidx_me.fromPrim 333u ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                IdentNumber = tnodeidx_me.fromPrim 333u;
        }
        Assert.True(( ( r_cn :?> ConfNode_Target ).Values = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID -1" )>]
    [<InlineData( "set ID 4294967296" )>]
    [<InlineData( "set ID aaa" )>]
    member _.set_Target_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set TPGT 333" )>]
    member _.set_Target_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetName = "";
            TargetAlias = "";
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.TargetPortalGroupTag = tpgt_me.fromPrim 333us ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                TargetPortalGroupTag = tpgt_me.fromPrim 333us;
        }
        Assert.True(( ( r_cn :?> ConfNode_Target ).Values = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set TPGT -1" )>]
    [<InlineData( "set TPGT 65536" )>]
    [<InlineData( "set TPGT aaa" )>]
    member _.set_Target_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set NAME ddd" )>]
    member _.set_Target_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetName = "";
            TargetAlias = "";
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.TargetName = "ddd" ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                TargetName = "ddd";
        }
        Assert.True(( ( r_cn :?> ConfNode_Target ).Values = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ALIAS eee" )>]
    member _.set_Target_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetName = "";
            TargetAlias = "";
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.TargetAlias = "eee" ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                TargetAlias = "eee";
        }
        Assert.True(( ( r_cn :?> ConfNode_Target ).Values = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_Target_007 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID 333" )>]
    member _.set_NetworkPortal_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.IdentNumber = netportidx_me.fromPrim 333u ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                IdentNumber = netportidx_me.fromPrim 333u;
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID -1" )>]
    [<InlineData( "set ID 4294967296" )>]
    [<InlineData( "set ID aaa" )>]
    member _.set_NetworkPortal_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set TPGT 333" )>]
    member _.set_NetworkPortal_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.TargetPortalGroupTag = tpgt_me.fromPrim 333us ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                TargetPortalGroupTag = tpgt_me.fromPrim 333us;
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set TPGT -1" )>]
    [<InlineData( "set TPGT 65536" )>]
    [<InlineData( "set TPGT aaa" )>]
    member _.set_NetworkPortal_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set TARGETADDRESS aaaaa" )>]
    member _.set_NetworkPortal_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.TargetAddress = "aaaaa" ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                TargetAddress = "aaaaa";
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set PORTNUMBER 444" )>]
    member _.set_NetworkPortal_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.PortNumber = 444us ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                PortNumber = 444us;
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set PORTNUMBER -1" )>]
    [<InlineData( "set PORTNUMBER 65536" )>]
    [<InlineData( "set PORTNUMBER aaa" )>]
    member _.set_NetworkPortal_007 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set DISABLENAGLE true" )>]
    member _.set_NetworkPortal_008 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.DisableNagle = true ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                DisableNagle = true;
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set DISABLENAGLE -1" )>]
    [<InlineData( "set DISABLENAGLE aaa" )>]
    member _.set_NetworkPortal_009 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set RECEIVEBUFFERSIZE 456" )>]
    member _.set_NetworkPortal_010 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.ReceiveBufferSize = 456 ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                ReceiveBufferSize = 456;
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set RECEIVEBUFFERSIZE -2147483649" )>]
    [<InlineData( "set RECEIVEBUFFERSIZE 2147483648" )>]
    [<InlineData( "set RECEIVEBUFFERSIZE aaa" )>]
    member _.set_NetworkPortal_011 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set SENDBUFFERSIZE 456" )>]
    member _.set_NetworkPortal_012 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 0;
            SendBufferSize = 0;
            WhiteList = [];
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.SendBufferSize = 456 ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                SendBufferSize = 456;
        }
        Assert.True(( ( r_cn :?> ConfNode_NetworkPortal ).NetworkPortal = expconf ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set SENDBUFFERSIZE -2147483649" )>]
    [<InlineData( "set SENDBUFFERSIZE 2147483648" )>]
    [<InlineData( "set SENDBUFFERSIZE aaa" )>]
    member _.set_NetworkPortal_013 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_NetworkPortal_014 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID 333" )>]
    member _.set_DummyMedia_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let initnode = wnode.CreateUpdatedNode ( mediaidx_me.fromPrim 1u ) "aaa"
        let mutable flg1 = false

        ss.p_UpdateDummyMediaNode <- ( fun argcn argid argName ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argid = mediaidx_me.fromPrim 333u ))
            Assert.True(( argName = "aaa" ))
            initnode.CreateUpdatedNode argid argName
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 333u ))
        Assert.True(( ( r_cn :?> IMediaNode ).Name = "aaa" ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID -1" )>]
    [<InlineData( "set ID 4294967296" )>]
    [<InlineData( "set ID aaa" )>]
    member _.set_DummyMedia_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MEDIANAME aaaa" )>]
    member _.set_DummyMedia_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let initnode = wnode.CreateUpdatedNode ( mediaidx_me.fromPrim 1u ) "ggggg"
        let mutable flg1 = false

        ss.p_UpdateDummyMediaNode <- ( fun argcn argid argName ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argid = mediaidx_me.fromPrim 1u ))
            Assert.True(( argName = "aaaa" ))
            initnode.CreateUpdatedNode argid argName
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 1u ))
        Assert.True(( ( r_cn :?> IMediaNode ).Name = "aaaa" ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_DummyMedia_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DummyMediaNode :?> ConfNode_DummyMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID 333" )>]
    member _.set_PlainFileMedia_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdatePlainFileMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.IdentNumber = mediaidx_me.fromPrim 333u ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                IdentNumber = mediaidx_me.fromPrim 333u;
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_PlainFile( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID -1" )>]
    [<InlineData( "set ID 4294967296" )>]
    [<InlineData( "set ID aaa" )>]
    member _.set_PlainFileMedia_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MEDIANAME aaaa" )>]
    member _.set_PlainFileMedia_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdatePlainFileMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.MediaName = "aaaa" ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                MediaName = "aaaa";
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_PlainFile( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set FILENAME aaaa" )>]
    member _.set_PlainFileMedia_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdatePlainFileMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.FileName = "aaaa" ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                FileName = "aaaa";
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_PlainFile( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXMULTIPLICITY 445" )>]
    member _.set_PlainFileMedia_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdatePlainFileMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.MaxMultiplicity = 445u ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                MaxMultiplicity = 445u;
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_PlainFile( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MAXMULTIPLICITY -1" )>]
    [<InlineData( "set MAXMULTIPLICITY 4294967296" )>]
    [<InlineData( "set MAXMULTIPLICITY aaa" )>]
    member _.set_PlainFileMedia_007 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set QUEUEWAITTIMEOUT 555" )>]
    member _.set_PlainFileMedia_008 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdatePlainFileMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.QueueWaitTimeOut = 555 ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                QueueWaitTimeOut = 555;
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_PlainFile( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set QUEUEWAITTIMEOUT -2147483649" )>]
    [<InlineData( "set QUEUEWAITTIMEOUT 2147483648" )>]
    [<InlineData( "set QUEUEWAITTIMEOUT aaa" )>]
    member _.set_PlainFileMedia_009 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set WRITEPROTECT true" )>]
    member _.set_PlainFileMedia_010 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdatePlainFileMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.WriteProtect = true ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                WriteProtect = true;
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_PlainFile( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set WRITEPROTECT -1" )>]
    [<InlineData( "set WRITEPROTECT aaa" )>]
    member _.set_PlainFileMedia_011 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_PlainFileMedia_012 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID 333" )>]
    member _.set_MemBufferMedia_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 8192UL;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateMemBufferMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.IdentNumber = mediaidx_me.fromPrim 333u ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                IdentNumber = mediaidx_me.fromPrim 333u;
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_MemBuffer( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID -1" )>]
    [<InlineData( "set ID 4294967296" )>]
    [<InlineData( "set ID aaa" )>]
    member _.set_MemBufferMedia_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MEDIANAME aaaa" )>]
    member _.set_MemBufferMedia_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 8192UL;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateMemBufferMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.MediaName = "aaaa" ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                MediaName = "aaaa";
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_MemBuffer( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set BYTESCOUNT 123" )>]
    member _.set_MemBufferMedia_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 8192UL;
        }
        let initnode = wnode.CreateUpdatedNode conf
        let mutable flg1 = false

        ss.p_UpdateMemBufferMediaNode <- ( fun argcn argconf ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argconf.BytesCount = 123UL ))
            initnode.CreateUpdatedNode argconf
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        let expconf = {
            conf with
                BytesCount = 123UL;
        }
        Assert.True(( ( r_cn :?> IMediaNode ).MediaConfData = TargetGroupConf.U_MemBuffer( expconf ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set BYTESCOUNT -1" )>]
    [<InlineData( "set BYTESCOUNT 18446744073709551616" )>]
    [<InlineData( "set BYTESCOUNT aaa" )>]
    member _.set_MemBufferMedia_005 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_MemBufferMedia_006 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID 333" )>]
    member _.set_DebugMedia_001 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let initnode = wnode.CreateUpdatedNode ( mediaidx_me.fromPrim 1u ) "aaa"
        let mutable flg1 = false

        ss.p_UpdateDebugMediaNode <- ( fun argcn argid argName ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argid = mediaidx_me.fromPrim 333u ))
            Assert.True(( argName = "aaa" ))
            initnode.CreateUpdatedNode argid argName
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 333u ))
        Assert.True(( ( r_cn :?> IMediaNode ).Name = "aaa" ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set ID -1" )>]
    [<InlineData( "set ID 4294967296" )>]
    [<InlineData( "set ID aaa" )>]
    member _.set_DebugMedia_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_PARAMVAL_DATATYPE_MISMATCH"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set MEDIANAME aaaa" )>]
    member _.set_DebugMedia_003 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let wnode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let initnode = wnode.CreateUpdatedNode ( mediaidx_me.fromPrim 1u ) "ggggg"
        let mutable flg1 = false

        ss.p_UpdateDebugMediaNode <- ( fun argcn argid argName ->
            flg1 <- true
            Assert.True(( argcn = initnode ))
            Assert.True(( argid = mediaidx_me.fromPrim 1u ))
            Assert.True(( argName = "aaaa" ))
            initnode.CreateUpdatedNode argid argName
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsSome ))
        let r_ss, r_cc, r_cn = stat.Value
        Assert.True(( ss :> ServerStatus = r_ss ))
        Assert.True(( cc :> CtrlConnection = r_cc ))
        Assert.True(( ( r_cn :?> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 1u ))
        Assert.True(( ( r_cn :?> IMediaNode ).Name = "aaaa" ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "set aaa -1" )>]
    member _.set_DebugMedia_004 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let initnode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let r = CallCommandLoop cr ( Some ( ss, cc, initnode ) )
        Assert.True(( r = ( true, Some( ss, cc, initnode ) ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "CMDMSG_UNKNOWN_PARAMETER_NAME"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Validate_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "validate" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        cnr.AddNode cn
        let mutable flg1 = false

        ss.p_Validate <- ( fun () ->
            flg1 <- true
            []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_ALL_VALIDATED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Validate_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "validate" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( new StringTable( "" ), cnr, cnr.NextID ) :> IConfigureNode
        cnr.AddNode cn
        let mutable flg1 = false

        ss.p_Validate <- ( fun () ->
            flg1 <- true
            [ cn.NodeID, "abc" ]
        )
        ss.p_GetNode <- ( fun nid ->
            Assert.True(( nid = cn.NodeID ))
            cn
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        let expmsg = sprintf "%s : abc" cn.ShortDescriptString
        let out_rs = CheckOutputMessage out_ms out_ws "CR" expmsg
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> IConfigFileNode
        let cn2 = cn.ResetModifiedFlag()

        ss.p_ControllerNode <- ( cn2 :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> [] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "NOT MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some cn.RemoteCtrlValue;
            LogMaintenance = Some cn.LogMaintenanceValue;
            LogParameters = Some cn.LogParametersValue;
        }
        let cn2 = cn.CreateUpdatedNode conf

        ss.p_ControllerNode <- cn2
        ss.p_GetTargetDeviceNodes <- ( fun () -> [] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tdn2 = ( tdn :> IConfigFileNode ).ResetModifiedFlag() :?> ConfNode_TargetDevice

        ss.p_ControllerNode <- cn
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn2 ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ tdn2.TargetDeviceID ] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> Task.FromResult [] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[1].StartsWith "RUNNING" ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tdn2 = tdn.CreateUpdatedNode tdn.TargetDeviceID tdn.TargetDeviceName tdn.NegotiableParameters tdn.LogParameters

        ss.p_ControllerNode <- cn
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn2 ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> Task.FromResult [] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[1].StartsWith "UNLOADED(MOD)" ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tdn2 = ( tdn :> IConfigFileNode ).ResetModifiedFlag() :?> ConfNode_TargetDevice

        ss.p_ControllerNode <- cn
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn2 ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> Task.FromResult [] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[1].StartsWith "UNLOADED " ))
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_006 () =
        let st = StringTable( "" )
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        let tgn =
            new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID tdn.NodeID
        cnr.AddRelation tdn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn :?> ConfNode_TargetDevice ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ ( tdn :?> ConfNode_TargetDevice ).TargetDeviceID ] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [ { ID = tgn.TargetGroupID; Name = "" } ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "ACTIVE " ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_007 () =
        let st = StringTable( "" )
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        let tgn =
            new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID tdn.NodeID
        cnr.AddRelation tdn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn :?> ConfNode_TargetDevice ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ ( tdn :?> ConfNode_TargetDevice ).TargetDeviceID ] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "LOADED " ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_008 () =
        let st = StringTable( "" )
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        let tgn =
            let n = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
            n.CreateUpdatedNode n.TargetGroupID "" true
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID tdn.NodeID
        cnr.AddRelation tdn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn :?> ConfNode_TargetDevice ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ ( tdn :?> ConfNode_TargetDevice ).TargetDeviceID ] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "UNLOADED(MOD)" ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_009 () =
        let st = StringTable( "" )
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        let tgn =
            let n = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
            ( n :> IConfigFileNode ).ResetModifiedFlag()
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID tdn.NodeID
        cnr.AddRelation tdn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn :?> ConfNode_TargetDevice ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ ( tdn :?> ConfNode_TargetDevice ).TargetDeviceID ] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "UNLOADED " ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.StatusAll_010 () =
        let st = StringTable( "" )
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "statusall" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        let tgn =
            let n = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
            ( n :> IConfigFileNode ).ResetModifiedFlag()
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID tdn.NodeID
        cnr.AddRelation tdn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> [ tdn :?> ConfNode_TargetDevice ] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "UNLOADED " ))
        Assert.False(( flg1 ))
        Assert.False(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetDevice_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetTargetDeviceNodes <- ( fun () ->
            flg1 <- true
            []
        )
        ss.p_AddTargetDeviceNode <- ( fun newTdid tdName newNegParam newLogParam ->
            flg2 <- true
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetDevice_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdnodes = [
            for i = 0 to 5 do
                yield CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        ]

        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetTargetDeviceNodes <- ( fun () ->
            flg1 <- true
            tdnodes
        )
        ss.p_AddTargetDeviceNode <- ( fun newTdid tdName newNegParam newLogParam ->
            flg2 <- true
            let oldtdids =
                tdnodes |> Seq.map ( fun itr -> itr.TargetDeviceID )
            let oldtdnameds =
                tdnodes |> Seq.map ( fun itr -> itr.TargetDeviceName )
            Assert.False(( Seq.exists ( (=) newTdid ) oldtdids ))
            Assert.False(( Seq.exists ( (=) tdName ) oldtdnameds ))
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetDevice_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /n aaa" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdnodes = [
            for i = 0 to 5 do
                yield CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        ]

        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetTargetDeviceNodes <- ( fun () ->
            flg1 <- true
            tdnodes
        )
        ss.p_AddTargetDeviceNode <- ( fun newTdid tdName newNegParam newLogParam ->
            flg2 <- true
            Assert.True(( tdName = "aaa" ))
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetDevice_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /n aaa" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdnodes = [
            for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
                yield CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        ]
        ss.p_GetTargetDeviceNodes <- ( fun () -> tdnodes )
        ss.p_AddTargetDeviceNode <- ( fun newTdid tdName newNegParam newLogParam ->
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetDevice_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /n aaa" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let tdnodes = [
            for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
                yield CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        ]
        ss.p_GetTargetDeviceNodes <- ( fun () -> tdnodes )
        ss.p_AddTargetDeviceNode <- ( fun newTdid tdName newNegParam newLogParam ->
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> IConfigFileNode
        let cn2 = cn.ResetModifiedFlag()

        ss.p_ControllerNode <- ( cn2 :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn2 ))
            None
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "NOT MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some cn.RemoteCtrlValue;
            LogMaintenance = Some cn.LogMaintenanceValue;
            LogParameters = Some cn.LogParametersValue;
        }
        let cn2 = cn.CreateUpdatedNode conf

        ss.p_ControllerNode <- ( cn2 )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn2 ))
            None
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, cn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tdn2 = ( tdn :> IConfigFileNode ).ResetModifiedFlag() :?> ConfNode_TargetDevice

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tdn2 ))
            Some( tdn2 )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ tdn2.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tdn2 ))
            None
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tdn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for _ = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[1].StartsWith "RUNNING" ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tdn2 = tdn.CreateUpdatedNode tdn.TargetDeviceID tdn.TargetDeviceName tdn.NegotiableParameters tdn.LogParameters

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tdn2 ))
            Some( tdn2 )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tdn2 ))
            None
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tdn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[1].StartsWith "UNLOADED(MOD)" ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tdn2 = ( tdn :> IConfigFileNode ).ResetModifiedFlag() :?> ConfNode_TargetDevice

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tdn2 ))
            Some( tdn2 )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tdn2 ))
            None
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tdn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[1].StartsWith "UNLOADED " ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tgn ))
            Some( tdn )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tgn ))
            Some tgn
        )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [ { ID = tgn.TargetGroupID; Name = "" } ] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tgn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "ACTIVE " ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tgn ))
            Some( tdn )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tgn ))
            Some tgn
        )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [ { ID = tgn.TargetGroupID; Name = "" } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tgn ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "LOADED " ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tgn2 = tgn.CreateUpdatedNode tgn.TargetGroupID tgn.TargetGroupName tgn.EnabledAtStart
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tgn2 ))
            Some( tdn )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tgn2 ))
            Some tgn2
        )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tgn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "UNLOADED(MOD)" ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_009 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> IConfigFileNode
        let tgn2 = tgn.ResetModifiedFlag() :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tgn2 ))
            Some( tdn )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tgn2 ))
            Some tgn2
        )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tgn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "UNLOADED " ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Status_010 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "status" )
        let cn = CommandRunner_Test1.m_ControllerNode
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> IConfigFileNode
        let tgn2 = tgn.ResetModifiedFlag() :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_ControllerNode <- ( cn :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = tgn2 ))
            Some( tdn )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.True(( argnode = tgn2 ))
            Some tgn2
        )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn2 ) )
        Assert.True(( r ))
        Assert.True(( stat = Some ( ss, cc, tgn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.True(( lines.[2].StartsWith "UNLOADED " ))
        Assert.False(( flg1 ))
        Assert.False(( flg2 ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]




