//=============================================================================
// Haruka Software Storage.
// SCSI_Initiator.fs : Implement the SCSI Initiator function used in the integration test.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Net.Sockets
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open System.Security.Cryptography
open System.Text
open System.Collections.Immutable

//=============================================================================
// Type definition

/// Holds the residual count value in the SCSI Response PDU.
type ResidualCount =
    /// Value when O bit is 1
    | Overflow of uint32
    /// Value when O bit is 0
    | Underflow of uint32

/// Holds the value of the SCSI Response PDU
[<NoComparison>]
type TaskResult_ScsiCmd = {
    /// Response field value in PDU.
    Response : iScsiSvcRespCd;
    /// Status field value in PDU.
    Status : ScsiCmdStatCd;
    /// Response data sent via SCSI Response PDU and SCSI Data-In PDU.
    /// If Sense data is provided in the SCSI Response PDU, an empty array is set.
    ResData : PooledBuffer;
    /// SenseData SCSI Response PDU. 
    Sense : SenseData voption;
    /// Residual count value in the SCSI Response PDU.
    Residual : ResidualCount;
}

/// Holds the execution results of tasks received from Target.
[<NoComparison>]
type TaskResult =
    /// Response to SCSI Command.
    | SCSI of TaskResult_ScsiCmd
    /// Response to TMF.
    | TMF of TaskMgrResCd
    | NOP

//=============================================================================
// Class implementation

