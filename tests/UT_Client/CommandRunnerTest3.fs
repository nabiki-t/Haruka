//=============================================================================
// Haruka Software Storage.
// CommandRunnerTest3.fs : Test cases for CommandRunner class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Net
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

type CommandRunner_Test3() =

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
            Assert.Same( tgn2, argnode )
            Some( tdn )
        )
        cc.p_GetTargetDeviceProcs <- ( fun () -> Task.FromResult [] )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn2, argnode )
            Some tgn2
        )
        cc.p_GetActiveTargetGroups <- ( fun _ -> task { flg1 <- true; return [] } )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { flg2 <- true; return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn2 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn2 ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = new StreamReader( out_ms )
        let lines = [|
            for i = 1 to 3 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.StartsWith( "UNLOADED ", lines.[2] )
        Assert.False( flg1 )
        Assert.False( flg2 )

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddRelation cn.NodeID tdn.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_MISSING_NODE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        cnr.AddNode cn

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_MISSING_NODE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


    [<Fact>]
    member _.Delete_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let cn2 = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        cnr.AddNode cn
        cnr.AddNode cn2
        cnr.AddRelation cn.NodeID cn2.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_CTRL_NODE_NOT_DELETABLE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddRelation cn.NodeID ( tdn :> IConfigureNode ).NodeID

        ss.p_DeleteTargetDeviceNode <- ( fun argn ->
            Assert.Same( tdn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        let npn =
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
            new ConfNode_NetworkPortal( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddNode npn
        cnr.AddRelation cn.NodeID ( tdn :> IConfigureNode ).NodeID
        cnr.AddRelation cn.NodeID ( npn :> IConfigureNode ).NodeID

        ss.p_DeleteNetworkPortalNode <- ( fun argn ->
            Assert.Same( npn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_DeleteTargetGroupNode <- ( fun argn ->
            Assert.Same( tgn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tn
        cnr.AddRelation cn.NodeID ( tn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( tn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let bdn = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.zero, "", mult, otl )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode bdn
        cnr.AddRelation cn.NodeID ( bdn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( bdn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_009 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let ddn = new ConfNode_DummyDeviceLU( st, cnr, cnr.NextID, lun_me.zero, "", Constants.LU_DEF_MULTIPLICITY )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode ddn
        cnr.AddRelation cn.NodeID ( ddn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( ddn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_010 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let sfn =
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 1u;
                MediaName = "";
                FileName = "";
                BlockSize = Blocksize.BS_512;
                WriteProtect = false;
            }
            new ConfNode_PlainFileMedia( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode sfn
        cnr.AddRelation cn.NodeID ( sfn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( sfn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_011 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let sfn =
            let conf : TargetGroupConf.T_MemBuffer = {
                IdentNumber = mediaidx_me.fromPrim 1u;
                MediaName = "";
                BytesCount = 4UL;
                BlockSize = Blocksize.BS_512;
            }
            new ConfNode_MemBufferMedia( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode sfn
        cnr.AddRelation cn.NodeID ( sfn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( sfn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_012 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let dmn = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode dmn
        cnr.AddRelation cn.NodeID ( dmn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( dmn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_013 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete /i 0" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let dmn = new ConfNode_DebugMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode dmn
        cnr.AddRelation cn.NodeID ( dmn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( dmn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_014 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let cn = CommandRunner_Test1.m_TargetDeviceNode
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_CTRL_NODE_NOT_DELETABLE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_015 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn1 = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let cn2 = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode

        cnr.AddNode cn1
        cnr.AddNode cn2
        cnr.AddRelation cn1.NodeID cn2.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn2 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CMDMSG_CTRL_NODE_NOT_DELETABLE"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_016 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tdn
        cnr.AddRelation cn.NodeID ( tdn :> IConfigureNode ).NodeID

        ss.p_DeleteTargetDeviceNode <- ( fun argn ->
            Assert.Same( tdn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_017 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let npn =
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
            new ConfNode_NetworkPortal( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode npn
        cnr.AddRelation cn.NodeID ( npn :> IConfigureNode ).NodeID

        ss.p_DeleteNetworkPortalNode <- ( fun argn ->
            Assert.Same( npn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_018 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tgn
        cnr.AddRelation cn.NodeID ( tgn :> IConfigureNode ).NodeID

        ss.p_DeleteTargetGroupNode <- ( fun argn ->
            Assert.Same( tgn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_019 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode tn
        cnr.AddRelation cn.NodeID ( tn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( tn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_020 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let bdn = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.zero, "", mult, otl )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode bdn
        cnr.AddRelation cn.NodeID ( bdn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( bdn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, bdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_021 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let ddn = new ConfNode_DummyDeviceLU( st, cnr, cnr.NextID, lun_me.zero, "", Constants.LU_DEF_MULTIPLICITY )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode ddn
        cnr.AddRelation cn.NodeID ( ddn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( ddn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, ddn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_022 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let sfn =
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 1u;
                MediaName = "";
                FileName = "";
                BlockSize = Blocksize.BS_512;
                WriteProtect = false;
            }
            new ConfNode_PlainFileMedia( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode sfn
        cnr.AddRelation cn.NodeID ( sfn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( sfn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, sfn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_023 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let sfn =
            let conf : TargetGroupConf.T_MemBuffer = {
                IdentNumber = mediaidx_me.fromPrim 1u;
                MediaName = "";
                BytesCount = 4UL;
                BlockSize = Blocksize.BS_512;
            }
            new ConfNode_MemBufferMedia( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode sfn
        cnr.AddRelation cn.NodeID ( sfn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( sfn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, sfn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_024 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let dmn = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode dmn
        cnr.AddRelation cn.NodeID ( dmn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( dmn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, dmn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Delete_025 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "delete" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, cnr, cnr.NextID ) :> IConfigureNode
        let sfn = new ConfNode_DebugMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" )
        let mutable flg1 = false

        cnr.AddNode cn
        cnr.AddNode sfn
        cnr.AddRelation cn.NodeID ( sfn :> IConfigureNode ).NodeID

        ss.p_DeleteNodeInTargetGroup <- ( fun argn ->
            Assert.Same( sfn, argn )
            flg1 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, sfn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, cn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "MD" "Deleted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Start_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "start" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( td, argnode )
            flg1 <- true
            Some td
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )
        cc.p_StartTargetDeviceProc <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg2 <- true
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Started"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Start_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "start" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( td, argnode )
            flg1 <- true
            None
        )
        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, td ) ) |> ignore
        ) |> ignore
        Assert.True( flg1 )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Start_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "start" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some td )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Start_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "start" )
        let td =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some td )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Kill_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "kill" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( td, argnode )
            Some td
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )

        cc.p_KillTargetDeviceProc <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Killed"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Kill_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "kill" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some td )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Kill_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "kill" )
        let td =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some td )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Kill_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "kill" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( td, argnode )
            flg1 <- true
            None
        )

        let e =
            Assert.ThrowsAny( fun () ->
                CallCommandLoop cr ( Some ( ss, cc, td ) ) |> ignore
            )
        Assert.StartsWith( "Unexpected", e.Message )

        Assert.True( flg1 )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.SetLogParam_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setlogparam" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )
        cc.p_GetLogParameters <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg1 <- true
            task {
                return {
                    SoftLimit = 123u;
                    HardLimit = 456u;
                    LogLevel = LogLevel.LOGLEVEL_FAILED;
                }
            }
        )

        cc.p_SetLogParameters <- ( fun argid conf ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg2 <- true
            Assert.StrictEqual( 123u, conf.SoftLimit )
            Assert.StrictEqual( 456u, conf.HardLimit )
            Assert.StrictEqual( LogLevel.LOGLEVEL_FAILED, conf.LogLevel )
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_LOG_PARAM_UPDATED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SetLogParam_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setlogparam /s 999" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )
        cc.p_GetLogParameters <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg1 <- true
            task {
                return {
                    SoftLimit = 123u;
                    HardLimit = 456u;
                    LogLevel = LogLevel.LOGLEVEL_FAILED;
                }
            }
        )

        cc.p_SetLogParameters <- ( fun argid conf ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg2 <- true
            Assert.StrictEqual( 999u, conf.SoftLimit )
            Assert.StrictEqual( 456u, conf.HardLimit )
            Assert.StrictEqual( LogLevel.LOGLEVEL_FAILED, conf.LogLevel )
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_LOG_PARAM_UPDATED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SetLogParam_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setlogparam /h 888" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )
        cc.p_GetLogParameters <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg1 <- true
            task {
                return {
                    SoftLimit = 123u;
                    HardLimit = 456u;
                    LogLevel = LogLevel.LOGLEVEL_FAILED;
                }
            }
        )

        cc.p_SetLogParameters <- ( fun argid conf ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg2 <- true
            Assert.StrictEqual( 123u, conf.SoftLimit )
            Assert.StrictEqual( 888u, conf.HardLimit )
            Assert.StrictEqual( LogLevel.LOGLEVEL_FAILED, conf.LogLevel )
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_LOG_PARAM_UPDATED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SetLogParam_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setlogparam /l INFO" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )
        cc.p_GetLogParameters <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg1 <- true
            task {
                return {
                    SoftLimit = 123u;
                    HardLimit = 456u;
                    LogLevel = LogLevel.LOGLEVEL_FAILED;
                }
            }
        )

        cc.p_SetLogParameters <- ( fun argid conf ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg2 <- true
            Assert.StrictEqual( 123u, conf.SoftLimit )
            Assert.StrictEqual( 456u, conf.HardLimit )
            Assert.StrictEqual( LogLevel.LOGLEVEL_INFO, conf.LogLevel )
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_LOG_PARAM_UPDATED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SetLogParam_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setlogparam /l INFO" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SetLogParam_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setlogparam /l INFO" )
        let td =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.GetLogParam_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "getlogparam" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )
        cc.p_GetLogParameters <- ( fun argid ->
            Assert.StrictEqual( td.TargetDeviceID, argid )
            flg1 <- true
            task {
                return {
                    SoftLimit = 123u;
                    HardLimit = 456u;
                    LogLevel = LogLevel.LOGLEVEL_FAILED;
                }
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        Assert.True( flg1 )

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "SoftLimit"
        let lines = [|
            for i = 1 to 2 do
                yield ( out_rs.ReadLine() ).TrimStart()
        |]
        Assert.StartsWith( "HardLimit", lines.[0]  )
        Assert.StartsWith( "LogLevel", lines.[1]  )

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.GetLogParam_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "getlogparam" )
        let td = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.GetLogParam_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "getlogparam" )
        let td =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ td.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, td ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, td ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddPortal_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create networkportal" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn
        let mutable flg1 = false

        ss.p_AddNetworkPortalNode <- ( fun argtd argconf ->
            Assert.Same( tdn, argtd )
            Assert.StrictEqual( "", argconf.TargetAddress )
            Assert.StrictEqual( Constants.DEFAULT_ISCSI_PORT_NUM, argconf.PortNumber )
            flg1 <- true
            CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddPortal_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create networkportal" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        let npn =
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
            new ConfNode_NetworkPortal( st, cnr, cnr.NextID, conf )
        cnr.AddNode tdn
        cnr.AddNode npn
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID ( npn :> IConfigureNode ).NodeID 
        let mutable flg1 = false

        ss.p_AddNetworkPortalNode <- ( fun argtd argconf ->
            Assert.Same( tdn, argtd )
            Assert.StrictEqual( "", argconf.TargetAddress )
            Assert.StrictEqual( Constants.DEFAULT_ISCSI_PORT_NUM, argconf.PortNumber )
            Assert.NotStrictEqual( npn.NetworkPortal.IdentNumber, argconf.IdentNumber )
            flg1 <- true
            CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddPortal_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create networkportal /a aaa" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn
        let mutable flg1 = false

        ss.p_AddNetworkPortalNode <- ( fun argtd argconf ->
            Assert.Same( tdn, argtd )
            Assert.StrictEqual( "aaa", argconf.TargetAddress )
            Assert.StrictEqual( Constants.DEFAULT_ISCSI_PORT_NUM, argconf.PortNumber )
            flg1 <- true
            CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddPortal_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create networkportal /p 123" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn
        let mutable flg1 = false

        ss.p_AddNetworkPortalNode <- ( fun argtd argconf ->
            Assert.Same( tdn, argtd )
            Assert.StrictEqual( "", argconf.TargetAddress )
            Assert.StrictEqual( 123us, argconf.PortNumber )
            flg1 <- true
            CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddPortal_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create networkportal" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 2 do
            let nid = cnr.NextID
            let tgn = new ConfNode_TargetGroup( st, cnr, nid, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        for _ = ClientConst.MAX_CHILD_NODE_COUNT - 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let nid = cnr.NextID
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
            let tgn = new ConfNode_NetworkPortal( st, cnr, nid, conf ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        ss.p_AddNetworkPortalNode <- ( fun _ _ ->
            CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.AddPortal_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create networkportal" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 2 do
            let nid = cnr.NextID
            let tgn = new ConfNode_TargetGroup( st, cnr, nid, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        for _ = ClientConst.MAX_CHILD_NODE_COUNT - 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let nid = cnr.NextID
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
            let tgn = new ConfNode_NetworkPortal( st, cnr, nid, conf ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


    [<Fact>]
    member _.Create_TargetGroup_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create targetgroup" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn
        let mutable flg1 = false

        ss.p_AddTargetGroupNode <- ( fun argtd newTgid tgName eas ->
            flg1 <- true
            Assert.Same( tdn, argtd )
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetGroup_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create targetgroup" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "aaa", true, ModifiedStatus.NotModified )
        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID ( tgn :> IConfigureNode ).NodeID 
        let mutable flg1 = false

        ss.p_AddTargetGroupNode <- ( fun argtd newTgid tgName eas ->
            flg1 <- true
            Assert.Same( tdn, argtd )
            Assert.NotStrictEqual( tgn.TargetGroupID, newTgid )
            Assert.NotStrictEqual( tgn.TargetGroupName, tgName )
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetGroup_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create targetgroup /n bbb" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn
        let mutable flg1 = false

        ss.p_AddTargetGroupNode <- ( fun argtd newTgid tgName eas ->
            flg1 <- true
            Assert.Same( tdn, argtd )
            Assert.StrictEqual( "bbb", tgName )
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetGroup_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create targetgroup" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 2 do
            let nid = cnr.NextID
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
            let tgn = new ConfNode_NetworkPortal( st, cnr, nid, conf ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        for _ = ClientConst.MAX_CHILD_NODE_COUNT - 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let nid = cnr.NextID
            let tgn = new ConfNode_TargetGroup( st, cnr, nid, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        ss.p_AddTargetGroupNode <- ( fun argtd newTgid tgName eas ->
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_TargetGroup_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create targetgroup" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 2 do
            let nid = cnr.NextID
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
            let tgn = new ConfNode_NetworkPortal( st, cnr, nid, conf ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        for _ = ClientConst.MAX_CHILD_NODE_COUNT - 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let nid = cnr.NextID
            let tgn = new ConfNode_TargetGroup( st, cnr, nid, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
            cnr.AddNode tgn
            cnr.AddRelation ( tdn :> IConfigureNode ).NodeID nid

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode ) )
        Assert.True( r )
        Assert.True(( stat = Some( ss, cc, npnode ) ))

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_INVALID_PARAM_PATTERN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_002 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /fadr aaa /fmask bbb" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode ) )
        Assert.True( r )
        Assert.True(( stat = Some( ss, cc, npnode ) ))

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_INVALID_PARAM_PATTERN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_003 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t aaa" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode ) )
        Assert.True( r )
        Assert.True(( stat = Some( ss, cc, npnode ) ))

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CMDMSG_PARAMVAL_INVALID_PARAM_PATTERN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_004 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t any" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun _ c ->
            flg1 <- true
            Assert.StrictEqual( [ IPCondition.Any ], c.WhiteList )
            npnode
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True flg1

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "IP white list updated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_005 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /fadr 192.168.1.1 /fmask 255.255.0.0" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun _ c ->
            flg1 <- true
            let cond = IPCondition.IPFilter(
                ( IPAddress.Parse "192.168.1.1" ).GetAddressBytes(),
                ( IPAddress.Parse "255.255.0.0" ).GetAddressBytes()
            )
            Assert.StrictEqual( [ cond ], c.WhiteList )
            npnode
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True flg1

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "IP white list updated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_006 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t Any" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode1 = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let npnode2 = npnode1.CreateUpdatedNode {
            npnode1.NetworkPortal with 
                WhiteList = [
                    for i = 1 to Constants.MAX_IP_WHITELIST_COUNT - 1 do
                        yield IPCondition.Any;
                ];
        }

        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun _ c ->
            flg1 <- true
            Assert.StrictEqual( Constants.MAX_IP_WHITELIST_COUNT, c.WhiteList.Length )
            npnode2
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode2 ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True flg1

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "IP white list updated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_007 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t Any" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode1 = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let npnode2 = npnode1.CreateUpdatedNode {
            npnode1.NetworkPortal with 
                WhiteList = [
                    for i = 1 to Constants.MAX_IP_WHITELIST_COUNT do
                        yield IPCondition.Any;
                ];
        }

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode2 ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "CHKMSG_IP_WHITELIST_TOO_LONG"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_008 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t any" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let crnode1 = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let mutable flg1 = false

        ss.p_UpdateControllerNode <- ( fun _ ->
            flg1 <- true
            crnode1
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, crnode1 ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True flg1

        let out_rs = CheckOutputMessage out_ms out_ws "CR" "IP white list updated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_009 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /fadr 192.168.1.1 /fmask 255.255.0.0" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let crnode1 = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let mutable flg1 = false

        ss.p_UpdateControllerNode <- ( fun c ->
            flg1 <- true
            let cond = IPCondition.IPFilter(
                ( IPAddress.Parse "192.168.1.1" ).GetAddressBytes(),
                ( IPAddress.Parse "255.255.0.0" ).GetAddressBytes()
            )
            Assert.StrictEqual( [ cond ], c.RemoteCtrl.Value.WhiteList )
            crnode1
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, crnode1 ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True flg1

        let out_rs = CheckOutputMessage out_ms out_ws "CR" "IP white list updated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_010 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t any" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let crnode1 = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let crnode2 = crnode1.CreateUpdatedNode {
            crnode1.GetConfigureData() with
                RemoteCtrl = Some {
                    crnode1.GetConfigureData().RemoteCtrl.Value with
                        WhiteList = [
                            for i = 1 to Constants.MAX_IP_WHITELIST_COUNT - 1 do
                                yield IPCondition.Any
                        ]
                }
        }

        let mutable flg1 = false

        ss.p_UpdateControllerNode <- ( fun c ->
            flg1 <- true
            Assert.StrictEqual( Constants.MAX_IP_WHITELIST_COUNT, c.RemoteCtrl.Value.WhiteList.Length )
            crnode1
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, crnode2 ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True flg1

        let out_rs = CheckOutputMessage out_ms out_ws "CR" "IP white list updated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Add_IPWhiteList_011 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList /t any" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let crnode1 = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let crnode2 = crnode1.CreateUpdatedNode {
            crnode1.GetConfigureData() with
                RemoteCtrl = Some {
                    crnode1.GetConfigureData().RemoteCtrl.Value with
                        WhiteList = [
                            for i = 1 to Constants.MAX_IP_WHITELIST_COUNT do
                                yield IPCondition.Any
                        ]
                }
        }

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, crnode2 ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))

        let out_rs = CheckOutputMessage out_ms out_ws "CR" "CHKMSG_IP_WHITELIST_TOO_LONG"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Clear_IPWhiteList_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "clear IPWhiteList" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode1 = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun _ _ ->
            flg1 <- true
            npnode1
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode1 ) )
        Assert.True( r )
        Assert.True stat.IsSome
        Assert.True flg1
        match stat.Value with
        | ( _, _, n ) ->
            Assert.Empty( ( n :?> ConfNode_NetworkPortal ).NetworkPortal.WhiteList )

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "IP white list cleared"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Clear_IPWhiteList_002 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "clear IPWhiteList" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let npnode1 = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let npnode2 = npnode1.CreateUpdatedNode {
            npnode1.NetworkPortal with 
                WhiteList = [ IPCondition.Any ];
        }
        let mutable flg1 = false

        ss.p_UpdateNetworkPortalNode <- ( fun _ _ ->
            flg1 <- true
            npnode1
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode2 ) )
        Assert.True( r )
        Assert.True stat.IsSome
        Assert.True flg1
        match stat.Value with
        | ( _, _, n ) ->
            Assert.Empty( ( n :?> ConfNode_NetworkPortal ).NetworkPortal.WhiteList )

        let out_rs = CheckOutputMessage out_ms out_ws "NP" "IP white list cleared"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Clear_IPWhiteList_003 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "clear IPWhiteList" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let crnode1 = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let mutable flg1 = false

        ss.p_UpdateControllerNode <- ( fun _ ->
            flg1 <- true
            crnode1
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, crnode1 ) )
        Assert.True( r )
        Assert.True stat.IsSome
        Assert.True flg1
        match stat.Value with
        | ( _, _, n ) ->
            Assert.Empty( ( n :?> ConfNode_Controller ).GetConfigureData().RemoteCtrl.Value.WhiteList )

        let out_rs = CheckOutputMessage out_ms out_ws "CR" "IP white list cleared"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


    [<Fact>]
    member _.Clear_IPWhiteList_004  () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "clear IPWhiteList" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let crnode1 = CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller
        let crnode2 = crnode1.CreateUpdatedNode {
            crnode1.GetConfigureData() with
                RemoteCtrl = Some {
                    crnode1.GetConfigureData().RemoteCtrl.Value with
                        WhiteList = []
                }
        }
        let mutable flg1 = false

        ss.p_UpdateControllerNode <- ( fun _ ->
            flg1 <- true
            crnode1
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, crnode2 ) )
        Assert.True( r )
        Assert.True stat.IsSome
        Assert.True flg1
        match stat.Value with
        | ( _, _, n ) ->
            Assert.Empty( ( n :?> ConfNode_Controller ).GetConfigureData().RemoteCtrl.Value.WhiteList )

        let out_rs = CheckOutputMessage out_ms out_ws "CR" "IP white list cleared"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Load_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "load" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun _ _ -> Task.CompletedTask )
        cc.p_LoadTargetGroup <- ( fun argtdid argtgid ->
            flg3 <- true
            Assert.StrictEqual( argtdid, tdn.TargetDeviceID )
            Assert.StrictEqual( argtgid, tgn.TargetGroupID )
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Loaded"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Load_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "load" )
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> None )
        ss.p_GetAncestorTargetGroup <- ( fun argnode -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tgn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Load_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "load" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Load_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "load" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            let a = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            a.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "CMDMSG_CONFIG_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Load_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "load" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Unload_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unload" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_UnloadTargetGroup <- ( fun argtdid argtgid ->
            flg3 <- true
            Assert.StrictEqual( argtdid, tdn.TargetDeviceID )
            Assert.StrictEqual( argtgid, tgn.TargetGroupID )
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Unloaded"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Unload_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unload" )
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> None )
        ss.p_GetAncestorTargetGroup <- ( fun argnode -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tgn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Unload_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unload" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Unload_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unload" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Activate_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "activate" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_ActivateTargetGroup <- ( fun argtdid argtgid ->
            flg3 <- true
            Assert.StrictEqual( argtdid, tdn.TargetDeviceID )
            Assert.StrictEqual( argtgid, tgn.TargetGroupID )
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Activated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Activate_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "activate" )
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> None )
        ss.p_GetAncestorTargetGroup <- ( fun argnode -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tgn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Activate_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "activate" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Activate_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "activate" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


    [<Fact>]
    member _.Inactivate_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "inactivate" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_InactivateTargetGroup <- ( fun argtdid argtgid ->
            flg3 <- true
            Assert.StrictEqual( argtdid, tdn.TargetDeviceID )
            Assert.StrictEqual( argtgid, tgn.TargetGroupID )
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Inactivated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Inactivate_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "inactivate" )
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> None )
        ss.p_GetAncestorTargetGroup <- ( fun argnode -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tgn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Inactivate_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "inactivate" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg2 <- true
            Some tgn
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Inactivate_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "inactivate" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Target_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_AddTargetNode <- ( fun argtgnode conf ->
            Assert.Same( tgn, argtgnode )
            flg2 <- true
            ( CommandRunner_Test1.m_TargetNode :?> ConfNode_Target )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Target_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        let tgn1 = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tgn2 = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn1 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let tn2 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 11us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode tdn
        cnr.AddNode tgn1
        cnr.AddNode tgn2
        cnr.AddNode tn1
        cnr.AddNode tn2
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID ( tgn1 :> IConfigureNode ).NodeID
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID ( tgn2 :> IConfigureNode ).NodeID
        cnr.AddRelation ( tgn1 :> IConfigureNode ).NodeID ( tn1 :> IConfigureNode ).NodeID
        cnr.AddRelation ( tgn2 :> IConfigureNode ).NodeID ( tn2 :> IConfigureNode ).NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn1, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_AddTargetNode <- ( fun argtgnode conf ->
            Assert.Same( tgn1, argtgnode )
            Assert.NotStrictEqual( tn1.Values.IdentNumber, conf.IdentNumber )
            Assert.NotStrictEqual( tn1.Values.TargetName, conf.TargetName )
            Assert.NotStrictEqual( tn2.Values.IdentNumber, conf.IdentNumber )
            Assert.NotStrictEqual( tn2.Values.TargetName, conf.TargetName )
            flg2 <- true
            ( CommandRunner_Test1.m_TargetNode :?> ConfNode_Target )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn1 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn1 ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Target_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /n aaa" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tgn, argnode )
            flg1 <- true
            Some tdn
        )
        ss.p_AddTargetNode <- ( fun argtgnode conf ->
            Assert.Same( tgn, argtgnode )
            Assert.StrictEqual( "aaa", conf.TargetName )
            flg2 <- true
            ( CommandRunner_Test1.m_TargetNode :?> ConfNode_Target )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Target_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        let tgn1 = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        cnr.AddNode tgn1
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID ( tgn1 :> IConfigureNode ).NodeID

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let tn1 =
                let conf : TargetGroupConf.T_Target = {
                    IdentNumber = tnodeidx_me.fromPrim 10us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "";
                    TargetAlias = "";
                    LUN = [];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
                new ConfNode_Target( st, cnr, cnr.NextID, conf )
            cnr.AddNode tn1
            cnr.AddRelation ( tgn1 :> IConfigureNode ).NodeID ( tn1 :> IConfigureNode ).NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> Some tdn )
        ss.p_AddTargetNode <- ( fun argtgnode conf ->
            ( CommandRunner_Test1.m_TargetNode :?> ConfNode_Target )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn1 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn1 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Target_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        let tgn1 = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        cnr.AddNode tgn1
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID ( tgn1 :> IConfigureNode ).NodeID

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let tn1 =
                let conf : TargetGroupConf.T_Target = {
                    IdentNumber = tnodeidx_me.fromPrim 10us;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = "";
                    TargetAlias = "";
                    LUN = [];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
                new ConfNode_Target( st, cnr, cnr.NextID, conf )
            cnr.AddNode tn1
            cnr.AddRelation ( tgn1 :> IConfigureNode ).NodeID ( tn1 :> IConfigureNode ).NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> Some tdn )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn1 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn1 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SetChap_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "setchap /iu aaa /ip bbb /tu ccc /tp ddd" )
        let tn1 = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let tn2 = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argtnode conf ->
            flg1 <- true
            Assert.Same( tn1, argtnode )
            match conf.Auth with
            | TargetGroupConf.U_CHAP( x ) ->
                Assert.StrictEqual( "aaa", x.InitiatorAuth.UserName )
                Assert.StrictEqual( "bbb", x.InitiatorAuth.Password )
                Assert.StrictEqual( "ccc", x.TargetAuth.UserName )
                Assert.StrictEqual( "ddd", x.TargetAuth.Password )
            | _ -> Assert.Fail __LINE__
            tn2
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn1 ) )
        Assert.True( r )
        Assert.True( stat.IsSome )
        match stat.Value with
        | ( x_ss, x_cc, x_tn ) ->
            Assert.Same( ss, x_ss )
            Assert.Same( cc, x_cc )
            Assert.Same( tn2, x_tn )
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Set CHAP authentication"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<InlineData( "setchap /iu aaa /ip bbb" )>]
    [<InlineData( "setchap /iu aaa /ip bbb /tu ccc" )>]
    [<InlineData( "setchap /iu aaa /ip bbb /tp ddd" )>]
    member _.SetChap_002 ( cmdstr : string ) =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( cmdstr )
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argtnode conf ->
            flg1 <- true
            Assert.Same( tn, argtnode )
            match conf.Auth with
            | TargetGroupConf.U_CHAP( x ) ->
                Assert.StrictEqual( "aaa", x.InitiatorAuth.UserName )
                Assert.StrictEqual( "bbb", x.InitiatorAuth.Password )
                Assert.StrictEqual( "", x.TargetAuth.UserName )
                Assert.StrictEqual( "", x.TargetAuth.Password )
            | _ -> Assert.Fail __LINE__
            CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat.IsSome ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Set CHAP authentication"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.UnsetAuth_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "unsetauth" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_CHAP({
                    InitiatorAuth = {
                        UserName = "aaa";
                        Password = "bbb";
                    };
                    TargetAuth = {
                        UserName = "ccc";
                        Password = "ddd";
                    };
                });
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        cnr.AddNode tn
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argtnode conf ->
            flg1 <- true
            Assert.Same( tn, argtnode )
            match conf.Auth with
            | TargetGroupConf.U_None( _ ) -> ()
            | _ -> Assert.Fail __LINE__
            CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Authentication reset"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_LU_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /l 2" )
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_AddBlockDeviceLUNode <- ( fun tnode lun luname mm otl ->
            flg1 <- true
            Assert.Same( tn, tnode )
            Assert.True(( lun = lun_me.fromPrim 2UL ))
            CommandRunner_Test1.m_BlockDeviceLUNode :?>ConfNode_BlockDeviceLU
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_LU_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create" )
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_LU_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /l 3 /n aaa" )
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_AddBlockDeviceLUNode <- ( fun tnode lun luname mm otl ->
            flg1 <- true
            Assert.Same( tn, tnode )
            Assert.StrictEqual( lun_me.fromPrim 3UL, lun )
            Assert.StrictEqual( "aaa", luname )
            Assert.StrictEqual( Constants.LU_DEF_MULTIPLICITY, mm )
            CommandRunner_Test1.m_BlockDeviceLUNode :?>ConfNode_BlockDeviceLU
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_LU_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /l 2" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID tgn.NodeID

        let tn1 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn1
        cnr.AddRelation tgn.NodeID tn1.NodeID

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let mult = Constants.LU_DEF_MULTIPLICITY
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let n = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.zero, "", mult, otl ) :> IConfigureNode
            cnr.AddNode n
            cnr.AddRelation tn1.NodeID n.NodeID

        ss.p_AddBlockDeviceLUNode <- ( fun tnode lun luname mm otl ->
            CommandRunner_Test1.m_BlockDeviceLUNode :?>ConfNode_BlockDeviceLU
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn1 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn1 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_LU_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create /l 2" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID tgn.NodeID

        let tn1 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn1
        cnr.AddRelation tgn.NodeID tn1.NodeID

        for _ = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let mult = Constants.LU_DEF_MULTIPLICITY
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let n = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.zero, "", mult, otl ) :> IConfigureNode
            cnr.AddNode n
            cnr.AddRelation tn1.NodeID n.NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn1 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn1 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


    [<Fact>]
    member _.Attach_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "attach" )
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tn, argnode )
            flg1 <- true
            Some tgn
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Attach_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "attach /l 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mutable flg1 = false

        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddRelation ( tgn :> IConfigureNode ).NodeID ( tn :> IConfigureNode ).NodeID

        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tn, argnode )
            flg1 <- true
            Some tgn
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_MISSING_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Attach_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "attach /l 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl )
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddNode lunode
        cnr.AddRelation ( tgn :> IConfigureNode ).NodeID ( tn :> IConfigureNode ).NodeID
        cnr.AddRelation ( tn :> IConfigureNode ).NodeID ( lunode :> IConfigureNode ).NodeID

        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tn, argnode )
            flg1 <- true
            Some tgn
        )
        ss.p_AddTargetLURelation <- ( fun tnode arglunode ->
            Assert.True(( tnode = tn ))
            Assert.Same( lunode, arglunode )
            flg2 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Attach LU"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Attach_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "attach /l 99" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl )
        let mutable flg1 = false

        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddNode lunode
        cnr.AddRelation ( tgn :> IConfigureNode ).NodeID ( tn :> IConfigureNode ).NodeID
        cnr.AddRelation ( tn :> IConfigureNode ).NodeID ( lunode :> IConfigureNode ).NodeID

        ss.p_GetAncestorTargetGroup <- ( fun argnode ->
            Assert.Same( tn, argnode )
            flg1 <- true
            Some tgn
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_MISSING_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Attach_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "attach /l 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID tgn.NodeID

        let tn1 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "t001";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn1
        cnr.AddRelation tgn.NodeID tn1.NodeID

        let tn2 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 11us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "t002";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn2
        cnr.AddRelation tgn.NodeID tn2.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let mult = Constants.LU_DEF_MULTIPLICITY
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let n = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim( uint64 i ), "", mult, otl ) :> IConfigureNode
            cnr.AddNode n
            cnr.AddRelation tn1.NodeID n.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let wlun = lun_me.fromPrim( uint64 i + uint64 ClientConst.MAX_CHILD_NODE_COUNT )
            let mult = Constants.LU_DEF_MULTIPLICITY
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let n = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, wlun, "", mult, otl ) :> IConfigureNode
            cnr.AddNode n
            cnr.AddRelation tn2.NodeID n.NodeID

        ss.p_GetAncestorTargetGroup <- ( fun argnode -> Some ( tgn :?> ConfNode_TargetGroup ) )
        ss.p_AddTargetLURelation <- ( fun tnode arglunode -> () )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn2 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Attach LU"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Attach_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "attach /l 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf )
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation ( tdn :> IConfigureNode ).NodeID tgn.NodeID

        let tn1 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "t001";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn1
        cnr.AddRelation tgn.NodeID tn1.NodeID

        let tn2 =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 11us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "t002";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn2
        cnr.AddRelation tgn.NodeID tn2.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let mult = Constants.LU_DEF_MULTIPLICITY
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let n = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim( uint64 i ), "", mult, otl ) :> IConfigureNode
            cnr.AddNode n
            cnr.AddRelation tn1.NodeID n.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let wlun = lun_me.fromPrim( uint64 i + uint64 ClientConst.MAX_CHILD_NODE_COUNT )
            let mult = Constants.LU_DEF_MULTIPLICITY
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let n = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, wlun, "", mult, otl ) :> IConfigureNode
            cnr.AddNode n
            cnr.AddRelation tn2.NodeID n.NodeID

        ss.p_GetAncestorTargetGroup <- ( fun argnode -> Some ( tgn :?> ConfNode_TargetGroup ) )
        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn2 ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn2 ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Detach_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "detach" )
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Detach_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "detach /l 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )

        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddRelation ( tgn :> IConfigureNode ).NodeID ( tn :> IConfigureNode ).NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_MISSING_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Detach_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "detach /l 1" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl )
        let mutable flg2 = false

        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddNode lunode
        cnr.AddRelation ( tgn :> IConfigureNode ).NodeID ( tn :> IConfigureNode ).NodeID
        cnr.AddRelation ( tn :> IConfigureNode ).NodeID ( lunode :> IConfigureNode ).NodeID

        ss.p_DeleteTargetLURelation <- ( fun tnode arglunode ->
            Assert.Same( tn, tnode )
            Assert.Same( lunode, arglunode )
            flg2 <- true
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Detach LU"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Detach_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "detach /l 99" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified )
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf )
        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl )

        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddNode lunode
        cnr.AddRelation ( tgn :> IConfigureNode ).NodeID ( tn :> IConfigureNode ).NodeID
        cnr.AddRelation ( tn :> IConfigureNode ).NodeID ( lunode :> IConfigureNode ).NodeID

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "CMDMSG_ADDPARAM_MISSING_LUN"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_PlainFile_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create plainfile /n aaa" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( lunode, argnode )
            flg1 <- true
            Some tdn
        )

        ss.p_AddPlainFileMediaNode <- ( fun argcn conf ->
            Assert.Same( lunode, argcn )
            Assert.StrictEqual( "aaa", conf.FileName )
            flg2 <- true
            ( CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia )
        )

        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_PlainFile_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create plainfile /n a" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()
        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
        let dmn2 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 2u, "" ) :> IConfigureNode
        let mutable flg1 = false
        let mutable flg2 = false

        cnr.AddNode tdn
        cnr.AddNode tgn
        cnr.AddNode tn
        cnr.AddNode lunode
        cnr.AddNode dmn1
        cnr.AddNode dmn2
        cnr.AddRelation tdn.NodeID tgn.NodeID
        cnr.AddRelation tgn.NodeID tn.NodeID
        cnr.AddRelation tn.NodeID lunode.NodeID
        cnr.AddRelation lunode.NodeID dmn1.NodeID
        cnr.AddRelation lunode.NodeID dmn2.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( lunode, argnode )
            flg1 <- true
            Some ( tdn :?> ConfNode_TargetDevice )
        )

        ss.p_AddPlainFileMediaNode <- ( fun argcn conf ->
            Assert.Same( lunode, argcn )
            Assert.StrictEqual( "a", conf.FileName )
            Assert.NotStrictEqual( mediaidx_me.fromPrim 1u, conf.IdentNumber )
            Assert.NotStrictEqual( mediaidx_me.fromPrim 2u, conf.IdentNumber )
            flg2 <- true
            ( CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia )
        )

        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_PlainFile_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create plainfile /n a" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()

        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation tdn.NodeID tgn.NodeID

        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn
        cnr.AddRelation tgn.NodeID tn.NodeID

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        cnr.AddNode lunode
        cnr.AddRelation tn.NodeID lunode.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
            cnr.AddNode dmn1
            cnr.AddRelation lunode.NodeID dmn1.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Some ( tdn :?> ConfNode_TargetDevice )
        )
        ss.p_AddPlainFileMediaNode <- ( fun argcn conf ->
            ( CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_PlainFile_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create plainfile /n a" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()

        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation tdn.NodeID tgn.NodeID

        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn
        cnr.AddRelation tgn.NodeID tn.NodeID

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        cnr.AddNode lunode
        cnr.AddRelation tn.NodeID lunode.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
            cnr.AddNode dmn1
            cnr.AddRelation lunode.NodeID dmn1.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Some ( tdn :?> ConfNode_TargetDevice )
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_MemBuffer_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create membuffer /s 512" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( lunode, argnode )
            flg1 <- true
            Some tdn
        )

        ss.p_AddMemBufferMediaNode <- ( fun argcn conf ->
            Assert.Same( lunode, argcn )
            Assert.StrictEqual( 512UL, conf.BytesCount )
            flg2 <- true
            ( CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia )
        )

        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_MemBuffer_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create membuffer /s 512" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()

        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation tdn.NodeID tgn.NodeID

        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn
        cnr.AddRelation tgn.NodeID tn.NodeID

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        cnr.AddNode lunode
        cnr.AddRelation tn.NodeID lunode.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
            cnr.AddNode dmn1
            cnr.AddRelation lunode.NodeID dmn1.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Some ( tdn :?> ConfNode_TargetDevice )
        )
        ss.p_AddMemBufferMediaNode <- ( fun argcn conf ->
            ( CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_MemBuffer_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create membuffer /s 512" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()

        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation tdn.NodeID tgn.NodeID

        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn
        cnr.AddRelation tgn.NodeID tn.NodeID

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        cnr.AddNode lunode
        cnr.AddRelation tn.NodeID lunode.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
            cnr.AddNode dmn1
            cnr.AddRelation lunode.NodeID dmn1.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Some ( tdn :?> ConfNode_TargetDevice )
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_Debug_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create debug" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( lunode, argnode )
            flg1 <- true
            Some tdn
        )

        ss.p_AddDebugMediaNode <- ( fun argcn ident name ->
            Assert.Same( lunode, argcn )
            Assert.StrictEqual( "", name )
            flg2 <- true
            ( CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia )
        )

        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_Debug_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create debug" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()

        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation tdn.NodeID tgn.NodeID

        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn
        cnr.AddRelation tgn.NodeID tn.NodeID

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        cnr.AddNode lunode
        cnr.AddRelation tn.NodeID lunode.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT - 1 do
            let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
            cnr.AddNode dmn1
            cnr.AddRelation lunode.NodeID dmn1.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Some ( tdn :?> ConfNode_TargetDevice )
        )
        ss.p_AddDebugMediaNode <- ( fun argcn ident name ->
            ( CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia )
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node -> Task.FromResult () )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Created"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Create_Media_Debug_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "create debug" )
        let st = StringTable( "" )
        let cnr = new ConfNodeRelation()

        let tdn =
            let conf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "";
                EnableStatSNAckChecker = false;
            }
            new ConfNode_TargetDevice( st, cnr, cnr.NextID, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode tdn

        let tgn = new ConfNode_TargetGroup( st, cnr, cnr.NextID, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode tgn
        cnr.AddRelation tdn.NodeID tgn.NodeID

        let tn =
            let conf : TargetGroupConf.T_Target = {
                IdentNumber = tnodeidx_me.fromPrim 10us;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = "";
                TargetAlias = "";
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
            new ConfNode_Target( st, cnr, cnr.NextID, conf ) :> IConfigureNode
        cnr.AddNode tn
        cnr.AddRelation tgn.NodeID tn.NodeID

        let mult = Constants.LU_DEF_MULTIPLICITY
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let lunode = new ConfNode_BlockDeviceLU( st, cnr, cnr.NextID, lun_me.fromPrim 1UL, "", mult, otl ) :> IConfigureNode
        cnr.AddNode lunode
        cnr.AddRelation tn.NodeID lunode.NodeID

        for i = 1 to ClientConst.MAX_CHILD_NODE_COUNT do
            let dmn1 = new ConfNode_DummyMedia( st, cnr, cnr.NextID, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
            cnr.AddNode dmn1
            cnr.AddRelation lunode.NodeID dmn1.NodeID

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Some ( tdn :?> ConfNode_TargetDevice )
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "CMDMSG_TOO_MANY_CHILD"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.InitMedia_PlainFile_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "initmedia plainfile a 1" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_CreateMediaFile_PlainFile <- ( fun fname fsize ->
            Assert.StrictEqual( "a", fname )
            Assert.StrictEqual( 1L, fsize )
            flg1 <- true
            Task.FromResult 0UL
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Started"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMStatus_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imstatus" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            flg1 <- true
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMStatus_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imstatus" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            task {
                flg1 <- true
                let r : HarukaCtrlerCtrlRes.T_Procs = {
                    ProcID = 0UL;
                    PathName = "aaa";
                    FileType = "bbb";
                    Status = HarukaCtrlerCtrlRes.T_Status.U_NotStarted();
                    ErrorMessage = [ "xxx" ];
                }
                return [ r ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ProcID=0, NotStarted"
        let outline2 = ( out_rs.ReadLine() ).TrimStart()
        Assert.StartsWith( "xxx", outline2 )

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMStatus_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imstatus" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            task {
                flg1 <- true
                let r : HarukaCtrlerCtrlRes.T_Procs = {
                    ProcID = 0UL;
                    PathName = "aaa";
                    FileType = "bbb";
                    Status = HarukaCtrlerCtrlRes.T_Status.U_ProgressCreation( 0uy );
                    ErrorMessage = [];
                }
                return [ r ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ProcID=0, Progress"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMStatus_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imstatus" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            task {
                flg1 <- true
                let r : HarukaCtrlerCtrlRes.T_Procs = {
                    ProcID = 0UL;
                    PathName = "aaa";
                    FileType = "bbb";
                    Status = HarukaCtrlerCtrlRes.T_Status.U_Recovery( 0uy );
                    ErrorMessage = [];
                }
                return [ r ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ProcID=0, Recovery"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMStatus_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imstatus" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            task {
                flg1 <- true
                let r : HarukaCtrlerCtrlRes.T_Procs = {
                    ProcID = 0UL;
                    PathName = "aaa";
                    FileType = "bbb";
                    Status = HarukaCtrlerCtrlRes.T_Status.U_NormalEnd();
                    ErrorMessage = [];
                }
                return [ r ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ProcID=0, Succeeded"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMStatus_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imstatus" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            task {
                flg1 <- true
                let r : HarukaCtrlerCtrlRes.T_Procs = {
                    ProcID = 0UL;
                    PathName = "aaa";
                    FileType = "bbb";
                    Status = HarukaCtrlerCtrlRes.T_Status.U_AbnormalEnd();
                    ErrorMessage = [];
                }
                return [ r ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ProcID=0, Failed"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.IMKill_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "imkill 5" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        cc.p_KillInitMediaProc <- ( fun pid ->
            Assert.StrictEqual( 5UL, pid )
            flg1 <- true
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg1 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "Terminated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetDevice_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tdn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetDevice_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetDevice_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetDevice_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetSession_InTargetDevice <- ( fun argtdid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, argtdid )
                let sessList : TargetDeviceCtrlRes.T_Session list = [
                    {
                        TargetGroupID = tgid_me.fromPrim( 99u );
                        TargetNodeID = tnodeidx_me.fromPrim 11us;
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
                return sessList
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Session("
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetGroup_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tgn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetGroup_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetGroup_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID=tgn.TargetGroupID; Name=""; } ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_TargetGroup_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID=tgn.TargetGroupID; Name=""; } ] )

        cc.p_GetSession_InTargetGroup <- ( fun argtdid argtgid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, argtdid )
                Assert.StrictEqual( tgn.TargetGroupID, argtgid )
                let sessList : TargetDeviceCtrlRes.T_Session list = [
                    {
                        TargetGroupID = tgid_me.fromPrim( 99u );
                        TargetNodeID = tnodeidx_me.fromPrim 11us;
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
                return sessList
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Session("
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_Target_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Sessions_Target_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_Target_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID=tgn.TargetGroupID; Name=""; } ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Sessions_Target_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sessions" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID=tgn.TargetGroupID; Name=""; } ] )

        cc.p_GetSession_InTarget <- ( fun argtdid argtid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, argtdid )
                Assert.StrictEqual( tn.Values.IdentNumber, argtid )
                let sessList : TargetDeviceCtrlRes.T_Session list = [
                    {
                        TargetGroupID = tgid_me.fromPrim( 99u );
                        TargetNodeID = tnodeidx_me.fromPrim 11us;
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
                return sessList
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Session("
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SessKill_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sesskill 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tdn, argnode )
            flg1 <- true
            None
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg2 <- true
                return [ tdn.TargetDeviceID ]
            }
        )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tdn ) ) |> ignore
        ) |> ignore
        Assert.True( flg1 )
        Assert.True( flg2 )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.SessKill_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sesskill 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            task {
                flg2 <- true
                return []
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SessKill_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sesskill 1" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.SessKill_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "sesskill 99" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_DestructSession <- ( fun argtdid argtsih ->
            Assert.StrictEqual( tdn.TargetDeviceID, argtdid )
            Assert.StrictEqual( tsih_me.fromPrim 99us, argtsih )
            flg3 <- true
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Session terminated"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetDevice_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.Same( tdn, argnode )
            flg1 <- true
            None
        )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tdn ) ) |> ignore
        ) |> ignore
        Assert.True( flg1 )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Connections_TargetDevice_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->
            flg2 <- true
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg2 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetDevice_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetDevice_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections /s 1" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetConnection_InSession <- ( fun tdid tsih ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( tsih_me.fromPrim 1us, tsih )
                return [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ConnectionID = cid_me.fromPrim 1us;
                        ConnectionCount = concnt_me.fromPrim 1;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "a";
                            HeaderDigest = "b";
                            DataDigest = "c";
                            MaxRecvDataSegmentLength_I = 8192u;
                            MaxRecvDataSegmentLength_T = 8192u;
                        };
                        EstablishTime = DateTime();
                    }
                ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Connection"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetDevice_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetConnection_InTargetDevice <- ( fun tdid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                let conlist : TargetDeviceCtrlRes.T_Connection list = [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ConnectionID = cid_me.fromPrim 1us;
                        ConnectionCount = concnt_me.fromPrim 1;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "a";
                            HeaderDigest = "b";
                            DataDigest = "c";
                            MaxRecvDataSegmentLength_I = 8192u;
                            MaxRecvDataSegmentLength_T = 8192u;
                        };
                        EstablishTime = DateTime();
                    }
                ]
                return conlist
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TD" "Connection"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_NetworkPortal_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let npn = CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetConnection_InNetworkPortal <- ( fun tdid npid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( npn.NetworkPortal.IdentNumber, npid )
                return [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ConnectionID = cid_me.fromPrim 1us;
                        ConnectionCount = concnt_me.fromPrim 1;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "a";
                            HeaderDigest = "b";
                            DataDigest = "c";
                            MaxRecvDataSegmentLength_I = 8192u;
                            MaxRecvDataSegmentLength_T = 8192u;
                        };
                        EstablishTime = DateTime();
                    }
                ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, npn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "NP" "Connection"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetGroup_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tgn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Connections_TargetGroup_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetGroup_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID=tgn.TargetGroupID; Name=""; } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_TargetGroup_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID=tgn.TargetGroupID; Name=""; } ] } )
        cc.p_GetConnection_InTargetGroup <- ( fun tdid tgid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( tgn.TargetGroupID, tgid )
                return [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ConnectionID = cid_me.fromPrim 1us;
                        ConnectionCount = concnt_me.fromPrim 1;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "a";
                            HeaderDigest = "b";
                            DataDigest = "c";
                            MaxRecvDataSegmentLength_I = 8192u;
                            MaxRecvDataSegmentLength_T = 8192u;
                        };
                        EstablishTime = DateTime();
                    }
                ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tgn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "TG" "Connection"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_Target_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, tn ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.Connections_Target_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_Target_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            |> _.SetModified()
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID=tgn.TargetGroupID; Name=""; } ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "T " "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_Target_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tn = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> task { return [ { ID=tgn.TargetGroupID; Name=""; } ] } )
        cc.p_GetConnection_InTarget <- ( fun tdid tid ->
            task {
                flg3 <- true
                Assert.StrictEqual( tdn.TargetDeviceID, tdid )
                Assert.StrictEqual( tn.Values.IdentNumber, tid )
                return [
                    {
                        TSIH = tsih_me.fromPrim 1us;
                        ConnectionID = cid_me.fromPrim 1us;
                        ConnectionCount = concnt_me.fromPrim 1;
                        ReceiveBytesCount = [];
                        SentBytesCount = [];
                        ConnectionParameters = {
                            AuthMethod = "a";
                            HeaderDigest = "b";
                            DataDigest = "c";
                            MaxRecvDataSegmentLength_I = 8192u;
                            MaxRecvDataSegmentLength_T = 8192u;
                        };
                        EstablishTime = DateTime();
                    }
                ]
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tn ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "T " "Connection"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.Connections_NoConnections_014 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "connections" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetConnection_InTargetDevice <- ( fun tdid -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdn ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, tdn ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "TD" ""
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUStatus_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let lun = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, lun ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.LUStatus_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, lunode ) ) |> ignore
        ) |> ignore

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.LUStatus_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUStatus_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUStatus_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )


        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUStatus_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            let n = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            n.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID = tgn.TargetGroupID; Name = "" } ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUStatus_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID = tgn.TargetGroupID; Name = "" } ] )

        cc.p_GetLUStatus <- ( fun tdid lun -> task {
            Assert.StrictEqual( tdn.TargetDeviceID, tdid )
            Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
            return {
                ReadBytesCount = [];
                WrittenBytesCount = [];
                ReadTickCount = [];
                WriteTickCount = [];
                ACAStatus = None;
                TaskDescriptions = [];
            }
        } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "LU Status"
        let outline2 = ( out_rs.ReadLine() ).TrimStart()
        Assert.True(( outline2.StartsWith "ACA : None" ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUStatus_008 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lustatus" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID = tgn.TargetGroupID; Name = "" } ] )

        cc.p_GetLUStatus <- ( fun tdid lun -> task {
            Assert.StrictEqual( tdn.TargetDeviceID, tdid )
            Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
            return {
                ReadBytesCount = [];
                WrittenBytesCount = [];
                ReadTickCount = [];
                WriteTickCount = [];
                ACAStatus = Some {
                    ITNexus = {
                        InitiatorName = "initiator001";
                        ISID = isid_me.fromElem 0uy 1uy 2us 3uy 4us;
                        TargetName = "target001";
                        TPGT = tpgt_me.fromPrim 0us;
                    };
                    StatusCode = uint8 ScsiCmdStatCd.CHECK_CONDITION;
                    SenseKey = uint8 SenseKeyCd.RECOVERED_ERROR;
                    AdditionalSenseCode = uint16 ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT;
                    IsCurrent = true;
                };
                TaskDescriptions = [];
            }
        } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))

        out_ws.Flush()
        out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "LU Status"
        let outline2 = ( out_rs.ReadLine() ).TrimStart()
        Assert.StartsWith( "ACA : {", outline2 )

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUReset_001 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, lunode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.LUReset_002 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> None )

        Assert.ThrowsAny( fun () ->
            CallCommandLoop cr ( Some ( ss, cc, lunode ) ) |> ignore
        ) |> ignore
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.LUReset_003 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUReset_004 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let tdn =
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
            |> _.SetModified()
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> task { return [ tdn.TargetDeviceID ] } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_DEVICE_NOT_RUNNING"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUReset_005 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_GROUP_UNLOADED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUReset_006 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn =
            let n = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
            n.SetModified()
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID = tgn.TargetGroupID; Name = "" } ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "ERRMSG_TARGET_GROUP_MODIFIED"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.LUReset_007 () =
        let in_ms, in_ws, in_rs, out_ms, out_ws, cr, ss, cc = GenStub( "lureset" )
        let tdn = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let tgn = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdn )
        ss.p_GetAncestorTargetGroup <- ( fun _ -> Some tgn )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdn.TargetDeviceID ] )
        cc.p_GetLoadedTargetGroups <- ( fun _ -> Task.FromResult [ { ID = tgn.TargetGroupID; Name = "" } ] )

        cc.p_LUReset <- ( fun tdid lun -> task {
            Assert.StrictEqual( tdn.TargetDeviceID, tdid )
            Assert.StrictEqual( ( lunode :> ILUNode ).LUN, lun )
            flg3 <- true
        })

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, lunode ) )
        Assert.True( r )
        Assert.True(( stat = Some ( ss, cc, lunode ) ))
        Assert.True( flg3 )
        let out_rs = CheckOutputMessage out_ms out_ws "LU" "LU Reseted"
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]


