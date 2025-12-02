//=============================================================================
// Haruka Software Storage.
// TaskWaiter.fs : Definitions of TaskWaiter class
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Immutable

//=============================================================================
// Type definition

/// Maintaining task state
[<NoComparison; Struct>]
type private TaskState<'TR> =
    /// Indicates that a Wait request has been made but a Release has not been made.
    | Wait of TaskCompletionSource<'TR>
    /// Indicates that a Release request has been made but a Wait has not been made.
    | Release of TaskCompletionSource<'TR>

/// <summary>
///  Implement a function to wait for task completion by specifying a number.
/// </summary>
/// <example>
/// <code>
/// <![CDATA[
/// let w = TaskWaiter< int, int >()
/// (* When Release is called after Wait, in which case the call to Wait will not return until Release is called. *)
/// (* thread 1 *)
/// let th1 = fun () -> task {
///   let! res = w.Wait( 0 )
///   printfn "result = %d" res
/// }
/// (* thread 1 *)
/// let th2 = fun () -> task {
///   Thread.Sleep 9999999
///   w.Release( 0 )
/// }
/// 
/// (* If Wait is called after Release, the Wait call will finish immediately. *)
/// task {
///   w.Release( 1 )
///   let! res = w.Wait( 1 )
///   printfn "result = %d" res
/// }
/// ]]>
/// </code>
/// </example>
/// <remarks>
///  TI specifies the data type to be used for the ID.
///  TR specifies the data type that will be the processing result of the task.
///  It is not assumed that Wait and Release will be performed multiple times simultaneously with the same ID specified.
///  If Wait is executed twice with the same ID and then Release is called twice, 
///  the first Release will release the locks of both threads that are waiting. 
///  The second Release will register the ID, and that ID will remain thereafter.
/// <code>
/// <![CDATA[
///   -------------------------------------------------------------------------> time
///                            The locks on both thread 1 and 2 are released.
///                            v
///   thread 1 ====Wait........|===============================================
///   thread 2 ==========Wait..|===============================================
///   thread 3 ================Release=========================================
///   thread 4 ==========================Release===============================
///                                      ^
///                                      A new ID will be registered.
/// ]]>
/// </code>
/// If two Release calls are made with the same ID and then two Wait calls are made, the second Release call is ignored.
/// The subsequent Wait call completes immediately, and the second Wait call continues to wait forever.
/// <code>
/// <![CDATA[
///   -------------------------------------------------------------------------> time
///   thread 1 ===Release======================================================
///   thread 2 ============Release=============================================
///                        ^
///                        ignored   Completes immediately
///                                  v
///   thread 3 ======================Wait======================================
///   thread 4 =============================Wait...............................
///                                         ^
///                                         Wait forever.
/// ]]>
/// </code>
/// </remarks>
type TaskWaiter<'TI, 'TR when 'TI : equality>() =

    /// Keeping the generated tasks
    let d = OptimisticLock( ImmutableDictionary< 'TI, TaskState<'TR> >.Empty )

    /// <summary>
    ///  Wait for task completion by specifying ID.
    ///  If the ID has already been released, the task will immediately terminate.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    /// <returns>
    ///  Generated Task.
    /// </returns>
    member _.Wait ( idx : 'TI ) : Task< 'TR > =
        let waitt =
            d.Update( fun oldd ->
                let r, v = oldd.TryGetValue( idx )
                if r then
                    match v with
                    | TaskState.Wait( x ) ->
                        struct( oldd, x )
                    | TaskState.Release( x ) ->
                        struct( oldd.Remove idx, x )
                else
                    let t = TaskCompletionSource<'TR>( TaskCreationOptions.RunContinuationsAsynchronously )
                    let t2 = TaskState.Wait( t )
                    struct( oldd.Add( idx, t2 ), t )
            )
        waitt.Task

    /// <summary>
    ///  Notify task completion by specifying ID.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    /// <param name="res">
    ///  Task execution result.
    /// </param>
    member this.Release ( idx : 'TI ) ( res : 'TR ) : unit =
        match this.UpdateForRelease idx with
        | ValueSome x ->
            x.SetResult res
        | _ ->
            ()

    /// <summary>
    ///  Notify that an exception occurred in a task.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    /// <param name="ex">
    ///  The exception that occurred.
    /// </param>
    member this.SetException ( idx : 'TI ) ( ex : Exception ) : unit =
        match this.UpdateForRelease idx with
        | ValueSome x ->
            x.SetException ex
        | _ ->
            ()

    /// <summary>
    ///  Update when task is completed.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    member private _.UpdateForRelease ( idx : 'TI ) : TaskCompletionSource< 'TR > voption =
        d.Update( fun oldd ->
            let r, v = oldd.TryGetValue idx
            if r then
                match v with
                | TaskState.Wait( x ) ->
                    struct( oldd.Remove idx, ValueSome x )
                | TaskState.Release( x ) ->
                    struct( oldd, ValueNone )
            else
                let t =
                    TaskCompletionSource<'TR>( TaskCreationOptions.RunContinuationsAsynchronously )
                let t2 = TaskState.Release( t )
                struct( oldd.Add( idx, t2 ), ValueSome t )
        )

    /// Registerd item count
    member _.Count = d.obj.Count
