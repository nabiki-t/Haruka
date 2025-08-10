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

//=============================================================================
// declaration


/// Internal record type of ResponseFenceLock class.
[<NoComparison>]
type ResponseFenceRec = {
    /// acquired count counter.
    /// -1 : Response fence lock was aquired.
    /// 0  : freed
    /// >0 : Normal lock were aquired.
    m_LockCounter : int64;

    /// waiting cueue
    m_Tasks : ImmutableQueue< struct( bool * ( unit -> unit ) ) >;

    /// waiting tasks count.( It must equal ( Seq.length m_Tasks ) )
    m_QueueedTaskCount : int;
}

/// Implementing iSCSI Response Fence (defined in RFC5048 3.3) mechanism.
type ResponseFence() =

    let m_Stat = OptimisticLock< ResponseFenceRec > ({
        m_LockCounter = 0L;
        m_Tasks = ImmutableQueue< struct( bool * ( unit -> unit ) ) >.Empty;
        m_QueueedTaskCount = 0;
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
                        m_LockCounter = -1L;
                        m_Tasks = oldStat.m_Tasks;
                        m_QueueedTaskCount = oldStat.m_QueueedTaskCount;
                    }
                    struct( newStat, ValueSome argTask )
                else
                    // Wait required
                    let newStat = {
                        m_LockCounter = oldStat.m_LockCounter;
                        m_Tasks = oldStat.m_Tasks.Enqueue( struct( true, argTask ) )
                        m_QueueedTaskCount = oldStat.m_QueueedTaskCount + 1;
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
                        m_LockCounter = oldStat.m_LockCounter + 1L;
                        m_Tasks = oldStat.m_Tasks;
                        m_QueueedTaskCount = oldStat.m_QueueedTaskCount;
                    }
                    struct( newStat, ValueSome argTask )
                else
                    // Wait required
                    let newStat = {
                        m_LockCounter = oldStat.m_LockCounter;
                        m_Tasks = oldStat.m_Tasks.Enqueue( struct( false, argTask ) )
                        m_QueueedTaskCount = oldStat.m_QueueedTaskCount + 1;
                    }
                    struct( newStat, ValueNone )
            )
        if runTask.IsSome then
            runTask.Value()

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
                        m_LockCounter = wnextCount;
                        m_Tasks = oldStat.m_Tasks;
                        m_QueueedTaskCount = oldStat.m_QueueedTaskCount;
                    }
                    struct( wns, Array.empty )
                else
                    let struct( f, t ) = oldStat.m_Tasks.Peek()
                    if f then
                        // The next task to be executed requires the response fence
                        if wnextCount = 0L then
                            // If the response fence lock can be acquired, acquire the lock and execute the next task.
                            let wns = {
                                m_LockCounter = -1L;
                                m_Tasks = oldStat.m_Tasks.Dequeue();
                                m_QueueedTaskCount = oldStat.m_QueueedTaskCount - 1;
                            }
                            struct( wns, [| t |] )
                        else
                            // If the response fence lock can not be acquired, there are no tasks to be executed.
                            let wns = {
                                m_LockCounter = wnextCount;
                                m_Tasks = oldStat.m_Tasks;
                                m_QueueedTaskCount = oldStat.m_QueueedTaskCount;
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
                            m_LockCounter = wnextCount + ( int64 builder.Count );
                            m_Tasks = nextq;
                            m_QueueedTaskCount = oldStat.m_QueueedTaskCount - builder.Count;
                        }
                        struct( wns, builder.ToArray() )
            )
        runTasks
        |> Array.iter ( fun itr -> itr() )

    /// Get queued task count.
    member _.Count : int =
        m_Stat.obj.m_QueueedTaskCount

    /// Get lock status
    member _.LockStatus : int64 =
        m_Stat.obj.m_LockCounter
