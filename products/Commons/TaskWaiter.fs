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
    /// Indicates that a Wait request has been made but a Notify has not been made.
    | Wait of TaskCompletionSource<'TR>
    /// Indicates that a Notify request has been made but a Wait has not been made.
    | Notify of TaskCompletionSource<'TR>

/// <summary>
///  Implement a function to wait for task completion by specifying a number.
/// </summary>
/// <example>
/// <code>
/// <![CDATA[
/// let w = TaskWaiter< int, int >()
/// (* When Notify is called after Wait, in which case the call to Wait will not return until Notify is called. *)
/// (* thread 1 *)
/// let th1 = fun () -> task {
///   let! res = w.Wait( 0 )
///   printfn "result = %d" res
///   w.Reset()
/// }
/// (* thread 2 *)
/// let th2 = fun () -> task {
///   Thread.Sleep 9999999
///   w.Notify( 0 )
/// }
/// 
/// (* If Wait is called after Notify, the Wait call will finish immediately. *)
/// task {
///   w.Notify( 1 )
///   let! res = w.Wait( 1 )
///   printfn "result = %d" res
///   w.Reset()
/// }
/// ]]>
/// </code>
/// </example>
/// <remarks>
///  TI specifies the data type to be used for the ID.
///  TR specifies the data type that will be the processing result of the task.
/// </remarks>
type TaskWaiter<'TI, 'TR when 'TI : equality>() =

    /// Keeping the generated tasks
    let d = OptimisticLock( ImmutableDictionary< 'TI, TaskState<'TR> >.Empty )

    /// <summary>
    ///  Wait for task completion by specifying ID.
    ///  If the ID has already been notified, the task will immediately terminate.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    /// <returns>
    ///  Task execution result.
    /// </returns>
    member _.Wait ( idx : 'TI ) : Task< 'TR > =
        let waitt =
            d.Update( fun oldd ->
                let r, v = oldd.TryGetValue( idx )
                if r then
                    match v with
                    | TaskState.Wait( x ) ->
                        struct( oldd, x )
                    | TaskState.Notify( x ) ->
                        struct( oldd, x )
                else
                    let t = TaskCompletionSource<'TR>( TaskCreationOptions.RunContinuationsAsynchronously )
                    let t2 = TaskState.Wait( t )
                    struct( oldd.Add( idx, t2 ), t )
            )
        waitt.Task

    /// <summary>
    ///  Wait for task completion by specifying ID.
    ///  If the ID has already been notified, the task will immediately terminate.
    ///  Delete the task when the method finishes.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    /// <returns>
    ///  Task execution result.
    /// </returns>
    member this.WaitAndReset ( idx : 'TI ) : Task< 'TR > =
        task {
            let! r = this.Wait idx
            this.Reset idx
            return r
        }

    /// <summary>
    ///  Notify task completion by specifying ID.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    /// <param name="res">
    ///  Task execution result.
    /// </param>
    member this.Notify ( idx : 'TI ) ( res : 'TR ) : unit =
        match this.UpdateForNotify idx with
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
        match this.UpdateForNotify idx with
        | ValueSome x ->
            x.SetException ex
        | _ ->
            ()

    /// <summary>
    ///  Remove ols task.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    member _.Reset ( idx : 'TI ) : unit =
        d.Update( fun o -> o.Remove idx )
        |> ignore



    /// <summary>
    ///  Notify that an exception to all tasks.
    /// </summary>
    /// <param name="ex">
    ///  The exception that occurred.
    /// </param>
    member _.SetExceptionForAll ( ex : Exception ) : unit =
        let vw =
            d.Update( fun oldd ->
                let newd = 
                    oldd
                    |> Seq.filter ( fun itr -> match itr.Value with TaskState.Notify( _ ) -> true | _ -> false )
                    |> _.ToImmutableDictionary()
                let waits =
                    oldd
                    |> Seq.choose( fun itr ->
                        match itr.Value with
                        | TaskState.Wait( x ) -> Some x
                        | TaskState.Notify( _ ) -> None
                    )
                    |> Seq.toArray
                struct( newd, waits )
            )
        for itr in vw do
            itr.SetException ex


    /// <summary>
    ///  Update when task is completed.
    /// </summary>
    /// <param name="idx">
    ///  Task ID.
    /// </param>
    member private _.UpdateForNotify ( idx : 'TI ) : TaskCompletionSource< 'TR > voption =
        d.Update( fun oldd ->
            let r, v = oldd.TryGetValue idx
            if r then
                match v with
                | TaskState.Wait( x ) ->
                    struct( oldd, ValueSome x )
                | TaskState.Notify( _ ) ->
                    struct( oldd, ValueNone )
            else
                let t =
                    TaskCompletionSource<'TR>( TaskCreationOptions.RunContinuationsAsynchronously )
                let t2 = TaskState.Notify( t )
                struct( oldd.Add( idx, t2 ), ValueSome t )
        )

    /// Registerd item count
    member _.Count = d.obj.Count
