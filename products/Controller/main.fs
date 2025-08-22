//=============================================================================
// Haruka Software Storage.
// main.fs : Haruka controller process main module.
//

module Haruka.Controller.main

open System
open System.Threading
open System.IO

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

/// <summary>
///  Show instruction message.
/// </summary>
let help() : unit =
    fprintfn stderr "%s. Version %d.%d.%d" Constants.PRODUCT_DESCRIPTION Constants.MAJOR_VERSION Constants.MINOR_VERSION Constants.PRODUCT_RIVISION
    fprintfn stderr "Copyright (C) nabiki_t. All rights reserved."
    fprintfn stderr ""
    fprintfn stderr "Instructions :"
    fprintfn stderr "  Controller SV path"
    fprintfn stderr "    Start haruka controller process."
    fprintfn stderr "    SV   : Fixed value."
    fprintfn stderr "    Path : The path name of the working directory."
    fprintfn stderr ""
    fprintfn stderr "  Controller ID path [/p port] [/a pddress] [/o]"
    fprintfn stderr "    Create a work folder and generate an initial definition file."
    fprintfn stderr "    ID      : Fixed value."
    fprintfn stderr "    path    : Path name of the working directory which would be create."
    fprintfn stderr "    port    : TCP port number on which the controller listens for connections."
    fprintfn stderr "    address : Address where the controller listens for connections."
    fprintfn stderr "    /o      : Overwrite if specified directory already exists."
    fprintfn stderr ""
    fprintfn stderr "  Controller IM PLAINFILE /f filename /s filesize [/x]"
    fprintfn stderr "    Create a plain file media file."
    fprintfn stderr "    IM        : Fixed value."
    fprintfn stderr "    PLAINFILE : Fixed value."
    fprintfn stderr "    filename  : Path name of the media file which would be create."
    fprintfn stderr "    filesize  : file size."
    fprintfn stderr "    /x        : Output messages by XML."
    fprintfn stderr ""

/// <summary>
///  Start controller server process.
/// </summary>
/// <param name="cmd">
///  Command line arguments.
/// </param>
let Server( cmd : CommandParser<CtrlCmdType> ) : unit =
    let workDir = cmd.DefaultNamelessString 0 ""

    // lock configuration directory.
    let lockFilePath = Functions.AppendPathName workDir Constants.CONTROLLER_LOCK_NAME
    use lockFile =
        try
            new FileStream( lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 0, FileOptions.DeleteOnClose )
        with
        | :? IOException ->
            fprintfn stderr "Specified configuration directory is already used for another instance."
            fprintfn stderr ""
            exit( 1 )

    // Target device executable file path name.
    let tdExePath = 
        let curExeName = System.Reflection.Assembly.GetEntryAssembly()
        let curExeDir = Path.GetDirectoryName curExeName.Location
        Functions.AppendPathName curExeDir Constants.TARGET_DEVICE_EXE_NAME

    // Init media executable file path name.
    let imExePath = 
        let curExeName = System.Reflection.Assembly.GetEntryAssembly()
        let curExeDir = Path.GetDirectoryName curExeName.Location
        Functions.AppendPathName curExeDir Constants.MEDIA_CREATION_EXE_NAME

    // Process started as normal console application.
    // If it failed to load configurations, en exception will be raised.
    // In this case, this process must be terminate.
    let k = new HKiller() :> IKiller
    let cotr = new Controller( workDir, k, tdExePath, imExePath )
    cotr.LoadInitialTargetDeviceProcs()
    cotr.WaitRequest()

    // Wait infinity
    let s = new SemaphoreSlim(1)
    s.Wait()
    s.Wait()
    k.NoticeTerminate()

    lockFile.Close()
    lockFile.Dispose()

/// <summary>
///  Create default configurations.
/// </summary>
/// <param name="st">
///  Message table.
/// </param>
/// <param name="cmd">
///  Commend line arguments.
/// </param>
let CreateDefaultConfig ( st : StringTable ) ( cmd : CommandParser<CtrlCmdType> ) : unit =
    let dname = cmd.DefaultNamelessString 0 ""
    let port = cmd.DefaultNamedUInt32 "/p" ( uint32 Constants.DEFAULT_MNG_CLI_PORT_NUM )
    let adr = cmd.DefaultNamedString "/a" "::1"
    let ov = cmd.NamedArgs.ContainsKey "/o"

    let dex = Directory.Exists dname
    if dex && ( not ov ) then
        let msg = st.GetMessage( "WORKING_DIR_ALREADY_EXISTS" )
        fprintfn stderr "%s" msg
    else
        if dex then
            let rec loop ( wpath : string ) =
                Directory.GetFiles wpath
                |> Array.iter File.Delete
                Directory.GetDirectories wpath
                |> Array.iter loop
                Directory.Delete wpath
            loop dname

        // create working directory
        Directory.CreateDirectory dname |> ignore

        // Create default controller config file
        let defConf : HarukaCtrlConf.T_HarukaCtrl =
            {
                RemoteCtrl = Some {
                    PortNum = uint16 port;
                    Address = adr;
                    WhiteList = [];
                };
                LogMaintenance = None;
                LogParameters = None;
            }
        let ctrlConfFileName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
        HarukaCtrlConf.ReaderWriter.WriteFile ctrlConfFileName defConf

        // Create log output directory
        let logDirName = Functions.AppendPathName dname Constants.CONTROLLER_LOG_DIR_NAME
        if not ( Directory.Exists logDirName ) then
            Directory.CreateDirectory logDirName |> ignore

[<EntryPoint>]
let main ( args : string[] ) : int =
    let st = StringTable( "Controller" )
    let exitStatus =
        try
            let cmd = CmdArgs.Recognize st args
            match cmd.Varb with
            | Server ->
                Server cmd
                0
            | InitWorkDir ->
                CreateDefaultConfig st cmd
                0
            | InitMedia_PlainFile ->
                if InitMedia.CreatePlainFile stdout st cmd then
                    0
                else
                    1   // failed
        with
        | :? CommandInputError as x ->
            fprintf stderr "%s" x.Message
            fprintf stderr ""
            help()
            exit( 1 )
        | e ->
            fprintf stderr "%s" e.Message
            fprintf stderr ""
            help()
            exit( 1 )
    exitStatus
