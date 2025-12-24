//=============================================================================
// Haruka Software Storage.
// ControllerFunc.fs : Implementation for ControllerFunc class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Diagnostics
open System.IO

open Haruka.Constants
open Haruka.Commons
open System.Text

//=============================================================================
// Class implementation

type ControllerFunc() =

    /// <summary>
    ///  Initialize Haruka configuration directory
    /// </summary>
    /// <param name="workPath">
    ///  Path name of the working folder.
    /// </param>
    /// <param name="controllPortNo">
    ///  The TCP port number used for client connections to the controller.
    /// </param>
    static member InitializeConfigDir ( workPath : string ) ( controllPortNo : int ) : unit =
        let curdir = Path.GetDirectoryName workPath

        // Initialize Haruka configuration directory
        let ctrlProc1 = new Process(
            StartInfo = ProcessStartInfo(
                FileName = GlbFunc.controllerExePath,
                Arguments = sprintf "ID \"%s\" /p %d /a ::1 /o" workPath controllPortNo,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = curdir
            ),
            EnableRaisingEvents = true
        )
        if ctrlProc1.Start() |> not then
            raise <| TestException( "Failed to start controller proc." )

        if ctrlProc1.WaitForExit( 5000 ) |> not then
            raise <| TestException( "The controller process does not terminate." )

        if ctrlProc1.ExitCode <> 0 then
            raise <| TestException( "The controller process terminated abnormally." )

    /// Start Haruka controller and client process.
    static member StartHarukaController( workPath : string ) ( controllPortNo : int ) : ( Process * ClientProc ) =
        let testCommonExeName = 
            let curExeName = System.Reflection.Assembly.GetEntryAssembly()
            let curExeDir = Path.GetDirectoryName curExeName.Location
            Functions.AppendPathName curExeDir "TestCommon.exe"

        // Initialize Haruka configuration directory
        ControllerFunc.InitializeConfigDir workPath controllPortNo

        // Start controller process
        Environment.SetEnvironmentVariable( GlbFunc.STUB_PROC_TYPE, "ControllerStarter" )
        let ctrlProc2 = new Process(
            StartInfo = ProcessStartInfo(
                FileName = testCommonExeName,
                Arguments = "\"" + workPath + "\"",
                CreateNoWindow = false,
                RedirectStandardError = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                WorkingDirectory = workPath
            ),
            EnableRaisingEvents = true
        )

        if ctrlProc2.Start() |> not then
            raise <| TestException( "Failed to start controller proc." )

        // Start client process
        let clientProc = ClientProc( "::1", controllPortNo, workPath )

        if ctrlProc2.HasExited then
            raise <| TestException( "Unexpected termination the controlelr process." )
        if clientProc.HasExited then
            raise <| TestException( "Unexpected termination the client process." )

        ( ctrlProc2, clientProc )

