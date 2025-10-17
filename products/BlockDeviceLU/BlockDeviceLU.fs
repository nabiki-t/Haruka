//=============================================================================
// Haruka Software Storage.
// BlockDeviceLU.fs : Defines BlockDeviceLU class.
// BlockDeviceLU class implement SBC-2 compliant direct access block device
// functionality.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.Immutable
open System.Diagnostics

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// SCSI Task status value definition.
[<NoComparison>]
type TaskStatus =
    /// Dormant(S0 or S1)
    | TASK_STAT_Dormant of IBlockDeviceTask

    /// Running( equals S2:Enabled state )
    | TASK_STAT_Running of IBlockDeviceTask

    static member getTask ( arg : TaskStatus ) =
        match arg with
        | TASK_STAT_Dormant( x )
        | TASK_STAT_Running( x ) ->
            x

/// BlockDevice type
[<Struct>]
type BlockDeviceType =
    /// Normal block device LU
    | BDT_Normal
    /// Dummy device for REPORT LUNS well known LU
    | BDT_Dummy

[<NoComparison>]
type TaskSet = {
    /// Task queue of task set.
    Queue : ImmutableArray< TaskStatus >;

    /// ACA status
    ACA : ( ITNexus * SCSIACAException ) voption;
}

//=============================================================================
// Class implementation

