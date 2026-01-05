//=============================================================================
// Haruka Software Storage.
// Stub.fs : Defines stub classes for debug use.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks
open System.Net

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// <summary>
///  Default stub class for IIscsiTask.
/// </summary>
type public CISCSITask_Stub() =

    let mutable f_GetTaskType : ( unit -> iSCSITaskType ) option = None
    let mutable f_GetTaskTypeName : ( unit -> string ) option = None
    let mutable f_GetCmdSN : ( unit -> CMDSN_T voption ) option = None
    let mutable f_GetLUN : ( unit -> LUN_T voption ) option = None
    let mutable f_GetImmidiate: ( unit -> bool voption ) option = None
    let mutable f_GetInitiatorTaskTag : ( unit -> ITT_T voption ) option = None
    let mutable f_IsExecutable : ( unit -> bool ) option = None
    let mutable f_GetAllegiantConnection : ( unit -> struct( CID_T * CONCNT_T ) ) option = None
    let mutable f_GetExecuteTask : ( unit -> struct( ( unit -> unit ) * IIscsiTask ) ) option = None
    let mutable f_IsRemovable : ( unit -> bool ) option = None
    let mutable f_Executed : ( unit -> bool ) option = None

    member val dummy : obj = box () with get, set
    member _.p_GetTaskType with set v = f_GetTaskType <- Some( v )
    member _.p_GetTaskTypeName with set v = f_GetTaskTypeName <- Some( v )
    member _.p_GetCmdSN with set v = f_GetCmdSN <- Some( v )
    member _.p_GetLUN with set v = f_GetLUN <- Some( v )
    member _.p_GetImmidiate with set v = f_GetImmidiate <- Some( v )
    member _.p_GetInitiatorTaskTag with set v = f_GetInitiatorTaskTag <- Some( v )
    member _.p_IsExecutable with set v = f_IsExecutable <- Some( v )
    member _.p_GetAllegiantConnection with set v = f_GetAllegiantConnection <- Some( v )
    member _.p_GetExecuteTask with set v = f_GetExecuteTask <- Some( v )
    member _.p_IsRemovable with set v = f_IsRemovable <- Some( v )
    member _.p_Executed with set v = f_Executed <- Some( v )

    interface IIscsiTask with
        override _.TaskType with get() =
            f_GetTaskType.Value ()
        override _.TaskTypeName with get() =
            f_GetTaskTypeName.Value ()
        override _.CmdSN with get() =
            f_GetCmdSN.Value ()
        override _.LUN with get() =
            f_GetLUN.Value ()
        override _.Immidiate with get() =
            f_GetImmidiate.Value ()
        override _.InitiatorTaskTag with get() =
            f_GetInitiatorTaskTag.Value ()
        override _.IsExecutable with get() =
            f_IsExecutable.Value ()
        override _.AllegiantConnection with get() =
            f_GetAllegiantConnection.Value ()
        override _.GetExecuteTask () =
            f_GetExecuteTask.Value ()
        override _.IsRemovable with get() =
            f_IsRemovable.Value ()
        override _.Executed with get() =
            f_Executed.Value ()

