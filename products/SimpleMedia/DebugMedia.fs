//=============================================================================
// Haruka Software Storage.
// DebugMedia.fs : Defines DebugMedia class.
// DebugMedia class implement debug functionality.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Concurrent
open System.Collections.Immutable
open System.Threading

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// <summary>
///  Indicates when a debug action should occur.
/// </summary>
type private DebugEvent =
    | TestUnitReady
    | ReadCapacity
    | Read of ( uint64 * uint64 )
    | Write of ( uint64 * uint64 )
    | Format

/// <summary>
///  Indicates what to do when a registered event occurs.
/// </summary>
type private DebugAction =
    | ACA of string
    | LUReset of string
    | Count of ( int * int[] )
    | Delay of int  // ignored for TestUnitReady and ReadCapacity event.
    | Wait          // ignored for TestUnitReady and ReadCapacity event.

[<NoComparison>]
type private DebugRegist = {
    event : DebugEvent;
    action : DebugAction;
}

//=============================================================================
// Class implementation

/// <summary>
///  DebugMedia class definition.
/// </summary>
/// <param name="m_StatusMaster">
///  Interface of StatusMaster instance.
/// </param>
/// <param name="m_Config">
///  Configuration information of this media object.
/// </param>
/// <param name="m_Killer">
///  Killer object that notice terminate request to this object.
/// </param>
/// <param name="m_LUN">
///  LUN of LU which access to this media.
/// </param>
type DebugMedia
    (
        m_StatusMaster : IStatus,
        m_Config : TargetGroupConf.T_DebugMedia,
        m_Killer : IKiller,
        m_LUN : LUN_T
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Peripheral media object
    let m_Peripheral = m_StatusMaster.CreateMedia m_Config.Peripheral m_LUN m_Killer

    /// Debug actions
    let m_Action = OptimisticLock( Map< int, DebugRegist > [||] )

    /// An object for waiting on tasks.
    let m_TaskWaiter = OptimisticLock( ImmutableDictionary< TSIH_T, TaskWaiterWithTag<ITT_T,unit,string> >.Empty )

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            g.Gen2( loginfo, "DebugMedia", "" )
        )

    //=========================================================================
    // Interface method

    interface IMedia with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.Terminate." )
                )
    
        // ------------------------------------------------------------------------
        // Implementation of Initialize method
        override _.Initialize() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.Initialize." )
                )
            m_Action.Update ( fun _ -> Map< int, DebugRegist > [||] ) |> ignore
            m_Peripheral.Initialize()

        // ------------------------------------------------------------------------
        // Implementation of Finalize method
        override _.Closing() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.Closing." )
                )
            m_Action.Update ( fun _ -> Map< int, DebugRegist > [||] ) |> ignore
            m_Peripheral.Closing()

        // ------------------------------------------------------------------------
        // Implementation of TestUnitReady method
        override this.TestUnitReady( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : ASCCd voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.TestUnitReady." )
                )

            // Do debug action
            let act = m_Action.obj
            for itr in act do
                match itr.Value.event with
                | DebugEvent.TestUnitReady ->
                    this.DoActionSync source itr.Value.action // ignore Delay action.
                | _ -> ()

            // Call peripheral media interface.
            m_Peripheral.TestUnitReady initiatorTaskTag source

        // ------------------------------------------------------------------------
        // Implementation of ReadCapacity method
        override this.ReadCapacity( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : uint64 =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.ReadCapacity." )
                )

            // Do debug action
            let act = m_Action.obj
            for itr in act do
                match itr.Value.event with
                | DebugEvent.ReadCapacity ->
                    this.DoActionSync source itr.Value.action // ignore Delay action.
                | _ -> ()

            // Call peripheral media interface.
            m_Peripheral.ReadCapacity initiatorTaskTag source

        // ------------------------------------------------------------------------
        // Implementation of Read method
        override this.Read
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( buffer : ArraySegment<byte> )
            : Task<int> =

            task {
                let lbau64 = blkcnt_me.toUInt64 argLBA
                let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DebugMedia.Read." ) )

                let readBlockCount =
                    let struct( d, r ) = Math.DivRem( uint64 buffer.Count, Constants.MEDIA_BLOCK_SIZE )
                    if r > 0UL then
                        d + 1UL
                    else
                        d

                // Do debug action
                let act = m_Action.obj
                for itr in act do
                    match itr.Value.event with
                    | DebugEvent.Read( s, e ) ->
                        if lbau64 > e || ( lbau64 + readBlockCount - 1UL ) < s then
                            ()
                        else
                            do! this.DoAction source initiatorTaskTag "Read" itr.Value.action
                    | _ -> ()

                // Call peripheral media interface.
                return! m_Peripheral.Read initiatorTaskTag source argLBA buffer
            }

        // ------------------------------------------------------------------------
        // Implementation of Write method
        override this.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( offset : uint64 )
            ( data : ArraySegment<byte> )
            : Task<int> =

            task {
                let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DebugMedia.Write." ) )

                let writeStartLBA = ( blkcnt_me.toUInt64 argLBA ) + ( offset / Constants.MEDIA_BLOCK_SIZE )
                let writeBlockCount = 
                    let struct( d, r ) = Math.DivRem( uint64 data.Count, Constants.MEDIA_BLOCK_SIZE )
                    if r > 0UL then
                        d + 1UL
                    else
                        d

                // Do debug action
                let act = m_Action.obj
                for itr in act do
                    match itr.Value.event with
                    | DebugEvent.Write( s, e ) ->
                        if writeStartLBA > e || ( writeStartLBA + writeBlockCount - 1UL ) < s then
                            ()
                        else
                            do! this.DoAction source initiatorTaskTag "Write" itr.Value.action
                    | _ -> ()

                // Call peripheral media interface.
                return! m_Peripheral.Write initiatorTaskTag source argLBA offset data
            }

        // ------------------------------------------------------------------------
        // Implementation of Format method
        override this.Format( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : Task<unit> =
            task {
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                        let loginfo = struct( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                        g.Gen1( loginfo, "DebugMedia.Format." )
                    )

                // Do debug action
                let act = m_Action.obj
                for itr in act do
                    match itr.Value.event with
                    | DebugEvent.Format ->
                        do! this.DoAction source initiatorTaskTag "Format" itr.Value.action
                    | _ -> ()

                // Call peripheral media interface.
                return! m_Peripheral.Format initiatorTaskTag source
            }

        // ------------------------------------------------------------------------
        // Notify logical unit reset.
        override _.NotifyLUReset ( initiatorTaskTag : ITT_T voption ) ( source : CommandSourceInfo voption ) : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, source, initiatorTaskTag, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.NotifyLUReset." )
                )

            // Call peripheral media interface.
            m_Peripheral.NotifyLUReset initiatorTaskTag source

        // ------------------------------------------------------------------------
        // Media control request.
        override this.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            task {
                match request with
                | MediaCtrlReq.U_Debug( x ) ->
                    let result =
                        match x with
                        | MediaCtrlReq.U_GetAllTraps() ->
                            this.MediaControl_GetAllTraps()
                        | MediaCtrlReq.U_AddTrap( y ) ->
                            this.MediaControl_AddTrap y
                        | MediaCtrlReq.U_ClearTraps() ->
                            this.MediaControl_ClearTraps()
                        | MediaCtrlReq.U_GetCounterValue( y ) ->
                            this.MediaControl_GetCounterValue y
                        | MediaCtrlReq.U_GetTaskWaitStatus() ->
                            this.MediaControl_GetTaskWaitStatus()
                        | MediaCtrlReq.U_Resume( y ) ->
                            this.MediaControl_Resume y
                    return MediaCtrlRes.U_Debug result
//                | _ ->
//                    return MediaCtrlRes.U_Unexpected( sprintf "Unexpected request. File=%s, Line=%d" __SOURCE_FILE__ __LINE__ )
            }

        // ------------------------------------------------------------------------
        // Get block count
        override _.BlockCount = m_Peripheral.BlockCount

        // ------------------------------------------------------------------------
        // Get write protect
        override _.WriteProtect = m_Peripheral.WriteProtect

        // ------------------------------------------------------------------------
        // Media index ID
        override _.MediaIndex = m_Config.IdentNumber

        // ------------------------------------------------------------------------
        // String that descripts this media.
        override _.DescriptString = "Debug Media"

        // ------------------------------------------------------------------------
        // Obtain the total number of read bytes.
        override _.GetReadBytesCount() : ResCountResult[] =
            m_Peripheral.GetReadBytesCount()

        // ------------------------------------------------------------------------
        // Obtain the total number of written bytes.
        override _.GetWrittenBytesCount() : ResCountResult[] =
            m_Peripheral.GetWrittenBytesCount()

        // ------------------------------------------------------------------------
        // Obtain the tick count of read operation.
        override _.GetReadTickCount() : ResCountResult[] =
            m_Peripheral.GetReadTickCount()

        // ------------------------------------------------------------------------
        // Obtain the tick count of write operation.
        override _.GetWriteTickCount() : ResCountResult[] =
            m_Peripheral.GetWriteTickCount()

        // ------------------------------------------------------------------------
        // Get sub media object.
        override _.GetSubMedia() : IMedia list = [ m_Peripheral ]

    //=========================================================================
    // private method

    /// <summary>
    ///  Do action. Delay action is also executed.
    /// </summary>
    /// <param name="source">
    ///  Information about the source of the command.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <param name="methodName">
    ///  A name that represents the task to be performed.
    /// </param>
    /// <param name="a">
    ///  Debug action.
    /// </param>
    member private this.DoAction ( source : CommandSourceInfo ) ( itt : ITT_T ) ( methodName : string ) ( a : DebugAction ) : Task<unit> =
        task {
            match a with
            | DebugAction.Delay( x ) ->
                do! Task.Delay x
            | DebugAction.Wait ->
                let tw =
                    m_TaskWaiter.Update( fun old ->
                        let r, v = old.TryGetValue( source.TSIH )
                        if r then
                            struct( old, v )
                        else
                            let tw = TaskWaiterWithTag<ITT_T,unit,string>()
                            let next = old.Add( source.TSIH, tw )
                            struct( next, tw )
                    )
                do! tw.WaitAndReset( itt, methodName )
            | _ as y ->
                this.DoActionSync source y
        }

    /// <summary>
    ///  Execute any action other than the Delay action.
    /// </summary>
    /// <param name="source">
    ///  Information about the source of the command.
    /// </param>
    /// <param name="a">
    ///  Debug action.
    /// </param>
    member private _.DoActionSync ( source : CommandSourceInfo ) ( a : DebugAction ) : unit =
        match a with
        | DebugAction.ACA( x ) ->
            raise <| SCSIACAException( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.LOGICAL_UNIT_FAILURE, x )
        | DebugAction.LUReset( x ) ->
            raise <| Exception( x )
        | DebugAction.Count( _, x2 ) ->
            Interlocked.Increment( &( x2.[0] ) ) |> ignore
        | DebugAction.Delay( x ) ->
            ()  // ignore DebugAction.Delay action
        | DebugAction.Wait ->
            ()  // ignore DebugAction.Wait action

    /// <summary>
    ///  GetAllTraps media control request.
    /// </summary>
    /// <returns>
    ///  Response data that will be returned to client.
    /// </returns>
    member private _.MediaControl_GetAllTraps () : MediaCtrlRes.T_Debug =
        MediaCtrlRes.U_AllTraps( {
            Trap =
                m_Action.obj.Values
                |> Seq.map DebugMedia.ConvertInternalToResTrap
                |> Seq.toList;
        })

    /// <summary>
    ///  AddTrap media control request.
    /// </summary>
    /// <param name="y" >
    ///  AddTrap argument data.
    /// </param>
    /// <returns>
    ///  Response data that will be returned to client.
    /// </returns>
    member private _.MediaControl_AddTrap ( y : MediaCtrlReq.T_AddTrap ) : MediaCtrlRes.T_Debug =
        m_Action.Update ( fun old ->
            if old.Count >= Constants.DEBUG_MEDIA_MAX_TRAP_COUNT then
                let result2 =
                    MediaCtrlRes.U_AddTrapResult({
                        Result = false;
                        ErrorMessage = "The number of registered traps has exceeded the limit.";
                    })
                struct( old, result2 )
            else
                let errorCheck =
                    match y.Event with
                    | MediaCtrlReq.U_Read( z ) ->
                        z.StartLBA <= z.EndLBA
                    | MediaCtrlReq.U_Write( z ) ->
                        z.StartLBA <= z.EndLBA
                    | _ ->
                        true
                if errorCheck then
                    let next = old.Add( old.Count, DebugMedia.ConvertReqTrapToInternal y )
                    let result2 =
                        MediaCtrlRes.U_AddTrapResult({
                            Result = true;
                            ErrorMessage = "";
                        })
                    struct( next, result2 )
                else
                    let result2 =
                        MediaCtrlRes.U_AddTrapResult({
                            Result = false;
                            ErrorMessage = "Invalid value.";
                        })
                    struct( old, result2 )
        )

    /// <summary>
    ///  ClearTraps media control request.
    /// </summary>
    /// <returns>
    ///  Response data that will be returned to client.
    /// </returns>
    member private _.MediaControl_ClearTraps () : MediaCtrlRes.T_Debug =
        m_Action.Update ( fun _ -> Map< int, DebugRegist > [||] ) |> ignore
        MediaCtrlRes.U_ClearTrapsResult( {
            Result = true;
            ErrorMessage = "";
        })

    /// <summary>
    ///  AddTrap media control request.
    /// </summary>
    /// <param name="index" >
    ///  Counter index value specified in AddTrap media control request.
    /// </param>
    /// <returns>
    ///  Response data that will be returned to client.
    /// </returns>
    /// <remarks>
    ///  If there is no counter that matches the index value, -1 is returned.
    ///  If there are multiple counters that match the index value, the first counter value is returned.
    /// </remarks>
    member private _.MediaControl_GetCounterValue ( index : int ) : MediaCtrlRes.T_Debug =
        let act = m_Action.obj
        let r =
            act.Values
            |> Seq.choose ( fun itr ->
                match itr.action with
                | DebugAction.Count( zi, zv ) when zi = index ->
                    Some( zv.[0] )
                | _ ->
                    None
            )
            |> Seq.toArray
        if r.Length > 0 then
            MediaCtrlRes.U_CounterValue( r.[0] )
        else
            MediaCtrlRes.U_CounterValue( -1 )

    /// <summary>
    ///  GetTaskWaitStatus media control request.
    /// </summary>
    /// <returns>
    ///  Response data that will be returned to client.
    ///  If there are too many waiting tasks, the excess will not be returned.
    /// </returns>
    member private _.MediaControl_GetTaskWaitStatus () : MediaCtrlRes.T_Debug =
        MediaCtrlRes.U_AllTaskWaitStatus( {
            TaskWaitStatus = 
                m_TaskWaiter.obj
                |> Seq.map ( fun itr ->
                    itr.Value.Registered
                    |> Seq.map ( fun struct( itt, _, method ) ->
                        {
                            TSIH = itr.Key;
                            ITT = itt;
                            Description = method;
                        } : MediaCtrlRes.T_TaskWaitStatus
                    )
                )
                |> Seq.concat
                |> Seq.toList
                |> List.truncate Constants.DEBUG_MEDIA_MAX_TASK_WAIT_STATUS;
        })

    /// <summary>
    ///  Resume media control request.
    /// </summary>
    /// <returns>
    ///  Response data that will be returned to client.
    ///  In the current implementation, it always returns a successful completion.
    /// </returns>
    member private _.MediaControl_Resume ( t : MediaCtrlReq.T_Resume ) : MediaCtrlRes.T_Debug =
        let itt = t.ITT
        let r, v = m_TaskWaiter.obj.TryGetValue t.TSIH
        if not r then
            MediaCtrlRes.U_ResumeResult( {
                Result = false;
                ErrorMessage = "Specified task with TSIH does not exist.";
            })
        else
            let ittExist =
                v.Registered
                |> Seq.exists ( fun struct( wi, _, _ ) -> wi = itt )
            if not ittExist then
                MediaCtrlRes.U_ResumeResult( {
                    Result = false;
                    ErrorMessage = "Specified task with ITT does not exist.";
                })
            else
                // Note that Resume operations are always executed serially and do not conflict.
                v.Notify( itt, (), "" )
                MediaCtrlRes.U_ResumeResult( {
                    Result = true;
                    ErrorMessage = "";
                })

    //=========================================================================
    // static method

    /// <summary>
    ///  Convert MediaCtrlReq.T_AddTrap record type to internal structure.
    /// </summary>
    /// <param name="trap">
    ///  Requested data.
    /// </param>
    /// <returns>
    ///  Converted data.
    /// </returns>
    static member private ConvertReqTrapToInternal ( trap : MediaCtrlReq.T_AddTrap ) : DebugRegist =
        let de =
            match trap.Event with
            | MediaCtrlReq.U_TestUnitReady() ->
                DebugEvent.TestUnitReady
            | MediaCtrlReq.U_ReadCapacity() ->
                DebugEvent.ReadCapacity
            | MediaCtrlReq.U_Read( x ) ->
                DebugEvent.Read( x.StartLBA, x.EndLBA )
            | MediaCtrlReq.U_Write( x ) ->
                DebugEvent.Write( x.StartLBA, x.EndLBA )
            | MediaCtrlReq.U_Format() ->
                DebugEvent.Format
        let da =
            match trap.Action with
            | MediaCtrlReq.U_ACA( x ) ->
                DebugAction.ACA( x )
            | MediaCtrlReq.U_LUReset( x ) ->
                DebugAction.LUReset( x )
            | MediaCtrlReq.U_Count( x ) ->
                DebugAction.Count( x, [| 0 |] )
            | MediaCtrlReq.U_Delay( x ) ->
                DebugAction.Delay( x )
            | MediaCtrlReq.U_Wait() ->
                DebugAction.Wait
        {
            event = de;
            action = da;
        }

    /// <summary>
    ///  Convert internal structure to MediaCtrlRes.T_Trap record.
    /// </summary>
    /// <param name="dr">
    ///  Debug event and action record.
    /// </param>
    /// <returns>
    ///  Converted MediaCtrlRes.T_Trap record.
    /// </returns>
    static member private ConvertInternalToResTrap ( dr : DebugRegist ) : ( MediaCtrlRes.T_Trap )  =
        let te =
            match dr.event with
            | DebugEvent.TestUnitReady ->
                MediaCtrlRes.U_TestUnitReady()
            | DebugEvent.ReadCapacity ->
                MediaCtrlRes.U_ReadCapacity()
            | DebugEvent.Read( s, e ) ->
                MediaCtrlRes.U_Read( {
                    StartLBA = s;
                    EndLBA = e;
                })
            | DebugEvent.Write( s, e ) ->
                MediaCtrlRes.U_Write( {
                    StartLBA = s;
                    EndLBA = e;
                })
            | DebugEvent.Format ->
                MediaCtrlRes.U_Format()
        let ta =
            match dr.action with
            | DebugAction.ACA( x ) ->
                MediaCtrlRes.U_ACA( x )
            | DebugAction.LUReset( x ) ->
                MediaCtrlRes.U_LUReset( x )
            | DebugAction.Count( xi, xv ) ->
                MediaCtrlRes.U_Count( { Index = xi; Value = xv.[0]; } )
            | DebugAction.Delay( x ) ->
                MediaCtrlRes.U_Delay( x )
            | DebugAction.Wait ->
                MediaCtrlRes.U_Wait()
        {
            Event = te;
            Action = ta;
        }