/// <summary>
///  A class that implements the functionality of a SCSI initiator.
/// </summary>
/// <param name="m_ISCIInitiator">
///  An instance of the iSCSI initiator class that is logged in.
/// </param>
type SCSI_Initiator( m_ISCIInitiator : iSCSI_Initiator ) as this =

    /// CID of the connection.
    /// In the SCSI_Initiator class, the number of connections per session is limited to one.
    let m_CID = m_ISCIInitiator.CID.[0]

    /// TSIH value.
    let m_TSIH = m_ISCIInitiator.Params.TSIH

    /// An object to wait for the task completion status.
    let m_ReceiveWaiter = TaskWaiter< ITT_T, TaskResult >()

    /// An object used to wait after sending a command until values ​​are registered in m_OutBuffer and m_InBuffer.
    let m_RegistWaiter = TaskWaiter< ITT_T, uint32 >()

    /// A collection object that holds output data until the command completes.
    let m_OutBuffer = OptimisticLock( ImmutableDictionary< ITT_T, PooledBuffer >.Empty )

    /// A collection object for accumulating input data received until the command is completed.
    let m_InBuffer = OptimisticLock( ImmutableDictionary< ITT_T, SCSIDataInPDU list >.Empty )

    /// Set to True if a request to stop PDU reception processing is made.
    let mutable m_ExitFlg = false

    do
        Functions.StartTask this.Receiver

    //=========================================================================
    // property

    /// CID of the connection.
    member _.CID = m_CID

    /// TSIH value.
    member _.TSIH = m_TSIH

    /// Other Session Parameters 
    member _.SessionParams = m_ISCIInitiator.Params

    /// Other Connection Parameters
    member _.ConnectionParams = ( m_ISCIInitiator.Connection m_CID ).Params

    //=========================================================================
    // Static method

    /// <summary>
    ///  Connect to the target.
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Session wide parameters.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Connection wide parameters.
    /// </param>
    /// <returns>
    ///  The connected instance of the SCSI_Initiator class.
    /// </returns>
    static member Create ( exp_SessParams : SessParams ) ( exp_ConnParams : ConnParams ) : Task<SCSI_Initiator> =
        task {
            let! iit = iSCSI_Initiator.CreateInitialSession exp_SessParams exp_ConnParams
            return SCSI_Initiator( iit )
        }

    //=========================================================================
    // Public method

    /// <summary>
    ///  Stop PDU reception processing.
    /// </summary>
    member _.StopReceiver() : Task<unit> =
        task {
            if not( Volatile.Read &m_ExitFlg ) then
                Volatile.Write( &m_ExitFlg, true )
                let sendData = PooledBuffer.Rent [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]
                let! itt, _ = m_ISCIInitiator.SendNOPOutPDU m_CID BitI.T lun_me.zero ( ttt_me.fromPrim 0xFFFFFFFFu ) sendData
                let! _ = m_ReceiveWaiter.WaitAndReset itt
                sendData.Return()
        }

    /// <summary>
    ///  Restart PDU reception processing.
    /// </summary>
    member _.RestartReceiver() : unit =
        Volatile.Write( &m_ExitFlg, false )
        Functions.StartTask this.Receiver

    /// <summary>
    ///  Send SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="cdb">
    ///  SCSI command CDB.
    /// </param>
    /// <param name="param">
    ///  Output data.
    /// </param>
    /// <param name="edtl">
    ///  Expected Data Transfer Length.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member _.SendSCSICommand ( att : TaskATTRCd ) ( lun : LUN_T ) ( cdb : byte[] ) ( param : PooledBuffer ) ( edtl : uint32 ) : Task<ITT_T> =
        task {
            let writeCmd = ( param.Length > 0 )
            let rFlag = if writeCmd then BitR.F else BitR.T
            let wFlag = if writeCmd then BitW.T else BitW.F
            let fbl = m_ISCIInitiator.Params.FirstBurstLength

            if ( not writeCmd ) || param.uLength <= fbl then
                // If it is an read command or the amount of data to be transmitted is equal to or less than FirstBurstLength,
                // the command is transmitted as is.
                // In this case, no R2T PDUs are expected to be received.
                let! itt, _ = m_ISCIInitiator.SendSCSICommandPDU m_CID BitI.F BitF.T rFlag wFlag att lun edtl cdb param 0u

                if not writeCmd then
                    m_InBuffer.Update( fun old ->
                        if old.ContainsKey itt |> not then
                            old.Add( itt, [] )
                        else
                            old // Unexpected
                    )
                    |> ignore

                m_RegistWaiter.Notify( itt, edtl )
                return itt
            else
                let! itt, _ = m_ISCIInitiator.SendSCSICommandPDU m_CID BitI.F BitF.T rFlag wFlag att lun edtl cdb PooledBuffer.Empty 0u

                m_OutBuffer.Update( fun old ->
                    if old.ContainsKey itt |> not then
                        old.Add( itt, param )
                    else
                        old // Unexpedted
                )
                |> ignore

                m_RegistWaiter.Notify( itt, edtl )
                return itt
        }

    /// <summary>
    ///  Send TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argFunction">
    ///  TaskManagementFunctionRequestPDU Function field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <param name="argReferencedTaskTag">
    ///  TaskManagementFunctionRequestPDU ReferencedTaskTag field value.
    /// </param>
    /// <param name="argRefCmdSN">
    ///  TaskManagementFunctionRequestPDU RefCmdSN field value.
    ///  If ValueNone is specified, RefCmdSN is set to CmdSN value of this TMF command.
    /// </param>
    /// <param name="argExpDataSN">
    ///  TaskManagementFunctionRequestPDU ExpDataSN field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member _.SendTaskManagementFunctionRequest
            ( argI : BitI )
            ( argFunction : TaskMgrReqCd )
            ( argLUN : LUN_T )
            ( argReferencedTaskTag : ITT_T )
            ( argRefCmdSN : CMDSN_T voption )
            ( argExpDataSN : DATASN_T ) : Task<ITT_T> =
        task {
            let! itt, _ = m_ISCIInitiator.SendTaskManagementFunctionRequestPDU m_CID argI argFunction argLUN argReferencedTaskTag argRefCmdSN argExpDataSN
            m_RegistWaiter.Notify( itt, 0u )
            return itt
        }

    /// <summary>
    ///  To abort an immidiate task, send an ABORT TASK TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <param name="argReferencedTaskTag">
    ///  TaskManagementFunctionRequestPDU ReferencedTaskTag field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_AbortImmidiateTask ( argI : BitI ) ( argLUN : LUN_T ) ( argReferencedTaskTag : ITT_T )  : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.ABORT_TASK argLUN argReferencedTaskTag ValueNone datasn_me.zero

    /// <summary>
    ///  To abort a non-immidiate task, send an ABORT TASK TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <param name="argReferencedTaskTag">
    ///  TaskManagementFunctionRequestPDU ReferencedTaskTag field value.
    /// </param>
    /// <param name="argRefCmdSN">
    ///  TaskManagementFunctionRequestPDU RefCmdSN field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_AbortNonImmidiateTask ( argI : BitI ) ( argLUN : LUN_T ) ( argReferencedTaskTag : ITT_T ) ( argRefCmdSN : CMDSN_T ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.ABORT_TASK argLUN argReferencedTaskTag ( ValueSome argRefCmdSN ) datasn_me.zero

    /// <summary>
    ///  Send an ABORT TASK SET TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_AbortTaskSet ( argI : BitI ) ( argLUN : LUN_T ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.ABORT_TASK_SET argLUN ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero

    /// <summary>
    ///  Send an CLEAR ACA TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_ClearACA ( argI : BitI ) ( argLUN : LUN_T ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.CLEAR_ACA argLUN ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero

    /// <summary>
    ///  Send an CLEAR TASK SET TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_ClearTaskSet ( argI : BitI ) ( argLUN : LUN_T ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.CLEAR_TASK_SET argLUN ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero

    /// <summary>
    ///  Send an LOGICAL UNIT RESET TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_LogicalUnitReset ( argI : BitI ) ( argLUN : LUN_T ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.LOGICAL_UNIT_RESET argLUN ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero

    /// <summary>
    ///  Send an TARGET WARM RESET TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_TargetWarmReset ( argI : BitI ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.TARGET_WARM_RESET lun_me.zero ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero

    /// <summary>
    ///  Send an TARGET COLD RESET TMF request.
    /// </summary>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.SendTMFRequest_TargetColdReset ( argI : BitI ) : Task<ITT_T> =
        this.SendTaskManagementFunctionRequest argI TaskMgrReqCd.TARGET_COLD_RESET lun_me.zero ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero

    /// <summary>
    ///  Send INQUIRY SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argEVPD">
    ///  EVPD(enable vital product data) bit
    /// </param>
    /// <param name="argPageCode">
    ///  PAGE CODE field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Inquiry
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argEVPD : EVPD )
            ( argPageCode : byte )
            ( argAllocationLength : uint16 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.Inquiry argEVPD argPageCode argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty ( uint32 argAllocationLength )
        }

    /// <summary>
    ///  Send MODE SELECT(6) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argPageFormat">
    ///  PF bit
    /// </param>
    /// <param name="argSavePages">
    ///  SP bit
    /// </param>
    /// <param name="param">
    ///  Output data.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ModeSelect6
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argPageFormat : PF )
            ( argSavePages : SP )
            ( param : ModeParameter6 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let paramBytes = GenScsiParams.ModeSelect6 param
            if paramBytes.Count >= 256 then
                raise <| TestException( "Too long parameter data." )
            let cdb = GenScsiCDB.ModeSelect6 argPageFormat argSavePages ( byte paramBytes.Length ) argNACA LINK.F
            let! r = this.SendSCSICommand att lun cdb paramBytes paramBytes.uLength
            paramBytes.Return()
            return r
        }

    /// <summary>
    ///  Send MODE SELECT(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argPageFormat">
    ///  PF bit
    /// </param>
    /// <param name="argSavePages">
    ///  SP bit
    /// </param>
    /// <param name="param">
    ///  Output data.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ModeSelect10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argPageFormat : PF )
            ( argSavePages : SP )
            ( param : ModeParameter10 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let paramBytes = GenScsiParams.ModeSelect10 param
            if paramBytes.Length >= 65536 then
                raise <| TestException( "Too long parameter data." )
            let cdb = GenScsiCDB.ModeSelect10 argPageFormat argSavePages ( uint16 paramBytes.Length ) argNACA LINK.F
            let! r = this.SendSCSICommand att lun cdb paramBytes paramBytes.uLength
            paramBytes.Return()
            return r
        }

    /// <summary>
    ///  Send MODE SENSE(6) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argDBD">
    ///  DBD bit
    /// </param>
    /// <param name="argPC">
    ///  PC field
    /// </param>
    /// <param name="argPageCode">
    ///  PAGE CODE field
    /// </param>
    /// <param name="argSubPageCode">
    ///  SUB PAGE CODE field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ModeSense6
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argDBD : DBD )
            ( argPC : byte )
            ( argPageCode : byte )
            ( argSubPageCode : byte )
            ( argAllocationLength : byte )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ModeSense6 argDBD argPC argPageCode argSubPageCode argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty ( uint32 argAllocationLength )
        }

    /// <summary>
    ///  Send MODE SENSE(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLLBAA">
    ///  LLBAA bit
    /// </param>
    /// <param name="argDBD">
    ///  DBD bit
    /// </param>
    /// <param name="argPC">
    ///  PC field
    /// </param>
    /// <param name="argPageCode">
    ///  PAGE CODE field
    /// </param>
    /// <param name="argSubPageCode">
    ///  SUB PAGE CODE field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ModeSense10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLLBAA : LLBAA )
            ( argDBD : DBD )
            ( argPC : byte )
            ( argPageCode : byte )
            ( argSubPageCode : byte )
            ( argAllocationLength : uint16 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ModeSense10 argLLBAA argDBD argPC argPageCode argSubPageCode argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty ( uint32 argAllocationLength )
        }

    /// <summary>
    ///  Send PERSISTENT RESERVE IN SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argServiceAction">
    ///   SERVICE ACTION field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_PersistentReserveIn
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argServiceAction : byte )
            ( argAllocationLength : uint16 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.PersistentReserveIn argServiceAction argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty ( uint32 argAllocationLength )
        }

    /// <summary>
    ///  Send PERSISTENT RESERVE OUT SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argServiceAction">
    ///   SERVICE ACTION field, must be other than REGISTER AND MOVE.
    /// </param>
    /// <param name="argScope">
    ///   SCOPE field
    /// </param>
    /// <param name="argType">
    ///   TYPE field
    /// </param>
    /// <param name="param">
    ///   Parameter data.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_PersistentReserveOut_BasicParam
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argServiceAction : byte )
            ( argScope : byte )
            ( argType : byte )
            ( param : Haruka.BlockDeviceLU.BasicParameterList )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let paramBytes = GenScsiParams.PersistentReserveOut_BasicParameterList param
            let cdb = GenScsiCDB.PersistentReserveOut argServiceAction argScope argType paramBytes.uLength argNACA LINK.F
            let! r = this.SendSCSICommand att lun cdb paramBytes paramBytes.uLength
            paramBytes.Return()
            return r
        }

    /// <summary>
    ///  Send PERSISTENT RESERVE OUT SCSI Command with REGISTER AND MOVE service action.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argScope">
    ///   SCOPE field
    /// </param>
    /// <param name="argType">
    ///   TYPE field
    /// </param>
    /// <param name="param">
    ///   Parameter data.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_PersistentReserveOut_MoveParam
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argScope : byte )
            ( argType : byte )
            ( param : Haruka.BlockDeviceLU.MoveParameterList )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let paramBytes = GenScsiParams.PersistentReserveOut_MoveParameterList param
            let cdb = GenScsiCDB.PersistentReserveOut 0x07uy argScope argType paramBytes.uLength argNACA LINK.F
            let! r = this.SendSCSICommand att lun cdb paramBytes paramBytes.uLength
            paramBytes.Return()
            return r
        }

    /// <summary>
    ///  Send PRE-FETCH(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argIMMED">
    ///   IMMED bit
    /// </param>
    /// <param name="argLBA">
    ///   LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argPreFetchLength">
    ///  PREFETCH LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_PreFetch10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argIMMED : IMMED )
            ( argLBA : BLKCNT32_T )
            ( argPreFetchLength : BLKCNT16_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.PreFetch10 argIMMED argLBA 0uy argPreFetchLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 0u
        }

    /// <summary>
    ///  Send PRE-FETCH(16) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argIMMED">
    ///   IMMED bit
    /// </param>
    /// <param name="argLBA">
    ///   LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argPreFetchLength">
    ///  PREFETCH LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_PreFetch16
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argIMMED : IMMED )
            ( argLBA : BLKCNT64_T )
            ( argPreFetchLength : BLKCNT32_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.PreFetch16 argIMMED argLBA 0uy argPreFetchLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 0u
        }

    /// <summary>
    ///  Send REPORT LUNS SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argSelectReport">
    ///   SELECT REPORT field
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ReportLUNs
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argSelectReport : byte )
            ( argAllocationLength : uint32 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ReportLUNs argSelectReport argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty argAllocationLength
        }

    /// <summary>
    ///  Send REQUEST SENSE SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argDESC">
    ///   DESC bit
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_RequestSense
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argDESC : DESC )
            ( argAllocationLength : byte )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.RequestSense argDESC argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty ( uint32 argAllocationLength )
        }

    /// <summary>
    ///  Send TEST UNIT READY SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_TestUnitReady
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.TestUnitReady argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 0u
        }

    /// <summary>
    ///  Send FORMAT UNIT SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_FormatUnit
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 0u
        }

    /// <summary>
    ///  Send READ(6) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field. Block count.
    ///  If argTransferLength is 0, a transfer of 256 blocks is requested.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Read6
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argBlockSize : Blocksize )
            ( argTransferLength : BLKCNT8_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            if ( uint64 wBlockSizse ) * ( uint64 argTransferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or argTransferLength is too large." )
            let cdb = GenScsiCDB.Read6 argLBA argTransferLength argNACA LINK.F
            let edtl =
                if argTransferLength = blkcnt_me.zero8 then
                    wBlockSizse * 256u
                else
                    ( wBlockSizse * uint32 argTransferLength )
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty edtl
        }

    /// <summary>
    ///  Send READ(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field. Block count.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Read10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argBlockSize : Blocksize )
            ( argTransferLength : BLKCNT16_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            if ( uint64 wBlockSizse ) * ( uint64 argTransferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or argTransferLength is too large." )
            let cdb = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F argLBA 0uy argTransferLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty ( wBlockSizse * uint32 argTransferLength )
        }

    /// <summary>
    ///  Send READ(12) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field. Block count.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Read12
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argBlockSize : Blocksize )
            ( argTransferLength : BLKCNT32_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            if ( uint64 wBlockSizse ) * ( uint64 argTransferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or argTransferLength is too large." )
            let cdb = GenScsiCDB.Read12 0uy DPO.F FUA.F FUA_NV.F argLBA 0uy argTransferLength argNACA LINK.F
            let edlt = wBlockSizse * ( argTransferLength |> blkcnt_me.toUInt32 )
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty edlt
        }

    /// <summary>
    ///  Send READ(16) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field. Block count.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Read16
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT64_T )
            ( argBlockSize : Blocksize )
            ( argTransferLength : BLKCNT32_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            if ( uint64 wBlockSizse ) * ( uint64 argTransferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or argTransferLength is too large." )
            let cdb = GenScsiCDB.Read16 0uy DPO.F FUA.F FUA_NV.F argLBA 0uy argTransferLength argNACA LINK.F
            let edlt = wBlockSizse * ( argTransferLength |> blkcnt_me.toUInt32 )
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty edlt
        }

    /// <summary>
    ///  Send READ CAPACITY(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ReadCapacity10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ReadCapacity10 blkcnt_me.zero32 PMI.F argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 8u
        }

    /// <summary>
    ///  Send READ CAPACITY(16) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ReadCapacity16
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argAllocationLength : uint32 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ReadCapacity16 blkcnt_me.zero64 argAllocationLength PMI.F argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty argAllocationLength
        }

    /// <summary>
    ///  Send SYNCHRONIZE CACHE(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argNumberOfBlockes">
    ///   NUMBER OF BLOCKS field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_SynchronizeCache10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argNumberOfBlockes : BLKCNT16_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.SynchronizeCache10 SYNC_NV.F IMMED.F argLBA 0uy argNumberOfBlockes argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 0u
        }

    /// <summary>
    ///  Send SYNCHRONIZE CACHE(16) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argNumberOfBlockes">
    ///   NUMBER OF BLOCKS field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_SynchronizeCache16
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT64_T )
            ( argNumberOfBlockes : BLKCNT32_T )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.SynchronizeCache16 SYNC_NV.F IMMED.F argLBA 0uy argNumberOfBlockes argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty 0u
        }

    /// <summary>
    ///  Send WRITE(6) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argOutputData">
    ///  The output data that is send to the target.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Write6
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argBlockSize : Blocksize )
            ( argOutputData : PooledBuffer )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            if argOutputData.Length = 0 then
                raise <| TestException( "The Write(6) command cannot transfer 0 bytes of data." )
            let transferLength = argOutputData.uLength / wBlockSizse
            if transferLength * wBlockSizse <> argOutputData.uLength then
                raise <| TestException( "Data length is not a multiple of block length." )
            if transferLength > 256u then
                raise <| TestException( "Output data length is too long" )
            if ( uint64 wBlockSizse ) * ( uint64 transferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or output data is too large." )
            let btl =
                if transferLength = 256u then
                    blkcnt_me.zero8
                else
                    transferLength
                    |> byte
                    |> blkcnt_me.ofUInt8
            let cdb = GenScsiCDB.Write6 argLBA btl argNACA LINK.F
            return! this.SendSCSICommand att lun cdb argOutputData argOutputData.uLength
        }

    /// <summary>
    ///  Send WRITE(10) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argOutputData">
    ///  The output data that is send to the target.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Write10
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argBlockSize : Blocksize )
            ( argOutputData : PooledBuffer )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            let transferLength = argOutputData.uLength / wBlockSizse
            if transferLength * wBlockSizse <> argOutputData.uLength then
                raise <| TestException( "Data length is not a multiple of block length." )
            if transferLength > 0xFFFFu then
                raise <| TestException( "Output data length is too long" )
            if ( uint64 wBlockSizse ) * ( uint64 transferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or output data is too large." )
            let trBlockCnt =
                transferLength
                |> uint16
                |> blkcnt_me.ofUInt16
            let cdb = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F argLBA 0uy trBlockCnt argNACA LINK.F
            return! this.SendSCSICommand att lun cdb argOutputData argOutputData.uLength
        }

    /// <summary>
    ///  Send WRITE(12) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argOutputData">
    ///  The output data that is send to the target.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Write12
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT32_T )
            ( argBlockSize : Blocksize )
            ( argOutputData : PooledBuffer )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            let transferLength = argOutputData.uLength / wBlockSizse
            if transferLength * wBlockSizse <> argOutputData.uLength then
                raise <| TestException( "Data length is not a multiple of block length." )
            if ( uint64 wBlockSizse ) * ( uint64 transferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or output data is too large." )
            let trBlockCnt =
                transferLength
                |> blkcnt_me.ofUInt32
            let cdb = GenScsiCDB.Write12 0uy DPO.F FUA.F FUA_NV.F argLBA 0uy trBlockCnt argNACA LINK.F
            return! this.SendSCSICommand att lun cdb argOutputData argOutputData.uLength
        }

    /// <summary>
    ///  Send WRITE(16) SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argBlockSize">
    ///  media block size in bytes.
    /// </param>
    /// <param name="argOutputData">
    ///  The output data that is send to the target.
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_Write16
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argLBA : BLKCNT64_T )
            ( argBlockSize : Blocksize )
            ( argOutputData : PooledBuffer )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let wBlockSizse = Blocksize.toUInt32 argBlockSize
            let transferLength = argOutputData.uLength / wBlockSizse
            if transferLength * wBlockSizse <> argOutputData.uLength then
                raise <| TestException( "Data length is not a multiple of block length." )
            if ( uint64 wBlockSizse ) * ( uint64 transferLength ) >= 0x100000000UL then
                raise <| TestException( "argBlockSize or output data is too large." )
            let trBlockCnt =
                transferLength
                |> blkcnt_me.ofUInt32
            let cdb = GenScsiCDB.Write16 0uy DPO.F FUA.F FUA_NV.F argLBA 0uy trBlockCnt argNACA LINK.F
            return! this.SendSCSICommand att lun cdb argOutputData argOutputData.uLength
        }

    /// <summary>
    ///  Send REPORT SUPPORTED OPERATION CODES SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argReportingOptions">
    ///  REPORTING OPTIONS field
    /// </param>
    /// <param name="argRequestedOperationCode">
    ///  REQUESTED OPERATION CODE field
    /// </param>
    /// <param name="argRequestedServiceAction">
    ///  REQUESTED SERVICE ACTION field
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ReportSupportedOperationCodes
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argReportingOptions : byte )
            ( argRequestedOperationCode : byte )
            ( argRequestedServiceAction : uint16 )
            ( argAllocationLength : uint32 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ReportSupportedOperationCodes argReportingOptions argRequestedOperationCode argRequestedServiceAction argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty argAllocationLength
        }

    /// <summary>
    ///  Send REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS SCSI Command.
    /// </summary>
    /// <param name="att">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="lun">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Initiator task tag.
    /// </returns>
    member this.Send_ReportSupportedTaskManagementFunctions
            ( att : TaskATTRCd )
            ( lun : LUN_T )
            ( argAllocationLength : uint32 )
            ( argNACA : NACA ) : Task<ITT_T> =
        task {
            let cdb = GenScsiCDB.ReportSupportedTaskManagementFunctions argAllocationLength argNACA LINK.F
            return! this.SendSCSICommand att lun cdb PooledBuffer.Empty argAllocationLength
        }

    /// <summary>
    ///  Send Nop-Out PDU.
    /// </summary>
    member _.Send_NotOut() : Task =
        task {
            let! _ = m_ISCIInitiator.SendNOPOutPDU m_CID BitI.T lun_me.zero ( ttt_me.fromPrim 0xFFFFFFFFu ) PooledBuffer.Empty
            ()
        }

    /// <summary>
    ///  Waits for a response.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member _.WaitResponse ( itt : ITT_T ) : Task<TaskResult> =
        m_ReceiveWaiter.WaitAndReset itt

    /// <summary>
    ///  Wait for a response to the SCSI command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member _.WaitSCSIResponse ( itt : ITT_T ) : Task<TaskResult_ScsiCmd> =
        task {
            let! r = m_ReceiveWaiter.WaitAndReset itt
            let r2 =
                match r with
                | TaskResult.SCSI( x ) ->
                    x
                | _ ->
                    raise <| TestException( "Unexpected response." )
            return r2
        }

    /// <summary>
    ///  Wait for a response to the SCSI command with GOOG status.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse data.
    /// </returns>
    member _.WaitSCSIResponseGoogStatus ( itt : ITT_T ) : Task<PooledBuffer> =
        task {
            let! r = this.WaitSCSIResponse itt
            if r.Response <> iScsiSvcRespCd.COMMAND_COMPLETE then
                raise <| TestException( "Unexpected Response value." )
            if r.Status <> ScsiCmdStatCd.GOOD then
                raise <| TestException( "Unexpected Status value." )
            if r.Sense.IsSome then
                raise <| TestException( r.Sense.Value.DescString )
            return r.ResData
        }

    /// <summary>
    ///  Wait for a response to the TMF.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member _.WaitTMFResponse ( itt : ITT_T ) : Task<TaskMgrResCd> =
        task {
            let! r = m_ReceiveWaiter.WaitAndReset itt
            let r2 =
                match r with
                | TaskResult.TMF( x ) ->
                    x
                | _ ->
                    raise <| TestException( "Unexpected response." )
            return r2
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=0.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_Standerd ( itt : ITT_T ) : Task<StanderdInquiry> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_Standerd r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=1 and page code specifies Unit Serial Number VPD page.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_UnitSerialNumberVPD ( itt : ITT_T ) : Task<UnitSerialNumberVPD> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_UnitSerialNumberVPD r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=1 and page code specifies Device Identifier VPD page.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_DeviceIdentifierVPD ( itt : ITT_T ) : Task<DeviceIdentifierVPD> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_DeviceIdentifierVPD r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=1 and page code specifies Extended Inquiry Data VPD page.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_ExtendedInquiryDataVPD ( itt : ITT_T ) : Task<ExtendedInquiryDataVPD> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_ExtendedInquiryDataVPD r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=1 and page code specifies Block Limit VPD page.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_BlockLimitVPD ( itt : ITT_T ) : Task<BlockLimitVPD> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_BlockLimitVPD r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=1 and page code specifies Block Device Characteristics VPD page.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_BlockDeviceCharacteristicsVPD ( itt : ITT_T ) : Task<BlockDeviceCharacteristicsVPD> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_BlockDeviceCharacteristicsVPD r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the Inquiry command with EVPD=1 and page code specifies Supported VPD page.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_Inquiry_SupportedVPD ( itt : ITT_T ) : Task<SupportedVPD> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.Inquiry_SupportedVPD r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the MODE SENSE(6) command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ModeSense6 ( itt : ITT_T ) : Task<ModeParameter6> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ModeSense6 r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the MODE SENSE(10) command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ModeSense10 ( itt : ITT_T ) : Task<ModeParameter10> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ModeSense10 r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the PERSISTENT RESERVE IN command with the READ KEYS service action.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_PersistentReserveIn_ReadKey ( itt : ITT_T ) : Task<PR_ReadKey> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.PersistentReserveIn_ReadKey r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the PERSISTENT RESERVE IN command with the READ RESERVATION service action.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_PersistentReserveIn_ReadReservation ( itt : ITT_T ) : Task<PR_ReadReservation> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.PersistentReserveIn_ReadReservation r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the PERSISTENT RESERVE IN command with the REPORT CAPABILITIES service action.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_PersistentReserveIn_ReportCapabilities ( itt : ITT_T ) : Task<PR_ReportCapabilities> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.PersistentReserveIn_ReportCapabilities r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the PERSISTENT RESERVE IN command with the READ FULL STATUS service action.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_PersistentReserveIn_ReadFullStatus ( itt : ITT_T ) : Task<PR_ReadFullStatus> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.PersistentReserveIn_ReadFullStatus r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the REPORT LUNS command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ReportLUNs ( itt : ITT_T ) : Task<struct( uint32 * LUN_T[] )> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ReportLUNs r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the READ CAPACITY(10) command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ReadCapacity10 ( itt : ITT_T ) : Task<struct( uint32 * uint32 )> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ReadCapacity10 r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the READ CAPACITY(16) command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ReadCapacity16 ( itt : ITT_T ) : Task<ReadCapacity16Param> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ReadCapacity16 r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the REPORT SUPPORTED OPERATION CODES command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ReportSupportedOperationCodes_AllCommand ( itt : ITT_T ) : Task<SOCParam_All> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ReportSupportedOperationCodes_AllCommand r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the REPORT SUPPORTED OPERATION CODES command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ReportSupportedOperationCodes_OneCommand ( itt : ITT_T ) : Task<SOCParam_One> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ReportSupportedOperationCodes_OneCommand r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Wait for a response to the REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command.
    /// </summary>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <returns>
    ///  Received rasponse.
    /// </returns>
    member this.Wait_ReportSupportedTaskManagementFunctions ( itt : ITT_T ) : Task<RSTMFParam> =
        task {
            let! r = this.WaitSCSIResponseGoogStatus itt
            let rp = GenScsiParams.ReportSupportedTaskManagementFunctions r
            r.Return()
            return rp
        }

    /// <summary>
    ///  Close the session.
    /// </summary>
    member this.Close() : Task<unit> =
        task {
            do! this.StopReceiver()
            do! m_ISCIInitiator.CloseSession m_CID BitI.T
        }

    //=========================================================================
    // Private method

    /// <summary>
    ///  Perform PDU reception processing.
    /// </summary>
    member private this.Receiver() : Task =
        let loop() =
            task {
                try
                    let! pdu = m_ISCIInitiator.Receive m_CID
                    match pdu with
                    | :? SCSIResponsePDU as x ->
                        return! this.Receive_SCSIResponsePDU x

                    | :? TaskManagementFunctionResponsePDU as x ->
                        return! this.Receive_TaskManagementFunctionResponsePDU x

                    | :? SCSIDataInPDU as x ->
                        return! this.Receive_SCSIDataInPDU x

                    | :? R2TPDU as x ->
                        return! this.Receive_R2TPDU x

                    | :? NOPInPDU as x ->
                        if Volatile.Read &m_ExitFlg && ( PooledBuffer.ValueEqualsWithArray x.PingData [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;|] ) then
                            m_ReceiveWaiter.Notify( x.InitiatorTaskTag, TaskResult.NOP )
                            return false
                        else
                            return true

                    | _ ->
                        m_ReceiveWaiter.SetExceptionForAll ( SessionRecoveryException( "Unexpected PDU was received.", m_TSIH ) )
                        return false
                with
                | _ as ex ->
                    if not ( Volatile.Read &m_ExitFlg ) then
                        // Unexpected error
                        m_ReceiveWaiter.SetExceptionForAll ( SessionRecoveryException( ex.Message, m_TSIH ) )
                    return false
            }
        Functions.loopAsync loop

    /// <summary>
    ///  A SCSIResponse PDU was received.
    /// </summary>
    /// <param name="pdu">
    ///  Received PDU.
    /// </param>
    /// <returns>
    ///  Whether it ended normally or not.
    /// </returns>
    member private _.Receive_SCSIResponsePDU ( pdu : SCSIResponsePDU ) : Task<bool> =
        task {
            let itt = pdu.InitiatorTaskTag

            // Delete Data-out buffer data.
            let! edtl = m_RegistWaiter.WaitAndReset itt
            m_OutBuffer.Update ( fun old ->
                let r, v = old.TryGetValue itt
                if r then
                    v.Return()
                    old.Remove itt
                else
                    old
            )
            |> ignore

            // Delete Data-In buffer data.
            let dataInPdus =
                m_InBuffer.Update ( fun old ->
                    match old.TryGetValue itt with
                    | true, v ->
                        struct( old.Remove itt, v )
                    | _ ->
                        struct( old, [] )
                )

            let r = {
                Response = pdu.Response;
                Status = pdu.Status;
                ResData = 
                    if pdu.SenseData.Count <= 0 then 
                        SCSIDataInPDU.AppendDataInList pdu.ResponseData dataInPdus ( int edtl )
                    else
                        PooledBuffer.Empty;
                Sense = 
                    if pdu.SenseData.Count > 0 then
                        ParseSenseData.Parse( pdu.SenseData.ToArray() )
                    else
                        ValueNone;
                Residual = 
                    if pdu.O then
                        ResidualCount.Overflow( pdu.ResidualCount )
                    else
                        ResidualCount.Underflow( pdu.ResidualCount );
            }
            pdu.DataInBuffer.Return()
            m_ReceiveWaiter.Notify( itt, TaskResult.SCSI( r ) )
            return true
        }

    /// <summary>
    ///  A TaskManagementFunctionResponsePDU PDU was received.
    /// </summary>
    /// <param name="pdu">
    ///  Received PDU.
    /// </param>
    /// <returns>
    ///  Whether it ended normally or not.
    /// </returns>
    member private _.Receive_TaskManagementFunctionResponsePDU ( pdu : TaskManagementFunctionResponsePDU ) : Task<bool> =
        task {
            let itt = pdu.InitiatorTaskTag

            // Delete Data-out and Data-In buffer data.
            let! _ = m_RegistWaiter.WaitAndReset itt
            m_OutBuffer.Update ( fun old ->
                match old.TryGetValue itt with
                | true, v ->
                    v.Return()
                    old.Remove itt
                | _ ->
                    old
            )
            |> ignore
            m_InBuffer.Update ( fun old -> old.Remove itt )
            |> ignore

            m_ReceiveWaiter.Notify( itt, TaskResult.TMF( pdu.Response ) )
            return true
        }

    /// <summary>
    ///  A SCSIDataInPDU PDU was received.
    /// </summary>
    /// <param name="pdu">
    ///  Received PDU.
    /// </param>
    /// <returns>
    ///  Whether it ended normally or not.
    /// </returns>
    member private _.Receive_SCSIDataInPDU ( pdu : SCSIDataInPDU ) : Task<bool> =
        task {
            let itt = pdu.InitiatorTaskTag

            let! _ = m_RegistWaiter.Wait itt
            let r =
                m_InBuffer.Update ( fun old ->
                    match old.TryGetValue itt with
                    | true, v ->
                        let n1 = old.Remove itt
                        let n2 = n1.Add( itt, pdu :: v )
                        struct( n2, true )
                    | _ ->
                        struct( old, false )
                )
            if not r then
                // unexpected
                m_ReceiveWaiter.SetExceptionForAll ( SessionRecoveryException( "DataIn PDU with unexpected ITT was received.", m_TSIH ) )
                return false
            else
                return true
        }

    /// <summary>
    ///  A R2TPDU PDU was received.
    /// </summary>
    /// <param name="pdu">
    ///  Received PDU.
    /// </param>
    /// <returns>
    ///  Whether it ended normally or not.
    /// </returns>
    member private _.Receive_R2TPDU ( pdu : R2TPDU ) : Task<bool> =
        task {
            let mbl = m_ISCIInitiator.Params.MaxBurstLength
            let mrdsl = m_ISCIInitiator.Connection( m_CID ).Params.MaxRecvDataSegmentLength_T

            let! _ = m_RegistWaiter.Wait pdu.InitiatorTaskTag
            let r, v = m_OutBuffer.obj.TryGetValue pdu.InitiatorTaskTag
            if not r then
                // unexpected
                m_ReceiveWaiter.SetExceptionForAll ( SessionRecoveryException( "R2T PDU with unexpected ITT was received.", m_TSIH ) )
                return false
            else
                let segs =
                    Functions.DivideRespDataSegment pdu.BufferOffset pdu.DesiredDataTransferLength mbl mrdsl
                    |> List.indexed
                    |> List.map ( fun ( idx, struct( s, l , f ) ) -> struct( idx, s, l, f ) )

                for struct( idx, s, l, f ) in segs do
                    let sendData = PooledBuffer.Rent( v, int s, int l )
                    let datasn = datasn_me.fromPrim ( uint32 idx )
                    do! m_ISCIInitiator.SendSCSIDataOutPDU m_CID ( BitF.ofBool f ) pdu.InitiatorTaskTag pdu.LUN pdu.TargetTransferTag datasn s sendData
                    sendData.Return()

                return true

        }