/// <summary>
///  Default stub class for IConfiguration.
/// </summary>
type public CConfiguration_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_GetNetworkPortal : ( unit -> TargetDeviceConf.T_NetworkPortal list ) option = None
    let mutable f_GetTargetGroupID : ( unit -> TGID_T[] ) option = None
    let mutable f_GetTargetGroupConf : ( TGID_T -> TargetGroupConf.T_TargetGroup option ) option = None
    let mutable f_GetAllTargetGroupConf : ( unit -> ( TargetGroupConf.T_TargetGroup * IKiller ) [] ) option = None
    let mutable f_UnloadTargetGroup : ( TGID_T -> unit ) option = None
    let mutable f_LoadTargetGroup : ( TGID_T -> bool ) option = None
    let mutable f_GetDefaultLogParameters : ( unit -> struct ( uint32 * uint32 * LogLevel ) ) option = None
    let mutable f_GetISCSINegoParamCO : ( unit -> IscsiNegoParamCO ) option = None
    let mutable f_GetISCSINegoParamSW : ( unit -> IscsiNegoParamSW ) option = None
    let mutable f_GetDeviceName : ( unit -> string ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_GetNetworkPortal with set v = f_GetNetworkPortal <- Some( v )
    member _.p_GetTargetGroupID with set v = f_GetTargetGroupID <- Some( v )
    member _.p_GetTargetGroupConf with set v = f_GetTargetGroupConf <- Some( v )
    member _.p_GetAllTargetGroupConf with set v = f_GetAllTargetGroupConf <- Some( v )
    member _.p_UnloadTargetGroup with set v = f_UnloadTargetGroup <- Some( v )
    member _.p_LoadTargetGroup with set v = f_LoadTargetGroup <- Some( v )
    member _.p_GetDefaultLogParameters with set v = f_GetDefaultLogParameters <- Some( v )
    member _.p_GetISCSINegoParamCO with set v = f_GetISCSINegoParamCO <- Some( v )
    member _.p_GetISCSINegoParamSW with set v = f_GetISCSINegoParamSW <- Some( v )
    member _.p_GetDeviceName with set v = f_GetDeviceName <- Some( v )

    interface IConfiguration with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.GetNetworkPortal() =
            f_GetNetworkPortal.Value ()
        override _.GetTargetGroupID() =
            f_GetTargetGroupID.Value ()
        override _.GetTargetGroupConf ( id : TGID_T ) =
            f_GetTargetGroupConf.Value id
        override _.GetAllTargetGroupConf () =
            f_GetAllTargetGroupConf.Value ()
        override _.UnloadTargetGroup ( id : TGID_T ) =
            f_UnloadTargetGroup.Value id
        override _.LoadTargetGroup ( id : TGID_T ) =
            f_LoadTargetGroup.Value id
        override _.GetDefaultLogParameters() =
            f_GetDefaultLogParameters.Value()
        override _.IscsiNegoParamCO with get() =
            f_GetISCSINegoParamCO.Value ()
        override _.IscsiNegoParamSW with get() =
            f_GetISCSINegoParamSW.Value ()
        override _.DeviceName with get() =
            f_GetDeviceName.Value ()

/// <summary>
///  Default stub class for IStatus.
/// </summary>
type public CStatus_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_GetNetworkPortal : ( unit -> TargetDeviceConf.T_NetworkPortal list ) option = None
    let mutable f_GetActiveTargetGroup : ( unit -> TargetGroupConf.T_TargetGroup list ) option = None
    let mutable f_GetActiveTarget : ( unit -> TargetGroupConf.T_Target list ) option = None
    let mutable f_GetTargetFromLUN : ( LUN_T -> TargetGroupConf.T_Target list ) option = None
    let mutable f_GetISCSINegoParamCO : ( unit -> IscsiNegoParamCO ) option = None
    let mutable f_GetISCSINegoParamSW : ( unit -> IscsiNegoParamSW ) option = None
    let mutable f_CreateLoginNegociator : ( System.IO.Stream -> DateTime -> TPGT_T -> NETPORTIDX_T -> ILoginNegociator ) option = None
    let mutable f_GetTSIH : ( ITNexus -> TSIH_T ) option = None
    let mutable f_GenNewTSIH : ( unit -> TSIH_T ) option = None
    let mutable f_GetSession : ( TSIH_T -> ISession voption ) option = None
    let mutable f_GetITNexusFromLUN : ( LUN_T -> ITNexus[] ) option = None
    let mutable f_CreateNewSession : ( ITNexus -> TSIH_T -> IscsiNegoParamSW -> CMDSN_T -> ISession voption ) option = None
    let mutable f_RemoveSession : ( TSIH_T -> unit ) option = None
    let mutable f_GetLU : ( LUN_T -> ILU voption ) option = None
    let mutable f_CreateMedia : ( Haruka.IODataTypes.TargetGroupConf.T_MEDIA -> LUN_T -> IKiller -> IMedia ) option = None
    let mutable f_NotifyLUReset : ( LUN_T -> ILU -> unit ) option = None
    let mutable f_ProcessControlRequest : ( unit -> System.Threading.Tasks.Task ) option = None
    let mutable f_Start : ( unit -> unit ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_GetNetworkPortal with set v = f_GetNetworkPortal <- Some( v )
    member _.p_GetActiveTargetGroup with set v = f_GetActiveTargetGroup <- Some( v )
    member _.p_GetActiveTarget with set v = f_GetActiveTarget <- Some( v )
    member _.p_GetTargetFromLUN with set v = f_GetTargetFromLUN <- Some( v )
    member _.p_GetISCSINegoParamCO with set v = f_GetISCSINegoParamCO <- Some( v )
    member _.p_GetISCSINegoParamSW with set v = f_GetISCSINegoParamSW <- Some( v )
    member _.p_CreateLoginNegociator with set v = f_CreateLoginNegociator <- Some( v )
    member _.p_GetTSIH with set v = f_GetTSIH <- Some( v )
    member _.p_GenNewTSIH with set v = f_GenNewTSIH <- Some( v )
    member _.p_GetSession with set v = f_GetSession <- Some( v )
    member _.p_GetITNexusFromLUN with set v = f_GetITNexusFromLUN <- Some( v )
    member _.p_CreateNewSession with set v = f_CreateNewSession <- Some( v )
    member _.p_RemoveSession with set v = f_RemoveSession <- Some( v )
    member _.p_GetLU with set v = f_GetLU <- Some( v )
    member _.p_CreateMedia with set v = f_CreateMedia <- Some( v )
    member _.p_NotifyLUReset with set v = f_NotifyLUReset <- Some( v )
    member _.p_ProcessControlRequest with set v = f_ProcessControlRequest <- Some( v )
    member _.p_Start with set v = f_Start <- Some( v )

    interface IStatus with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.GetNetworkPortal () =
            f_GetNetworkPortal.Value ()
        override _.GetActiveTargetGroup () =
            f_GetActiveTargetGroup.Value ()
        override _.GetActiveTarget () =
            f_GetActiveTarget.Value ()
        override _.GetTargetFromLUN ( lun : LUN_T ) =
            f_GetTargetFromLUN.Value lun
        override _.IscsiNegoParamCO with get() =
            f_GetISCSINegoParamCO.Value ()
        override _.IscsiNegoParamSW with get() =
            f_GetISCSINegoParamSW.Value ()
        override _.CreateLoginNegociator ( sock : System.Net.Sockets.NetworkStream ) ( ct : DateTime ) ( targetPortalGroupTag : TPGT_T ) ( netPortIdx : NETPORTIDX_T ) =
            f_CreateLoginNegociator.Value sock ct targetPortalGroupTag netPortIdx
        override _.GetTSIH ( argI_TNexus : ITNexus ) : TSIH_T =
            f_GetTSIH.Value argI_TNexus
        override _.GenNewTSIH () : TSIH_T =
            f_GenNewTSIH.Value()
        override _.GetSession ( tsih : TSIH_T ) : ISession voption =
            f_GetSession.Value tsih
        override _.GetITNexusFromLUN ( lun : LUN_T ) : ITNexus[] =
            f_GetITNexusFromLUN.Value lun
        override _.CreateNewSession ( argI_TNexus:ITNexus ) ( argTSIH : TSIH_T ) ( sessionParameter : IscsiNegoParamSW ) ( newCmdSN : CMDSN_T ) : ISession voption =
            f_CreateNewSession.Value argI_TNexus argTSIH sessionParameter newCmdSN
        override this.RemoveSession ( tsih : TSIH_T ) : unit =
            f_RemoveSession.Value tsih
        override _.GetLU ( argLUN : LUN_T ) : ILU voption =
            f_GetLU.Value argLUN
        override _.CreateMedia ( confInfo : Haruka.IODataTypes.TargetGroupConf.T_MEDIA ) ( lun : LUN_T ) ( argKiller : IKiller ) : IMedia =
            f_CreateMedia.Value confInfo lun argKiller
        override _.NotifyLUReset ( lun : LUN_T ) ( lu : ILU ) : unit =
            f_NotifyLUReset.Value lun lu
        override _.ProcessControlRequest() : System.Threading.Tasks.Task =
            f_ProcessControlRequest.Value()
        override _.Start() : unit =
            f_Start.Value()

/// <summary>
///  Default stub class for ISession.
/// </summary>
type public CSession_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_GetCreateDate : ( unit -> DateTime ) option = None
    let mutable f_GetSessionParameter : ( unit -> IscsiNegoParamSW ) option = None
    let mutable f_GetTSIH : ( unit -> TSIH_T ) option = None
    let mutable f_GetI_TNexus : ( unit -> ITNexus ) option = None
    let mutable f_GetNextTTT : ( unit -> TTT_T ) option = None
    let mutable f_IsExistCID : ( CID_T -> bool ) option = None
    let mutable f_AddNewConnection : ( System.IO.Stream -> DateTime -> CID_T -> NETPORTIDX_T -> TPGT_T -> IscsiNegoParamCO -> bool  ) option = None
    let mutable f_ReinstateConnection : ( System.IO.Stream -> DateTime -> CID_T -> NETPORTIDX_T -> TPGT_T -> IscsiNegoParamCO -> bool  ) option = None
    let mutable f_RemoveConnection : ( CID_T -> CONCNT_T -> unit ) option = None
    let mutable f_PushReceivedPDU : ( IConnection -> ILogicalPDU -> unit ) option = None
    let mutable f_UpdateMaxCmdSN : ( unit -> struct( CMDSN_T * CMDSN_T ) ) option = None
    let mutable f_GetConnection : ( CID_T -> CONCNT_T -> IConnection voption ) option = None
    let mutable f_GetAllConnections : ( unit -> IConnection array ) option = None
    let mutable f_GetSCSITaskRouter : ( unit -> IProtocolService ) option = None
    let mutable f_IsAlive : ( unit -> bool ) option = None
    let mutable f_DestroySession : ( unit -> unit ) option = None
    let mutable f_SendSCSIResponse : ( SCSICommandPDU -> CID_T -> CONCNT_T -> uint32 -> iScsiSvcRespCd -> ScsiCmdStatCd -> PooledBuffer -> PooledBuffer -> uint32 -> ResponseFenceNeedsFlag -> unit ) option = None
    let mutable f_RejectPDUByLogi : ( CID_T -> CONCNT_T -> ILogicalPDU -> RejectReasonCd -> unit ) option = None
    let mutable f_RejectPDUByHeader : ( CID_T -> CONCNT_T -> byte[] -> RejectReasonCd -> unit ) option = None
    let mutable f_SendOtherResponsePDU : ( CID_T -> CONCNT_T -> ILogicalPDU -> unit ) option = None
    let mutable f_ResendPDU : ( CID_T -> CONCNT_T -> ILogicalPDU -> unit ) option = None
    let mutable f_ResendPDUForRSnack : ( CID_T -> CONCNT_T -> ILogicalPDU -> unit ) option = None
    let mutable f_NoticeUpdateSessionParameter : ( IscsiNegoParamSW -> unit ) option = None
    let mutable f_NoticeUnlockResponseFence : ( ResponseFenceNeedsFlag -> unit ) option = None
    let mutable f_Abort_iSCSITask : ( ( IIscsiTask -> bool ) -> bool ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_GetCreateDate with set v = f_GetCreateDate <- Some( v )
    member _.p_GetSessionParameter with set v = f_GetSessionParameter <- Some( v )
    member _.p_GetTSIH with set v = f_GetTSIH <- Some( v )
    member _.p_GetI_TNexus with set v = f_GetI_TNexus <- Some( v )
    member _.p_GetNextTTT with set v = f_GetNextTTT <- Some( v )
    member _.p_IsExistCID with set v = f_IsExistCID <- Some( v )
    member _.p_AddNewConnection with set v = f_AddNewConnection <- Some( v )
    member _.p_ReinstateConnection with set v = f_ReinstateConnection <- Some( v )
    member _.p_RemoveConnection with set v = f_RemoveConnection <- Some( v )
    member _.p_PushReceivedPDU with set v = f_PushReceivedPDU <- Some( v )
    member _.p_UpdateMaxCmdSN with set v = f_UpdateMaxCmdSN <- Some( v )
    member _.p_GetConnection with set v = f_GetConnection <- Some( v )
    member _.p_GetAllConnections with set v = f_GetAllConnections <- Some( v )
    member _.p_GetSCSITaskRouter with set v = f_GetSCSITaskRouter <- Some( v )
    member _.p_IsAlive with set v = f_IsAlive <- Some( v )
    member _.p_DestroySession with set v = f_DestroySession <- Some( v )
    member _.p_SendSCSIResponse with set v = f_SendSCSIResponse <- Some( v )
    member _.p_RejectPDUByLogi with set v = f_RejectPDUByLogi <- Some( v )
    member _.p_RejectPDUByHeader with set v = f_RejectPDUByHeader <- Some( v )
    member _.p_SendOtherResponsePDU with set v = f_SendOtherResponsePDU <- Some( v )
    member _.p_ResendPDU with set v = f_ResendPDU <- Some( v )
    member _.p_ResendPDUForRSnack with set v = f_ResendPDUForRSnack <- Some( v )
    member _.p_NoticeUpdateSessionParameter with set v = f_NoticeUpdateSessionParameter <- Some( v )
    member _.p_NoticeUnlockResponseFence with set v = f_NoticeUnlockResponseFence <- Some( v )
    member _.p_Abort_iSCSITask with set v = f_Abort_iSCSITask <- Some( v )

    interface ISession with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.CreateDate with get() =
            f_GetCreateDate.Value ()
        override _.SessionParameter with get() =
            f_GetSessionParameter.Value ()
        override _.TSIH with get() =
            f_GetTSIH.Value ()
        override _.I_TNexus with get() =
            f_GetI_TNexus.Value ()
        override _.NextTTT with get() =
            f_GetNextTTT.Value ()
        override _.IsExistCID ( cid : CID_T ) =
            f_IsExistCID.Value cid
        override _.AddNewConnection
                ( sock : System.IO.Stream )
                ( conTime:DateTime )
                ( newCID : CID_T )
                ( netPortIdx : NETPORTIDX_T )
                ( tpgt : TPGT_T )
                ( iSCSIParamsCO : IscsiNegoParamCO ) :
                bool =
            f_AddNewConnection.Value sock conTime newCID netPortIdx tpgt iSCSIParamsCO
        override _.ReinstateConnection
                ( sock : System.IO.Stream )
                ( conTime:DateTime )
                ( newCID : CID_T )
                ( netPortIdx : NETPORTIDX_T )
                ( tpgt : TPGT_T )
                ( iSCSIParamsCO : IscsiNegoParamCO ) :
                bool =
            f_ReinstateConnection.Value sock conTime newCID netPortIdx tpgt iSCSIParamsCO
        override _.RemoveConnection ( cid : CID_T ) ( concnt : CONCNT_T ) : unit =
            f_RemoveConnection.Value cid concnt
        override _.PushReceivedPDU ( conn : IConnection ) ( pdu : ILogicalPDU ) : unit =
            f_PushReceivedPDU.Value conn pdu
        override _.UpdateMaxCmdSN() : struct( CMDSN_T * CMDSN_T ) =
            f_UpdateMaxCmdSN.Value ()
        override _.GetConnection ( cid : CID_T ) ( counter : CONCNT_T ) : IConnection voption =
            f_GetConnection.Value cid counter
        override _.GetAllConnections () : IConnection array =
            f_GetAllConnections.Value ()
        override _.SCSITaskRouter with get() : IProtocolService =
            f_GetSCSITaskRouter.Value ()
        override _.IsAlive with get() : bool =
            f_IsAlive.Value ()
        override _.DestroySession () : unit = 
            f_DestroySession.Value ()
        override _.SendSCSIResponse
                ( reqCmdPDU : SCSICommandPDU )
                ( cid : CID_T )
                ( counter : CONCNT_T )
                ( recvDataLength : uint32 )
                ( argRespCode : iScsiSvcRespCd )
                ( argStatCode : ScsiCmdStatCd )
                ( senseData : PooledBuffer )
                ( resData : PooledBuffer )
                ( allocationLength : uint32 ) 
                ( needResponseFence : ResponseFenceNeedsFlag ) :
                unit =
            f_SendSCSIResponse.Value reqCmdPDU cid counter recvDataLength argRespCode argStatCode senseData resData allocationLength needResponseFence
        override _.RejectPDUByLogi ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) ( argReason : RejectReasonCd ) : unit =
            f_RejectPDUByLogi.Value cid counter pdu argReason
        override _.RejectPDUByHeader ( cid : CID_T ) ( counter : CONCNT_T ) ( header : byte[] ) ( argReason : RejectReasonCd ) : unit =
            f_RejectPDUByHeader.Value cid counter header argReason
        override _.SendOtherResponsePDU ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) : unit =
            f_SendOtherResponsePDU.Value cid counter pdu
        override _.ResendPDU ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) : unit =
            f_ResendPDU.Value cid counter pdu
        override _.ResendPDUForRSnack ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) : unit =
            f_ResendPDUForRSnack.Value cid counter pdu
        override _.NoticeUpdateSessionParameter ( argSWParams : IscsiNegoParamSW ) : unit =
            f_NoticeUpdateSessionParameter.Value argSWParams
        override _.NoticeUnlockResponseFence ( mode : ResponseFenceNeedsFlag ) : unit =
            f_NoticeUnlockResponseFence.Value mode
        override _.Abort_iSCSITask ( f : ( IIscsiTask -> bool ) ) : bool =
            f_Abort_iSCSITask.Value f