/// <summary>
///  Implementing Block device LU functionality
/// </summary>
/// <param name="m_DeviceType">
///  Specify the type of this LU.
/// </param>
/// <param name="m_StatusMaster">
///  Reference to the Status Master instance..
/// </param>
/// <param name="m_LUN">
///  LUN allocated to this LU.
/// </param>
/// <param name="m_LogicalUnitInfo">
///  Configuration information.
/// </param>
/// <param name="m_WorkDirPath">
///  Working folder path name.
/// </param>
/// <param name="m_TargetGroupKiller">
///  Killer object.
/// </param>
type BlockDeviceLU
    (
        m_DeviceType : BlockDeviceType,
        m_StatusMaster : IStatus,
        m_LUN : LUN_T,
        m_LogicalUnitInfo : TargetGroupConf.T_BlockDevice,
        m_WorkDirPath : string,
        m_TargetGroupKiller : IKiller
    ) as this =

    /// Semaphore that gurds the task set.
    let m_TaskSetQueue = new LambdaQueue( 1u )
    let m_ExecuteQueue = new TaskQueue( 4u )

    /// the Task set.
    let mutable m_TaskSet = {
        Queue = ImmutableArray< TaskStatus >.Empty;
        ACA = ValueNone;
    }

    /// Unit Attention condition(Key is the Initiator port name)
    let m_UnitAttention = new ConcurrentDictionary< string, SCSIACAException >()

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Media object that is accessed at this LU
    let m_Media =
        m_StatusMaster.CreateMedia
            m_LogicalUnitInfo.Peripheral
            m_LUN
            m_TargetGroupKiller

    /// Mode parameter values.
    let m_ModeParameter = new ModeParameter( m_Media, m_LUN )

    // Persistent Reservation save file name
    let m_PRSaveFileName =
        match m_DeviceType with
        | BDT_Normal ->
            Functions.AppendPathName m_WorkDirPath Constants.PR_SAVE_FILE_NAME
        | BDT_Dummy -> ""

    /// Persistent Reservation manager
    let m_PRManager =
        new PRManager(
            m_StatusMaster,
            this :> IInternalLU,
            m_LUN,
            m_PRSaveFileName,
            m_TargetGroupKiller
        )

    // Logical Unit Reset flag.
    // Default is false. If logical unit reset is started, this flag is set to true.
    let mutable  m_LUResetFlag = false

    /// Resource counter for read data
    let m_ReadBytesCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for written data
    let m_WrittenBytesCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for read response time
    let m_ReadTickCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for write response time
    let m_WriteTickCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )


    do
        m_TargetGroupKiller.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, ValueNone, ValueNone, ValueSome m_LUN, "BlockDeviceLU", "" ) )

    //=========================================================================
    // Interface method

    interface ILU with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            m_TaskSetQueue.Stop()
            m_ExecuteQueue.Stop()

        // ------------------------------------------------------------------------
        // ABORT TASK task management function request.
        override this.AbortTask ( source : CommandSourceInfo ) ( initiatorTaskTag:ITT_T ) ( referencedTaskTag:ITT_T ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "BlockDeviceLU.AbortTask." ) )
            HLogger.Trace( LogID.I_TMF_REQUESTED, fun g ->
                let msg = sprintf "AbortTask( ReferencedTaskTag=0x%08X )" ( itt_me.toPrim referencedTaskTag ) 
                g.Gen1( loginfo, msg )
            )

            if Volatile.Read( &m_LUResetFlag ) then
                // Request was ignored in LUReset.
                HLogger.Trace( LogID.I_IGNORED_REQ_IN_LURESET, fun g -> g.Gen1( loginfo, "BlockDeviceLU.AbortTask." ) )
            else
                m_TaskSetQueue.Enqueue( fun () ->
                    try
                        // Remove the tasks that will be terminated from the task queue.
                        let oldTaskSet = m_TaskSet
                        let builder = ImmutableArray.CreateBuilder< TaskStatus >()
                        builder.Capacity <- oldTaskSet.Queue.Length
                        for itr in oldTaskSet.Queue do
                            let t = TaskStatus.getTask itr
                            if t.TaskType = BlockDeviceTaskType.ScsiTask && t.InitiatorTaskTag = referencedTaskTag then
                                // Notify removed tasks to termination
                                HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, referencedTaskTag, t.DescString ) )
                                t.NotifyTerminate false
                            else
                                builder.Add itr

                        // Replace the contents of the task queue
                        m_TaskSet <- {
                            oldTaskSet with
                                Queue = builder.DrainToImmutable()
                        }

                        // return response
                        source.ProtocolService.SendOtherResponse source.CID source.ConCounter {
                            Response = TaskMgrResCd.FUNCTION_COMPLETE;
                            InitiatorTaskTag = initiatorTaskTag;
                            StatSN = statsn_me.zero;
                            ExpCmdSN = cmdsn_me.zero;
                            MaxCmdSN = cmdsn_me.zero;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                        } m_LUN

                    with
                    | _ as x ->
                        // When an unknown error occurs and an LU reset is attempted, 
                        // the task may not be notified of completion reliably.
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                        this.NotifyLUReset source initiatorTaskTag
                )

        /// ABORT TASK SET task management function request.
        override this.AbortTaskSet ( source : CommandSourceInfo ) ( initiatorTaskTag:ITT_T ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "BlockDeviceLU.AbortTaskSet." ) )
            HLogger.Trace( LogID.I_TMF_REQUESTED, fun g -> g.Gen1( loginfo, "AbortTaskSet()" ) )

            if Volatile.Read( &m_LUResetFlag ) then
                // Request was ignored in LUReset.
                HLogger.Trace( LogID.I_IGNORED_REQ_IN_LURESET, fun g -> g.Gen1( loginfo, "BlockDeviceLU.AbortTaskSet." ) )
            else
                m_TaskSetQueue.Enqueue( fun () ->
                    try
                        // Remove the tasks that will be terminated from the task queue.
                        let oldTaskSet = m_TaskSet
                        let builder = ImmutableArray.CreateBuilder< TaskStatus >()
                        builder.Capacity <- oldTaskSet.Queue.Length
                        for itr in oldTaskSet.Queue do
                            let t = TaskStatus.getTask itr
                            if t.TaskType = BlockDeviceTaskType.ScsiTask && ITNexus.Equals( t.Source.I_TNexus, source.I_TNexus ) then
                                // Notify removed tasks to termination
                                HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, t.InitiatorTaskTag, t.DescString ) )
                                t.NotifyTerminate false
                            else
                                builder.Add itr

                        // Replace the contents of the task queue
                        m_TaskSet <- {
                            oldTaskSet with
                                Queue = builder.DrainToImmutable()
                        }

                        // return response
                        source.ProtocolService.SendOtherResponse source.CID source.ConCounter {
                            Response = TaskMgrResCd.FUNCTION_COMPLETE;
                            InitiatorTaskTag = initiatorTaskTag;
                            StatSN = statsn_me.zero;
                            ExpCmdSN = cmdsn_me.zero;
                            MaxCmdSN = cmdsn_me.zero;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                        } m_LUN

                    with
                    | _ as x ->
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                        this.NotifyLUReset source initiatorTaskTag
                )

        // ------------------------------------------------------------------------
        // CLEAR ACA task management function request.
        override this.ClearACA ( source : CommandSourceInfo ) ( initiatorTaskTag:ITT_T ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "BlockDeviceLU.ClearACA." ) )
            HLogger.Trace( LogID.I_TMF_REQUESTED, fun g -> g.Gen1( loginfo, "ClearACA()" ) )

            if Volatile.Read( &m_LUResetFlag ) then
                // Request was ignored in LUReset.
                HLogger.Trace( LogID.I_IGNORED_REQ_IN_LURESET, fun g -> g.Gen1( loginfo, "BlockDeviceLU.ClearACA." ) )
            else
                m_TaskSetQueue.Enqueue( fun () ->
                    try
                        // Remove ACA tasks from the task queue.
                        let oldTaskSet = m_TaskSet
                        let builder = ImmutableArray.CreateBuilder< TaskStatus >()
                        builder.Capacity <- oldTaskSet.Queue.Length
                        for itr in oldTaskSet.Queue do
                            let t = TaskStatus.getTask itr
                            if t.TaskType = BlockDeviceTaskType.ScsiTask && ITNexus.Equals( t.Source.I_TNexus, source.I_TNexus ) && t.SCSICommand.ATTR = TaskATTRCd.ACA_TASK then
                                // Notify removed tasks to termination
                                HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, t.InitiatorTaskTag, t.DescString ) )
                                t.NotifyTerminate false
                            else
                                builder.Add itr

                        // Replace the contents of the task queue
                        m_TaskSet <- {
                            Queue = builder.DrainToImmutable()
                            ACA =
                                // Clear ACA status
                                match oldTaskSet.ACA with
                                | ValueSome( itn, _ ) when ITNexus.Equals( itn, source.I_TNexus ) ->
                                    ValueNone
                                | _ ->
                                    oldTaskSet.ACA
                        }

                        // return response
                        source.ProtocolService.SendOtherResponse source.CID source.ConCounter {
                            Response = TaskMgrResCd.FUNCTION_COMPLETE;
                            InitiatorTaskTag = initiatorTaskTag;
                            StatSN = statsn_me.zero;
                            ExpCmdSN = cmdsn_me.zero;
                            MaxCmdSN = cmdsn_me.zero;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                        } m_LUN

                    with
                    | _ as x ->
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                        this.NotifyLUReset source initiatorTaskTag
                )

        // ------------------------------------------------------------------------
        // CLEAR TASK SET task management function request.
        override this.ClearTaskSet ( source : CommandSourceInfo ) ( initiatorTaskTag:ITT_T ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "BlockDeviceLU.ClearTaskSet." ) )
            HLogger.Trace( LogID.I_TMF_REQUESTED, fun g -> g.Gen1( loginfo, "ClearTaskSet()" ) )

            if Volatile.Read( &m_LUResetFlag ) then
                // Request was ignored in LUReset.
                HLogger.Trace( LogID.I_IGNORED_REQ_IN_LURESET, fun g -> g.Gen1( loginfo, "BlockDeviceLU.ClearTaskSet." ) )
            else
                m_TaskSetQueue.Enqueue( fun () ->
                    try
                        let oldTaskSet = m_TaskSet

                        // Terminate all of task in the task queue
                        for itr in oldTaskSet.Queue do
                            let t = TaskStatus.getTask itr
                            HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, t.InitiatorTaskTag, t.DescString ) )
                            t.NotifyTerminate ( ITNexus.Equals( t.Source.I_TNexus, source.I_TNexus ) |> not )

                        // Remove all of tasks from the task queue.
                        m_TaskSet <- {
                            oldTaskSet with
                                Queue = ImmutableArray< TaskStatus >.Empty
                        }

                        // return response
                        source.ProtocolService.SendOtherResponse source.CID source.ConCounter {
                            Response = TaskMgrResCd.FUNCTION_COMPLETE;
                            InitiatorTaskTag = initiatorTaskTag;
                            StatSN = statsn_me.zero;
                            ExpCmdSN = cmdsn_me.zero;
                            MaxCmdSN = cmdsn_me.zero;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                        } m_LUN
                    with
                    | _ as x ->
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                        this.NotifyLUReset source initiatorTaskTag
                )

        // ------------------------------------------------------------------------
        // LOGICAL UNIT RESET task management function request.
        override this.LogicalUnitReset ( source : CommandSourceInfo voption ) ( initiatorTaskTag : ITT_T voption ) : unit =
            let loginfo = struct ( m_ObjID, source, initiatorTaskTag, ValueSome( m_LUN ) )
            let fromI = source.IsSome && initiatorTaskTag.IsSome
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "BlockDeviceLU.LogicalUnitReset." ) )
            HLogger.Trace( LogID.I_TMF_REQUESTED, fun g -> g.Gen1( loginfo, "LogicalUnitReset()" ) )

            if Volatile.Read( &m_LUResetFlag ) then
                // Request was ignored in LUReset.
                HLogger.Trace( LogID.I_IGNORED_REQ_IN_LURESET, fun g -> g.Gen1( loginfo, "BlockDeviceLU.LogicalUnitReset." ) )
            else
                // Set LUResetFlag to true, and start logical unit reset.
                Volatile.Write( &m_LUResetFlag, true )

                m_TaskSetQueue.Enqueue( fun () ->

                    try
                        let oldTaskSet = m_TaskSet

                        // If a reset is requested for the dummy device LU (RAID controller), the entire Target Device is rebooted.
                        if m_DeviceType = BlockDeviceType.BDT_Dummy then
                            HLogger.Trace( LogID.F_LURESET_REQ_TO_DUMMY_LU, fun g -> g.Gen0( loginfo ) )
                            exit 1

                        // Terminate all of task in the task queue
                        for itr in oldTaskSet.Queue do
                            let t = TaskStatus.getTask itr
                            HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, t.InitiatorTaskTag, t.DescString ) )
                            let f = ( not fromI ) || ( ITNexus.Equals( t.Source.I_TNexus, source.Value.I_TNexus ) |> not )
                            t.NotifyTerminate f

                        m_TaskSet <- {
                            // Remove all of tasks from the task queue.
                            Queue = ImmutableArray< TaskStatus >.Empty;
                            // Clear ACA state.
                            ACA = ValueNone;
                        }

                        // Notify LU reset to the Media object
                        try
                            HLogger.Trace( LogID.I_NOTIFY_LURESET_TO_MEDIA, fun g -> g.Gen2( loginfo, m_Media.MediaIndex, m_Media.DescriptString ) )
                            m_Media.NotifyLUReset initiatorTaskTag source
                        with
                        | e ->
                            // ignore all of exceptions
                            HLogger.IgnoreException( fun g -> g.GenExp( loginfo, e ) )


                        // return response
                        if fromI then
                            source.Value.ProtocolService.SendOtherResponse source.Value.CID source.Value.ConCounter {
                                Response = TaskMgrResCd.FUNCTION_COMPLETE;
                                InitiatorTaskTag = initiatorTaskTag.Value;
                                StatSN = statsn_me.zero;
                                ExpCmdSN = cmdsn_me.zero;
                                MaxCmdSN = cmdsn_me.zero;
                                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                            } m_LUN

                        // Notify LU reset to StatusMaster object.
                        m_StatusMaster.NotifyLUReset ( m_LUN ) ( this :> ILU )

                    with
                    | _ as x ->
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                        // Notify LU reset to the Media object
                        try
                            HLogger.Trace( LogID.I_NOTIFY_LURESET_TO_MEDIA, fun g -> g.Gen2( loginfo, m_Media.MediaIndex, m_Media.DescriptString ) )
                            m_Media.NotifyLUReset initiatorTaskTag source
                        with
                        | e ->  // ignore all of exceptions
                            HLogger.IgnoreException( fun g -> g.GenExp( loginfo, e ) )

                        // Notify LU reset to StatusMaster object.
                        m_StatusMaster.NotifyLUReset ( m_LUN ) ( this :> ILU )
                )

        // ------------------------------------------------------------------------
        // SCSI Command request.
        override this.SCSICommand ( source : CommandSourceInfo ) ( command:SCSICommandPDU ) ( data:SCSIDataOutPDU list ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "BlockDeviceLU.SCSICommand." ) )

            if Volatile.Read( &m_LUResetFlag ) then
                // Request was ignored in LUReset.
                HLogger.Trace( LogID.I_IGNORED_REQ_IN_LURESET, fun g -> g.Gen1( loginfo, "BlockDeviceLU.SCSICommand." ) )
            else
                m_TaskSetQueue.Enqueue( fun () ->
                    // If the value of the NACA bit cannot be determined, consider CA.
                    let mutable errNACAval : bool = false
                    try
                        // Create new SCSI task object
                        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB source m_ObjID command
                        errNACAval <- cdb.NACA

                        m_TaskSet <-
                            m_TaskSet
                            |> this.AddNewScsiTaskToQueue source command cdb data
                            |> this.StartExecutableSCSITasks
                    with
                    | :? SCSIACAException as x ->
                        // ACA established
                        try
                            m_TaskSet <- 
                                m_TaskSet
                                |> this.EstablishNewACAStatus source x command errNACAval BlockDeviceTaskType.ScsiTask
                                |> this.StartExecutableSCSITasks 
                        with
                        | _ as x2 ->
                            // If the exception is duplicated, perform logical unit reset.
                            HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x2 ) )
                            this.NotifyLUReset source command.InitiatorTaskTag

                    | _ as x ->
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                        // Perform Logical Unit Reset
                        this.NotifyLUReset source command.InitiatorTaskTag
                )

        // ------------------------------------------------------------------------
        // Get Logical Unit Reset status flag value.
        override _.LUResetStatus with get() =
            Volatile.Read &m_LUResetFlag

        // ------------------------------------------------------------------------
        // Obtain the total number of read bytes.
        override _.GetReadBytesCount() : ResCountResult [] =
            m_ReadBytesCounter.Get DateTime.UtcNow

        // ------------------------------------------------------------------------
        // Obtain the total number of written bytes.
        override _.GetWrittenBytesCount() : ResCountResult [] =
            m_WrittenBytesCounter.Get DateTime.UtcNow

        // ------------------------------------------------------------------------
        // Obtain the tick count of read operation.
        override _.GetReadTickCount() : ResCountResult [] =
            // Tick ​​counts are calculated in Stopwatch.Frequency units, so they are converted to milliseconds.
            m_ReadTickCounter.Get DateTime.UtcNow
            |> Array.map ( fun itr -> {
                itr with
                    Value = itr.Value / ( Stopwatch.Frequency / 1000L )
            })

        // ------------------------------------------------------------------------
        // Obtain the tick count of write operation.
        override _.GetWriteTickCount() : ResCountResult [] =
            // Tick ​​counts are calculated in Stopwatch.Frequency units, so they are converted to milliseconds.
            m_WriteTickCounter.Get DateTime.UtcNow
            |> Array.map ( fun itr -> {
                itr with
                    Value = itr.Value / ( Stopwatch.Frequency / 1000L )
            })

        // ------------------------------------------------------------------------
        // Obtain current ACA status.
        override _.ACAStatus : struct ( ITNexus * ScsiCmdStatCd * SenseKeyCd * ASCCd * bool ) voption =
            let oldTskset = m_TaskSet
            if oldTskset.ACA.IsNone then
                ValueNone
            else
                let it, e = oldTskset.ACA.Value
                ValueSome struct( it, e.Status, e.SenseKey, e.ASC, e.IsCurrent )

        // ------------------------------------------------------------------------
        // Get media object.
        override _.GetMedia() : IMedia =
            m_Media

        // ------------------------------------------------------------------------
        //   Get used count of the task queue.
        override _.GetTaskQueueUsage ( tsih : TSIH_T ) : int =
            let q = m_TaskSet.Queue
            let rec loop ( cnt : int ) ( sum : int ) =
                if cnt < q.Length then
                    let t = TaskStatus.getTask q.[ cnt ]
                    let nsum =
                        if t.Source.TSIH = tsih then
                            sum + 1
                        else
                            sum
                    loop ( cnt + 1 ) nsum
                else
                    sum
            loop 0 0

    interface IInternalLU with
        
        // ------------------------------------------------------------------------
        //   Get ILU interface that overrided from current BlockDeviceLU object.
        override this.LUInterface : ILU =
            this :> ILU

        // ------------------------------------------------------------------------
        //   Get peripheral media object interface.
        override _.Media : IMedia = m_Media

        // ------------------------------------------------------------------------
        //  Get unit attention that established to specified I_T_Nexus.
        override _.GetUnitAttention ( nexus : ITNexus ) : SCSIACAException voption =
            let r, ex = m_UnitAttention.TryGetValue( nexus.InitiatorPortName )
            if r then
                ValueSome ex
            else
                ValueNone

        // ------------------------------------------------------------------------
        //  Clear unit attention that established to specified IT_Nexus.
        override _.ClearUnitAttention ( nexus : ITNexus ) : unit =
            m_UnitAttention.Remove( nexus.InitiatorPortName ) |> ignore
        
        // ------------------------------------------------------------------------
        // Establish new unit attention for specified initiator port.
        override _.EstablishUnitAttention ( iport : string ) ( ex : SCSIACAException ) : unit =
            let loginfo = struct ( m_ObjID, ValueNone, ValueNone, ValueSome m_LUN )
            HLogger.Trace( LogID.I_UA_ESTABLISHED, fun g -> g.Gen2( loginfo, iport, ex.Message ) )
            m_UnitAttention.AddOrUpdate(
                iport,
                ex,
                ( fun _ _ -> ex )
            ) |> ignore

        // ------------------------------------------------------------------------
        //  Get LUN value.
        override _.LUN = m_LUN

        // ------------------------------------------------------------------------
        //  Notify that task is terminated nomaly.
        override this.NotifyTerminateTask ( argTask : IBlockDeviceTask ) : unit =
            m_TaskSetQueue.Enqueue( fun () ->
                let loginfo = struct ( m_ObjID, ValueSome argTask.Source, ValueSome( argTask.InitiatorTaskTag ), ValueSome( m_LUN ) )
                HLogger.Trace( LogID.V_SCSI_TASK_TERMINATED, fun g -> g.Gen1( loginfo, argTask.DescString ) )

                try
                    m_TaskSet <-
                        m_TaskSet
                        |> this.DeleteTask argTask
                        |> this.StartExecutableSCSITasks
                    argTask.ReleasePooledBuffer()
                with
                | _ as x ->
                    // No errors are expected here.
                    HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                    this.NotifyLUReset argTask.Source argTask.InitiatorTaskTag
            )

        // ------------------------------------------------------------------------
        //  Notify that task is terminated with exception.
        override this.NotifyTerminateTaskWithException ( argTask : IBlockDeviceTask ) ( ex : Exception ) : unit =
            m_TaskSetQueue.Enqueue( fun () ->
                let loginfo = struct ( m_ObjID, ValueSome argTask.Source, ValueSome( argTask.InitiatorTaskTag ), ValueSome( m_LUN ) )
                HLogger.Trace( LogID.W_SCSI_TASK_TERMINATED_WITH_EXP, fun g -> g.Gen1( loginfo, argTask.DescString ) )
                match ex with
                | :? SCSIACAException as x ->
                    try
                        let oldTaskSet = m_TaskSet
                        let isExist =
                            oldTaskSet.Queue
                            |> Seq.tryFind ( fun itr -> Object.ReferenceEquals( TaskStatus.getTask itr, argTask ) )
                            |> Option.isSome
                        if not isExist then
                            // Ignore this notification.
                            // This may occur if an error is reported late from an old task after an LU reset.
                            ()
                        else
                            let wcdb = argTask.CDB  // argTask must not be SendErrorStatusTask
                            m_TaskSet <-
                                oldTaskSet
                                |> this.DeleteTask argTask
                                |> this.EstablishNewACAStatus argTask.Source x argTask.SCSICommand wcdb.Value.NACA argTask.TaskType
                                |> this.StartExecutableSCSITasks
                            argTask.ReleasePooledBuffer()
                    with
                    | _ as x2 ->
                        // If the exception is duplicated, perform logical unit reset.
                        HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x2 ) )
                        this.NotifyLUReset argTask.Source argTask.InitiatorTaskTag
                | _ as x ->
                    // If unexpected exception is raised, perform logical unit reset.
                    HLogger.UnexpectedException( fun g -> g.GenExp( loginfo, x ) )
                    this.NotifyLUReset argTask.Source argTask.InitiatorTaskTag
            )


        // ------------------------------------------------------------------------
        //  Abort tasks from specified I_T Nesus.
        override _.AbortTasksFromSpecifiedITNexus ( self : IBlockDeviceTask ) ( itn : ITNexus[] ) ( abortAllACATask : bool ) : unit =
            // ****************************************************************
            // This method is called in critical section of BlockDeviceLU task set lock.
            // ****************************************************************

            let loginfo = struct ( m_ObjID, ValueSome( self.Source ), ValueSome( self.InitiatorTaskTag ), ValueSome( m_LUN ) )
            let oldTaskSet = m_TaskSet
            let builder = ImmutableArray.CreateBuilder< TaskStatus >()
            builder.Capacity <- oldTaskSet.Queue.Length

            for itr in oldTaskSet.Queue do
                let t = TaskStatus.getTask itr
                let wj = itn |> Array.exists ( fun itr -> ITNexus.Equals( itr, t.Source.I_TNexus ) )
                if ( Object.ReferenceEquals( t, self ) |> not ) && ( t.TaskType = BlockDeviceTaskType.ScsiTask ) &&
                    ( ( abortAllACATask && t.SCSICommand.ATTR = TaskATTRCd.ACA_TASK ) || wj ) then
                        // Notify removed tasks to termination
                        // this process must be performed synchronously
                        HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, t.InitiatorTaskTag, t.DescString ) )
                        t.NotifyTerminate false
                else
                    builder.Add itr

            // Replace the contents of the task queue
            m_TaskSet <- {
                oldTaskSet with
                    Queue = builder.DrainToImmutable()
            }

        // ------------------------------------------------------------------------
        //  Notification of number of bytes read to calculate usage statistics.
        override _.NotifyReadBytesCount ( d : DateTime ) ( cnt : int64 ) : unit =
            m_ReadBytesCounter.AddCount d cnt

        // ------------------------------------------------------------------------
        //  Notification of number of written to calculate usage statistics.
        override _.NotifyWrittenBytesCount ( d : DateTime ) ( cnt : int64 ) : unit =
            m_WrittenBytesCounter.AddCount d cnt

        // ------------------------------------------------------------------------
        //  Notification of tick count for read to calculate usage statistics.
        //  tc must be Stopwatch.ElapsedTicks value.
        override _.NotifyReadTickCount ( d : DateTime ) ( tc : int64 ) : unit =
            m_ReadTickCounter.AddCount d tc

        // ------------------------------------------------------------------------
        //  Notification of tick count for write to calculate usage statistics.
        //  tc must be Stopwatch.ElapsedTicks value.
        override _.NotifyWriteTickCount ( d : DateTime ) ( tc : int64 ) : unit =
            m_WriteTickCounter.AddCount d tc

    //=========================================================================
    // Private method

    /// <summary>
    ///  Search task queue by Initiator Task Tag.
    /// </summary>
    /// <param name="q">
    ///  SCSI task queue vector.
    /// </param>
    /// <param name="itt">
    ///  Initiator Task Tag
    /// </param>
    /// <returns>
    ///  If a SCSI task that has initiator task tag same as specified at argument is exist,
    ///  returns that index value on the argument q, otherwise -1.
    /// </returns>
    /// <remarks>
    ///   Call this method in critical section at task set lock.
    /// </remarks>
    static member private FindQueueByITT ( q : ImmutableArray< TaskStatus > ) ( itt : ITT_T ) : int =
        let rec loop ( cnt : int ) : int =
            if cnt < q.Length then
                let t1 = q.[ cnt ] |> TaskStatus.getTask
                if itt = t1.InitiatorTaskTag then
                    cnt
                else
                    loop ( cnt + 1 )
            else
                -1
        loop 0

    /// <summary>
    ///   Add new Scsi task to task queue.
    /// </summary>
    /// <param name="source">
    ///  Source information of received SCSI command.
    /// </param>
    /// <param name="command">
    ///  Received SCSI command.
    /// </param>
    /// <param name="cdb">
    ///  CDB.
    /// </param>
    /// <param name="data">
    ///  Received Data-Out PDUs list..
    /// </param>
    /// <param name="curTS">
    ///  Current task set status.
    /// </param>
    /// <returns>
    ///  Next task set status.
    /// </returns>
    /// <remarks>
    ///  Call this method in critical section at task set lock.
    /// </remarks>
    member private this.AddNewScsiTaskToQueue ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( cdb : ICDB ) ( data : SCSIDataOutPDU list ) ( curTS : TaskSet ) : TaskSet =
        let itt = command.InitiatorTaskTag
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LUN ) )

        let t : IBlockDeviceTask =
            match curTS.ACA with
            | ValueSome( faultITN, _ ) -> // ACA was established.
                if ITNexus.Equals( source.I_TNexus, faultITN ) then  // this task was received from fault initiator port.
                    if command.ATTR <> TaskATTRCd.ACA_TASK then
                        // non ACA task is terminated in ACA ACTIVE status.
                        HLogger.Trace(
                            LogID.E_TASK_IRREGAL_TERMINATED,
                            fun g -> g.Gen2(
                                loginfo,
                                Constants.getSenseKeyNameFromValue SenseKeyCd.ILLEGAL_REQUEST,
                                "In this LU, ACA was established, non ACA task received from fault initiator is terminated in ACA ACTIVE status."
                            )
                        )

                        // The data segment is no longer used.
                        // Also, there will be no opportunity to release it after this, so it will be returned here.
                        data
                        |> Seq.map _.DataSegment
                        |> Seq.insertAt 0 command.DataSegment
                        |> PooledBuffer.Return

                        // Insert a task that send ACA active status to task queue.
                        new SendErrorStatusTask(
                            m_StatusMaster,
                            source,
                            { command with DataSegment = PooledBuffer.Empty },  // for safety
                            this,
                            m_ModeParameter.D_SENSE,
                            iScsiSvcRespCd.TARGET_FAILURE,
                            ScsiCmdStatCd.ACA_ACTIVE
                        )

                    elif this.CheckDuplicateACATask curTS.Queue then
                        // ACA task is alrady existed. Only one ACA task can be ran in LU.
                        HLogger.Trace(
                            LogID.E_TASK_IRREGAL_TERMINATED,
                            fun g -> g.Gen2(
                                loginfo,
                                Constants.getSenseKeyNameFromValue SenseKeyCd.ILLEGAL_REQUEST,
                                "ACA was established and ACA task is already existed in task queue, second ACA task is terminated in ACA ACTIVE status."
                            )
                        )

                        // The data segment is no longer used.
                        // Also, there will be no opportunity to release it after this, so it will be returned here.
                        data
                        |> Seq.map _.DataSegment
                        |> Seq.insertAt 0 command.DataSegment
                        |> PooledBuffer.Return

                        // Insert a task that send ACA active status to task queue.
                        new SendErrorStatusTask(
                            m_StatusMaster,
                            source,
                            { command with DataSegment = PooledBuffer.Empty },  // for safety
                            this,
                            m_ModeParameter.D_SENSE,
                            iScsiSvcRespCd.TARGET_FAILURE,
                            ScsiCmdStatCd.ACA_ACTIVE
                        )

                    else
                        // This task is executable.
                        match m_DeviceType with
                        | BDT_Normal ->
                            new ScsiTask( m_StatusMaster, source, command, cdb, data, this, m_Media, m_ModeParameter, m_PRManager, false )
                        | BDT_Dummy ->
                            new ScsiTaskForDummyDevice( m_StatusMaster, source, command, cdb, data, this, m_Media, m_ModeParameter, m_PRManager, false )
                
                // this task was received from not fault initiator port.
                else
                    let r = m_PRManager.decideACANoncompliant source m_LUN itt cdb command.DataSegment data faultITN
                    if r then
                        // This task can also be run when ACA is established, without following SAM-2 specifications.( reffer SPC-3 5.6.10.5 )
                        match m_DeviceType with
                        | BDT_Normal ->
                            new ScsiTask( m_StatusMaster, source, command, cdb, data, this, m_Media, m_ModeParameter, m_PRManager, true )
                        | BDT_Dummy ->
                            new ScsiTaskForDummyDevice( m_StatusMaster, source, command, cdb, data, this, m_Media, m_ModeParameter, m_PRManager, true )


                    elif command.ATTR = TaskATTRCd.ACA_TASK then
                        // ACA task is terminated in ACA ACTIVE status
                        HLogger.Trace(
                            LogID.E_TASK_IRREGAL_TERMINATED,
                            fun g -> g.Gen2(
                                loginfo,
                                Constants.getSenseKeyNameFromValue SenseKeyCd.ILLEGAL_REQUEST,
                                "In this LU, ACA was established, ACA task received from non fault initiator is terminated in ACA ACTIVE status."
                            )
                        )

                        // The data segment is no longer used.
                        // Also, there will be no opportunity to release it after this, so it will be returned here.
                        data
                        |> Seq.map _.DataSegment
                        |> Seq.insertAt 0 command.DataSegment
                        |> PooledBuffer.Return

                        // Insert a task that send ACA active status to task queue.
                        new SendErrorStatusTask(
                            m_StatusMaster,
                            source,
                            { command with DataSegment = PooledBuffer.Empty },  // for safety
                            this,
                            m_ModeParameter.D_SENSE,
                            iScsiSvcRespCd.TARGET_FAILURE,
                            ScsiCmdStatCd.ACA_ACTIVE
                        )

                    elif cdb.NACA then
                        // The data segment is no longer used.
                        // Also, there will be no opportunity to release it after this, so it will be returned here.
                        data
                        |> Seq.map _.DataSegment
                        |> Seq.insertAt 0 command.DataSegment
                        |> PooledBuffer.Return

                        // Insert a task that send ACA active status to task queue.
                        new SendErrorStatusTask(
                            m_StatusMaster,
                            source,
                            { command with DataSegment = PooledBuffer.Empty },  // for safety
                            this,
                            m_ModeParameter.D_SENSE,
                            iScsiSvcRespCd.TARGET_FAILURE,
                            ScsiCmdStatCd.ACA_ACTIVE
                        )

                    else
                        // The data segment is no longer used.
                        // Also, there will be no opportunity to release it after this, so it will be returned here.
                        data
                        |> Seq.map _.DataSegment
                        |> Seq.insertAt 0 command.DataSegment
                        |> PooledBuffer.Return

                        // Insert a task that send BUSY status to task queue.
                        // UA_INTLCK_CTRL is 10b. 
                        // When task is terminated in BUSY, LU shall not establish unit attention condition and not clear any existed unit attention condition.
                        new SendErrorStatusTask(
                            m_StatusMaster,
                            source,
                            { command with DataSegment = PooledBuffer.Empty },  // for safety
                            this,
                            m_ModeParameter.D_SENSE,
                            iScsiSvcRespCd.TARGET_FAILURE,
                            ScsiCmdStatCd.BUSY
                        )
 
            | ValueNone ->
                // If ACA was not established, non ACA tasks can be ran.
                if command.ATTR = TaskATTRCd.ACA_TASK then
                    data
                    |> Seq.map _.DataSegment
                    |> Seq.insertAt 0 command.DataSegment
                    |> PooledBuffer.Return

                    let errmsg = "ACA task received, but in this LU, ACA was not established."
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_MESSAGE_ERROR, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_MESSAGE_ERROR, errmsg )
                else
                    // This task is executable.
                    match m_DeviceType with
                    | BDT_Normal ->
                        new ScsiTask( m_StatusMaster, source, command, cdb, data, this, m_Media, m_ModeParameter, m_PRManager, false )
                    | BDT_Dummy ->
                        new ScsiTaskForDummyDevice( m_StatusMaster, source, command, cdb, data, this, m_Media, m_ModeParameter, m_PRManager, false )

        this.CheckOverlappedTask curTS.Queue source ( t.InitiatorTaskTag )

        let builder = ImmutableArray.CreateBuilder< TaskStatus >()
        builder.Capacity <- curTS.Queue.Length + 1
        for i in curTS.Queue do
            builder.Add i
        builder.Add( TASK_STAT_Dormant( t ) )
        {
            curTS with
                Queue = builder.DrainToImmutable()
        }
        
    /// <summary>
    ///  Check overlapeed command is exist or not.
    /// </summary>
    /// <param name="argQ">
    ///  Current task queue status.
    /// </param>
    /// <param name="source">
    ///  Source information of received SCSI command.
    /// </param>
    /// <param name="itt">
    ///  Initiator Task Tag of received SCSI command.
    /// </param>
    /// <exception>
    ///  If overlapped command is already existed, SCSIACAException is raised.
    /// </exception>
    /// <remarks>
    ///   Call this method in critical section at task set lock.
    /// </remarks>
    member private _.CheckOverlappedTask ( argQ : ImmutableArray< TaskStatus > ) ( source : CommandSourceInfo ) ( itt : ITT_T ) : unit =
        if ( BlockDeviceLU.FindQueueByITT argQ itt ) <> -1 then
            // ACA estblished
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LUN ) )
            let msg = "Overlapped task is detected."
            HLogger.ACAException( loginfo, SenseKeyCd.ABORTED_COMMAND, ASCCd.OVERLAPPED_COMMANDS_ATTEMPTED, msg )
            SCSIACAException(
                source,
                ScsiCmdStatCd.CHECK_CONDITION,
                SenseData(
                    true,
                    SenseKeyCd.ABORTED_COMMAND,
                    ASCCd.OVERLAPPED_COMMANDS_ATTEMPTED,
                    msg
                ),
                msg
            ) |> raise

    /// <summary>
    ///  Check ACA task is already exists, or not.
    /// </summary>
    /// <param name="argQ">
    ///  Current task queue status.
    /// </param>
    /// <returns>
    ///  If ACA task is already exists, returns True.
    /// </returns>
    /// <remarks>
    ///   Call this method in critical section at task set lock.
    /// </remarks>
    member private _.CheckDuplicateACATask ( argQ : ImmutableArray< TaskStatus > ) : bool =
        let rec loop ( cnt : int ) =
            if cnt < argQ.Length then
                let t = TaskStatus.getTask argQ.[ cnt ]
                if t.TaskType = BlockDeviceTaskType.ScsiTask && t.SCSICommand.ATTR = TaskATTRCd.ACA_TASK then
                    true
                else
                    loop ( cnt + 1 )
            else
                false
        loop 0

    /// <summary>
    ///   Search executable task and run that task.
    /// </summary>
    /// <remarks>
    ///   Call this method in critical section at task set lock.
    /// </remarks>
    member private this.StartExecutableSCSITasks ( curTS : TaskSet ) : TaskSet =

