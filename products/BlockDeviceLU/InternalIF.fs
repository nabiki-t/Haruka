//=============================================================================
// Haruka Software Storage.
// InternalIF.fs : Defines internal interface, used in BlockDeviceLU module.

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Interface declaration

/// <summary>
///   Interface of BlockDeviceLU class, that used in BlockDeviceLU module only.
/// </summary>
type IInternalLU =

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get ILU interface that overrided from current BlockDeviceLU object.
    /// </summary>
    abstract LUInterface : ILU

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get peripheral media object interface.
    /// </summary>
    abstract Media : IMedia

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Get unit attention that established to specified initiator port.
    /// </summary>
    /// <param name="nexus">
    ///  IT_Nexus
    /// </param>
    /// <returns>
    ///  Sense data that established at unit attention to specified initiator port.
    ///  Or None, if it is not established unit attention.
    /// </returns>
    abstract GetUnitAttention : nexus : ITNexus -> SCSIACAException voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Clear unit attention that established to specified initiator port.
    /// </summary>
    /// <param name="nexus">
    ///  IT_Nexus
    /// </param>
    abstract ClearUnitAttention : nexus : ITNexus -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Establish new unit attention for specified initiator port.
    /// </summary>
    /// <param name="iport">
    ///  Initiator port name which unit attention should be established.
    /// </param>
    /// <param name="ex">
    ///  Unit attention.
    /// </param>
    abstract EstablishUnitAttention : iport:string -> ex:SCSIACAException -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Get LUN value.
    /// </summary>
    abstract LUN : LUN_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Get LUN value.
    /// </summary>
    abstract OptimalTransferLength : BLKCNT32_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notify that task is terminated nomaly.
    /// </summary>
    /// <param name="argTask">
    ///  Terminated task.
    /// </param>
    abstract NotifyTerminateTask : argTask:IBlockDeviceTask -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notify that task is terminated with exception.
    /// </summary>
    /// <param name="argTask">
    ///  Terminated task.
    /// </param>
    /// <param name="ex">
    ///  ACA exception.
    /// </param>
    abstract NotifyTerminateTaskWithException : argTask:IBlockDeviceTask -> ex:Exception -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Abort tasks from specified I_T Nesus.
    /// </summary>
    /// <param name="self">
    ///  Task that occurs this request.
    /// </param>
    /// <param name="itn">
    ///  The source I_T nexus of the task to abort.
    /// </param>
    /// <param name="abortAllACATask">
    ///  Whether to terminate all ACA tasks.
    /// </param>
    abstract AbortTasksFromSpecifiedITNexus : self:IBlockDeviceTask -> itn:ITNexus[] -> abortAllACATask:bool -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notification of number of bytes read to calculate usage statistics.
    /// </summary>
    /// <param name="d">
    ///  time
    /// </param>
    /// <param name="cnt">
    ///  Bytes count.
    /// </param>
    abstract NotifyReadBytesCount : d:DateTime -> cnt:int64 -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notification of number of written to calculate usage statistics.
    /// </summary>
    /// <param name="d">
    ///  time
    /// </param>
    /// <param name="cnt">
    ///  Bytes count.
    /// </param>
    abstract NotifyWrittenBytesCount : d:DateTime -> cnt:int64 -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notification of tick count for read to calculate usage statistics.
    /// </summary>
    /// <param name="d">
    ///  time
    /// </param>
    /// <param name="tc">
    ///  Tick count.
    /// </param>
    abstract NotifyReadTickCount : d:DateTime -> tc:int64 -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notification of tick count for write to calculate usage statistics.
    /// </summary>
    /// <param name="d">
    ///  time
    /// </param>
    /// <param name="tc">
    ///  Tick count.
    /// </param>
    abstract NotifyWriteTickCount : d:DateTime -> tc:int64 -> unit
