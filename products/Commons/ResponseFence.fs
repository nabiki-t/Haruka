//=============================================================================
// Haruka Software Storage.
// ResponseFence.fs : Defines ResponseFence class.
// ResponseFence class realize iSCSI Response Fence (defined in RFC5048 3.3) mechanism.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Runtime.CompilerServices

//=============================================================================
// Type definition

/// Whether response fence is required
[<Struct; IsReadOnly>]
type ResponseFenceNeedsFlag =
    /// This PDU is not used at response.
    | Irrelevant
    /// This response PDU is always sent immediately
    | Immediately
    /// response fence is not required, but the other PDU required response fence is sending,
    /// This PDU must be wait complete sending that other PDU.
    | R_Mode
    /// response fence is required.
    | W_Mode

    /// convert ResponseFenceNeedsFlag value to string
    static member toString : ( ResponseFenceNeedsFlag -> string ) =
        function
        | Irrelevant  -> "Irrelevant"
        | Immediately  -> "Immediately"
        | R_Mode  -> "R_Mode"
        | W_Mode  -> "W_Mode"

/// Internal record type of ResponseFenceLock class.
[<NoComparison>]
type ResponseFenceRec = {
    /// The tick count the lock was last acquired or released.
    m_Tick : int64

    /// acquired count counter.
    /// -1 : Response fence lock was aquired.
    /// 0  : freed
    /// >0 : Normal lock were aquired.
    m_LockCounter : int64;

    /// waiting queue
    m_Tasks : ImmutableQueue< struct( bool * ( unit -> unit ) ) >;

    /// waiting tasks count.( It must equal ( Seq.length m_Tasks ) )
    m_QueuedTaskCount : int;
}

//=============================================================================
// Class implementation

