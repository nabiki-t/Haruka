namespace Haruka.Test

open System
open System.IO
open System.Threading
open System.Diagnostics

open Xunit

open Haruka
open Haruka.Commons

type TestFunctions() =

    // Start Haruka controller and client process.
    static member StartHarukaController( workPath : string ) ( controllPortNo : int ) : ( Process * ClientProc ) =
        let stdOutLogName = Functions.AppendPathName workPath "stdout.txt"
        let stdErrLogName = Functions.AppendPathName workPath "stderr.txt"
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
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workPath
            ),
            EnableRaisingEvents = true
        )

        let stdoutLog = File.Open( stdOutLogName, FileMode.Create, FileAccess.Write, FileShare.Read )
        let stderrLog = File.Open( stdErrLogName, FileMode.Create, FileAccess.Write, FileShare.Read )

        Assert.True(( ctrlProc2.Start() ))

        fun () -> task {
            do! ctrlProc2.StandardOutput.BaseStream.CopyToAsync stdoutLog
        }
        |> Functions.StartTask
        
        fun () -> task {
            do! ctrlProc2.StandardError.BaseStream.CopyToAsync stderrLog
        }
        |> Functions.StartTask

        // Start client process
        let clientProc = ClientProc( "::1", controllPortNo, workPath )

        //Thread.Sleep 1000
        Assert.False(( ctrlProc2.HasExited ))
        Assert.False(( clientProc.HasExited ))

        ( ctrlProc2, clientProc )
