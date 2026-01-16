//=============================================================================
// Haruka Software Storage.
// CommandRunnerTest1.fs : Test cases for CommandRunner class.
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
// Type definition

type ServerStatusStub( m_MessageTable : StringTable ) =
    inherit ServerStatus( m_MessageTable )

    let mutable m_LoadConfigure : ( CtrlConnection -> bool -> Task ) option = None
    let mutable m_Publish : ( CtrlConnection -> Task ) option = None
    let mutable m_Validate : ( unit -> ( CONFNODE_T * string ) list ) option = None
    let mutable m_GetNode : ( CONFNODE_T -> IConfigureNode ) option = None
    let mutable m_IsModified : bool option = None
    let mutable m_ControllerNodeID : CONFNODE_T option = None
    let mutable m_ControllerNode : ConfNode_Controller option = None
    let mutable m_UpdateControllerNode : ( HarukaCtrlConf.T_HarukaCtrl -> ConfNode_Controller ) option = None
    let mutable m_GetTargetDeviceNodes : ( unit -> ConfNode_TargetDevice list ) option = None
    let mutable m_AddTargetDeviceNode : ( TDID_T -> string -> bool -> TargetDeviceConf.T_NegotiableParameters -> TargetDeviceConf.T_LogParameters -> ConfNode_TargetDevice ) option = None
    let mutable m_DeleteTargetDeviceNode : ( ConfNode_TargetDevice -> unit ) option = None
    let mutable m_UpdateTargetDeviceNode : ( ConfNode_TargetDevice -> TDID_T -> string -> bool -> TargetDeviceConf.T_NegotiableParameters -> TargetDeviceConf.T_LogParameters -> ConfNode_TargetDevice ) option = None
    let mutable m_AddNetworkPortalNode : ( ConfNode_TargetDevice -> TargetDeviceConf.T_NetworkPortal -> ConfNode_NetworkPortal ) option = None
    let mutable m_DeleteNetworkPortalNode : ( ConfNode_NetworkPortal -> unit ) option = None
    let mutable m_UpdateNetworkPortalNode : ( ConfNode_NetworkPortal -> TargetDeviceConf.T_NetworkPortal -> ConfNode_NetworkPortal ) option = None
    let mutable m_AddTargetGroupNode : ( ConfNode_TargetDevice -> TGID_T -> string -> bool -> ConfNode_TargetGroup ) option = None
    let mutable m_DeleteTargetGroupNode : ( ConfNode_TargetGroup -> unit ) option = None
    let mutable m_UpdateTargetGroupNode : ( ConfNode_TargetGroup -> TGID_T -> string -> bool -> ConfNode_TargetGroup ) option = None
    let mutable m_DeleteNodeInTargetGroup : ( IConfigureNode -> unit ) option = None
    let mutable m_AddTargetNode : ( ConfNode_TargetGroup -> TargetGroupConf.T_Target -> ConfNode_Target ) option = None
    let mutable m_UpdateTargetNode : ( ConfNode_Target -> TargetGroupConf.T_Target -> ConfNode_Target ) option = None
    let mutable m_AddTargetLURelation : ( ConfNode_Target -> ILUNode -> unit ) option = None
    let mutable m_DeleteTargetLURelation : ( ConfNode_Target -> ILUNode -> unit ) option = None
    let mutable m_AddBlockDeviceLUNode : ( ConfNode_Target -> LUN_T -> string -> uint32 -> Blocksize -> BLKCNT32_T -> ConfNode_BlockDeviceLU ) option = None
    let mutable m_UpdateBlockDeviceLUNode : ( ConfNode_BlockDeviceLU -> LUN_T -> string -> uint32 -> Blocksize -> BLKCNT32_T -> ConfNode_BlockDeviceLU ) option = None
    let mutable m_AddDummyDeviceLUNode : ( ConfNode_Target -> LUN_T -> string -> uint32 -> ConfNode_DummyDeviceLU ) option = None
    let mutable m_UpdateDummyDeviceLUNode : ( ConfNode_DummyDeviceLU -> LUN_T -> string -> uint32 -> ConfNode_DummyDeviceLU ) option = None
    let mutable m_AddPlainFileMediaNode : ( IConfigureNode -> TargetGroupConf.T_PlainFile -> ConfNode_PlainFileMedia ) option = None
    let mutable m_UpdatePlainFileMediaNode : ( ConfNode_PlainFileMedia -> TargetGroupConf.T_PlainFile -> ConfNode_PlainFileMedia ) option = None
    let mutable m_AddMemBufferMediaNode : ( IConfigureNode -> TargetGroupConf.T_MemBuffer -> ConfNode_MemBufferMedia ) option = None
    let mutable m_UpdateMemBufferMediaNode : ( ConfNode_MemBufferMedia -> TargetGroupConf.T_MemBuffer -> ConfNode_MemBufferMedia ) option = None
    let mutable m_AddDummyMediaNode : ( IConfigureNode -> MEDIAIDX_T -> string -> ConfNode_DummyMedia ) option = None
    let mutable m_UpdateDummyMediaNode : ( ConfNode_DummyMedia -> MEDIAIDX_T -> string -> ConfNode_DummyMedia ) option = None
    let mutable m_AddDebugMediaNode : ( IConfigureNode -> MEDIAIDX_T -> string -> ConfNode_DebugMedia ) option = None
    let mutable m_UpdateDebugMediaNode : ( ConfNode_DebugMedia -> MEDIAIDX_T -> string -> ConfNode_DebugMedia ) option = None
    let mutable m_GetAncestorTargetDevice : ( IConfigureNode -> ConfNode_TargetDevice option ) option = None
    let mutable m_GetAncestorTargetGroup : ( IConfigureNode -> ConfNode_TargetGroup option ) option = None
    let mutable m_GetAncestorLogicalUnit : ( IConfigureNode -> ILUNode option ) option = None
    let mutable m_TryCheckTargetDeviceUnloaded : ( CtrlConnection -> IConfigureNode -> Task<bool> ) option = None
    let mutable m_CheckTargetDeviceUnloaded : ( CtrlConnection -> IConfigureNode -> Task ) option = None
    let mutable m_TryCheckTargetGroupUnloaded : ( CtrlConnection -> IConfigureNode -> Task<bool> ) option = None
    let mutable m_CheckTargetGroupUnloaded : ( CtrlConnection -> IConfigureNode -> Task ) option = None
    let mutable m_ExportTemporaryDump : ( CONFNODE_T -> bool -> string ) option = None
    let mutable m_ImportTemporaryDump : ( string -> CONFNODE_T -> bool -> IConfigureNode ) option = None

    member _.p_LoadConfigure with set v = m_LoadConfigure <- Some( v )
    member _.p_Publish with set v = m_Publish <- Some( v )
    member _.p_Validate with set v = m_Validate <- Some( v )
    member _.p_GetNode with set v = m_GetNode <- Some( v )
    member _.p_IsModified with set v = m_IsModified <- Some( v )
    member _.p_ControllerNodeID with set v = m_ControllerNodeID <- Some( v )
    member _.p_ControllerNode with set v = m_ControllerNode <- Some( v )
    member _.p_UpdateControllerNode with set v = m_UpdateControllerNode <- Some( v )
    member _.p_GetTargetDeviceNodes with set v = m_GetTargetDeviceNodes <- Some( v )
    member _.p_AddTargetDeviceNode with set v = m_AddTargetDeviceNode <- Some( v )
    member _.p_DeleteTargetDeviceNode with set v = m_DeleteTargetDeviceNode <- Some( v )
    member _.p_UpdateTargetDeviceNode with set v = m_UpdateTargetDeviceNode <- Some( v )
    member _.p_AddNetworkPortalNode with set v = m_AddNetworkPortalNode <- Some( v )
    member _.p_DeleteNetworkPortalNode with set v = m_DeleteNetworkPortalNode <- Some( v )
    member _.p_UpdateNetworkPortalNode with set v = m_UpdateNetworkPortalNode <- Some( v )
    member _.p_AddTargetGroupNode with set v = m_AddTargetGroupNode <- Some( v )
    member _.p_DeleteTargetGroupNode with set v = m_DeleteTargetGroupNode <- Some( v )
    member _.p_UpdateTargetGroupNode with set v = m_UpdateTargetGroupNode <- Some( v )
    member _.p_DeleteNodeInTargetGroup with set v = m_DeleteNodeInTargetGroup <- Some( v )
    member _.p_AddTargetNode with set v = m_AddTargetNode <- Some( v )
    member _.p_UpdateTargetNode with set v = m_UpdateTargetNode <- Some( v )
    member _.p_AddTargetLURelation with set v = m_AddTargetLURelation <- Some( v )
    member _.p_DeleteTargetLURelation with set v = m_DeleteTargetLURelation <- Some( v )
    member _.p_AddBlockDeviceLUNode with set v = m_AddBlockDeviceLUNode <- Some( v )
    member _.p_UpdateBlockDeviceLUNode with set v = m_UpdateBlockDeviceLUNode <- Some( v )
    member _.p_AddDummyDeviceLUNode with set v = m_AddDummyDeviceLUNode <- Some( v )
    member _.p_UpdateDummyDeviceLUNode with set v = m_UpdateDummyDeviceLUNode <- Some( v )
    member _.p_AddPlainFileMediaNode with set v = m_AddPlainFileMediaNode <- Some( v )
    member _.p_UpdatePlainFileMediaNode with set v = m_UpdatePlainFileMediaNode <- Some( v )
    member _.p_AddMemBufferMediaNode with set v = m_AddMemBufferMediaNode <- Some( v )
    member _.p_UpdateMemBufferMediaNode with set v = m_UpdateMemBufferMediaNode <- Some( v )
    member _.p_AddDummyMediaNode with set v = m_AddDummyMediaNode <- Some( v )
    member _.p_UpdateDummyMediaNode with set v = m_UpdateDummyMediaNode <- Some( v )
    member _.p_AddDebugMediaNode with set v = m_AddDebugMediaNode <- Some( v )
    member _.p_UpdateDebugMediaNode with set v = m_UpdateDebugMediaNode <- Some( v )
    member _.p_GetAncestorTargetDevice with set v = m_GetAncestorTargetDevice <- Some( v )
    member _.p_GetAncestorTargetGroup with set v = m_GetAncestorTargetGroup <- Some( v )
    member _.p_GetAncestorLogicalUnit with set v = m_GetAncestorLogicalUnit <- Some( v )
    member _.p_TryCheckTargetDeviceUnloaded with set v = m_TryCheckTargetDeviceUnloaded <- Some( v )
    member _.p_CheckTargetDeviceUnloaded with set v = m_CheckTargetDeviceUnloaded <- Some( v )
    member _.p_TryCheckTargetGroupUnloaded with set v = m_TryCheckTargetGroupUnloaded <- Some( v )
    member _.p_CheckTargetGroupUnloaded with set v = m_CheckTargetGroupUnloaded <- Some( v )
    member _.p_ExportTemporaryDump with set v = m_ExportTemporaryDump <- Some( v )
    member _.p_ImportTemporaryDump with set v = m_ImportTemporaryDump <- Some( v )

    override _.LoadConfigure con forCLI = m_LoadConfigure.Value con forCLI
    override _.Publish con = m_Publish.Value con
    override _.Validate() = m_Validate.Value()
    override _.GetNode nodeID = m_GetNode.Value nodeID
    override _.IsModified = m_IsModified.Value
    override _.ControllerNodeID = m_ControllerNodeID.Value
    override _.ControllerNode = m_ControllerNode.Value
    override _.UpdateControllerNode conf = m_UpdateControllerNode.Value conf
    override _.GetTargetDeviceNodes() = m_GetTargetDeviceNodes.Value()
    override _.AddTargetDeviceNode argTargetDeviceID argTargetDeviceName argEnableStatSNAckChecker argNegotiableParameters argLogParameters = m_AddTargetDeviceNode.Value argTargetDeviceID argTargetDeviceName argEnableStatSNAckChecker argNegotiableParameters argLogParameters
    override _.DeleteTargetDeviceNode tdnode = m_DeleteTargetDeviceNode.Value tdnode
    override _.UpdateTargetDeviceNode tdnode argTargetDeviceID argTargetDeviceName argEnableStatSNAckChecker argNegotiableParameters argLogParameters = m_UpdateTargetDeviceNode.Value tdnode argTargetDeviceID argTargetDeviceName argEnableStatSNAckChecker argNegotiableParameters argLogParameters
    override _.AddNetworkPortalNode tdnode argNetworkPortal = m_AddNetworkPortalNode.Value tdnode argNetworkPortal
    override _.DeleteNetworkPortalNode npnode = m_DeleteNetworkPortalNode.Value npnode
    override _.UpdateNetworkPortalNode npnode argNetworkPortal = m_UpdateNetworkPortalNode.Value npnode argNetworkPortal
    override _.AddTargetGroupNode tdnode argTargetGroupID argTargetGroupName argEnabledAtStart = m_AddTargetGroupNode.Value tdnode argTargetGroupID argTargetGroupName argEnabledAtStart
    override _.DeleteTargetGroupNode tgnode = m_DeleteTargetGroupNode.Value tgnode
    override _.UpdateTargetGroupNode tgnode argTargetGroupID argTargetGroupName argEnabledAtStart = m_UpdateTargetGroupNode.Value tgnode argTargetGroupID argTargetGroupName argEnabledAtStart
    override _.DeleteNodeInTargetGroup argNode = m_DeleteNodeInTargetGroup.Value argNode
    override _.AddTargetNode tgNode argConf = m_AddTargetNode.Value tgNode argConf
    override _.UpdateTargetNode tnode argConf = m_UpdateTargetNode.Value tnode argConf
    override _.AddTargetLURelation tNode luNode = m_AddTargetLURelation.Value tNode luNode
    override _.DeleteTargetLURelation tNode luNode = m_DeleteTargetLURelation.Value tNode luNode
    override _.AddBlockDeviceLUNode tnode argLUN argLUName argMaxMultiplicity argFallbackBlockSize argOptimalTransferLength = m_AddBlockDeviceLUNode.Value tnode argLUN argLUName argMaxMultiplicity argFallbackBlockSize argOptimalTransferLength
    override _.UpdateBlockDeviceLUNode lunode argLUN argLUName argMaxMultiplicity argFallbackBlockSize argOptimalTransferLength = m_UpdateBlockDeviceLUNode.Value lunode argLUN argLUName argMaxMultiplicity argFallbackBlockSize argOptimalTransferLength
    override _.AddDummyDeviceLUNode tnode argLUN argLUName argMaxMultiplicity = m_AddDummyDeviceLUNode.Value tnode argLUN argLUName argMaxMultiplicity
    override _.UpdateDummyDeviceLUNode lunode argLUN argLUName argMaxMultiplicity = m_UpdateDummyDeviceLUNode.Value lunode argLUN argLUName argMaxMultiplicity
    override _.AddPlainFileMediaNode parentNode argValue = m_AddPlainFileMediaNode.Value parentNode argValue
    override _.UpdatePlainFileMediaNode mediaNode argValue = m_UpdatePlainFileMediaNode.Value mediaNode argValue
    override _.AddMemBufferMediaNode parentNode argValue = m_AddMemBufferMediaNode.Value parentNode argValue
    override _.UpdateMemBufferMediaNode mediaNode ident = m_UpdateMemBufferMediaNode.Value mediaNode ident
    override _.AddDummyMediaNode parentNode ident name = m_AddDummyMediaNode.Value parentNode ident name
    override _.UpdateDummyMediaNode mediaNode ident name = m_UpdateDummyMediaNode.Value mediaNode ident name
    override _.AddDebugMediaNode parentNode ident name = m_AddDebugMediaNode.Value parentNode ident name
    override _.UpdateDebugMediaNode mediaNode ident name = m_UpdateDebugMediaNode.Value mediaNode ident name
    override _.GetAncestorTargetDevice node = m_GetAncestorTargetDevice.Value node
    override _.GetAncestorTargetGroup node = m_GetAncestorTargetGroup.Value node
    override _.GetAncestorLogicalUnit node = m_GetAncestorLogicalUnit.Value node
    override _.TryCheckTargetDeviceUnloaded cc node = m_TryCheckTargetDeviceUnloaded.Value cc node
    override _.CheckTargetDeviceUnloaded cc node = m_CheckTargetDeviceUnloaded.Value cc node
    override _.TryCheckTargetGroupUnloaded cc node = m_TryCheckTargetGroupUnloaded.Value cc node
    override _.CheckTargetGroupUnloaded cc node = m_CheckTargetGroupUnloaded.Value cc node
    override _.ExportTemporaryDump n r = m_ExportTemporaryDump.Value n r
    override _.ImportTemporaryDump testr n r = m_ImportTemporaryDump.Value testr n r