/// Implementing iSCSI Response Fence (defined in RFC5048 3.3) mechanism.
type ResponseFence() =

    let m_Stat = OptimisticLock< ResponseFenceRec > ({
        m_Tick = Environment.TickCount64;
        m_LockCounter = 0L;
        m_Tasks = ImmutableQueue< struct( bool * ( unit -> unit ) ) >.Empty;
        m_QueuedTaskCount = 0;
    })

    /// <summary>
    ///  Acquire the lock that requires a response fence.
    ///  The lock is acquired successfully only if no other locks are acquired.
    /// </summary>
    /// <param name="argTask">
    ///  The next procedure that must be executed.
    /// </param>
    /// <remarks>
    ///  If the lock is successfully acquired, the function specified in the argument is executed synchronously.
    ///  Thus, in this case, control will not be returned until argTask has finished processing.
    ///  If it failed to acquire the lock, the function of argTask is queued and this method returns immediately.
    ///  Queued task is executed when the lock is freed.
    /// </remarks>
    member _.RFLock ( argTask : ( unit -> unit ) ) : unit =
        let runTask =
            m_Stat.Update( fun oldStat ->
                if oldStat.m_LockCounter = 0L then
                    // Succeed lock acquisition
                    let newStat = {
                        m_Tick = Environment.TickCount64;
                        m_LockCounter = -1L;
                        m_Tasks = oldStat.m_Tasks;
                        m_QueuedTaskCount = oldStat.m_QueuedTaskCount;
                    }
                    struct( newStat, ValueSome argTask )
                else
                    // Wait required
                    let newStat = {
                        m_Tick = oldStat.m_Tick;
                        m_LockCounter = oldStat.m_LockCounter;
                        m_Tasks = oldStat.m_Tasks.Enqueue( struct( true, argTask ) )
                        m_QueuedTaskCount = oldStat.m_QueuedTaskCount + 1;
                    }
                    struct( newStat, ValueNone )
            )
        if runTask.IsSome then
            runTask.Value()

    /// <summary>
    ///  Acquire the lock that not requires a response fence.
    ///  The lock is acquied scuuessflully if RFLock is not acquired.
    /// </summary>
    /// <param name="argTask">
    ///  The next procedure that must be executed.
    /// </param>
    /// <remarks>
    ///  If the lock is successfully acquired, the function specified in the argument is executed synchronously.
    ///  Thus, in this case, control will not be returned until argTask has finished processing.
    ///  If it failed to acquire the lock, the function of argTask is queued and this method returns immediately.
    ///  Queued task is executed when the lock is freed.
    /// </remarks>
    member _.NormalLock ( argTask : ( unit -> unit ) ) : unit =
        let runTask =
            m_Stat.Update( fun oldStat ->
                if oldStat.m_LockCounter >= 0L && oldStat.m_Tasks.IsEmpty then
                    // Succeed lock acquisition
                    let newStat = {
                        m_Tick =
                            if oldStat.m_LockCounter = 0L then
                                Environment.TickCount64 // When a new lock is acquired, the tick count is updated.
                            else
                                oldStat.m_Tick;         // If only the number of unlocked R locks is added, the tick count is not updated.
                        m_LockCounter = oldStat.m_LockCounter + 1L;
                        m_Tasks = oldStat.m_Tasks;
                        m_QueuedTaskCount = oldStat.m_QueuedTaskCount;
                    }
                    struct( newStat, ValueSome argTask )
                else
                    // Wait required
                    let newStat = {
                        m_Tick = oldStat.m_Tick;
                        m_LockCounter = oldStat.m_LockCounter;
                        m_Tasks = oldStat.m_Tasks.Enqueue( struct( false, argTask ) )
                        m_QueuedTaskCount = oldStat.m_QueuedTaskCount + 1;
                    }
                    struct( newStat, ValueNone )
            )
        if runTask.IsSome then
            runTask.Value()

    /// <summary>
    ///  Acquires a lock according to the requested lock type and executes the task.
    /// </summary>
    /// <param name="lockType">
    ///  The type of lock to acquire.
    /// </param>
    /// <param name="argTask">
    ///  The next procedure that must be executed.
    /// </param>
    member this.Lock ( lockType : ResponseFenceNeedsFlag ) ( argTask : ( unit -> unit ) ) : unit =
        match lockType with
        | ResponseFenceNeedsFlag.Irrelevant ->
            ()  // Silentry ignore

        | ResponseFenceNeedsFlag.Immediately ->
            // Run task immidiatly without response fence lock
            argTask()

        | ResponseFenceNeedsFlag.R_Mode ->
            // Need R-Mode lock at response fence.
            this.NormalLock argTask

        | ResponseFenceNeedsFlag.W_Mode ->
            // Need W-Mode lock at response fence.
            this.RFLock argTask


    /// <summary>
    ///  Free the acquired lock.
    /// </summary>
    /// <remarks>
    ///  When the lock is released and the queued task becomes executable,
    ///  the next lock is acquired and these tasks are executed synchronously.
    ///  In this case, this method will not be returned until all executable tasks has finished processing.
    /// </remarks>
    member _.Free() : unit =
        let runTasks =
            m_Stat.Update( fun oldStat ->
                let wnextCount =
                    if oldStat.m_LockCounter <= 0L then
                        // Response fence lock is freed.
                        0L;
                    else
                        // Nornal lock is freed.
                        oldStat.m_LockCounter - 1L;
                if oldStat.m_Tasks.IsEmpty then
                    // There are no new tasks to be executed.
                    let wns = {
                        m_Tick = Environment.TickCount64;
                        m_LockCounter = wnextCount;
                        m_Tasks = oldStat.m_Tasks;
                        m_QueuedTaskCount = oldStat.m_QueuedTaskCount;
                    }
                    struct( wns, Array.empty )
                else
                    let struct( f, t ) = oldStat.m_Tasks.Peek()
                    if f then
                        // The next task to be executed requires the response fence
                        if wnextCount = 0L then
                            // If the response fence lock can be acquired, acquire the lock and execute the next task.
                            let wns = {
                                m_Tick = Environment.TickCount64;
                                m_LockCounter = -1L;
                                m_Tasks = oldStat.m_Tasks.Dequeue();
                                m_QueuedTaskCount = oldStat.m_QueuedTaskCount - 1;
                            }
                            struct( wns, [| t |] )
                        else
                            // If the response fence lock can not be acquired, there are no tasks to be executed.
                            let wns = {
                                m_Tick = Environment.TickCount64;
                                m_LockCounter = wnextCount;
                                m_Tasks = oldStat.m_Tasks;
                                m_QueuedTaskCount = oldStat.m_QueuedTaskCount;
                            }
                            struct( wns, Array.empty )
                    else
                        // The next task to be executed is not require the response fence.
                        // * Notice that wnextCount must not be less than 0.

                        // Get consecutive tasks that do not require a response fence.
                        let builder = new List< unit -> unit >()
                        builder.Add t
                        let rec loop ( q : ImmutableQueue< struct( bool * ( unit -> unit ) ) > ) =
                            if q.IsEmpty then
                                q
                            else
                                let struct( f, wt ) = q.Peek()
                                if f then
                                    q
                                else
                                    builder.Add wt
                                    loop ( q.Dequeue() )
                        let nextq = loop ( oldStat.m_Tasks.Dequeue() )
                        let wns = {
                            m_Tick = Environment.TickCount64;
                            m_LockCounter = wnextCount + ( int64 builder.Count );
                            m_Tasks = nextq;
                            m_QueuedTaskCount = oldStat.m_QueuedTaskCount - builder.Count;
                        }
                        struct( wns, builder.ToArray() )
            )
        runTasks
        |> Array.iter ( fun itr -> itr() )

    /// Get queued task count.
    member _.TaskCount : int =
        m_Stat.obj.m_QueuedTaskCount

    /// Get lock counter
    member _.LockCounter : int64 =
        m_Stat.obj.m_LockCounter

    /// Get lock status
    /// It returns conbination of tick ​​count, lock counter, queued task count.
    member _.LockStatus : struct( int64 * int64 * int ) =
        let r = m_Stat.obj
        struct( r.m_Tick, r.m_LockCounter, r.m_QueuedTaskCount )
