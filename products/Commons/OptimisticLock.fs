//=============================================================================
// Haruka Software Storage.
// OptimisticLock.fs : Definitions of OptimisticLock class
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Threading

//=============================================================================
// Class implementation

/// <summary>
///  Achieving optimistic lock algorithm for reference type objects.
/// </summary>
/// <param name="init">
///  Initial value.
/// </param>
type OptimisticLock< 'T when 'T : not struct >( init : 'T ) =

    /// refelense of the object
    let mutable m_Obj = init

    /// Get a reference to the object.
    member _.obj : 'T =
        Volatile.Read<'T>( &m_Obj )

    /// <summary>
    ///  Update the object.
    /// </summary>
    /// <param name="f">
    ///  An update function that receives the old value and returns the updated value.
    ///  This function may be called multiple times if a contention occurs.
    /// </param>
    /// <returns>
    ///  Pair of old object and updated object.
    /// </returns>
    /// <remarks>
    /// <code>
    /// <![CDATA[
    ///  type ExpRec = { A1 : int }
    ///  let theObj = OptimisticExclusion <ExpRec>( { A1 = 0 } )
    ///  let struct( oldOne, newOne ) =
    ///    theObj.Update ( fun oldVal -> { A1 = oldVal.A1 + 1 } )
    ///  printfn "%d" oldOne.A1 // 0 is displayed.
    ///  printfn "%d" newOne.A1 // 1 is displayed.
    /// ]]>
    /// </code>
    ///  If the update function returns old object is returned as is, update process is canceled.
    ///  In this case, even if there is a conflict in the update process, no retry is performed.
    /// </remarks>
    member this.Update( f : 'T -> 'T ) : struct( 'T * 'T ) =
        // get old value
        let oldV = Volatile.Read<'T>( &m_Obj )
        // create updated object
        let newV = f( oldV )
        // If the old object is returned as is, exit without updating it.
        if Object.ReferenceEquals( newV, oldV ) then
            struct( oldV, newV )
        else
            // exchange the object
            let r = Interlocked.CompareExchange( &m_Obj, newV, oldV )
            if Object.ReferenceEquals( r, oldV ) then
                // if succeed, return the pair of old object and updated object.
                struct( oldV, newV )
            else
                // retry
                this.Update f
    
    /// <summary>
    ///  Update the object.
    /// </summary>
    /// <param name="f">
    ///  An update function that receives the old value and returns the updated value.
    ///  This function may be called multiple times if a contention occurs.
    /// </param>
    /// <returns>
    ///  The function that specified in argument is returns pair of the updated object and any value.
    ///  Update method is returns the above any value.
    /// </returns>
    /// <remarks>
    /// <code>
    /// <![CDATA[
    ///  type ExpRec = { A1 : int }
    ///  let theObj = OptimisticExclusion<ExpRec>( { A1 = 0 } )
    ///  let work =                 // Any value returned by the function specified in the argument is returned.
    ///    theObj.Update ( fun oldVal ->
    ///      let newVal = { A1 = oldVal.A1 + 1 }
    ///      struct( newVal, 99 )   // Returns the updated object and any values.
    ///    )
    ///  printfn "%d" work          // 99 is displayed.
    ///  printfn "%d" theObj.obj.A1 // 1 is displayed.
    /// ]]>
    /// </code>
    ///  If the update function returns old object is returned as is, update process is canceled.
    ///  In this case, even if there is a conflict in the update process, no retry is performed.
    /// </remarks>
    member this.Update<'A>( f : 'T -> struct( 'T * 'A ) ) : 'A =
        // get old value
        let oldV = Volatile.Read<'T>( &m_Obj )
        // create updated object
        let struct( newV, rv ) = f( oldV )
        // If the old object is returned as is, exit without updating it.
        if Object.ReferenceEquals( newV, oldV ) then
            rv
        else
            // exchange the object
            let r = Interlocked.CompareExchange( &m_Obj, newV, oldV )
            if Object.ReferenceEquals( r, oldV ) then
                // if succeed, return the any value returned by function 'f'.
                rv
            else
                // retry
                this.Update f

    
    /// <summary>
    ///  Update the object.
    /// </summary>
    /// <param name="f">
    ///  An update function that receives the old value and returns the updated value.
    ///  This function may be called multiple times if a contention occurs.
    /// </param>
    /// <returns>
    ///  The function that specified in argument is returns pair of the updated object and any value.
    ///  Update method is returns the above any value.
    /// </returns>
    /// <remarks>
    /// <code>
    /// <![CDATA[
    ///  type ExpRec = { A1 : int }
    ///  let r = OptimisticExclusion<ExpRec>( { A1 = 0 } )
    ///  r.Update( fun oldVal retryStat ->
    ///      let nextRetryStat =
    ///          match retryStat with
    ///          | ValueNone ->
    ///              printfn "First try"
    ///              1
    ///          | ValueSome( x ) ->
    ///              printfn "Retry %d" x
    ///              x + 1
    ///      if nextRetryStat < 5 then
    ///          r.Update( fun wo -> { A1 = wo.A1 + 10 } ) |> ignore
    ///      let newVal = {
    ///          A1 = oldVal.A1 + 1
    ///      }
    ///      struct ( newVal, ValueSome nextRetryStat, 99 )
    ///  )
    ///  |> printfn "Updated, result=%d"
    ///  printfn "A1=%d" r.obj.A1
    ///  --------------------------------------------------
    ///  following output are displayed.
    ///  First try
    ///  Retry 1
    ///  Retry 2
    ///  Retry 3
    ///  Retry 4
    ///  Updated, result=99
    ///  A1=41
    /// ]]>
    /// </code>
    ///  If the update function returns old object is returned as is, update process is canceled.
    ///  In this case, even if there is a conflict in the update process, no retry is performed.
    /// </remarks>
    member _.Update<'A, 'B>( f : 'T -> 'A ValueOption -> struct( 'T * 'A ValueOption * 'B ) ) : 'B =
        let rec loop ( rtstat : 'A ValueOption ) =
            // get old value
            let oldV = Volatile.Read<'T>( &m_Obj )
            // create updated object
            let struct( newV, nextRTStat, retval ) = f oldV rtstat
            // If the old object is returned as is, exit without updating it.
            if Object.ReferenceEquals( newV, oldV ) then
                retval
            else
                // exchange the object
                let r = Interlocked.CompareExchange( &m_Obj, newV, oldV )
                if Object.ReferenceEquals( r, oldV ) then
                    // if succeed, return the any value returned by function 'f'.
                    retval
                else
                    // retry
                    loop nextRTStat
        loop ValueNone
