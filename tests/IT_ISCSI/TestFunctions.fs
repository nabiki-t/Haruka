//=============================================================================
// Haruka Software Storage.
// TestFunctions.fs : Define global functions used in integration tests.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Diagnostics

open Xunit

open Haruka
open Haruka.Commons

//=============================================================================
// Class implementation

type TestFunctions() =

    // Start Haruka controller and client process.
    static member StartHarukaController( workPath : string ) ( controllPortNo : int ) : ( Process * ClientProc ) =
        let testCommonExeName = 
            let curExeName = System.Reflection.Assembly.GetEntryAssembly()
            let curExeDir = Path.GetDirectoryName curExeName.Location
            Functions.AppendPathName curExeDir "TestCommon.exe"

        // Initialize Haruka configuration directory
        iSCSI_Initiator.InitializeConfigDir workPath controllPortNo

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

        Assert.True(( ctrlProc2.Start() ))

        // Start client process
        let clientProc = ClientProc( "::1", controllPortNo, workPath )

        //Thread.Sleep 1000
        Assert.False(( ctrlProc2.HasExited ))
        Assert.False(( clientProc.HasExited ))

        ( ctrlProc2, clientProc )
