//=============================================================================
// Haruka Software Storage.
// RWLock.fs : reader-writer lock object..
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

//=============================================================================
// Class implementation

/// <summary>
/// Provides a reader-writer lock that supports asynchronous operations.
/// </summary>
type RWLock() =

    let m_Lock = new SemaphoreSlim( 1 )
    let mutable m_WCnt = 0
    let mutable m_RCnt = 0
    let m_WWaiter = Queue< TaskCompletionSource< unit > >()
    let m_RWaiter = Queue< TaskCompletionSource< unit > >()

    let getTCS() =
      TaskCompletionSource<unit>( TaskCreationOptions.RunContinuationsAsynchronously )


    /// <summary>
    ///  Asynchronously acquires the lock in read mode.
    /// </summary>
    /// <returns>
    ///  A task that represents the asynchronous wait for the read lock.
    /// </returns>
    member _.RLock() =
        task {
            do! m_Lock.WaitAsync()
            do!
                try
                    if m_RCnt = 0 && m_WCnt = 0 then
                        m_RCnt <- m_RCnt + 1
                        Task.FromResult()
                    elif m_RCnt = 0 && m_WCnt > 0 then
                        let t = getTCS()
                        m_RWaiter.Enqueue t
                        t.Task
                    elif m_RCnt > 0 && m_WCnt = 0 && m_WWaiter.Count > 0 then
                        let t = getTCS()
                        m_RWaiter.Enqueue t
                        t.Task
                    elif m_RCnt > 0 && m_WCnt = 0 && m_WWaiter.Count = 0 then
                        m_RCnt <- m_RCnt + 1
                        Task.FromResult()
                    else
                        raise <| Exception( "Unexpected status in RWLock.RLock" )
                finally
                    m_Lock.Release() |> ignore
        }

    /// <summary>
    ///  Asynchronously acquires the lock in write mode.
    /// </summary>
    /// <returns>
    ///  A task that represents the asynchronous wait for the write lock.
    /// </returns>
    member _.WLock() =
        task {
            do! m_Lock.WaitAsync()
            do!
                try
                    if m_RCnt = 0 && m_WCnt = 0 then
                        m_WCnt <- m_WCnt + 1
                        Task.FromResult()
                    elif ( m_RCnt = 0 && m_WCnt = 1 ) || ( m_RCnt > 0 && m_WCnt = 0 ) then
                        let t = getTCS()
                        m_WWaiter.Enqueue t
                        t.Task
                    else
                        raise <| Exception( "Unexpected status in RWLock.WLock" )
                finally
                    m_Lock.Release() |> ignore
        }

    /// <summary>
    ///  Releases the lock, allowing other waiting readers or writers to proceed.
    /// </summary>
    /// <returns>
    ///  A task that represents the asynchronous release operation.
    /// </returns>
    member _.Release() =
        task {
            do! m_Lock.WaitAsync()
            try
                if m_RCnt = 0 && m_WCnt = 0 then
                    ()
                elif m_RCnt = 0 && m_WCnt = 1 then
                    if m_WWaiter.Count > 0 then
                        m_WWaiter.Dequeue() |> _.SetResult()
                    elif m_RWaiter.Count > 0 then
                        m_RCnt <- m_RWaiter.Count
                        m_WCnt <- 0
                        for _ = 1 to m_RCnt do
                            m_RWaiter.Dequeue() |> _.SetResult()
                    else
                        m_WCnt <- 0
                elif m_RCnt > 1 && m_WCnt = 0 then
                    m_RCnt <- m_RCnt - 1
                elif m_RCnt = 1 && m_WCnt = 0 then
                    if m_WWaiter.Count > 0 then
                        m_RCnt <- 0
                        m_WCnt <- 1
                        m_WWaiter.Dequeue() |> _.SetResult()
                    elif m_RWaiter.Count > 0 then
                        m_RCnt <- m_RWaiter.Count
                        m_WCnt <- 0
                        for _ = 1 to m_RCnt do
                            m_RWaiter.Dequeue() |> _.SetResult()
                    else
                        m_RCnt <- 0
                else
                    raise <| Exception( "Unexpected status in RWLock.Release" )
            finally
                m_Lock.Release() |> ignore
        }

    /// Gets the number of concurrent readers holding the lock.
    member _.RCnt = m_RCnt

    /// Gets the number of writers holding the lock.
    member _.WCnt = m_WCnt

    /// Gets the number of tasks waiting for a read lock.
    member _.RWaiter = m_RWaiter.Count

    /// Gets the number of tasks waiting for a write lock.
    member _.WWaiter = m_WWaiter.Count

    /// <summary>
    ///  Gets a snapshot of the current lock status (RCnt, WCnt, RWaiter, WWaiter).
    /// </summary>
    /// <remarks>
    ///  Note that because mutual exclusion is not implemented, the four values ​​obtained may be inconsistent.
    /// </remarks>
    member _.Stat = ( m_RCnt, m_WCnt, m_RWaiter.Count, m_WWaiter.Count )
