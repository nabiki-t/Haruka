//=============================================================================
// Haruka Software Storage.
// BlockDeviceTask.fs : Defines IBlockDeviceTask interface.
// Objects that implements IBlockDeviceTask interface represents tasks performed in BlockDeviceLU. 
// 

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System.Threading.Tasks
open System.Runtime.CompilerServices
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// Type values of BlockDeviceTask
[<Struct; IsReadOnly>]
type BlockDeviceTaskType =
    /// This task is SCSI task
    | ScsiTask
    /// This task is not SCSI task that used in BlockDeviceLU.
    | InternalTask

//=============================================================================
// Interface declaration

/// Definition of IBlockDeviceTask interface.
type IBlockDeviceTask =

    /// Return task type.
    abstract TaskType : BlockDeviceTaskType

    /// Return source information of this task.
    abstract Source : CommandSourceInfo
    
    /// Return  Initiator task tag.
    abstract InitiatorTaskTag : ITT_T

    /// Return SCSI Command object of this object.
    abstract SCSICommand : SCSICommandPDU

    /// Return CDB of this object
    abstract CDB : ICDB voption

    /// <summary>
    /// Execute this task
    /// </summary>
    /// <remarks>
    /// Notice that this method returns a function ( unit -> task ), not a task object.
    /// Returned procedure must be run in asynchronously.
    /// </remarks>
    abstract Execute : unit -> ( unit -> Task<unit> )

    /// Get task description string.
    abstract DescString : string

    /// Notify task terminate request
    /// There is not guarantee but abort this task quickly as soon as possible.
    abstract NotifyTerminate : bool -> unit

    /// Return ACANoncompliant flag value
    abstract ACANoncompliant : bool

    /// Release PooledBuffer
    abstract ReleasePooledBuffer : unit -> unit
