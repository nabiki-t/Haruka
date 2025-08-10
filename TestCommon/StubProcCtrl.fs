namespace Haruka.Test

open System
open System.IO
open System.IO.Pipes
open System.Threading

open Haruka.Commons
open Haruka.Constants

// A class for simulating the standard input/output and standard error output of a child process using MediaCreateProcStub,
// which is defined as the main function of TestCommon.exe.
type StubProcCtrl( dirName : string ) =

    let m_ArgsDebugFileName =
        let g = Guid.NewGuid()
        let fname = Functions.AppendPathName dirName ( g.ToString() )
        Environment.SetEnvironmentVariable( GlbFunc.ARGS_DEBUG_FILE, fname )
        fname

    let m_StdinDebugPipeName =
        let g = Guid.NewGuid()
        Environment.SetEnvironmentVariable( GlbFunc.STDIN_DEBUG_PIPE, g.ToString() )
        g.ToString()

    let m_StdoutDebugPipeName =
        let g = Guid.NewGuid()
        Environment.SetEnvironmentVariable( GlbFunc.STDOUT_DEBUG_PIPE, g.ToString() )
        g.ToString()

    let m_StderrDebugPipeName =
        let g = Guid.NewGuid()
        Environment.SetEnvironmentVariable( GlbFunc.STDERR_DEBUG_PIPE, g.ToString() )
        g.ToString()

    let m_WaitDebugPipeName =
        let g = Guid.NewGuid()
        Environment.SetEnvironmentVariable( GlbFunc.WAIT_DEBUG_PIPE, g.ToString() )
        g.ToString()

    let m_StdInPipe =
        new NamedPipeServerStream( m_StdinDebugPipeName, PipeDirection.In )

    let m_StdOutPipe =
        new NamedPipeServerStream( m_StdoutDebugPipeName, PipeDirection.Out )

    let m_StdErrPipe =
        new NamedPipeServerStream( m_StderrDebugPipeName, PipeDirection.Out )

    let m_TermWaitPipe =
        new NamedPipeServerStream( m_WaitDebugPipeName, PipeDirection.Out )

    let mutable m_StdInStream : StreamReader = null
    let mutable m_StdOutStream : StreamWriter = null
    let mutable m_StdErrStream : StreamWriter = null
    let mutable m_TermWaitStream : StreamWriter = null

    do
        Environment.SetEnvironmentVariable( GlbFunc.STUB_PROC_TYPE, "MediaCreateProcStub" )

    member _.Wait() : unit =
        m_StdInPipe.WaitForConnection()
        m_StdOutPipe.WaitForConnection()
        m_StdErrPipe.WaitForConnection()
        m_TermWaitPipe.WaitForConnection()
        m_StdInStream <- new StreamReader( m_StdInPipe )
        m_StdOutStream <- new StreamWriter( m_StdOutPipe )
        m_StdErrStream <- new StreamWriter( m_StdErrPipe )
        m_TermWaitStream <- new StreamWriter( m_TermWaitPipe )

    member _.GetStdInResult() : string =
        m_StdInStream.ReadLine()

    member _.SetStdOutResult ( s: string ) : unit =
        m_StdOutStream.WriteLine s
        m_StdOutStream.Flush()

    member _.SetStdErrResult ( s: string ) : unit =
        m_StdErrStream.WriteLine s
        m_StdErrStream.Flush()

    member _.GetArguments() : string[] =
        File.ReadAllLines m_ArgsDebugFileName

    member _.Terminate( e : int ) : unit =
        m_TermWaitStream.WriteLine( sprintf "%d" e )
        m_TermWaitStream.Flush()

    member _.Dispose() : unit =
        m_StdInStream.Dispose()
        m_StdOutStream.Dispose()
        m_StdErrStream.Dispose()
        m_TermWaitStream.Dispose()
        m_StdInPipe.Dispose()
        m_StdOutPipe.Dispose()
        m_StdErrPipe.Dispose()
        m_TermWaitPipe.Dispose()