/// <summary>
///  Default stub class for IProtocolService.
/// </summary>
type public CProtocolService_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_AbortTask : ( IIscsiTask -> LUN_T -> ITT_T -> unit ) option = None
    let mutable f_AbortTaskSet : ( IIscsiTask -> LUN_T -> unit ) option = None
    let mutable f_ClearACA : ( IIscsiTask -> LUN_T -> unit ) option = None
    let mutable f_ClearTaskSet : ( IIscsiTask -> LUN_T -> unit ) option = None
    let mutable f_LogicalUnitReset : ( IIscsiTask -> LUN_T -> unit ) option = None
    let mutable f_TargetReset : ( IIscsiTask -> LUN_T -> unit ) option = None
    let mutable f_SCSICommand : ( CID_T -> CONCNT_T -> SCSICommandPDU -> SCSIDataOutPDU list -> unit ) option = None
    let mutable f_SendSCSIResponse : ( SCSICommandPDU -> CID_T -> CONCNT_T -> uint32 -> iScsiSvcRespCd -> ScsiCmdStatCd -> PooledBuffer -> PooledBuffer -> uint32 -> ResponseFenceNeedsFlag -> unit ) option = None
    let mutable f_SendOtherResponse : ( CID_T -> CONCNT_T -> ILogicalPDU -> LUN_T -> unit ) option = None
    let mutable f_GetTSIH : ( unit -> TSIH_T ) option = None
    let mutable f_NoticeSessionRecovery : ( string -> unit ) option = None
    let mutable f_GetSessionParameter : ( unit -> IscsiNegoParamSW ) option = None
    let mutable f_GetLUNs : ( unit -> LUN_T[] ) option = None
    let mutable f_GetTaskQueueUsage : ( unit -> int ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_AbortTask with set v = f_AbortTask <- Some( v )
    member _.p_AbortTaskSet with set v = f_AbortTaskSet <- Some( v )
    member _.p_ClearACA with set v = f_ClearACA <- Some( v )
    member _.p_ClearTaskSet with set v = f_ClearTaskSet <- Some( v )
    member _.p_LogicalUnitReset with set v = f_LogicalUnitReset <- Some( v )
    member _.p_TargetReset with set v = f_TargetReset <- Some( v )
    member _.p_SCSICommand with set v = f_SCSICommand <- Some( v )
    member _.p_SendSCSIResponse with set v = f_SendSCSIResponse <- Some( v )
    member _.p_SendOtherResponse with set v = f_SendOtherResponse <- Some( v )
    member _.p_GetTSIH with set v = f_GetTSIH <- Some( v )
    member _.p_NoticeSessionRecovery with set v = f_NoticeSessionRecovery <- Some( v )
    member _.p_GetSessionParameter with set v = f_GetSessionParameter <- Some( v )
    member _.p_GetLUNs with set v = f_GetLUNs <- Some( v )
    member _.p_GetTaskQueueUsage with set v = f_GetTaskQueueUsage <- Some( v )

    interface IProtocolService with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.AbortTask ( iScsiTask:  IIscsiTask ) ( lun : LUN_T ) ( referencedTaskTag : ITT_T ) : unit =
            f_AbortTask.Value iScsiTask lun referencedTaskTag
        override _.AbortTaskSet ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            f_AbortTaskSet.Value iScsiTask lun
        override _.ClearACA ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            f_ClearACA.Value iScsiTask lun
        override _.ClearTaskSet ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            f_ClearTaskSet.Value iScsiTask lun
        override _.LogicalUnitReset ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            f_LogicalUnitReset.Value iScsiTask lun
        override _.TargetReset ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            f_TargetReset.Value iScsiTask lun
        override _.SCSICommand ( cid:CID_T ) ( counter:CONCNT_T ) ( command:SCSICommandPDU ) ( data:SCSIDataOutPDU list ) : unit =
            f_SCSICommand.Value cid counter command data
        override _.SendSCSIResponse
                ( reqCmdPDU : SCSICommandPDU )
                ( cid : CID_T )
                ( counter : CONCNT_T )
                ( recvDataLength : uint32 )
                ( argRespCode : iScsiSvcRespCd )
                ( argStatCode : ScsiCmdStatCd )
                ( senseData : PooledBuffer )
                ( resData : PooledBuffer )
                ( allocationLength : uint32 ) 
                ( needResponseFence : ResponseFenceNeedsFlag ) :
                unit =
                    f_SendSCSIResponse.Value reqCmdPDU cid counter recvDataLength argRespCode argStatCode senseData resData allocationLength needResponseFence
        override _.SendOtherResponse ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) ( lun : LUN_T ) : unit =
            f_SendOtherResponse.Value cid counter pdu lun
        override _.TSIH with get() : TSIH_T =
            f_GetTSIH.Value ()
        override _.NoticeSessionRecovery ( msg:string ) : unit =
            f_NoticeSessionRecovery.Value msg
        override _.SessionParameter with get() =
            f_GetSessionParameter.Value ()
        override _.GetLUNs() : LUN_T[] =
            f_GetLUNs.Value ()
        override _.GetTaskQueueUsage() : int =
            f_GetTaskQueueUsage.Value ()