#if DEBUG
        HLogger.Trace( LogID.V_TRACE, fun g ->
            let loginfo = struct ( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            let usedCount = curTS.Queue |> Seq.length
            let runningCount = curTS.Queue |> Seq.filter ( function TASK_STAT_Running( _ ) -> true | _ -> false ) |> Seq.length
            let dormantCount = usedCount - runningCount
            g.Gen1( loginfo, sprintf "Task queue: Used=%d, Running=%d, Dormant=%d" usedCount runningCount dormantCount )
        )
#endif

        let builder = ImmutableArray.CreateBuilder< TaskStatus >()
        builder.Capacity <- curTS.Queue.Length

        if curTS.ACA.IsNone then
            // If ACA is not established, SIMPLE, ORDERED, HEAD OF QUEUE task is executable.
            let mutable ss = true   // If true, SIMPLE task can be execute.
            let mutable os = true   // If true, ORDERD task can be execute.
            for itr in curTS.Queue do
                match itr with
                | TASK_STAT_Dormant( x ) when x.TaskType = BlockDeviceTaskType.ScsiTask ->
                    match x.SCSICommand.ATTR with
                    | TaskATTRCd.TAGLESS_TASK
                    | TaskATTRCd.SIMPLE_TASK ->
                        // If SIMPLE tasks at the top of the queue is in dormant state, that tasks is executed.
                        let nextTaskStat =
                            if ss then
                                this.RunSCSITask x
                            else
                                itr

                        // next SIMPLE task is executable, too.
                        builder.Add nextTaskStat
                        ss <- true && ss
                        os <- false

                    | TaskATTRCd.ORDERED_TASK ->
                        // If an ORDERD task at the top of the queue is in dormant state, that tasks is executed.
                        let nextTaskStat =
                            if os then
                                this.RunSCSITask x
                            else
                                itr

                        // next SIMPLE or ORDERD task can not execute.
                        builder.Add nextTaskStat
                        ss <- false
                        os <- false

                    | _ ->
                        // If HEAD OF QUEUE or ACA task is in dormant state, that tasks is executed.
                        let nexttaskStat = this.RunSCSITask x

                        // next SIMPLE or ORDERD task can not execute.
                        builder.Add nexttaskStat
                        ss <- false
                        os <- false

                | TASK_STAT_Dormant( x ) ->
                    // Internal task is always executable.
                    // Whether the next SIMPLE or ORDERD task is executable takes over the previous situation. 
                    this.RunSCSITask x |> builder.Add

                | TASK_STAT_Running( x ) when x.TaskType = BlockDeviceTaskType.ScsiTask ->
                    match x.SCSICommand.ATTR with
                    | TaskATTRCd.TAGLESS_TASK
                    | TaskATTRCd.SIMPLE_TASK ->
                        // next SIMPLE task is executable, too.
                        builder.Add itr
                        ss <- true && ss
                        os <- false

                    | TaskATTRCd.ORDERED_TASK ->
                        // next SIMPLE or ORDERD task can not execute.
                        builder.Add itr
                        ss <- false
                        os <- false

                    | _ ->
                        // next SIMPLE or ORDERD task can not execute.
                        builder.Add itr
                        ss <- false
                        os <- false

                | TASK_STAT_Running( _ ) ->
                    builder.Add itr

        else
            // If ACA is established, only ACA task is executable.
            for itr in curTS.Queue do
                match itr with
                | TASK_STAT_Dormant( x ) when x.ACANoncompliant || x.SCSICommand.ATTR = TaskATTRCd.ACA_TASK ->
                    // Only ACA task or ACA non compliant task can be run.
                    // (* If taskItr is internal task, its ACANoncompliant flag value is always true.
                    //    And, internal task is always executable. )
                    this.RunSCSITask x
                    |> builder.Add
                | _ ->
                    builder.Add itr

        // replace containts of the task queue
        {
            curTS with
                Queue = builder.DrainToImmutable()
        }

    /// <summary>
    ///  Execute specified task.
    /// </summary>
    /// <param name="bdTask">
    ///  The task to be exexute.
    /// </param>
    /// <returns>
    ///  Next status of the task that has been executed. 
    /// </returns>
    /// <remarks>
    ///   Call this method in critical section at task set lock.
    /// </remarks>
    member private this.RunSCSITask ( bdTask : IBlockDeviceTask ) : TaskStatus =
        let cmdSource = bdTask.Source
        let loginfo = struct ( m_ObjID, ValueSome( cmdSource ), ValueSome( bdTask.InitiatorTaskTag ), ValueSome( m_LUN ) )
        HLogger.Trace( LogID.V_SCSI_TASK_STARTED, fun g -> g.Gen1( loginfo, bdTask.DescString ) )

        // Set task state to running
        // Following procedure must be ran in critical section at task set lock.

        // Check persistent reservation
        if m_PRManager.IsBlockedByPersistentReservation cmdSource bdTask then
            // Run a SendErrorStatusTask instead of the original task to response RESERVATION_CONFLICT.
            let errTask =
                new SendErrorStatusTask(
                    m_StatusMaster,
                    cmdSource,
                    { bdTask.SCSICommand with DataSegment = PooledBuffer.Empty },  // for safety
                    this,
                    m_ModeParameter.D_SENSE,
                    iScsiSvcRespCd.COMMAND_COMPLETE,
                    ScsiCmdStatCd.RESERVATION_CONFLICT
                ) :> IBlockDeviceTask

            // "bdTask" is removed from the queue, freeing the buffer.
            bdTask.ReleasePooledBuffer()

            // Execute SendErrorStatusTask
            m_ExecuteQueue.Enqueue( errTask.Execute() )
            TASK_STAT_Running( errTask )
        else
            // Check Unit Attention status
            match this.CheckUnitAttentionStatus bdTask with
            | ValueNone ->
                // Execute this task
                m_ExecuteQueue.Enqueue( bdTask.Execute() )
                TaskStatus.TASK_STAT_Running( bdTask )

            | ValueSome ex ->
                // Unit attention exist
                // send UA to the initiator
                let errTask =
                    new SendErrorStatusTask(
                        m_StatusMaster,
                        cmdSource,
                        { bdTask.SCSICommand with DataSegment = PooledBuffer.Empty },  // for safety
                        this,
                        m_ModeParameter.D_SENSE,
                        iScsiSvcRespCd.COMMAND_COMPLETE,
                        ScsiCmdStatCd.CHECK_CONDITION,
                        ex.SenseData
                    ) :> IBlockDeviceTask

                // "bdTask" is removed from the queue, freeing the buffer.
                bdTask.ReleasePooledBuffer()

                // Execute SendErrorStatusTask
                m_ExecuteQueue.Enqueue( errTask.Execute() )
                TASK_STAT_Running( errTask )

    /// <summary>
    ///  Handle SCSIACAException
    /// </summary>
    /// <param name="source">
    ///  Source of the failed command.
    /// </param>
    /// <param name="ex">
    ///  raised exception
    /// </param>
    /// <param name="command">
    ///  Command that raised the exception.
    /// </param>
    /// <param name="naca">
    ///  NACA falg value that is specified in the failed command.
    /// </param>
    /// <param name="taskType">
    ///  Type of failed task.
    /// </param>
    /// <param name="curTS">
    ///  Current task set status.
    /// </param>
    /// <remarks>
    ///  Call this method in critical section at task set lock.
    ///  If failed task is internal task, arguments from source to naca is specified from original SCSI command.
    /// </remarks>
    member private this.EstablishNewACAStatus
                        ( source : CommandSourceInfo )
                        ( ex : SCSIACAException )
                        ( command : SCSICommandPDU )
                        ( naca : bool )
                        ( taskType : BlockDeviceTaskType )
                        ( curTS : TaskSet ) : TaskSet =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( m_LUN ) )
        let ptn =
            if taskType = BlockDeviceTaskType.InternalTask then
                // ACA exception can be raised in only SCSI task, others are unexpected error.
                // This pattern is unexpected.
                HLogger.Trace( LogID.E_UNEXPECTED_ACA_EXCEPTION, fun g -> g.Gen1( loginfo, "Internal task reise an ACA exception. " ) )
                // escalation to LU reset
                raise <| Exception( "Internal task reise an ACA exception, but this pattern is unexpected, so escalation to LU reset." )

            elif ex.Status <> ScsiCmdStatCd.CHECK_CONDITION then
                // other than CHECK CONDITION status has not be noticed with an exception.
                HLogger.Trace(
                    LogID.I_UNEXPECTED_ACA_EXCEPTION,
                    fun g -> g.Gen2(
                        loginfo,
                        "Other than CHECK CONDITION status is raised with ACA exception. It is regarded that the task is normal ended. ",
                        ex.StackTrace
                    )
                )
                2
            elif curTS.ACA.IsNone then
                if command.ATTR = TaskATTRCd.ACA_TASK then
                    if naca then
                        HLogger.Trace( LogID.I_ACA_TASK_ABORTED_IN_NORMAL_STAT, fun g -> g.Gen0 loginfo )
                        3   // Establish new ACA status.
                    else
                        HLogger.Trace( LogID.I_ACA_TASK_ABORTED_IN_NORMAL_STAT, fun g -> g.Gen0 loginfo )
                        4   // Responce CA. ACA is not established.
                else
                    if naca then
                        HLogger.Trace( LogID.I_ESTABLISH_NEW_ACA, fun g -> g.Gen0 loginfo )
                        3   // Establish new ACA status.
                    else
                        HLogger.Trace( LogID.I_RESPONSE_WITH_CA_NOT_EST_ACA, fun g -> g.Gen0 loginfo )
                        4   // Responce CA. ACA is not established.
            elif ITNexus.Equals( fst curTS.ACA.Value, source.I_TNexus ) then
                if command.ATTR = TaskATTRCd.ACA_TASK then
                    if naca then
                        HLogger.Trace( LogID.I_CLEAR_ACA_AND_EST_NEW_ACA, fun g -> g.Gen0 loginfo )
                        3   // Clear ACA. And new ACA is established.
                    else
                        HLogger.Trace( LogID.I_CLEAR_ACA_AND_RESPONSE_CA, fun g -> g.Gen0 loginfo )
                        4   // Clear ACA. Response with CA.
                else
                    HLogger.Trace( LogID.I_NORMAL_TASK_ABORTED_IN_ACA_STAT, fun g -> g.Gen0 loginfo )
                    7   // Task aborted.
            else
                if command.ATTR = TaskATTRCd.ACA_TASK then
                    // ACA is established and an ACA task that came from non-fault initiator is failed.
                    // This condition is unexpected.
                    HLogger.Trace( LogID.E_UNEXPECTED_ACA_EXCEPTION, fun g -> g.Gen1( loginfo, "ACA task from non-fault initiator is failed." ) )
                    // escalation to LU reset
                    raise <| Exception( "ACA task from non-fault initiator is failed, but this pattern is unexpected, so escalation to LU reset. " )

                else
                    // ACA is established and an normal task( not ACA task ) that came from non-fault initiator is failed.
                    HLogger.Trace( LogID.I_NORMAL_TASK_ABORTED_IN_ACA_STAT, fun g -> g.Gen0 loginfo )
                    7   // Task aborted.

        match ptn with
        | 2 ->
            // send response message with raised status.
            let newTask =
                new SendErrorStatusTask(
                    m_StatusMaster,
                    source,
                    { command with DataSegment = PooledBuffer.Empty },
                    this,
                    m_ModeParameter.D_SENSE,
                    iScsiSvcRespCd.COMMAND_COMPLETE,
                    ex.Status,
                    ex.SenseData
                ) :> IBlockDeviceTask
            {
                curTS with
                    Queue = curTS.Queue.Add( TASK_STAT_Dormant( newTask ) )
            }

        | 3 ->
            // Send CHECK CONDITION status.
            let newTask =
                new SendErrorStatusTask(
                    m_StatusMaster,
                    source,
                    { command with DataSegment = PooledBuffer.Empty },
                    this,
                    m_ModeParameter.D_SENSE,
                    iScsiSvcRespCd.COMMAND_COMPLETE,
                    ScsiCmdStatCd.CHECK_CONDITION,
                    ex.SenseData
                ) :> IBlockDeviceTask
            {
                Queue = curTS.Queue.Add( TASK_STAT_Dormant( newTask ) )
                // establish new ACA status.
                ACA = ValueSome( source.I_TNexus, ex )
            }

        | 4 ->
            // Send CHECK CONDITION status.
            let newTask =
                new SendErrorStatusTask(
                    m_StatusMaster,
                    source,
                    { command with DataSegment = PooledBuffer.Empty },
                    this,
                    m_ModeParameter.D_SENSE,
                    iScsiSvcRespCd.COMMAND_COMPLETE,
                    ScsiCmdStatCd.CHECK_CONDITION,
                    ex.SenseData
                ) :> IBlockDeviceTask
            {
                Queue = curTS.Queue.Add( TASK_STAT_Dormant( newTask ) )
                // ACA is not established.
                ACA = ValueNone
            }

        | 7 ->
            // Send TASK ABORTED
            let newTask =
                new SendErrorStatusTask(
                    m_StatusMaster,
                    source,
                    { command with DataSegment = PooledBuffer.Empty },
                    this,
                    m_ModeParameter.D_SENSE,
                    iScsiSvcRespCd.TARGET_FAILURE,
                    ScsiCmdStatCd.TASK_ABORTED
                ) :> IBlockDeviceTask
            {
                curTS with
                    Queue = curTS.Queue.Add( TASK_STAT_Dormant( newTask ) )
            }

        | _ ->
            // Nothing to do.
            curTS

    /// <summary>
    ///  Check unit attention status when SCSI task is enabled.
    /// </summary>
    /// <param name="argtask">
    ///  Enabled SCSI task.
    /// </param>
    /// <remarks>
    ///  If the enabled task is other than Inquiry, ReportLUNs and RequestSense, the exception queue in UA is raised.
    /// </remarks>
    member private _.CheckUnitAttentionStatus ( argtask : IBlockDeviceTask ) : SCSIACAException voption =
        let loginfo = struct ( m_ObjID, ValueSome( argtask.Source ), ValueSome( argtask.InitiatorTaskTag ), ValueSome( m_LUN ) )
        if argtask.TaskType = BlockDeviceTaskType.ScsiTask then
            let argtask_CDB = argtask.CDB
            match argtask_CDB.Value.Type with
            | CDBTypes.Inquiry
            | CDBTypes.ReportLUNs
            | CDBTypes.RequestSense ->
                ValueNone
            | _ ->
                // Check unit attention status
                let r, ex = m_UnitAttention.TryRemove( argtask.Source.I_TNexus.InitiatorPortName )
                if r then
                    HLogger.Trace( LogID.W_UNIT_ATTENTION_EXISTED, fun g -> g.Gen1( loginfo, ex.Message ) )
                    ValueSome ex
                else
                    ValueNone
        else
            ValueNone


    /// <summary>
    ///  Delete specified task from queue.
    /// </summary>
    /// <param name="deltask">
    ///  task have to be deleted.
    /// </param>
    /// <param name="curTS">
    ///  Current task set status.
    /// </param>
    /// <remarks>
    ///   Call this method in critical section at task set lock.
    /// </remarks>
    member private _.DeleteTask ( deltask : IBlockDeviceTask ) ( curTS : TaskSet ) : TaskSet =
        if HLogger.IsVerbose then
            let loginfo = struct ( m_ObjID, ValueSome( deltask.Source ), ValueSome( deltask.InitiatorTaskTag ), ValueSome( m_LUN ) )
            HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( loginfo, "SCSI task is deleted." ) )

        let builder = ImmutableArray.CreateBuilder< TaskStatus >()
        builder.Capacity <- curTS.Queue.Length
        for itr in curTS.Queue do
            let wr = TaskStatus.getTask itr
            if Object.ReferenceEquals( wr, deltask ) |> not then
                builder.Add itr
        {
            curTS with
                Queue = builder.DrainToImmutable()
        }

    /// <summary>
    ///  Perform Logical Unit Reset for internal cause.
    /// </summary>
    /// <param name="source">
    ///  command source information of the command that raised LU-reset.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag of the command that raised LU-reset.
    /// </param>
    member private this.NotifyLUReset ( source : CommandSourceInfo ) ( itt : ITT_T ) : unit =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        // If LUReset is already performed, silentry ignore this notify.
        if not <| Volatile.Read( &m_LUResetFlag ) then
            // Set LUResetFlag to true, and start logical unit reset.
            Volatile.Write( &m_LUResetFlag, true )

            let oldTaskSet = m_TaskSet

            // Notify LU reset to all tasks.
            for itr in oldTaskSet.Queue do
                let t = TaskStatus.getTask itr
                HLogger.Trace( LogID.I_TASK_NOTIFY_TERMINATE, fun g -> g.Gen2( loginfo, t.InitiatorTaskTag, t.DescString ) )
                try
                    t.NotifyTerminate ( ITNexus.Equals( t.Source.I_TNexus, source.I_TNexus ) |> not )
                with
                | e ->
                    // ignore all of exceptions
                    HLogger.IgnoreException( fun g -> g.GenExp( loginfo, e ) )

            m_TaskSet <- {
                Queue = ImmutableArray< TaskStatus >.Empty;
                ACA = ValueNone;
            }

            // Notify LU reset to the Media object
            try
                HLogger.Trace( LogID.I_NOTIFY_LURESET_TO_MEDIA, fun g -> g.Gen2( loginfo, m_Media.MediaIndex, m_Media.DescriptString ) )
                m_Media.NotifyLUReset ( ValueSome itt ) ( ValueSome source )
            with
            | e ->
                // ignore all of exceptions
                HLogger.IgnoreException( fun g -> g.GenExp( loginfo, e ) )

            // Notify LU reset to StatusMaster object.
            m_StatusMaster.NotifyLUReset ( m_LUN ) ( this :> ILU )