type CtrlConnectionStub( st : StringTable ) =
    inherit CtrlConnection( st, new MemoryStream(), CtrlSessionID.NewID() )

    let mutable m_Logout : ( unit -> Task ) option = None
    let mutable m_NoOperation : ( unit -> Task ) option = None
    let mutable m_GetControllerConfig : ( unit -> Task< HarukaCtrlConf.T_HarukaCtrl > ) option = None
    let mutable m_SetControllerConfig : ( HarukaCtrlConf.T_HarukaCtrl -> Task ) option = None
    let mutable m_GetTargetDeviceDir : ( unit -> Task<TDID_T list> ) option = None
    let mutable m_CreateTargetDeviceDir : ( TDID_T -> Task ) option = None
    let mutable m_DeleteTargetDeviceDir : ( TDID_T -> Task ) option = None
    let mutable m_GetTargetDeviceConfig : ( TDID_T -> Task< TargetDeviceConf.T_TargetDevice > ) option = None
    let mutable m_CreateTargetDeviceConfig : ( TDID_T -> TargetDeviceConf.T_TargetDevice -> Task ) option = None
    let mutable m_GetTargetGroupID : ( TDID_T -> Task<TGID_T list> ) option = None
    let mutable m_GetTargetGroupConfig : ( TDID_T -> TGID_T -> Task<TargetGroupConf.T_TargetGroup> ) option = None
    let mutable m_GetAllTargetGroupConfig : ( TDID_T -> Task<TargetGroupConf.T_TargetGroup list> ) option = None
    let mutable m_CreateTargetGroupConfig : ( TDID_T -> TargetGroupConf.T_TargetGroup -> Task ) option = None
    let mutable m_DeleteTargetGroupConfig : ( TDID_T -> TGID_T -> Task ) option = None
    let mutable m_GetLUWorkDir : ( TDID_T -> Task< LUN_T list > ) option = None
    let mutable m_CreateLUWorkDir : ( TDID_T -> LUN_T -> Task ) option = None
    let mutable m_DeleteLUWorkDir : ( TDID_T -> LUN_T -> Task ) option = None
    let mutable m_GetTargetDeviceProcs : ( unit -> Task< TDID_T list > ) option = None
    let mutable m_KillTargetDeviceProc : ( TDID_T -> Task ) option = None
    let mutable m_StartTargetDeviceProc : ( TDID_T -> Task ) option = None
    let mutable m_CreateMediaFile_PlainFile : ( string -> int64 -> Task<uint64> ) option = None
    let mutable m_GetInitMediaStatus : ( unit -> Task< HarukaCtrlerCtrlRes.T_Procs list > ) option = None
    let mutable m_KillInitMediaProc : ( uint64 -> Task ) option = None
    let mutable m_GetActiveTargetGroups : ( TDID_T -> Task< TargetDeviceCtrlRes.T_ActiveTGInfo list > ) option = None
    let mutable m_GetLoadedTargetGroups : ( TDID_T -> Task< TargetDeviceCtrlRes.T_LoadedTGInfo list > ) option = None
    let mutable m_InactivateTargetGroup : ( TDID_T -> TGID_T -> Task ) option = None
    let mutable m_ActivateTargetGroup : ( TDID_T -> TGID_T -> Task ) option = None
    let mutable m_UnloadTargetGroup : ( TDID_T -> TGID_T -> Task ) option = None
    let mutable m_LoadTargetGroup : ( TDID_T -> TGID_T -> Task ) option = None
    let mutable m_SetLogParameters : ( TDID_T -> TargetDeviceConf.T_LogParameters -> Task ) option = None
    let mutable m_GetLogParameters : ( TDID_T -> Task< TargetDeviceConf.T_LogParameters > ) option = None
    let mutable m_GetDeviceName : ( TDID_T -> Task< string > ) option = None
    let mutable m_GetSession_InTargetDevice : ( TDID_T -> Task< TargetDeviceCtrlRes.T_Session list > ) option = None
    let mutable m_GetSession_InTargetGroup : ( TDID_T -> TGID_T -> Task< TargetDeviceCtrlRes.T_Session list > ) option = None
    let mutable m_GetSession_InTarget : ( TDID_T -> TNODEIDX_T -> Task< TargetDeviceCtrlRes.T_Session list > ) option = None
    let mutable m_DestructSession : ( TDID_T -> TSIH_T -> Task ) option = None
    let mutable m_GetConnection_InTargetDevice : ( TDID_T -> Task< TargetDeviceCtrlRes.T_Connection list > ) option = None
    let mutable m_GetConnection_InNetworkPortal : ( TDID_T -> NETPORTIDX_T -> Task< TargetDeviceCtrlRes.T_Connection list > ) option = None
    let mutable m_GetConnection_InTargetGroup : ( TDID_T -> TGID_T -> Task< TargetDeviceCtrlRes.T_Connection list > ) option = None
    let mutable m_GetConnection_InTarget : ( TDID_T -> TNODEIDX_T -> Task< TargetDeviceCtrlRes.T_Connection list > ) option = None
    let mutable m_GetConnection_InSession : ( TDID_T -> TSIH_T -> Task< TargetDeviceCtrlRes.T_Connection list > ) option = None
    let mutable m_GetLUStatus : ( TDID_T -> LUN_T -> Task< TargetDeviceCtrlRes.T_LUStatus_Success > ) option = None
    let mutable m_LUReset : ( TDID_T -> LUN_T -> Task ) option = None
    let mutable m_GetMediaStatus : ( TDID_T -> LUN_T -> MEDIAIDX_T -> Task< TargetDeviceCtrlRes.T_MediaStatus_Success > ) option = None
    let mutable m_DebugMedia_GetAllTraps : ( TDID_T -> LUN_T -> MEDIAIDX_T -> Task< MediaCtrlRes.T_Trap list > ) option = None
    let mutable m_DebugMedia_AddTrap : ( TDID_T -> LUN_T -> MEDIAIDX_T -> MediaCtrlReq.T_Event -> MediaCtrlReq.T_Action -> Task ) option = None
    let mutable m_DebugMedia_ClearTraps : ( TDID_T -> LUN_T -> MEDIAIDX_T -> Task ) option = None
    let mutable m_DebugMedia_GetCounterValue : ( TDID_T -> LUN_T -> MEDIAIDX_T -> int -> Task< int > ) option = None
    let mutable m_DebugMedia_GetTaskWaitStatus : ( TDID_T -> LUN_T -> MEDIAIDX_T -> Task< MediaCtrlRes.T_TaskWaitStatus list > ) option = None
    let mutable m_DebugMedia_Resume : ( TDID_T -> LUN_T -> MEDIAIDX_T -> TSIH_T -> ITT_T -> Task ) option = None

    member _.p_Logout with set v = m_Logout <- Some( v )
    member _.p_NoOperation with set v = m_NoOperation <- Some( v )
    member _.p_GetControllerConfig with set v = m_GetControllerConfig <- Some( v )
    member _.p_SetControllerConfig with set v = m_SetControllerConfig <- Some( v )
    member _.p_GetTargetDeviceDir with set v = m_GetTargetDeviceDir <- Some( v )
    member _.p_CreateTargetDeviceDir with set v = m_CreateTargetDeviceDir <- Some( v )
    member _.p_DeleteTargetDeviceDir with set v = m_DeleteTargetDeviceDir <- Some( v )
    member _.p_GetTargetDeviceConfig with set v = m_GetTargetDeviceConfig <- Some( v )
    member _.p_CreateTargetDeviceConfig with set v = m_CreateTargetDeviceConfig <- Some( v )
    member _.p_GetTargetGroupID with set v = m_GetTargetGroupID <- Some( v )
    member _.p_GetTargetGroupConfig with set v = m_GetTargetGroupConfig <- Some( v )
    member _.p_GetAllTargetGroupConfig with set v = m_GetAllTargetGroupConfig <- Some( v )
    member _.p_CreateTargetGroupConfig with set v = m_CreateTargetGroupConfig <- Some( v )
    member _.p_DeleteTargetGroupConfig with set v = m_DeleteTargetGroupConfig <- Some( v )
    member _.p_GetLUWorkDir with set v = m_GetLUWorkDir <- Some( v )
    member _.m_CreateLUWorkDip with set v = m_CreateLUWorkDir <- Some( v )
    member _.p_DeleteLUWorkDir with set v = m_DeleteLUWorkDir <- Some( v )
    member _.p_GetTargetDeviceProcs with set v = m_GetTargetDeviceProcs <- Some( v )
    member _.p_KillTargetDeviceProc with set v = m_KillTargetDeviceProc <- Some( v )
    member _.p_StartTargetDeviceProc with set v = m_StartTargetDeviceProc <- Some( v )
    member _.p_CreateMediaFile_PlainFile with set v = m_CreateMediaFile_PlainFile <- Some( v )
    member _.p_GetInitMediaStatus with set v = m_GetInitMediaStatus <- Some( v )
    member _.p_KillInitMediaProc with set v = m_KillInitMediaProc <- Some( v )
    member _.p_GetActiveTargetGroups with set v = m_GetActiveTargetGroups <- Some( v )
    member _.p_GetLoadedTargetGroups with set v = m_GetLoadedTargetGroups <- Some( v )
    member _.p_InactivateTargetGroup with set v = m_InactivateTargetGroup <- Some( v )
    member _.p_ActivateTargetGroup with set v = m_ActivateTargetGroup <- Some( v )
    member _.p_UnloadTargetGroup with set v = m_UnloadTargetGroup <- Some( v )
    member _.p_LoadTargetGroup with set v = m_LoadTargetGroup <- Some( v )
    member _.p_SetLogParameters with set v = m_SetLogParameters <- Some( v )
    member _.p_GetLogParameters with set v = m_GetLogParameters <- Some( v )
    member _.p_GetDeviceName with set v = m_GetDeviceName <- Some( v )
    member _.p_GetSession_InTargetDevice with set v = m_GetSession_InTargetDevice <- Some( v )
    member _.p_GetSession_InTargetGroup with set v = m_GetSession_InTargetGroup <- Some( v )
    member _.p_GetSession_InTarget with set v = m_GetSession_InTarget <- Some( v )
    member _.p_DestructSession with set v = m_DestructSession <- Some( v )
    member _.p_GetConnection_InTargetDevice with set v = m_GetConnection_InTargetDevice <- Some( v )
    member _.p_GetConnection_InNetworkPortal with set v = m_GetConnection_InNetworkPortal <- Some( v )
    member _.p_GetConnection_InTargetGroup with set v = m_GetConnection_InTargetGroup <- Some( v )
    member _.p_GetConnection_InTarget with set v = m_GetConnection_InTarget <- Some( v )
    member _.p_GetConnection_InSession with set v = m_GetConnection_InSession <- Some( v )
    member _.p_GetLUStatus with set v = m_GetLUStatus <- Some( v )
    member _.p_LUReset with set v = m_LUReset <- Some( v )
    member _.p_GetMediaStatus with set v = m_GetMediaStatus <- Some( v )
    member _.p_DebugMedia_GetAllTraps with set v = m_DebugMedia_GetAllTraps <- Some( v )
    member _.p_DebugMedia_AddTrap with set v = m_DebugMedia_AddTrap <- Some( v )
    member _.p_DebugMedia_ClearTraps with set v = m_DebugMedia_ClearTraps <- Some( v )
    member _.p_DebugMedia_GetCounterValue with set v = m_DebugMedia_GetCounterValue <- Some( v )
    member _.p_DebugMedia_GetTaskWaitStatus with set v = m_DebugMedia_GetTaskWaitStatus <- Some( v )
    member _.p_DebugMedia_Resume with set v = m_DebugMedia_Resume <- Some( v )

    override _.Logout () = m_Logout.Value()
    override _.NoOperation () = m_NoOperation.Value()
    override _.GetControllerConfig () = m_GetControllerConfig.Value()
    override _.SetControllerConfig conf = m_SetControllerConfig.Value conf
    override _.GetTargetDeviceDir () = m_GetTargetDeviceDir.Value()
    override _.CreateTargetDeviceDir tdid = m_CreateTargetDeviceDir.Value tdid
    override _.DeleteTargetDeviceDir tdid = m_DeleteTargetDeviceDir.Value tdid
    override _.GetTargetDeviceConfig tdid = m_GetTargetDeviceConfig.Value tdid
    override _.CreateTargetDeviceConfig tdid config = m_CreateTargetDeviceConfig.Value tdid config
    override _.GetTargetGroupID tdid = m_GetTargetGroupID.Value tdid
    override _.GetTargetGroupConfig tdid tgid = m_GetTargetGroupConfig.Value tdid tgid
    override _.GetAllTargetGroupConfig tdid = m_GetAllTargetGroupConfig.Value tdid
    override _.CreateTargetGroupConfig tdid config = m_CreateTargetGroupConfig.Value tdid config
    override _.DeleteTargetGroupConfig tdid tgid = m_DeleteTargetGroupConfig.Value tdid tgid
    override _.GetLUWorkDir tdid = m_GetLUWorkDir.Value tdid
    override _.CreateLUWorkDir tdid lun = m_CreateLUWorkDir.Value tdid lun
    override _.DeleteLUWorkDir tdid lun = m_DeleteLUWorkDir.Value tdid lun
    override _.GetTargetDeviceProcs() = m_GetTargetDeviceProcs.Value()
    override _.KillTargetDeviceProc tdid = m_KillTargetDeviceProc.Value tdid
    override _.StartTargetDeviceProc tdid = m_StartTargetDeviceProc.Value tdid
    override _.CreateMediaFile_PlainFile fileName fileSize = m_CreateMediaFile_PlainFile.Value fileName fileSize
    override _.GetInitMediaStatus () = m_GetInitMediaStatus.Value ()
    override _.KillInitMediaProc pid = m_KillInitMediaProc.Value pid
    override _.GetActiveTargetGroups tdid = m_GetActiveTargetGroups.Value tdid
    override _.GetLoadedTargetGroups tdid = m_GetLoadedTargetGroups.Value tdid
    override _.InactivateTargetGroup tdid tgid = m_InactivateTargetGroup.Value tdid tgid
    override _.ActivateTargetGroup tdid tgid = m_ActivateTargetGroup.Value tdid tgid
    override _.UnloadTargetGroup tdid tgid = m_UnloadTargetGroup.Value tdid tgid
    override _.LoadTargetGroup tdid tgid = m_LoadTargetGroup.Value tdid tgid
    override _.SetLogParameters tdid logConf = m_SetLogParameters.Value tdid logConf
    override _.GetLogParameters tdid = m_GetLogParameters.Value tdid
    override _.GetDeviceName tdid = m_GetDeviceName.Value tdid
    override _.GetSession_InTargetDevice tdid = m_GetSession_InTargetDevice.Value tdid
    override _.GetSession_InTargetGroup tdid tgid = m_GetSession_InTargetGroup.Value tdid tgid
    override _.GetSession_InTarget tdid tnode = m_GetSession_InTarget.Value tdid tnode
    override _.DestructSession tdid tsih = m_DestructSession.Value tdid tsih
    override _.GetConnection_InTargetDevice tdid = m_GetConnection_InTargetDevice.Value tdid
    override _.GetConnection_InNetworkPortal tdid netnode = m_GetConnection_InNetworkPortal.Value tdid netnode
    override _.GetConnection_InTargetGroup tdid tgid = m_GetConnection_InTargetGroup.Value tdid tgid
    override _.GetConnection_InTarget tdid tnode = m_GetConnection_InTarget.Value tdid tnode
    override _.GetConnection_InSession tdid tsih = m_GetConnection_InSession.Value tdid tsih
    override _.GetLUStatus tdid lun = m_GetLUStatus.Value tdid lun
    override _.LUReset tdid lun = m_LUReset.Value tdid lun
    override _.GetMediaStatus tdid lun mnode = m_GetMediaStatus.Value tdid lun mnode
    override _.DebugMedia_GetAllTraps tdid lun mediaid = m_DebugMedia_GetAllTraps.Value tdid lun mediaid
    override _.DebugMedia_AddTrap tdid lun mediaid event action = m_DebugMedia_AddTrap.Value tdid lun mediaid event action
    override _.DebugMedia_ClearTraps tdid lun mediaid = m_DebugMedia_ClearTraps.Value tdid lun mediaid
    override _.DebugMedia_GetCounterValue tdid lun mediaid counterno = m_DebugMedia_GetCounterValue.Value tdid lun mediaid counterno
    override _.DebugMedia_GetTaskWaitStatus tdid lun mediaid = m_DebugMedia_GetTaskWaitStatus.Value tdid lun mediaid
    override _.DebugMedia_Resume tdid lun mediaid tsih itt = m_DebugMedia_Resume.Value tdid lun mediaid tsih itt