/// <summary>
///  Default stub class for ILU.
/// </summary>
type public CLU_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_AbortTask : ( CommandSourceInfo -> ITT_T -> ITT_T -> unit ) option = None
    let mutable f_AbortTaskSet : ( CommandSourceInfo -> ITT_T -> unit ) option = None
    let mutable f_ClearACA : ( CommandSourceInfo -> ITT_T -> unit ) option = None
    let mutable f_ClearTaskSet : ( CommandSourceInfo -> ITT_T -> unit ) option = None
    let mutable f_LogicalUnitReset : ( CommandSourceInfo voption -> ITT_T voption -> bool -> unit ) option = None
    let mutable f_SCSICommand : ( CommandSourceInfo -> SCSICommandPDU -> SCSIDataOutPDU list -> unit ) option = None
    let mutable f_GetLUResetStatus : ( unit -> bool ) option = None
    let mutable f_GetReadBytesCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetWrittenBytesCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetReadTickCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetWriteTickCount : ( unit -> ResCountResult array ) option = None
    let mutable f_ACAStatus : ( unit -> struct( ITNexus * ScsiCmdStatCd * SenseKeyCd * ASCCd * bool ) voption ) option = None
    let mutable f_GetMedia : ( unit -> IMedia ) option = None
    let mutable f_GetTaskQueueUsage : ( TSIH_T -> int ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_AbortTask with set v = f_AbortTask <- Some( v )
    member _.p_AbortTaskSet with set v = f_AbortTaskSet <- Some( v )
    member _.p_ClearACA with set v = f_ClearACA <- Some( v )
    member _.p_ClearTaskSet with set v = f_ClearTaskSet <- Some( v )
    member _.p_LogicalUnitReset with set v = f_LogicalUnitReset <- Some( v )
    member _.p_SCSICommand with set v = f_SCSICommand <- Some( v )
    member _.p_GetLUResetStatus with set v = f_GetLUResetStatus <- Some( v )
    member _.p_GetReadBytesCount with set v = f_GetReadBytesCount <- Some( v )
    member _.p_GetWrittenBytesCount with set v = f_GetWrittenBytesCount <- Some( v )
    member _.p_GetReadTickCount with set v = f_GetReadTickCount <- Some( v )
    member _.p_GetWriteTickCount with set v = f_GetWriteTickCount <- Some( v )
    member _.p_ACAStatus with set v = f_ACAStatus <- Some( v )
    member _.p_GetMedia with set v = f_GetMedia <- Some( v )
    member _.p_GetTaskQueueUsage with set v = f_GetTaskQueueUsage <- Some( v )

    interface ILU with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.AbortTask ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) ( referencedTaskTag : ITT_T ) : unit =
            f_AbortTask.Value source initiatorTaskTag referencedTaskTag
        override _.AbortTaskSet ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) : unit =
            f_AbortTaskSet.Value source initiatorTaskTag
        override _.ClearACA ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) : unit =
            f_ClearACA.Value source initiatorTaskTag
        override _.ClearTaskSet ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) : unit =
            f_ClearTaskSet.Value source initiatorTaskTag
        override _.LogicalUnitReset ( source : CommandSourceInfo voption ) ( initiatorTaskTag : ITT_T voption ) ( needResp : bool ) : unit =
            f_LogicalUnitReset.Value source initiatorTaskTag needResp
        override _.SCSICommand ( source : CommandSourceInfo ) ( command:SCSICommandPDU ) ( data:SCSIDataOutPDU list ) : unit =
            f_SCSICommand.Value source command data
        override _.LUResetStatus with get() : bool =
            f_GetLUResetStatus.Value ()
        override _.GetReadBytesCount() =
            f_GetReadBytesCount.Value ()
        override _.GetWrittenBytesCount() =
            f_GetWrittenBytesCount.Value ()
        override _.GetReadTickCount() =
            f_GetReadTickCount.Value ()
        override _.GetWriteTickCount() =
            f_GetWriteTickCount.Value ()
        override _.ACAStatus =
            f_ACAStatus.Value ()
        override _.GetMedia() =
            f_GetMedia.Value()
        override _.GetTaskQueueUsage ( tsih : TSIH_T ) : int =
            f_GetTaskQueueUsage.Value tsih

