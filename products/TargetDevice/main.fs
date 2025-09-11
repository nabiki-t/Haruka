//=============================================================================
// Haruka Software Storage.
// main.fs : Target device main module.
//

//=============================================================================
// module name

module Haruka.TargetDevice.main

//=============================================================================
// Import declaration

open System
open System.Text
open System.IO

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open System.Threading

//=============================================================================
// module implementation

/// Show instruction message 
let help() : unit =
    printfn "%s. Version %d.%d.%d" Constants.PRODUCT_DESCRIPTION Constants.MAJOR_VERSION Constants.MINOR_VERSION Constants.PRODUCT_RIVISION
    printfn "Copyright (C) nabiki_t. All rights reserved."
    printfn ""
    printfn "Instructions :"
    printfn "    TargetDevice WorkFolderPath"
    printfn ""


/// <summary>
///   Entry point when this module directly stated by user or windows service manager.
/// </summary>
/// <param name="argv">
///   Command arguments. If any arguments are specified, Haruka process starts without windows service manager.
/// </param>
/// <returns>
///   Always return 0. It has no meenings.
/// </returns>
[<EntryPoint>]
let main ( argv : string[] ) : int = 
    let objid = objidx_me.NewID()

    // Initialize HLogger object
    HLogger.SetLogParameters(
        Constants.LOGPARAM_DEF_SOFTLIMIT,
        Constants.LOGPARAM_DEF_HARDLIMIT,
        Constants.DEFAULT_LOG_OUTPUT_CYCLE,
        LogLevel.LOGLEVEL_INFO,
        stderr
    )
    HLogger.Initialize()

    ThreadPool.SetMinThreads( 16, 8 ) |> ignore

    let argstr =
        let sb = new StringBuilder()
        for itr in argv do
            sb.Append "\"" |> ignore
            sb.Append itr |> ignore
            sb.Append "\" " |> ignore
        sb.ToString()
    HLogger.Trace( LogID.I_PROCESS_STARTED, fun g -> g.Gen1( objid, argstr ) )

    if argv.Length <= 0 || argv.[0].Length <= 0 then
        HLogger.Trace( LogID.F_STARTUP_ARGUMENT_ERROR, fun g -> g.Gen1( objid, "Missing arguments." ) )
        eprintfn "ERROR : Missing arguments."
        eprintfn ""
        help()
        exit 0

    if not( Directory.Exists argv.[0] ) then
        HLogger.Trace( LogID.F_STARTUP_ARGUMENT_ERROR, fun g -> g.Gen1( objid, "Specified working directory is not exist." ) )
        eprintfn "ERROR : Specified working directory %s is not exist." argv.[0]
        eprintfn ""
        help()
        exit 0

    try
        // Initialize process environment and start server service.
        let k = new HKiller() :> IKiller
        let sm = new StatusMaster( argv.[0], k, stdin, stdout ) :>IStatus
        sm.Start()

        // Wait for user input.
        sm.ProcessControlRequest().RunSynchronously()

        k.NoticeTerminate()

    with
    | _ as x ->
        // if failed to start service, output log message and terminate the process.
        HLogger.UnexpectedException( fun g -> g.GenExp( objid, x ) )

    // Terminate the current process.
    // the return code has no useful information.
    HLogger.Trace( LogID.I_PROCESS_ENDED, fun g -> g.Gen0 objid )
    0

