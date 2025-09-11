// Haruka Software Storage.
// LogAggregator.fs : LogAggregator.fs defines LogAggregator class that implements
//                    functions of collect log output from TargetDevice processes.

//=============================================================================
// Namespace declaration

namespace Haruka.Controller

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks.Dataflow

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Class implementation

/// <summary>
///  LogAggregator class.
/// </summary>
/// <param name="m_LogDirPath">
///  Path name of directory that store log files.
/// </param>
/// <param name="m_Config">
///  Log maintenanse configurations.
/// </param>
/// <param name="m_Killer">
///  Killer object
/// </param>
/// <param name="m_DateGetter">
///  function that returns current UTC DateTime value.
/// </param>
/// <param name="m_TickCounter">
///  function that returns integer tick count.
/// </param>
type LogAggregator (
    m_LogDirPath : string,
    m_Config : HarukaCtrlConf.T_LogMaintenance,
    m_Killer : IKiller,
    m_DateGetter : unit -> DateTime,
    m_TickCounter : unit -> int
) as this =

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// log output lines counter
    let mutable m_OutputCounter = 0UL

    /// Log message queue
    let m_MsgQueue = new BufferBlock<string>()

    do
        m_Killer.Add ( this :> IComponent )
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, "LogAggregator", "" ) )

    //-------------------------------------------------------------------------
    // constructor

    /// <summary>
    ///  Standard constructor.
    /// </summary>
    /// <param name="argLogDirPath">
    ///  Path name of directory that store log files.
    /// </param>
    /// <param name="argConfig">
    ///  Log maintenanse configurations.
    /// </param>
    /// <param name="argKiller">
    ///  Killer object
    /// </param>
    new (
        argLogDirPath : string,
        argConfig : HarukaCtrlConf.T_LogMaintenance,
        argKiller : IKiller
    ) =
        let dateGetter() : DateTime = DateTime.UtcNow
        let tickCounter() : int = Environment.TickCount
        LogAggregator( argLogDirPath, argConfig, argKiller, dateGetter, tickCounter )

    //-------------------------------------------------------------------------
    // interface imprementation

    interface IComponent with

        // ------------------------------------------------------------------------
        //   Notince terminate request.
        member _.Terminate() : unit =
            ( m_MsgQueue :> ITargetBlock<string> ).Post "" |> ignore

    /// Start log aggregation procedure
    member this.Initialize() : unit =
        let buf = m_MsgQueue :> ISourceBlock<string>

        HLogger.Trace( LogID.V_TRACE, fun g -> g.Gen1( m_ObjID, "Log aggregation procedure started." ) )

        // log output procedure
        let proc ( s : StreamWriter option, cd : DateTime ) =
            task {
                try
                    // Wait and receive next message
                    let! v = buf.ReceiveAsync()

                    if m_Killer.IsNoticed then
                        if s.IsSome then
                            s.Value.Close()
                            do! s.Value.DisposeAsync()
                        return struct ( false, ( None, cd ) )
                    else
                        match m_Config.OutputDest with
                        | HarukaCtrlConf.U_ToFile( y ) ->
                            let utcNow = m_DateGetter()
                            let currentDate = utcNow.Date

                            // close current file.
                            if cd <> currentDate && s.IsSome then
                                s.Value.Close()
                                do! s.Value.DisposeAsync()

                            // Delete old files
                            if cd <> currentDate then
                                this.MaintainLogFiles()

                            // open next file
                            let nextFile =
                                if cd <> currentDate then
                                    let fname =
                                        Functions.AppendPathName
                                            m_LogDirPath
                                            ( sprintf "%04d%02d%02d.txt" currentDate.Year currentDate.Month currentDate.Day )
                                    try
                                        let newFile = new StreamWriter( File.OpenWrite( fname ) )
                                        newFile.BaseStream.Seek( 0, SeekOrigin.End ) |> ignore
                                        Some( newFile )
                                    with
                                    | _ ->
                                        None
                                else
                                    s

                            // Outpu message
                            match nextFile with
                            | Some( x ) ->
                                do! x.WriteLineAsync( v )
                                if y.ForceSync then
                                    do! x.FlushAsync()
                            | None -> ()
                            return struct ( true, ( nextFile, currentDate ) )
                        | HarukaCtrlConf.U_ToStdout( _ ) ->
                            stdout.WriteLine v
                            return struct ( true, ( s, cd ) )

                with
                | _ ->
                    if s.IsSome then
                        s.Value.Close()
                        do! s.Value.DisposeAsync()
                    // All errors are ignored.
                    return struct ( true, ( None, cd ) )
            }

        Functions.StartTask( fun () ->
            Functions.loopAsyncWithState proc ( None, DateTime() )
            |> Functions.TaskIgnore
        )

    /// <summary>
    ///  Add child process to log aggregation target.
    /// </summary>
    /// <param name="s">
    ///  An end of the pipe to which log messages are written from the TargetDevice process.
    /// </param>
    member _.AddChild( s : StreamReader ) : unit =

        // Update log output counter
        let rec updateLogCounter() =
            let savedTimeSlot = uint64 ( m_TickCounter() / 1000 )
            let init = Interlocked.Read( &m_OutputCounter )
            let currentTimeSlot = init >>> 32
            let wcnt = init &&& 0xFFFFFFFFUL
            let next, result =
                let wTotalLimit =
                    match m_Config.OutputDest with
                    | HarukaCtrlConf.U_ToFile( x ) ->
                        x.TotalLimit
                    | HarukaCtrlConf.U_ToStdout( x ) ->
                        x
                if currentTimeSlot <> savedTimeSlot then
                    // initialize counter
                    ( ( savedTimeSlot <<< 32 ) ||| 1UL ), true
                elif wcnt < uint64 wTotalLimit then
                    // Add counter value
                    ( ( savedTimeSlot <<< 32 ) ||| ( wcnt + 1UL ) ), true
                else
                    // counter is overed from limit value.
                    ( ( savedTimeSlot <<< 32 ) ||| wcnt ), false
            if init = Interlocked.CompareExchange( &m_OutputCounter, next, init ) then
                result
            else
                updateLogCounter()  // retry

        // log collect loop
        let loop() =
            task {
                try
                    let! line = s.ReadLineAsync()
                    if line <> null && updateLogCounter() then
                        let! _ = ( m_MsgQueue :> ITargetBlock<string> ).SendAsync line
                        ()
                    return ( line <> null )
                with
                | _ as x ->
                    // ignore all of error, but terminate collection for this process.
                    return false
            }

        let t() =
           task {
               do! Functions.loopAsync loop
               s.Close()
               s.Dispose()
           }
        Functions.StartTask t

    /// Delete old logfiles.
    member _.MaintainLogFiles() : unit =
        match m_Config.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            let v1 =
                Directory.GetFiles m_LogDirPath
                |> Array.sort
            Array.iter File.Delete v1.[ int x.MaxFileCount .. ]
        | HarukaCtrlConf.U_ToStdout( _ ) ->
            ()