/// <summary>
///  Default stub class for IMedia.
/// </summary>
type public CMedia_Stub() =

    let mutable f_Initialize : ( unit -> unit ) option = None
    let mutable f_Closing : ( unit -> unit ) option = None
    let mutable f_TestUnitReady : ( ITT_T -> CommandSourceInfo -> ASCCd voption ) option = None
    let mutable f_ReadCapacity : ( ITT_T -> CommandSourceInfo -> uint64 ) option = None
    let mutable f_Read : ( ITT_T -> CommandSourceInfo -> BLKCNT64_T -> ArraySegment<byte> -> Task<int> ) option = None
    let mutable f_Write : ( ITT_T -> CommandSourceInfo -> BLKCNT64_T -> uint64 -> ArraySegment<byte> -> Task<int> ) option = None
    let mutable f_Format : ( ITT_T -> CommandSourceInfo -> Task<unit> ) option = None
    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_NotifyLUReset : ( ITT_T voption -> CommandSourceInfo voption -> unit ) option = None
    let mutable f_MediaControl : ( MediaCtrlReq.T_Request -> Task<MediaCtrlRes.T_Response> ) option = None
    let mutable f_GetBlockCount : ( unit -> uint64 ) option = None
    let mutable f_GetWriteProtect : ( unit -> bool ) option = None
    let mutable f_GetMediaIndex : ( unit -> MEDIAIDX_T ) option = None
    let mutable f_GetDescriptString : ( unit -> string ) option = None
    let mutable f_GetReadBytesCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetWrittenBytesCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetReadTickCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetWriteTickCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetSubMedia : ( unit -> IMedia list ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Initialize with set v = f_Initialize <- Some( v )
    member _.p_Closing with set v = f_Closing <- Some( v )
    member _.p_TestUnitReady with set v = f_TestUnitReady <- Some( v )
    member _.p_ReadCapacity with set v = f_ReadCapacity <- Some( v )
    member _.p_Read with set v = f_Read <- Some( v )
    member _.p_Write with set v = f_Write <- Some( v )
    member _.p_Format with set v = f_Format <- Some( v )
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_NotifyLUReset with set v = f_NotifyLUReset <- Some( v )
    member _.p_MediaControl with set v = f_MediaControl <- Some( v )
    member _.p_GetBlockCount with set v = f_GetBlockCount <- Some( v )
    member _.p_GetWriteProtect with set v = f_GetWriteProtect <- Some( v )
    member _.p_GetMediaIndex with set v = f_GetMediaIndex <- Some( v )
    member _.p_GetDescriptString with set v = f_GetDescriptString <- Some( v )
    member _.p_GetReadBytesCount with set v = f_GetReadBytesCount <- Some( v )
    member _.p_GetWrittenBytesCount with set v = f_GetWrittenBytesCount <- Some( v )
    member _.p_GetReadTickCount with set v = f_GetReadTickCount <- Some( v )
    member _.p_GetWriteTickCount with set v = f_GetWriteTickCount <- Some( v )
    member _.p_GetSubMedia with set v = f_GetSubMedia <- Some( v )

    interface IMedia with
        override _.Initialize() =
            f_Initialize.Value ()
        override _.Closing() =
            f_Closing.Value ()
        override _.TestUnitReady( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) =
            f_TestUnitReady.Value initiatorTaskTag source
        override _.ReadCapacity
                ( initiatorTaskTag : ITT_T )
                ( source : CommandSourceInfo ) :
                uint64 =
            f_ReadCapacity.Value initiatorTaskTag source
        override _.Read
                ( initiatorTaskTag : ITT_T )
                ( source : CommandSourceInfo )
                ( argLBA : BLKCNT64_T )
                ( buf : ArraySegment<byte> ) :
                Task<int> =
            f_Read.Value initiatorTaskTag source argLBA buf
        override _.Write
                ( initiatorTaskTag : ITT_T )
                ( source : CommandSourceInfo )
                ( argLBA : BLKCNT64_T )
                ( offset : uint64 ) 
                ( data : ArraySegment<byte> ) :
                Task<int> =
            f_Write.Value initiatorTaskTag source argLBA offset data
        override _.Format( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : Task<unit> =
            f_Format.Value initiatorTaskTag source
        override _.Terminate () =
            f_Terminate.Value ()
        override _.NotifyLUReset ( initiatorTaskTag:ITT_T voption ) ( source:CommandSourceInfo voption ) : unit =
            f_NotifyLUReset.Value initiatorTaskTag source
        override _.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            f_MediaControl.Value request
        override _.BlockCount : uint64 =
            f_GetBlockCount.Value ()
        override _.WriteProtect : bool =
            f_GetWriteProtect.Value ()
        override _.MediaIndex =
            f_GetMediaIndex.Value ()
        override _.DescriptString =
            f_GetDescriptString.Value ()
        override _.GetReadBytesCount() =
            f_GetReadBytesCount.Value ()
        override _.GetWrittenBytesCount() =
            f_GetWrittenBytesCount.Value ()
        override _.GetReadTickCount() =
            f_GetReadTickCount.Value ()
        override _.GetWriteTickCount() =
            f_GetWriteTickCount.Value ()
        override _.GetSubMedia() = f_GetSubMedia.Value()
        
/// <summary>
///  Default stub class for IPort.
/// </summary>
type public CPort_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_Start : ( unit -> bool ) option = None
    let mutable f_GetNetworkPortal : ( unit -> TargetDeviceConf.T_NetworkPortal ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_Start with set v = f_Start <- Some( v )
    member _.p_GetNetworkPortal with set v = f_GetNetworkPortal <- Some( v )

    interface IPort with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.Start () =
            f_Start.Value ()
        override _.NetworkPortal with get() =
            f_GetNetworkPortal.Value ()

/// <summary>
///  Default stub class for ILoginNegociator.
/// </summary>
type public CLoginNegociator_Stub() =

    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_Start : ( bool -> bool ) option = None

    member val dummy : obj = box () with get, set
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_Start with set v = f_Start <- Some( v )

    interface ILoginNegociator with
        override _.Terminate () =
            f_Terminate.Value ()
        override _.Start ( runSync : bool ) : bool =
            f_Start.Value runSync
        
/// <summary>
///  Default stub class for IConnection.
/// </summary>
type public CConnection_Stub() =

    let mutable f_ConnectedDate : ( unit -> DateTime ) option = None
    let mutable f_CurrentParams : ( unit -> IscsiNegoParamCO ) option = None
    let mutable f_TSIH : ( unit -> TSIH_T ) option = None
    let mutable f_CID : ( unit -> CID_T ) option = None
    let mutable f_NextStatSN : ( unit -> STATSN_T ) option = None
    let mutable f_ConCounter : ( unit -> CONCNT_T ) option = None
    let mutable f_NetPortIdx : ( unit -> NETPORTIDX_T ) option = None
    let mutable f_LocalAddress : ( unit -> IPEndPoint voption ) option = None
    let mutable f_Close : ( unit -> unit ) option = None
    let mutable f_Terminate : ( unit -> unit ) option = None
    let mutable f_StartFullFeaturePhase : ( unit -> unit ) option = None
    let mutable f_SendPDU : ( ILogicalPDU -> unit ) option = None
    let mutable f_ReSendPDU : ( ILogicalPDU -> unit ) option = None
    let mutable f_ReSendPDUForRSnack : ( ILogicalPDU -> unit ) option = None
    let mutable f_NotifyUpdateConnectionParameter : ( IscsiNegoParamCO -> unit ) option = None
    let mutable f_NotifyR2TSatisfied : ( ITT_T -> DATASN_T -> unit ) option = None
    let mutable f_NotifyDataAck : ( TTT_T -> LUN_T -> DATASN_T -> unit ) option = None
    let mutable f_GetSentDataInPDUForSNACK : ( ITT_T -> DATASN_T -> uint32 -> ILogicalPDU[] ) option = None
    let mutable f_GetSentResponsePDUForSNACK : ( STATSN_T -> uint32 -> ILogicalPDU[] ) option = None
    let mutable f_GetSentSCSIResponsePDUForR_SNACK : ( ITT_T -> ( SCSIDataInPDU[] * SCSIResponsePDU ) ) option = None
    let mutable f_R_SNACKRequest : ( ITT_T -> ( unit -> unit ) -> unit ) option = None
    let mutable f_GetReceiveBytesCount : ( unit -> ResCountResult array ) option = None
    let mutable f_GetSentBytesCount : ( unit -> ResCountResult array ) option = None

    member val dummy : obj = box () with get, set
    member _.p_ConnectedDate with set v = f_ConnectedDate <- Some( v )
    member _.p_CurrentParams with set v = f_CurrentParams <- Some( v )
    member _.p_TSIH with set v = f_TSIH <- Some( v )
    member _.p_CID with set v = f_CID <- Some( v )
    member _.p_NextStatSN with set v = f_NextStatSN <- Some( v )
    member _.p_ConCounter with set v = f_ConCounter <- Some( v )
    member _.p_NetPortIdx with set v = f_NetPortIdx <- Some( v )
    member _.p_LocalAddress with set v = f_LocalAddress <- Some( v )
    member _.p_Close with set v = f_Close <- Some( v )
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    member _.p_StartFullFeaturePhase with set v = f_StartFullFeaturePhase <- Some( v )
    member _.p_SendPDU with set v = f_SendPDU <- Some( v )
    member _.p_ReSendPDU with set v = f_ReSendPDU <- Some( v )
    member _.p_ReSendPDUForRSnack with set v = f_ReSendPDU <- Some( v )
    member _.p_NotifyUpdateConnectionParameter with set v = f_NotifyUpdateConnectionParameter <- Some( v )
    member _.p_NotifyR2TSatisfied with set v = f_NotifyR2TSatisfied <- Some( v )
    member _.p_NotifyDataAck with set v = f_NotifyDataAck <- Some( v )
    member _.p_GetSentDataInPDUForSNACK with set v = f_GetSentDataInPDUForSNACK <- Some( v )
    member _.p_GetSentResponsePDUForSNACK with set v = f_GetSentResponsePDUForSNACK <- Some( v )
    member _.p_GetSentSCSIResponsePDUForR_SNACK with set v = f_GetSentSCSIResponsePDUForR_SNACK <- Some( v )
    member _.p_R_SNACKRequest with set v = f_R_SNACKRequest <- Some( v )
    member _.p_GetReceiveBytesCount with set v = f_GetReceiveBytesCount <- Some( v )
    member _.p_GetSentBytesCount with set v = f_GetSentBytesCount <- Some( v )

    interface IConnection with
        override _.ConnectedDate with get() : DateTime =
            f_ConnectedDate.Value ()
        override _.CurrentParams with get() : IscsiNegoParamCO =
            f_CurrentParams.Value ()
        override _.TSIH with get() : TSIH_T =
            f_TSIH.Value()
        override _.CID with get() : CID_T =
            f_CID.Value()
        override _.NextStatSN : STATSN_T =
            f_NextStatSN.Value()
        override _.ConCounter with get() : CONCNT_T =
            f_ConCounter.Value()
        override _.NetPortIdx with get() : NETPORTIDX_T =
            f_NetPortIdx.Value()
        override _.LocalAddress with get() : IPEndPoint voption =
            f_LocalAddress.Value()
        override _.Close() : unit =
            f_Close.Value()
        override _.Terminate () =
            f_Terminate.Value ()
        override _.StartFullFeaturePhase () : unit =
            f_StartFullFeaturePhase.Value()
        override _.SendPDU ( pdu : ILogicalPDU ) : unit =
            f_SendPDU.Value pdu
        override _.ReSendPDU ( pdu : ILogicalPDU ) : unit =
            f_ReSendPDU.Value pdu
        override _.ReSendPDUForRSnack ( pdu : ILogicalPDU ) : unit =
            f_ReSendPDUForRSnack.Value pdu
        override _.NotifyUpdateConnectionParameter ( argCOParams : IscsiNegoParamCO ) : unit =
            f_NotifyUpdateConnectionParameter.Value argCOParams
        override _.NotifyR2TSatisfied ( itt : ITT_T ) ( r2tsn : DATASN_T ) : unit =
            f_NotifyR2TSatisfied.Value itt r2tsn
        override _.NotifyDataAck ( ttt : TTT_T ) ( lun : LUN_T ) ( begrun : DATASN_T ) : unit =
            f_NotifyDataAck.Value ttt lun begrun
        override _.GetSentDataInPDUForSNACK ( itt : ITT_T ) ( begrun : DATASN_T ) ( runlength : uint32 ) : ILogicalPDU[] =
            f_GetSentDataInPDUForSNACK.Value itt begrun runlength
        override _.GetSentResponsePDUForSNACK ( begrun : STATSN_T ) ( runlength : uint32 ) : ILogicalPDU[] =
            f_GetSentResponsePDUForSNACK.Value begrun runlength
        override _.GetSentSCSIResponsePDUForR_SNACK ( itt : ITT_T ) : ( SCSIDataInPDU[] * SCSIResponsePDU ) =
            f_GetSentSCSIResponsePDUForR_SNACK.Value itt
        override _.R_SNACKRequest ( itt : ITT_T ) ( cont : ( unit -> unit ) ) : unit =
            f_R_SNACKRequest.Value itt cont
        override _.GetReceiveBytesCount() =
            f_GetReceiveBytesCount.Value()
        override _.GetSentBytesCount() =
            f_GetSentBytesCount.Value()

/// <summary>
///  Default stub class for IKiller.
/// </summary>
type public CKiller_Stub() =
    let mutable f_Add : ( IComponent -> unit ) option = None
    let mutable f_NoticeTerminate : ( unit -> unit ) option = None
    let mutable f_IsNoticed : ( unit -> bool ) option = None

    member _.p_Add with set v = f_Add <- Some( v )
    member _.p_NoticeTerminate with set v = f_NoticeTerminate <- Some( v )
    member _.p_IsNoticed with set v = f_IsNoticed <- Some( v )

    interface IKiller with
        override _.Add( o : IComponent ) = f_Add.Value o
        override _.NoticeTerminate() = f_NoticeTerminate.Value()
        override _.IsNoticed = f_IsNoticed.Value()

/// <summary>
///  Default stub class for IComponent.
/// </summary>
type public CComponent_Stub() =
    let mutable f_Terminate : ( unit -> unit ) option = None
    member _.p_Terminate with set v = f_Terminate <- Some( v )
    interface IComponent with
        override _.Terminate() = f_Terminate.Value()

/// <summary>
///  Default stub class for IInternalLU.
/// </summary>
type public CInternalLU_Stub() =

    let mutable f_LUInterface : ( unit -> ILU ) option = None
    let mutable f_Media : ( unit -> IMedia ) option = None
    let mutable f_GetUnitAttention : ( ITNexus -> SCSIACAException voption ) option = None
    let mutable f_ClearUnitAttention : ( ITNexus -> unit ) option = None
    let mutable f_EstablishUnitAttention : ( string -> SCSIACAException -> unit ) option = None
    let mutable f_LUN : ( unit -> LUN_T ) option = None
    let mutable f_OptimalTransferLength : ( unit -> BLKCNT32_T ) option = None
    let mutable f_NotifyTerminateTask : ( IBlockDeviceTask -> unit ) option = None
    let mutable f_NotifyTerminateTaskWithException : ( IBlockDeviceTask -> Exception -> unit ) option = None
    let mutable f_AbortTasksFromSpecifiedITNexus : ( IBlockDeviceTask -> ITNexus[] -> bool -> unit ) option = None
    let mutable f_NotifyReadBytesCount : ( DateTime -> int64 -> unit ) option = None
    let mutable f_NotifyWrittenBytesCount : ( DateTime -> int64 -> unit ) option = None
    let mutable f_NotifyReadTickCount : ( DateTime -> int64 -> unit ) option = None
    let mutable f_NotifyWriteTickCount : ( DateTime -> int64 -> unit ) option = None

    member val dummy : obj = box () with get, set
    member _.p_LUInterface with set v = f_LUInterface <- Some( v )
    member _.p_Media with set v = f_Media <- Some( v )
    member _.p_GetUnitAttention with set v = f_GetUnitAttention <- Some( v )
    member _.p_ClearUnitAttention with set v = f_ClearUnitAttention <- Some( v )
    member _.p_EstablishUnitAttention with set v = f_EstablishUnitAttention <- Some( v )
    member _.p_LUN with set v = f_LUN <- Some( v )
    member _.p_OptimalTransferLength with set v = f_OptimalTransferLength <- Some( v )
    member _.p_NotifyTerminateTask with set v = f_NotifyTerminateTask <- Some( v )
    member _.p_NotifyTerminateTaskWithException with set v = f_NotifyTerminateTaskWithException <- Some( v )
    member _.p_AbortTasksFromSpecifiedITNexus with set v = f_AbortTasksFromSpecifiedITNexus <- Some( v )
    member _.p_NotifyReadBytesCount with set v = f_NotifyReadBytesCount <- Some( v )
    member _.p_NotifyWrittenBytesCount with set v = f_NotifyWrittenBytesCount <- Some( v )
    member _.p_NotifyReadTickCount with set v = f_NotifyReadTickCount <- Some( v )
    member _.p_NotifyWriteTickCount with set v = f_NotifyWriteTickCount <- Some( v )

    interface IInternalLU with
        override _.LUInterface with get() : ILU =
            f_LUInterface.Value ()
        override _.Media with get() : IMedia =
            f_Media.Value ()
        override _.GetUnitAttention ( nexus : ITNexus ) : SCSIACAException voption =
            f_GetUnitAttention.Value nexus
        override _.ClearUnitAttention ( nexus : ITNexus ) : unit =
            f_ClearUnitAttention.Value nexus
        override _.EstablishUnitAttention ( iport : string ) ( ex : SCSIACAException ) =
            f_EstablishUnitAttention.Value iport ex
        override _.LUN =
            f_LUN.Value()
        override _.OptimalTransferLength =
            f_OptimalTransferLength.Value()
        override _.NotifyTerminateTask ( argTask : IBlockDeviceTask ) : unit =
            f_NotifyTerminateTask.Value argTask
        override _.NotifyTerminateTaskWithException ( argTask : IBlockDeviceTask ) ( ex : Exception ) : unit =
            f_NotifyTerminateTaskWithException.Value argTask ex
        override _.AbortTasksFromSpecifiedITNexus ( self : IBlockDeviceTask ) ( itn : ITNexus[] ) ( abortAllACATask : bool ) =
            f_AbortTasksFromSpecifiedITNexus.Value self itn abortAllACATask
        override _.NotifyReadBytesCount ( d : DateTime ) ( cnt : int64 ) =
            f_NotifyReadBytesCount.Value d cnt
        override _.NotifyWrittenBytesCount ( d : DateTime ) ( cnt : int64 ) =
            f_NotifyWrittenBytesCount.Value d cnt
        override _.NotifyReadTickCount ( d : DateTime ) ( tm : int64 ) =
            f_NotifyReadTickCount.Value d tm
        override _.NotifyWriteTickCount ( d : DateTime ) ( tm : int64 ) =
            f_NotifyWriteTickCount.Value d tm

type ICDB_Stub =
    {
        m_Type : CDBTypes;
        m_OperationCode : byte;
        m_ServiceAction : uint16;
        m_NACA : bool;
        m_LINK : bool;
    }
    interface ICDB with
        member this.Type = this.m_Type
        member this.OperationCode = this.m_OperationCode
        member this.ServiceAction = this.m_ServiceAction
        member this.NACA = this.m_NACA
        member this.LINK = this.m_LINK
        member _.DescriptString = ""
