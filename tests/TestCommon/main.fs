module Haruka.Test.main

open System
open System.IO
open System.IO.Pipes
open System.Threading

open Haruka.Commons
open System.Diagnostics

// Defines a process to be used as a stub for MediaCreateProc.
// It impersonates the real MediaCreateProc and responds to standard input/output and standard error output.
// To access input and output from this process from your test cases, use the StubProcCtrl class.
let MediaCreateProcStub( args : string[] ) : int =

    let argsDebugFileName = Environment.GetEnvironmentVariable GlbFunc.ARGS_DEBUG_FILE
    let stdinDebugPipeName = Environment.GetEnvironmentVariable GlbFunc.STDIN_DEBUG_PIPE
    let stdoutDebugPipeName = Environment.GetEnvironmentVariable GlbFunc.STDOUT_DEBUG_PIPE
    let stderrDebugPipeName = Environment.GetEnvironmentVariable GlbFunc.STDERR_DEBUG_PIPE
    let waitDebugPipeName = Environment.GetEnvironmentVariable GlbFunc.WAIT_DEBUG_PIPE

    File.WriteAllLines( argsDebugFileName, args )

    fun () -> task {
        use stdinStream = new NamedPipeClientStream( ".", stdinDebugPipeName, PipeDirection.Out )
        stdinStream.Connect()
        use st = new StreamWriter( stdinStream )
        let mutable line = ""
        line <- stdin.ReadLine()
        while line <> null do
            st.WriteLine line
            st.Flush()
            line <- stdin.ReadLine()
    }
    |> Functions.StartTask

    fun () -> task {
        use stdoutStream = new NamedPipeClientStream( ".", stdoutDebugPipeName, PipeDirection.In )
        stdoutStream.Connect()
        use st = new StreamReader( stdoutStream )
        let mutable line = ""
        line <- st.ReadLine()
        while line <> null do
            stdout.WriteLine line
            stdout.Flush()
            line <- st.ReadLine()
    }
    |> Functions.StartTask

    fun () -> task {
        use stderrStream = new NamedPipeClientStream( ".", stderrDebugPipeName, PipeDirection.In )
        stderrStream.Connect()
        use st = new StreamReader( stderrStream )
        let mutable line = ""
        line <- st.ReadLine()
        while line <> null do
            stderr.WriteLine line
            stderr.Flush()
            line <- st.ReadLine()
    }
    |> Functions.StartTask


    use termWaitStream = new NamedPipeClientStream( ".", waitDebugPipeName, PipeDirection.In )
    termWaitStream.Connect()
    use st = new StreamReader( termWaitStream )
    let line = st.ReadLine()
    let exitNo =
        let r, v = Int32.TryParse line
        if r then v else 0

    exitNo

// Processing to monitor the termination of the parent process and ensure the termination of the controller process.
// Since it monitors standard input, if the parent process terminates, it can detect this and terminate the Controller.
// However, if this process is killed directly ( Process.Kill() is called ), the Controller cannot be terminated.
// The controller can be stopped by closing the standard input before terminating this process.
let ControllerStarter( workPath : string ) : int =

    let controllerExeName = GlbFunc.controllerExePath

    // Start controller process
    let ctrlProc2 = new Process(
        StartInfo = ProcessStartInfo(
            FileName = controllerExeName,
            Arguments = sprintf "SV \"%s\"" workPath,
            CreateNoWindow = false,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            WorkingDirectory = workPath
        ),
        EnableRaisingEvents = true
    )
    ctrlProc2.Start() |> ignore

    fun () -> task {
        while true do
            let s = ctrlProc2.StandardOutput.ReadLine()
            if s <> null then
                stdout.WriteLine s
                stdout.Flush()
    }
    |> Functions.StartTask
        
    fun () -> task {
        while true do
            let s = ctrlProc2.StandardError.ReadLine()
            if s <> null then
                stderr.WriteLine s
                stderr.Flush()
    }
    |> Functions.StartTask

    // Wait for the parent process termination
    let rec loop() =
        let s = stdin.ReadLine()
        if s <> null then loop()
    loop()

    // Terminate the controller launched as a child process
    ctrlProc2.Kill()

    stdout.Flush()
    stderr.Flush()

    // change current directory to the parent of working path
    let parentPath = Path.GetDirectoryName( workPath )
    if parentPath <> null && parentPath <> "" then
        Directory.SetCurrentDirectory parentPath

    // Delete working folder
    GlbFunc.DeleteDir workPath

    0

[<EntryPoint>]
let main ( args : string [] ) : int =

    let argStubProcType = Environment.GetEnvironmentVariable GlbFunc.STUB_PROC_TYPE
    match argStubProcType with
    | "MediaCreateProcStub" ->
        MediaCreateProcStub args
    | "ControllerStarter" ->
        ControllerStarter args.[0]
    | _ ->
        0
