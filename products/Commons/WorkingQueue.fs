//=============================================================================
// Haruka Software Storage.
// WorkingQueue.fs : Definitions of WorkingQueue class
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

//=============================================================================
// Class implementation

/// <summary>
///  This class provide the function that queueing the works and running by .NET task that in sequentially.
///  The works will be executed in exactly the same order as they ware enqueued.
/// </summary>
/// <param name="m_WorkFunc">
///  The function thet receives the work enqueued by 'Enqueue' method and process the work.
/// </param>
/// <param name="m_ConsCount">
///  Consumer count.
/// </param>
/// <remarks>
/// Simple usage example.
/// <code>
/// <![CDATA[
///  let wf( v : int ) = task { printfn "--%d--" v }
///  let w = new WorkingTaskQueue<int>( wf )
///  w.Enqueue( 0 ) // "--0--" is printed.
/// ]]>
/// </code>
///  The function specified at 'm_WorkFunc' must not raise any exception.
///  If an exception is raised, process is terminated.
/// <code>
/// <![CDATA[
///  let wf( v : int ) = task { raise <| Exception( "" ) }
///  let w = new WorkingTaskQueue<int>( wf )
///  w.Enqueue( 0 ) // When the task is executed, the process is killed.
/// ]]>
/// </code>
/// </remarks>
type WorkingTaskQueue<'T>( m_WorkFunc : ( 'T -> Task<unit> ), m_ConsCount : uint ) as this =

    /// the task queue
    let m_SendRespQueue = new BufferBlock< 'T voption >()

    /// continue flag.
    /// Loop in the 'Work' method is continued while 'm_ContinueFlg' is true.
    let mutable m_ContinueFlg = true

    /// Waiting and running task count
    let mutable m_RunWaitCount = 0u

    /// Number of tasks completed so far
    /// Note that the counter is not reset, so if it overflows it will return to 0.
    let mutable m_ProcessedCount = 0UL

    do
        // start working loop.
        for _ in [ 1u .. m_ConsCount ] do
            this.Work()|> ignore

    /// <summary>
    ///  Simplified constructor for the case of the consumer is only one.
    /// </summary>
    /// <param name="argWorkFunc">
    ///  The function thet receives the work enqueued by 'Enqueue' method and process the work.
    /// </param>
    new ( argWorkFunc : ( 'T -> Task<unit> ) ) =
        new WorkingTaskQueue< 'T >( argWorkFunc, 1u )

    /// <summary>
    ///  Execute the tasks.
    ///  This function executes the tasks one after the other.
    /// </summary>
    /// <remarks>
    ///  If called 'm_WorkFunc' raised an exception, the PROCESS IS TERMINATED.
    ///  So, m_WorkFunc function must handle all of exceptions.
    /// </remarks>
    member private _.Work() : Task<unit> =
        let buf = m_SendRespQueue :> ISourceBlock< 'T voption >
        task {
            try
                while Volatile.Read( &m_ContinueFlg ) do
                    let! v = buf.ReceiveAsync()
                    match v with
                    | ValueSome( x ) ->
                        do! m_WorkFunc x
                        Interlocked.Decrement( &m_RunWaitCount ) |> ignore
                        m_ProcessedCount <- m_ProcessedCount + 1UL
                    | ValueNone ->
                        Volatile.Write( &m_ContinueFlg, false )
            with
            | _ ->
                // Since it cannot cope with this pattern, it silently kills the process.
                exit( 1 )
        }

    /// <summary>
    ///  Enqueue the work.
    /// </summary>
    /// <param name="w">
    ///  The work given as an argument to the 'm_WorkFunc' function.
    /// </param>
    /// <remarks>
    ///  This function returns immediately without waiting for the task to finish.
    /// </remarks>
    member _.Enqueue ( w : 'T ) : unit =
        if Volatile.Read( &m_ContinueFlg ) then
            Interlocked.Increment( &m_RunWaitCount ) |> ignore
            ( m_SendRespQueue :> ITargetBlock< 'T voption > ).Post ( ValueSome w ) |> ignore

    /// terminate working loop
    member _.Stop() : unit =
        Volatile.Write( &m_ContinueFlg, false )
        ( m_SendRespQueue :> ITargetBlock< 'T voption > ).Post ValueNone |> ignore

    /// Get the number of tasks in the queue. Does not include running tasks.
    member _.Count : int =
        m_SendRespQueue.Count

    /// Get the number of tasks that are not running or are running.
    member _.RunWaitCount : uint32 =
        m_RunWaitCount

    /// Get the number of tasks that have been processed so far
    member _.ProcessedCount : uint64 = 
        m_ProcessedCount


/// <summary>
///  A specialized WorkingTaskQueue class to execute .NET tasks directly.
/// </summary>
/// <param name="m_ConsCount">
///  Consumer count.
/// </param>
/// <remarks>
/// <code>
///  let w = new TaskQueue()
///  w.Enqueue ( fun () -> task { printfn "AAA" } ) // "AAA" printed
/// </code>
/// </remarks>
type TaskQueue( m_ConsCount : uint ) =
    inherit WorkingTaskQueue< unit -> Task<unit> >( ( fun ( w : ( unit -> Task<unit> ) ) -> w() ), m_ConsCount )

    /// <summary>
    ///  Simplified constructor for the case of the consumer is only one.
    /// </summary>
    new () = new TaskQueue( 1u )



/// <summary>
///  TaskQueue with the ability to save state.
///  The execution multiplicity is always 1.
/// </summary>
/// <param name="init">
///  Initial status.
/// </param>
/// <remarks>
/// <code>
/// <![CDATA[
///  let q = new TaskQueueWithState<int>( 0 )
///  q.Enqueue( fun stat -> task {
///    printfn "%d" stat
///    return ( statu + 1 )
///  } )
/// ]]>
/// </code>
/// </remarks>
type TaskQueueWithState< 'A >( init : 'A ) =

    /// Task queue object.
    /// In order to preserve state, the execution multiplicity must always be 1.
    let m_Queue = new TaskQueue( 1u )

    /// saved status.
    let mutable m_stat = init

    /// <summary>
    ///  Enqueue a task. Task function form must be <c><![CDATA[ 'A -> Task<'A> ]]></c>.
    ///  The argument at the first execution is given as the value of the constructor argument 'init'.
    ///  When the function is executed for the second or subsequent times, the return value from the previous execution is given as the argument.
    /// </summary>
    /// <param name="f">Task function.</param>
    member _.Enqueue( f : ( 'A -> Task<'A> ) ) : unit =
        m_Queue.Enqueue( fun () ->
            task {
                let! w = f( m_stat )
                m_stat <- w
            }
        )

    /// <summary>
    ///  Terminate.
    /// </summary>
    member _.Stop() = m_Queue.Stop()

    /// Get the number of tasks in the queue. Does not include running tasks.
    member _.Count = m_Queue.Count

    /// Get the number of tasks that are not running or are running.
    member _.RunWaitCount = m_Queue.RunWaitCount


/// <summary>
///  This class provide the function that queueing the works and running by normal lambda function that in sequentially.
///  The works will be executed in exactly the same order as they ware enqueued.
/// </summary>
/// <param name="m_WorkFunc">
///  The function thet receives the work enqueued by 'Enqueue' method and process the work.
/// </param>
/// <param name="m_ConsCount">
///  Consumer count.
/// </param>
/// <remarks>
/// Simple usage example.
/// <code>
/// <![CDATA[
///  let wf( v : int ) = printfn "--%d--" v
///  let w = new WorkingLambdaQueue<int>( wf )
///  w.Enqueue( 0 ) // "--0--" is printed.
/// ]]>
/// </code>
///  The function specified at 'm_WorkFunc' must not raise any exception.
///  If an exception is raised, process is terminated.
/// <code>
/// <![CDATA[
///  let wf( v : int ) = raise( Exception( "" ) )
///  let w = new WorkingLambdaQueue<int>( wf )
///  w.Enqueue( 0 ) // When the task is executed, the process is killed.
/// ]]>
/// </code>
/// </remarks>
type WorkingLambdaQueue<'T>( m_WorkFunc : ( 'T -> unit ), m_ConsCount : uint ) as this =

    /// the task queue
    let m_SendRespQueue = new BufferBlock< 'T voption >()

    /// continue flag.
    /// Loop in the 'Work' method is continued while 'm_ContinueFlg' is true.
    let mutable m_ContinueFlg = true

    /// Waiting and running task count
    let mutable m_RunWaitCount = 0u

    /// Number of tasks completed so far
    /// Note that the counter is not reset, so if it overflows it will return to 0.
    let mutable m_ProcessedCount = 0UL

    do
        // start working loop.
        for _ in [ 1u .. m_ConsCount ] do
            this.Work()|> ignore

    /// <summary>
    ///  Simplified constructor for the case of the consumer is only one.
    /// </summary>
    /// <param name="argWorkFunc">
    ///  The function thet receives the work enqueued by 'Enqueue' method and process the work.
    /// </param>
    new ( argWorkFunc : ( 'T -> unit ) ) =
        new WorkingLambdaQueue< 'T >( argWorkFunc, 1u )

    /// <summary>
    ///  Execute the tasks.
    ///  This function executes the tasks one after the other.
    /// </summary>
    /// <remarks>
    ///  If called 'm_WorkFunc' raised an exception, the PROCESS IS TERMINATED.
    ///  So, m_WorkFunc function must handle all of exceptions.
    /// </remarks>
    member private _.Work() : Task<unit> =
        let buf = m_SendRespQueue :> ISourceBlock< 'T voption >
        task {
            try
                while Volatile.Read( &m_ContinueFlg ) do
                    let! v = buf.ReceiveAsync()
                    match v with
                    | ValueSome( x ) ->
                        m_WorkFunc x
                        Interlocked.Decrement( &m_RunWaitCount ) |> ignore
                        m_ProcessedCount <- m_ProcessedCount + 1UL
                    | ValueNone ->
                        Volatile.Write( &m_ContinueFlg, false )
            with
            | _ ->
                // Since it cannot cope with this pattern, it silently kills the process.
                exit( 1 )
        }

    /// <summary>
    ///  Enqueue the work.
    /// </summary>
    /// <param name="w">
    ///  The work given as an argument to the 'm_WorkFunc' function.
    /// </param>
    /// <remarks>
    ///  This function returns immediately without waiting for the task to finish.
    /// </remarks>
    member _.Enqueue ( w : 'T ) : unit =
        if Volatile.Read( &m_ContinueFlg ) then
            Interlocked.Increment( &m_RunWaitCount ) |> ignore
            ( m_SendRespQueue :> ITargetBlock< 'T voption > ).Post ( ValueSome w ) |> ignore

    /// terminate working loop
    member _.Stop() : unit =
        Volatile.Write( &m_ContinueFlg, false )
        ( m_SendRespQueue :> ITargetBlock< 'T voption > ).Post ValueNone |> ignore

    /// Get the number of tasks in the queue. Does not include running tasks.
    member _.Count : int =
        m_SendRespQueue.Count

    /// Get the number of tasks that are not running or are running.
    member _.RunWaitCount : uint32 =
        m_RunWaitCount

    /// Get the number of tasks that have been processed so far
    member _.ProcessedCount : uint64 = 
        m_ProcessedCount

/// <summary>
///  A specialized WorkingLambdaQueue class to execute normal lambda function directly.
/// </summary>
/// <param name="m_ConsCount">
///  Consumer count.
/// </param>
/// <remarks>
/// <code>
///  let w = new LambdaQueue()
///  w.Enqueue ( fun () -> printfn "AAA" ) // "AAA" printed
/// </code>
/// </remarks>
type LambdaQueue( m_ConsCount : uint ) =
    inherit WorkingLambdaQueue< unit -> unit >( ( fun ( w : ( unit -> unit ) ) -> w() ), m_ConsCount )

    /// <summary>
    ///  Simplified constructor for the case of the consumer is only one.
    /// </summary>
    new () = new LambdaQueue( 1u )
