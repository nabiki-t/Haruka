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
    | Count of int
    | Delay of int  // ignored for TestUnitReady and ReadCapacity event.

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
    let m_Action = List< DebugEvent * DebugAction >()

    /// Debug counters
    let m_Counters = ConcurrentDictionary< int, int >()

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
            m_Action.Clear()
            m_Counters.Clear()
            m_Peripheral.Initialize()

        // ------------------------------------------------------------------------
        // Implementation of Finalize method
        override _.Closing() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "DebugMedia.Closing." )
                )
            m_Action.Clear()
            m_Counters.Clear()
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
            for ( de, da ) in m_Action do
                match de with
                | DebugEvent.TestUnitReady ->
                    this.DoActionSync source da // ignore Delay action.
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
            for ( de, da ) in m_Action do
                match de with
                | DebugEvent.ReadCapacity ->
                    this.DoActionSync source da // ignore Delay action.
                | _ -> ()

            // Call peripheral media interface.
            m_Peripheral.ReadCapacity initiatorTaskTag source

        // ------------------------------------------------------------------------
        // Implementation of Read method
        override this.Read
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : uint64 )
            ( buffer : ArraySegment<byte> )
            : Task<int> =

            task {
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
                for i = 0 to m_Action.Count - 1 do
                    let ( de, da ) = m_Action.[i]
                    match de with
                    | DebugEvent.Read( s, e ) ->
                        if argLBA > e || ( argLBA + readBlockCount - 1UL ) < s then
                            ()
                        else
                            do! this.DoAction source da
                    | _ -> ()

                // Call peripheral media interface.
                return! m_Peripheral.Read initiatorTaskTag source argLBA buffer
            }

        // ------------------------------------------------------------------------
        // Implementation of Write method
        override this.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : uint64 )
            ( offset : uint64 )
            ( data : ArraySegment<byte> )
            : Task<int> =

            task {
                let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DebugMedia.Write." ) )

                let writeStartLBA = argLBA + ( offset / Constants.MEDIA_BLOCK_SIZE )
                let writeBlockCount = 
                    let struct( d, r ) = Math.DivRem( uint64 data.Count, Constants.MEDIA_BLOCK_SIZE )
                    if r > 0UL then
                        d + 1UL
                    else
                        d

                // Do debug action
                for i = 0 to m_Action.Count - 1 do
                    let ( de, da ) = m_Action.[i]
                    match de with
                    | DebugEvent.Write( s, e ) ->
                        if writeStartLBA > e || ( writeStartLBA + writeBlockCount - 1UL ) < s then
                            ()
                        else
                            do! this.DoAction source da
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
                for i = 0 to m_Action.Count - 1 do
                    let ( de, da ) = m_Action.[i]
                    match de with
                    | DebugEvent.Format ->
                        do! this.DoAction source da
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
        override _.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            task {
                match request with
                | MediaCtrlReq.U_Debug( x ) ->
                    let result =
                        match x with
                        | MediaCtrlReq.U_GetAllTraps() ->
                            MediaCtrlRes.U_AllTraps( {
                                Trap = 
                                    m_Action
                                    |> Seq.map ( DebugMedia.ConvertInternalToResTrap m_Counters )
                                    |> Seq.toList;
                            })

                        | MediaCtrlReq.U_AddTrap( y ) ->
                            if m_Action.Count >= Constants.DEBUG_MEDIA_MAX_TRAP_COUNT then
                                MediaCtrlRes.U_AddTrapResult({
                                    Result = false;
                                    ErrorMessage = "The number of registered traps has exceeded the limit.";
                                })
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
                                    m_Action.Add( DebugMedia.ConvertReqTrapToInternal y )
                                    match y.Action with
                                    | MediaCtrlReq.U_Count( z ) ->
                                        // add or clear counter value
                                        m_Counters.AddOrUpdate( z, 0, fun _ _ -> 0 ) |> ignore
                                    | _ -> ()
                                    MediaCtrlRes.U_AddTrapResult({
                                        Result = true;
                                        ErrorMessage = "";
                                    })
                                else
                                    MediaCtrlRes.U_AddTrapResult({
                                        Result = false;
                                        ErrorMessage = "Invalid value.";
                                    })

                        | MediaCtrlReq.U_ClearTraps() ->
                            m_Action.Clear()
                            m_Counters.Clear()
                            MediaCtrlRes.U_ClearTrapsResult( {
                                Result = true;
                                ErrorMessage = "";
                            })

                        | MediaCtrlReq.U_GetCounterValue( y ) ->
                            let r, v = m_Counters.TryGetValue( y )
                            if r then
                                MediaCtrlRes.U_CounterValue( v )
                            else
                                MediaCtrlRes.U_CounterValue( -1 )

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
    /// <param name="a">
    ///  Debug action.
    /// </param>
    member private this.DoAction ( source : CommandSourceInfo ) ( a : DebugAction ) : Task<unit> =
        task {
            match a with
            | DebugAction.Delay( x ) ->
                do! Task.Delay x
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
        | DebugAction.Count( x ) ->
            m_Counters.AddOrUpdate( x, 1, fun _ o ->
                if o = Int32.MaxValue then 0 else o + 1
            )
            |> ignore
        | DebugAction.Delay( x ) ->
            ()  // ignore DebugAction.Delay action

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
    static member private ConvertReqTrapToInternal ( trap : MediaCtrlReq.T_AddTrap ) : ( DebugEvent * DebugAction ) =
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
                DebugAction.Count( x )
            | MediaCtrlReq.U_Delay( x ) ->
                DebugAction.Delay( x )
        ( de, da )

    /// <summary>
    ///  Convert internal structure to MediaCtrlRes.T_Trap record.
    /// </summary>
    /// <param name="counter">
    ///  debug counter
    /// </param>
    /// <param name="de">
    ///  Debug event data
    /// </param>
    /// <param name="da">
    ///  Debug action data
    /// </param>
    /// <returns>
    ///  Converted MediaCtrlRes.T_Trap record.
    /// </returns>
    static member private ConvertInternalToResTrap ( counter : ConcurrentDictionary< int, int > ) ( de : DebugEvent, da : DebugAction ) : ( MediaCtrlRes.T_Trap )  =
        let te =
            match de with
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
            match da with
            | DebugAction.ACA( x ) ->
                MediaCtrlRes.U_ACA( x )
            | DebugAction.LUReset( x ) ->
                MediaCtrlRes.U_LUReset( x )
            | DebugAction.Count( x ) ->
                let r, v = counter.TryGetValue( x )
                if r then
                    MediaCtrlRes.U_Count( { Index = x; Value = v; } )
                else
                    MediaCtrlRes.U_Count( { Index = x; Value = -1; } )
            | DebugAction.Delay( x ) ->
                MediaCtrlRes.U_Delay( x )
        {
            Event = te;
            Action = ta;
        }


