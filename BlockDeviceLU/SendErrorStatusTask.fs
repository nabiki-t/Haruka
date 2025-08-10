//=============================================================================
// Haruka Software Storage.
// SendErrorStatusTask.fs : Defines SendErrorStatusTask structure.
// SendErrorStatusTask imprements sending ACA active notification function.

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

[<NoComparison>]
type SendErrorStatusTask
    (
        m_StatusMaster : IStatus,
        m_Source : CommandSourceInfo,
        m_Command : SCSICommandPDU,
        m_LU : IInternalLU,
        dSense : bool,
        m_RespCode : iScsiSvcRespCd,
        m_StatCode : ScsiCmdStatCd,
        ?m_SenseData : SenseData
    ) =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Initiator task tag
    let m_ITT = m_Command.InitiatorTaskTag

    /// LUN
    let m_LUN = m_Command.LUN

    /// Terminate request flag.
    /// If this flag is true, this task must abort quickly.
    /// ( 0:response is not returned yet, 1:task is complete, 2:task is aborted)
    let mutable m_TerminateFlag = 0

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IBlockDeviceTask with

        /// Return task type.
        override _.TaskType : BlockDeviceTaskType =
            BlockDeviceTaskType.InternalTask

        /// Return source information of this task.
        override _.Source : CommandSourceInfo =
            m_Source
    
        /// Return  Initiator task tag.
        override _.InitiatorTaskTag : ITT_T =
            m_ITT

        /// Return SCSI Command object of this object.
        override _.SCSICommand : SCSICommandPDU =
            m_Command

        /// Return CDB of this object
        override _.CDB : ICDB voption =
            ValueNone

        /// Execute this SCSI task.
        override this.Execute() : unit -> Task<unit> =
            fun () -> task {
                let loginfo = struct ( m_ObjID, ValueSome( m_Source ), ValueSome( m_ITT ), ValueSome( m_LUN ) )
                try
                    let init = Interlocked.CompareExchange( &m_TerminateFlag, 1, 0 )
                    if init = 0 && m_TerminateFlag = 1 then
                        HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( loginfo, "Start to send error status data." ) )
                        // Send response data to the initiator
                        let senseData =
                            if box m_SenseData <> null && m_SenseData.IsSome then
                                ( m_SenseData.Value.GetSenseData dSense )
                            else
                                Array.empty
                        m_Source.ProtocolService.SendSCSIResponse
                            m_Command
                            m_Source.CID
                            m_Source.ConCounter
                            0u
                            m_RespCode
                            m_StatCode
                            ( PooledBuffer.Rent senseData )
                            PooledBuffer.Empty
                            0u
                            ResponseFenceNeedsFlag.W_Mode
                    else
                        HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( loginfo, "Already started to send error status data. Ignore request." ) )

                    // Notify end this task nomaly.
                    m_LU.NotifyTerminateTask ( this :> IBlockDeviceTask )
                with
                | _ as x ->
                    // Notify terminate this task with an exception.
                    HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( loginfo, sprintf "An exeption raised. Message=%s" x.Message ) )
                    // 論理ユニットリセットに倒さなければならない
                    m_LU.NotifyTerminateTaskWithException ( this :> IBlockDeviceTask ) x
            }

        // Get task description string.
        override _.DescString : string =
            let RespCodeStr = Constants.getiScsiSvcRespNameFromValue m_RespCode
            let StatCodeStr = Constants.getScsiCmdStatNameFromValue m_StatCode
            let SenseDataStr = if box m_SenseData <> null && m_SenseData.IsSome then m_SenseData.Value.DescString else ""
            "SendErrorStatusTask internal task. RespCode=" + RespCodeStr + ", StatCode=" + StatCodeStr + ", SenseData=" + SenseDataStr


        /// <summary>
        ///   Notify task terminate request
        /// </summary>
        /// <param name="needResp">
        ///   If task is terminated from the other I_T Nexus, set true to this value.
        /// </param>
        override _.NotifyTerminate( needResp : bool ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( m_Source ), ValueSome( m_ITT ), ValueSome( m_LUN ) )
            let init = Interlocked.CompareExchange( &m_TerminateFlag, 2, 0 )
            if init = 0 && m_TerminateFlag = 2 then
                // If this task is aborted by the other I_T Nexus, returns TASK ABORTED response.
                if needResp then
                    m_Source.ProtocolService.SendSCSIResponse
                        m_Command
                        m_Source.CID
                        m_Source.ConCounter
                        0u
                        iScsiSvcRespCd.COMMAND_COMPLETE
                        ScsiCmdStatCd.TASK_ABORTED
                        PooledBuffer.Empty
                        PooledBuffer.Empty
                        0u
                        ResponseFenceNeedsFlag.R_Mode
            else
                // Response is already returned.
                HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( loginfo, "Already started to send error status data. Ignore request." ) )

        /// Return ACANoncompliant flag value
        override _.ACANoncompliant : bool = true

        /// Release PooledBuffer
        override _.ReleasePooledBuffer() =
            // The buffer will be released by the SCSI task that caused the error, so nothing is done here.
            ()