//=============================================================================
// Class implementation

type CommandRunner_Test1() =

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

    let GenOutputStreamReader( ms : MemoryStream ) ( ws : StreamWriter ) =
        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        new StreamReader( ms )

    let Init ( caseName : string ) =
        let portNo = GlbFunc.nextTcpPortNo()
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "CommandRunner_Test1_" + caseName
        if Directory.Exists dname then GlbFunc.DeleteDir dname
        GlbFunc.CreateDir dname |> ignore
        ( portNo, dname )

    let CallCommandLoop ( cr : CommandRunner ) ( stat : ( ServerStatus * CtrlConnection * IConfigureNode ) option ) : ( bool * ( ServerStatus * CtrlConnection * IConfigureNode ) option ) =
        let pc = PrivateCaller( cr )
        let struct( r, stat ) =
            pc.Invoke( "CommandLoop", stat )
            :?> Task< struct( bool * ( ServerStatus * CtrlConnection * IConfigureNode ) option ) >
            |> Functions.RunTaskSynchronously
        ( r, stat )

    let CheckPromptAndMessage ( st : StreamReader ) ( pronpt : string ) ( msg : string ) : unit =
        let lineStr = st.ReadLine()
        let esc ( s : string ) =
            let sb = StringBuilder()
            for itr in s do
                if String.exists ( (=) itr ) "=$^{[(|)*+?\\" then
                    sb.Append '\\' |> ignore
                sb.Append itr |> ignore
            sb.ToString()
        let reg = Regex( sprintf "^%s> *%s.*$" ( esc pronpt ) ( esc msg ) )
        Assert.True(( reg.IsMatch lineStr ))

    let CommandLoop_UnknownCommand  ( command : string ) ( tnode : IConfigureNode ) ( prompt : string ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream command
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs prompt "CMDERR_UNKNOWN_COMMAND"

        GlbFunc.AllDispose [ in_ms; in_ws; in_rs; out_ms; out_ws; out_rs; ]

    static member m_ControllerNode =
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_Controller( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_TargetDeviceNode =
        let conf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "";
            EnableStatSNAckChecker = false;
        }
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_TargetDevice( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, GlbFunc.newTargetDeviceID(), conf ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_NetworkPortalNode =
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
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, conf ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_TargetGroupNode =
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, GlbFunc.newTargetGroupID(), "", true, ModifiedStatus.NotModified ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_TargetNode =
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetName = "";
            TargetAlias = "";
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        }
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_Target( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, conf ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_BlockDeviceLUNode =
        let cnr = new ConfNodeRelation()
        let mult = Constants.LU_DEF_MULTIPLICITY
        let fbs = Blocksize.BS_512
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let n = new ConfNode_BlockDeviceLU( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, lun_me.zero, "", mult, fbs, otl ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_DummyDeviceLUNode =
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_DummyDeviceLU( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, lun_me.zero, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_DummyMediaNode =
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_DummyMedia( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_PlainFileMediaNode =
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 1u;
            MediaName = "";
            FileName = "";
            MaxMultiplicity = 0u;
            QueueWaitTimeOut = 0;
            WriteProtect = false;
        }
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_PlainFileMedia( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, conf ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_MemBufferMediaNode =
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 1u;
            MediaName = "";
            BytesCount = 512UL;
        }
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, conf ) :> IConfigureNode
        cnr.AddNode n
        n

    static member m_DebugMediaNode =
        let cnr = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( new StringTable( "" ), cnr, confnode_me.fromPrim 0UL, mediaidx_me.fromPrim 1u, "" ) :> IConfigureNode
        cnr.AddNode n
        n


    static member m_CommandLoop_exit_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_set_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_start_data = [|
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_setlogparam_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_create_networkportal_error_data = [|
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_create_targetgroup_error_data = [|
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_add_ipwhitelist_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
    |]

    static member m_CommandLoop_add_ipwhitelist_error_data = [|
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_load_data = [|
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_load_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
    |]

    static member m_CommandLoop_setchap_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_create_media_data = [|
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_expection_data = [|
        [| CommandInputError( "aaaa" ) :> obj; "CR"; "aaaa" :> obj |];
        [| RequestError( "aaaa" ) :> obj; "CR"; "CMDERR_UNEXPECTED_REQUEST_ERROR" :> obj |];
        [| SocketException( 0 ) :> obj; "CR"; "CMDERR_CONNECTION_ERROR" :> obj |];
        [| IOException( "aaaa" ) :> obj; "CR"; "CMDERR_CONNECTION_ERROR" :> obj |];
        [| EditError( "aaaa" ) :> obj; "CR"; "CMDERR_UNEXPECTED_EDIT_ERROR" :> obj |];
        [| ConfigurationError( "aaaa" ) :> obj; "CR"; "aaaa" :> obj |];
    |]

    static member m_CommandLoop_sessions_data = [|
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
    |]

    static member m_CommandLoop_sessions_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_connections_data = [|
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
    |]

    static member m_CommandLoop_connections_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_lustatus_data = [|
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
    |]

    static member m_CommandLoop_lustatus_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_mediastatus_data = [|
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_mediastatus_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
    |]

    static member m_CommandLoop_add_trap_data = [|
        [| CommandRunner_Test1.m_DebugMediaNode :> obj; "MD" :> obj |];
    |]

    static member m_CommandLoop_add_trap_error_data = [|
        [| CommandRunner_Test1.m_ControllerNode :> obj; "CR" :> obj |];
        [| CommandRunner_Test1.m_TargetDeviceNode :> obj; "TD" :> obj |];
        [| CommandRunner_Test1.m_NetworkPortalNode :> obj; "NP" :> obj |];
        [| CommandRunner_Test1.m_TargetGroupNode :> obj; "TG" :> obj |];
        [| CommandRunner_Test1.m_TargetNode :> obj; "T " :> obj |];
        [| CommandRunner_Test1.m_BlockDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyDeviceLUNode :> obj; "LU" :> obj |];
        [| CommandRunner_Test1.m_DummyMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_PlainFileMediaNode :> obj; "MD" :> obj |];
        [| CommandRunner_Test1.m_MemBufferMediaNode :> obj; "MD" :> obj |];
    |]

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.CommandLoop_exit_001() =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "exit" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )

        let r, stat = CallCommandLoop cr None
        Assert.False(( r ))
        Assert.True(( stat.IsNone ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        let outline = out_rs.ReadLine()
        Assert.True(( outline.StartsWith "--> " ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]
    member _.CommandLoop_exit_002 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "exit" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st, p_IsModified = false )
        let mutable logoutFlg = false
        let cc = new CtrlConnectionStub( st, p_Logout = ( fun () -> task{ logoutFlg <- true } ) )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.False(( r ))
        Assert.True(( stat.IsNone ))
        Assert.True(( logoutFlg ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        let outline = out_rs.ReadLine()
        Assert.True(( outline.StartsWith ( prompt :?> string ) ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_login_001() =
        let portNo = GlbFunc.nextTcpPortNo()
        [|
            fun () -> task {
                let sl = new TcpListener( IPAddress.Parse "::1", portNo )
                sl.Start ()
                let! s = sl.AcceptSocketAsync()
                let c = new NetworkStream( s )

                // Receive Login request
                let! loginRequestStr = Functions.FramingReceiver c
                let loginRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString loginRequestStr
                match loginRequest.Request with
                | HarukaCtrlerCtrlReq.U_Login( x ) ->
                    Assert.False x
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
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c []
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let in_ms, in_ws, in_rs = GenCommandStream( sprintf "login /h ::1 /p %d" portNo )
                let out_ms, out_ws = GenOutputStream()
                let cr = new CommandRunner( st, in_rs, out_ws )

                let r, stat = CallCommandLoop cr None
                Assert.True(( r ))
                Assert.True(( stat.IsSome ))
                let ss, cc, cn = stat.Value
                match cn with
                | :? ConfNode_Controller ->
                    ()
                | _ -> Assert.Fail __LINE__

                out_ws.Flush()
                out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
                let out_rs = new StreamReader( out_ms )
                let outline = out_rs.ReadLine()

                Assert.True(( outline.StartsWith "--> " ))

                GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

    [<Fact>]
    member _.CommandLoop_login_002() =
        let portNo = GlbFunc.nextTcpPortNo()
        [|
            fun () -> task {
                let sl = new TcpListener( IPAddress.Parse "::1", portNo )
                sl.Start ()
                let! s = sl.AcceptSocketAsync()
                let c = new NetworkStream( s )

                // Receive Login request
                let! loginRequestStr = Functions.FramingReceiver c
                let loginRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString loginRequestStr
                match loginRequest.Request with
                | HarukaCtrlerCtrlReq.U_Login( x ) ->
                    Assert.True x
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
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
                do! ServerStatus_Test1.RespTargetDeviceDirs c []
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let in_ms, in_ws, in_rs = GenCommandStream( sprintf "login /h ::1 /p %d /f" portNo )
                let out_ms, out_ws = GenOutputStream()
                let cr = new CommandRunner( st, in_rs, out_ws )

                let r, stat = CallCommandLoop cr None
                Assert.True(( r ))
                Assert.True(( stat.IsSome ))
                let ss, cc, cn = stat.Value
                match cn with
                | :? ConfNode_Controller ->
                    ()
                | _ -> Assert.Fail __LINE__

                out_ws.Flush()
                out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
                let out_rs = new StreamReader( out_ms )
                let outline = out_rs.ReadLine()

                Assert.True(( outline.StartsWith "--> " ))

                GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

    [<Fact>]
    member _.CommandLoop_login_003() =
        let portNo = GlbFunc.nextTcpPortNo()
        [|
            fun () -> task {
                let sl = new TcpListener( IPAddress.Parse "::1", portNo )
                sl.Start ()
                let! s = sl.AcceptSocketAsync()
                let c = new NetworkStream( s )

                // Receive Login request
                let! loginRequestStr = Functions.FramingReceiver c
                let loginRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString loginRequestStr
                match loginRequest.Request with
                | HarukaCtrlerCtrlReq.U_Login( x ) ->
                    Assert.False x
                | _ ->
                    Assert.Fail __LINE__

                // send login response
                let sessID = CtrlSessionID.NewID()
                let rb =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_LoginResult( {
                                Result = false;
                                SessionID = sessID;
                        })
                    }
                do! Functions.FramingSender c rb
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                try
                    let st = new StringTable( "" )
                    let in_ms, in_ws, in_rs = GenCommandStream( sprintf "login /h ::1 /p %d" portNo )
                    let out_ms, out_ws = GenOutputStream()
                    let cr = new CommandRunner( st, in_rs, out_ws )

                    let r, stat = CallCommandLoop cr None
                    Assert.True(( r ))
                    Assert.True(( stat.IsNone ))

                    out_ws.Flush()
                    out_ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
                    let out_rs = new StreamReader( out_ms )
                    CheckPromptAndMessage out_rs "--" "CMDERR_FAILED_LOGIN"

                    GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]
                with
                | _ as x ->
                    printfn "aaa"
                    ()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

    [<Fact>]
    member _.CommandLoop_login_004() =
        let portNo = GlbFunc.nextTcpPortNo()
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( sprintf "login /h *** /p %d" portNo )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )

        let r, stat = CallCommandLoop cr None
        Assert.True(( r ))
        Assert.True(( stat.IsNone ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "--" "CMDERR_FAILED_CONNECT_CTRL"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_login_005  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "login" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<InlineData( "logout" )>]
    [<InlineData( "reload" )>]
    [<InlineData( "select 0" )>]
    [<InlineData( "unselect" )>]
    [<InlineData( "list" )>]
    [<InlineData( "listparent" )>]
    [<InlineData( "pwd" )>]
    [<InlineData( "values" )>]
    [<InlineData( "set a b" )>]
    [<InlineData( "validate" )>]
    [<InlineData( "publish" )>]
    [<InlineData( "nop" )>]
    [<InlineData( "statusall" )>]
    [<InlineData( "create" )>]
    [<InlineData( "status" )>]
    [<InlineData( "delete" )>]
    [<InlineData( "start" )>]
    [<InlineData( "kill" )>]
    [<InlineData( "setlogparam" )>]
    [<InlineData( "getlogparam" )>]
    [<InlineData( "create networkportal" )>]
    [<InlineData( "create targetgroup" )>]
    [<InlineData( "load" )>]
    [<InlineData( "unload" )>]
    [<InlineData( "activate" )>]
    [<InlineData( "inactivate" )>]
    [<InlineData( "setchap a b c d" )>]
    [<InlineData( "unsetauth" )>]
    [<InlineData( "attach" )>]
    [<InlineData( "detach" )>]
    [<InlineData( "create plainfile" )>]
    [<InlineData( "create membuffer" )>]
    [<InlineData( "initmedia plainfile" )>]
    [<InlineData( "imstatus" )>]
    [<InlineData( "imkill" )>]
    [<InlineData( "sessions" )>]
    [<InlineData( "sesskill 1" )>]
    [<InlineData( "connections" )>]
    [<InlineData( "lustatus" )>]
    [<InlineData( "lureset" )>]
    [<InlineData( "mediastatus" )>]
    [<InlineData( "aaaaa" )>]
    member _.CommandLoop_NotConnected_ErrorCommand ( command : string ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream command
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )

        let r, stat = CallCommandLoop cr None
        Assert.True(( r ))
        Assert.True(( stat.IsNone ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "--" "CMDERR_UNKNOWN_COMMAND"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_logout_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "logout" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st, p_IsModified = false )
        let mutable logoutFlg = false
        let cc = new CtrlConnectionStub( st, p_Logout = ( fun () -> task{ logoutFlg <- true } ) )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat.IsNone ))
        Assert.True(( logoutFlg ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        let outline = out_rs.ReadLine()
        Assert.True(( outline.StartsWith ( prompt :?> string ) ))

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_reload_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "reload" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st, p_IsModified = true )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_CONFIG_MODIFIED"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_select_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "select 0" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_MISSING_NODE"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_unselect_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "unselect" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) ""

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_list_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "list" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_MISSING_CHILD_NODE"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_listparent_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "listparent" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_MISSING_PARENT_NODE"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_pwd_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "pwd" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) tnode.ShortDescriptString

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_values_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "values" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        let ds = tnode.FullDescriptString
        CheckPromptAndMessage out_rs ( prompt :?> string ) ds.[0]

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_set_data" )>]
    member _.CommandLoop_set_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "set aaa bbb" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_UNKNOWN_PARAMETER_NAME"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_validate_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "validate" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let mutable flg = false
        let ss = new ServerStatusStub( st, p_Validate = ( fun () -> flg <- true; [] ) )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_ALL_VALIDATED"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_publish_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "publish" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let mutable flg = false
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        ss.p_Publish <- ( fun argcc ->
            task {
                flg <- true
                Assert.True(( argcc = cc ))
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_CONFIGURATION_PUBLISHED"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_nop_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "nop" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let mutable flg = false
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st, p_NoOperation = ( fun () -> task { flg <- true } ) )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) ""

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_statusall_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "statusall" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let mutable flg1 = false
        let mutable flg2 = false
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        ss.p_ControllerNode <- ( CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller )
        ss.p_GetTargetDeviceNodes <- ( fun () -> flg1 <- true; [] )
        cc.p_GetTargetDeviceProcs <- ( fun () -> task { flg2 <- true; return []} )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "NOT MODIFIED :"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_create_TargetDevice_001() =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let mutable flg1 = false
        let mutable flg2 = false
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = CommandRunner_Test1.m_ControllerNode

        ss.p_GetTargetDeviceNodes <- ( fun () -> flg1 <- true; [] )
        ss.p_AddTargetDeviceNode <- ( fun argTDID argTDN argESAC argNP argLP ->
            flg2 <- true
            CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "CR" "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_create_TargetDevice_002() =
        CommandLoop_UnknownCommand "create" CommandRunner_Test1.m_NetworkPortalNode "NP"

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_status_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "status" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let mutable flg1 = false
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        ss.p_ControllerNode <- ( CommandRunner_Test1.m_ControllerNode :?> ConfNode_Controller )
        ss.p_GetAncestorTargetDevice <- ( fun a ->
            Assert.True(( a = tnode ))
            flg1 <- true;
            None
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "NOT MODIFIED :"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_delete_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "delete /i 0" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_MISSING_NODE"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_start_data" )>]
    member _.CommandLoop_start_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "start" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun cn ->
            Assert.True(( cn = tnode ))
            flg1 <- true
            Some tdnode
        )

        cc.p_StartTargetDeviceProc <- ( fun tdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            flg2 <- true
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Started"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_start_002  () =
        CommandLoop_UnknownCommand "start" CommandRunner_Test1.m_ControllerNode "CR"

    [<Theory>]
    [<MemberData( "m_CommandLoop_start_data" )>]    // same as test data for start command
    member _.CommandLoop_kill_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "kill" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        ss.p_GetAncestorTargetDevice <- ( fun cn ->
            Assert.True(( cn = tnode ))
            flg1 <- true
            Some tdnode
        )

        cc.p_KillTargetDeviceProc <- ( fun tdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            flg2 <- true
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Killed"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_kill_002  () =
        CommandLoop_UnknownCommand "kill" CommandRunner_Test1.m_ControllerNode "CR"

    [<Fact>]
    member _.CommandLoop_setlogparam_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "setlogparam" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false

        cc.p_GetLogParameters <- ( fun tdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            flg1 <- true
            task {
                return {
                    SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                    HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                    LogLevel = LogLevel.LOGLEVEL_INFO;
                }
            }
        )

        cc.p_SetLogParameters <- ( fun tdid argp ->
            flg2 <- true
            task {
                Assert.True(( tdid = tdnode.TargetDeviceID ))
                Assert.True(( argp.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT ))
                Assert.True(( argp.HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT ))
                Assert.True(( argp.LogLevel = LogLevel.LOGLEVEL_INFO ))
            }
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tdnode ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "TD" "CMDMSG_LOG_PARAM_UPDATED"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_setlogparam_error_data" )>]
    member _.CommandLoop_setlogparam_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "setlogparam" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_getlogparam_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "getlogparam" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        cc.p_GetLogParameters <- ( fun tdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            flg1 <- true
            task {
                return {
                    SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                    HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                    LogLevel = LogLevel.LOGLEVEL_INFO;
                }
            }
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tdnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "TD" "SoftLimit"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]
        
    [<Theory>]
    [<MemberData( "m_CommandLoop_setlogparam_error_data" )>]    // same as test data for setlogparam command
    member _.CommandLoop_getlogparam_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "getlogparam" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_create_networkportal_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create networkportal" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        ss.p_AddNetworkPortalNode <- ( fun argtdnode conf ->
            flg1 <- true
            Assert.True(( argtdnode = tdnode ))
            CommandRunner_Test1.m_NetworkPortalNode :?> ConfNode_NetworkPortal
        )
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tdnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "TD" "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_create_networkportal_error_data" )>]
    member _.CommandLoop_create_networkportal_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "create networkportal" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_create_targetgroup_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create targetgroup" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false

        ss.p_AddTargetGroupNode <- ( fun argtdnode newTgid tgName eas ->
            flg1 <- true
            Assert.True(( tdnode = argtdnode ))
            tgnode
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tdnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tdnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "TD" "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_create_targetgroup_error_data" )>]
    member _.CommandLoop_create_targetgroup_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "create targetgroup" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_ipwhitelist_data" )>]
    member _.CommandLoop_add_IPWhiteList_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add IPWhiteList" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "CMDMSG_PARAMVAL_INVALID_PARAM_PATTERN"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_ipwhitelist_error_data" )>]
    member _.CommandLoop_add_IPWhiteList_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "add IPWhiteList" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_Clear_IPWhiteList_001 () =
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
        ss.p_CheckTargetDeviceUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, npnode1 ) )
        Assert.True(( r ))
        Assert.True stat.IsSome
        Assert.True flg1

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Fact>]
    member _.CommandLoop_Clear_IPWhiteList_002 () =
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
        Assert.True(( r ))
        Assert.True stat.IsSome
        Assert.True flg1

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_ipwhitelist_error_data" )>]
    member _.CommandLoop_Clear_IPWhiteList_003  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "clear IPWhiteList" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_data" )>]
    member _.CommandLoop_load_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "load" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argcn ->
            flg1 <- true
            Assert.True(( argcn = cn ))
            Some tdnode
        )
        ss.p_GetAncestorTargetGroup <- ( fun argcn ->
            flg2 <- true
            Assert.True(( argcn = cn ))
            Some tgnode
        )
        cc.p_LoadTargetGroup <- ( fun tdid tgid ->
            flg3 <- true
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( tgid = tgnode.TargetGroupID ))
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Loaded"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_error_data" )>]
    member _.CommandLoop_load_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "load" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_data" )>]
    member _.CommandLoop_unload_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "unload" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argcn ->
            flg1 <- true
            Assert.True(( argcn = cn ))
            Some tdnode
        )
        ss.p_GetAncestorTargetGroup <- ( fun argcn ->
            flg2 <- true
            Assert.True(( argcn = cn ))
            Some tgnode
        )
        cc.p_UnloadTargetGroup <- ( fun tdid tgid ->
            flg3 <- true
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( tgid = tgnode.TargetGroupID ))
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Unloaded"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_error_data" )>]
    member _.CommandLoop_unload_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "unload" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_data" )>]
    member _.CommandLoop_activate_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "activate" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argcn ->
            flg1 <- true
            Assert.True(( argcn = cn ))
            Some tdnode
        )
        ss.p_GetAncestorTargetGroup <- ( fun argcn ->
            flg2 <- true
            Assert.True(( argcn = cn ))
            Some tgnode
        )
        cc.p_ActivateTargetGroup <- ( fun tdid tgid ->
            flg3 <- true
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( tgid = tgnode.TargetGroupID ))
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Activated"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_error_data" )>]
    member _.CommandLoop_activate_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "activate" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_data" )>]
    member _.CommandLoop_inactivate_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "inactivate" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg1 = false
        let mutable flg2 = false
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun argcn ->
            flg1 <- true
            Assert.True(( argcn = cn ))
            Some tdnode
        )
        ss.p_GetAncestorTargetGroup <- ( fun argcn ->
            flg2 <- true
            Assert.True(( argcn = cn ))
            Some tgnode
        )
        cc.p_InactivateTargetGroup <- ( fun tdid tgid ->
            flg3 <- true
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( tgid = tgnode.TargetGroupID ))
            Task.FromResult ()
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))
        Assert.True(( flg2 ))
        Assert.True(( flg3 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Inactivated"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_load_error_data" )>]
    member _.CommandLoop_inactivate_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "inactivate" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_create_target_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun curnode -> 
            Assert.True(( curnode = tgnode ))
            Some( CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice )
        )
        ss.p_AddTargetNode <- ( fun argtgnode conf ->
            flg1 <- true
            Assert.True(( tgnode = argtgnode ))
            tnode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tgnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tgnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "TG" "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_setchap_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "setchap /iu a /ip b /tu c /tp d" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argtnode conf ->
            flg1 <- true
            Assert.True(( tnode = argtnode ))
            tnode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "T " "Set CHAP authentication"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_setchap_error_data" )>]
    member _.CommandLoop_setchap_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "setchap /iu a /ip b /tu c /tp d"  ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_unsetauth_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "unsetauth" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_UpdateTargetNode <- ( fun argtnode conf ->
            flg1 <- true
            Assert.True(( tnode = argtnode ))
            tnode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "T " "Authentication reset"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_setchap_error_data" )>]
    member _.CommandLoop_unsetauth_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "unsetauth"  ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_create_LU_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create /l 0" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU
        let mutable flg1 = false

        ss.p_AddBlockDeviceLUNode <- ( fun argtnode lun luname mm fbs otl ->
            flg1 <- true
            Assert.True(( tnode = argtnode ))
            lunode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "T " "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_attach_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "attach" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tgnode = CommandRunner_Test1.m_TargetGroupNode :?> ConfNode_TargetGroup
        let tnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target
        let mutable flg1 = false

        ss.p_GetAncestorTargetGroup <- ( fun argtnode ->
            flg1 <- true
            Assert.True(( argtnode = tnode ))
            Some tgnode
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "T " "CMDMSG_ADDPARAM_LUN"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_setchap_error_data" )>]
    member _.CommandLoop_attach_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "attach"  ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Fact>]
    member _.CommandLoop_detach_001 () =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "detach" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = CommandRunner_Test1.m_TargetNode :?> ConfNode_Target

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs "T " "CMDMSG_ADDPARAM_LUN"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_setchap_error_data" )>]
    member _.CommandLoop_detach_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "detach"  ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_create_media_data" )>]
    member _.CommandLoop_create_media_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create plainfile /n a" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let sfnode = CommandRunner_Test1.m_PlainFileMediaNode :?> ConfNode_PlainFileMedia
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun curnode -> 
            Assert.True(( curnode = cn ))
            Some( CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice )
        )
        ss.p_AddPlainFileMediaNode <- ( fun argcn conf ->
            flg1 <- true
            Assert.True(( cn = argcn ))
            sfnode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_create_media_data" )>]
    member _.CommandLoop_create_media_002 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create membuffer /s 512" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let sfnode = CommandRunner_Test1.m_MemBufferMediaNode :?> ConfNode_MemBufferMedia
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun curnode -> 
            Assert.True(( curnode = cn ))
            Some( CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice )
        )
        ss.p_AddMemBufferMediaNode <- ( fun argcn conf ->
            flg1 <- true
            Assert.True(( cn = argcn ))
            sfnode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_create_media_data" )>]
    member _.CommandLoop_create_media_003 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "create debug" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let sfnode = CommandRunner_Test1.m_DebugMediaNode :?> ConfNode_DebugMedia
        let mutable flg1 = false

        ss.p_GetAncestorTargetDevice <- ( fun curnode -> 
            Assert.True(( curnode = cn ))
            Some( CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice )
        )
        ss.p_AddDebugMediaNode <- ( fun argcn ident medianame ->
            flg1 <- true
            Assert.True(( cn = argcn ))
            sfnode
        )
        ss.p_CheckTargetGroupUnloaded <- ( fun cc node ->
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Created"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]
    member _.CommandLoop_initmedia_plainfile_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "initmedia plainfile a 1" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let mutable flg1 = false

        cc.p_CreateMediaFile_PlainFile <- ( fun fname fsize ->
            Assert.True(( fname = "a" ))
            Assert.True(( fsize = 1L ))
            flg1 <- true
            Task.FromResult 0UL
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Started : ProcID=0"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]
    member _.CommandLoop_imstatus_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "imstatus" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let mutable flg1 = false

        cc.p_GetInitMediaStatus <- ( fun () ->
            flg1 <- true
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))

        GlbFunc.AllDispose [ in_ms; in_ws; in_rs; out_ws; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]
    member _.CommandLoop_imkill_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "imkill 111" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let mutable flg1 = false

        cc.p_KillInitMediaProc <- ( fun pid ->
            Assert.True(( pid = 111UL ))
            flg1 <- true
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg1 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Terminated"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_sessions_data" )>]
    member _.CommandLoop_sessions_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "sessions" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdnode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        let funcGetSession() : Task< TargetDeviceCtrlRes.T_Session list > =
            task {
                flg3 <- true
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
                return sessList
            }
        cc.p_GetSession_InTargetDevice <- ( fun _ -> funcGetSession() )
        cc.p_GetSession_InTargetGroup <- ( fun _ _ -> funcGetSession() )
        cc.p_GetSession_InTarget <- ( fun _ _ -> funcGetSession() )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg3 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Session("

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_sessions_error_data" )>]
    member _.CommandLoop_sessions_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "sessions" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_start_data" )>]    // same as test data for start command
    member _.CommandLoop_sesskill_001 ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "sesskill 99" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let tnode = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdnode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_DestructSession <- ( fun argtdid argtsih ->
            Assert.True(( argtdid = tdnode.TargetDeviceID ))
            Assert.True(( argtsih = tsih_me.fromPrim 99us ))
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, tnode ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, tnode ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Session terminated"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Fact>]
    member _.CommandLoop_sesskill_002  () =
        CommandLoop_UnknownCommand "sesskill" CommandRunner_Test1.m_ControllerNode "CR"

    [<Theory>]
    [<MemberData( "m_CommandLoop_connections_data" )>]
    member _.CommandLoop_connections_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "connections" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let mutable flg3 = false

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdnode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        let funcGetConnection() : Task< TargetDeviceCtrlRes.T_Connection list > =
            task {
                flg3 <- true
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
        cc.p_GetConnection_InTargetDevice <- ( fun _ -> funcGetConnection() )
        cc.p_GetConnection_InNetworkPortal <- ( fun _ _ -> funcGetConnection() )
        cc.p_GetConnection_InTargetGroup <- ( fun _ _ -> funcGetConnection() )
        cc.p_GetConnection_InTarget <- ( fun _ _ -> funcGetConnection() )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))
        Assert.True(( flg3 ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Connection"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_connections_error_data" )>]
    member _.CommandLoop_connections_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "connections" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_lustatus_data" )>]
    member _.CommandLoop_lustatus_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "lustatus" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdnode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_GetLUStatus <- ( fun tdid lun -> task {
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( lun = ( cn :?> ILUNode ).LUN ))
            return {
                ReadBytesCount = [];
                WrittenBytesCount = [];
                ReadTickCount = [];
                WriteTickCount = [];
                ACAStatus = None;
            }
        } )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "LU Status"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_lustatus_error_data" )>]
    member _.CommandLoop_lustatus_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "lustatus" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_lustatus_data" )>]
    member _.CommandLoop_lureset_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "lureset" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice

        ss.p_GetAncestorTargetDevice <- ( fun _ -> Some tdnode )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_LUReset <- ( fun tdid lun -> task {
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( lun = ( cn :?> ILUNode ).LUN ))
        })

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "LU Reseted"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_lustatus_error_data" )>]
    member _.CommandLoop_lureset_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "lureset" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_mediastatus_data" )>]
    member _.CommandLoop_mediastatus_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "mediastatus" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some tdnode
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ ->Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_GetMediaStatus <- ( fun tdid lun mdid ->
            task {
                Assert.True(( tdid = tdnode.TargetDeviceID ))
                Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
                Assert.True(( mdid = ( cn :?> IMediaNode ).IdentNumber ))
                return {
                    ReadBytesCount = [];
                    WrittenBytesCount = [];
                    ReadTickCount = [];
                    WriteTickCount = [];
                }
            }
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Media Status"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_mediastatus_error_data" )>]
    member _.CommandLoop_mediastatus_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "mediastatus" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_data" )>]
    member _.CommandLoop_add_trap_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "add trap /e TestUnitReady /a ACA" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some tdnode
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_DebugMedia_AddTrap <- ( fun tdid lun mdid e a ->
            Assert.True(( e.IsU_TestUnitReady ))
            Assert.True(( a.IsU_ACA ))
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Trap added"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_error_data" )>]
    member _.CommandLoop_add_trap_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "add trap /e TestUnitReady /a ACA" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_data" )>]
    member _.CommandLoop_clear_trap_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "clear trap" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some tdnode
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_DebugMedia_ClearTraps <- ( fun tdid lun mdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
            Assert.True(( mdid = ( cn :?> IMediaNode ).IdentNumber ))
            Task.FromResult ()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Traps cleared"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_error_data" )>]
    member _.CommandLoop_clear_trap_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "clear trap" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_data" )>]
    member _.CommandLoop_traps_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "traps" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some tdnode
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_DebugMedia_GetAllTraps <- ( fun tdid lun mdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
            Assert.True(( mdid = ( cn :?> IMediaNode ).IdentNumber ))
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Registered traps"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_error_data" )>]
    member _.CommandLoop_traps_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "traps" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_data" )>]
    member _.CommandLoop_task_list_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "task list" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some tdnode
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_DebugMedia_GetTaskWaitStatus <- ( fun tdid lun mdid ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
            Assert.True(( mdid = ( cn :?> IMediaNode ).IdentNumber ))
            Task.FromResult []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Task wait status"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_error_data" )>]
    member _.CommandLoop_task_list_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "task list" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_data" )>]
    member _.CommandLoop_task_resume_001  ( node : obj ) ( prompt : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "task resume /t 1 /i 2" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = node :?> IConfigureNode
        let tdnode = CommandRunner_Test1.m_TargetDeviceNode :?> ConfNode_TargetDevice
        let lunode = CommandRunner_Test1.m_BlockDeviceLUNode :?> ConfNode_BlockDeviceLU

        ss.p_GetAncestorTargetDevice <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some tdnode
        )
        ss.p_GetAncestorLogicalUnit <- ( fun argnode ->
            Assert.True(( argnode = cn ))
            Some lunode
        )
        cc.p_GetTargetDeviceProcs <- ( fun _ -> Task.FromResult [ tdnode.TargetDeviceID ] )
        cc.p_DebugMedia_Resume <- ( fun tdid lun mdid tsih itt ->
            Assert.True(( tdid = tdnode.TargetDeviceID ))
            Assert.True(( lun = ( lunode :> ILUNode ).LUN ))
            Assert.True(( mdid = ( cn :?> IMediaNode ).IdentNumber ))
            Assert.True(( tsih = ( tsih_me.fromPrim 1us ) ))
            Assert.True(( itt = ( itt_me.fromPrim 2u ) ))
            Task.FromResult()
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) "Task( TSIH=1, ITT=2 ) resumed"

        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

    [<Theory>]
    [<MemberData( "m_CommandLoop_add_trap_error_data" )>]
    member _.CommandLoop_task_resume_002  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "task resume /t 1 /i 2" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_exit_data" )>]     // same as test data for exit command
    member _.CommandLoop_Other_001  ( node : obj ) ( prompt : obj ) =
        CommandLoop_UnknownCommand "aaaaaa" ( node :?> IConfigureNode ) ( prompt :?> string )

    [<Theory>]
    [<MemberData( "m_CommandLoop_expection_data" )>]
    member _.CommandLoop_expection_001 ( expd : obj ) ( prompt : obj ) ( msg : obj ) =
        let st = new StringTable( "" )
        let in_ms, in_ws, in_rs = GenCommandStream( "validate" )
        let out_ms, out_ws = GenOutputStream()
        let cr = new CommandRunner( st, in_rs, out_ws )
        let ss = new ServerStatusStub( st )
        let cc = new CtrlConnectionStub( st )
        let cn = CommandRunner_Test1.m_ControllerNode

        ss.p_Validate <- ( fun () ->
            raise <| ( expd :?> Exception )
            []
        )

        let r, stat = CallCommandLoop cr ( Some ( ss, cc, cn ) )
        Assert.True(( r ))
        Assert.True(( stat = Some( ss, cc, cn ) ))

        let out_rs = GenOutputStreamReader out_ms out_ws
        CheckPromptAndMessage out_rs ( prompt :?> string ) ( msg :?> string )
        GlbFunc.AllDispose [ in_ws; in_rs; in_ms; out_ws; out_rs; out_ms; ]

