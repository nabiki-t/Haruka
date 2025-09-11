//=============================================================================
// Haruka Software Storage.
// Glbfunc.fs : Implementation for ClientProc class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Diagnostics

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

// Haruka CLI client process wrapper.
type ClientProc ( address : string, portNumber : int, workPath : string ) =

    // Start client process
    let m_Proc =
        let p = new Process(
            StartInfo = ProcessStartInfo(
                FileName = GlbFunc.clientExePath,
                Arguments = sprintf "/f /h %s /p %d" address portNumber,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workPath
            ),
            EnableRaisingEvents = true
        )

        if p.Start() |> not then
            raise <| TestException( "Failed to start client proc." )

        //let buf = Array.zeroCreate<char>( 6 )
        let prompt = GlbFunc.ReadString p.StandardOutput 4
        if prompt <> "CR> " then
            raise <| TestException( sprintf "Next prompt is different from what is expected. Expect=CR> , Result=%s" prompt )

        p

    member _.HasExited =
        m_Proc.HasExited

    member _.ExitCode =
        m_Proc.ExitCode

    member _.Kill() =
        m_Proc.Kill()

    member _.GetResponse() =
        m_Proc.StandardOutput.ReadLine()

    member this.RunCommand ( command : string ) ( expect : string ) ( nextPrompt : string ) =
        let expLineCnt = if expect.Length > 0 then 1 else 0
        let r = this.RunCommandGetResp command expLineCnt nextPrompt
        if expect.Length > 0 then
            if r.[0].StartsWith expect |> not then
                raise <| TestException( sprintf "The response is different from what is expected. Expect=%s, Result=%s" expect r.[0] )
        
    member _.RunCommandGetResp ( command : string ) ( lineCnt : int ) ( nextPrompt : string ) : string[] =
        m_Proc.StandardInput.WriteLine command
        let result = [|
            for _ = 1 to lineCnt do
                yield m_Proc.StandardOutput.ReadLine()
        |]
        let resultPrompt = GlbFunc.ReadString m_Proc.StandardOutput 4
        if resultPrompt <> nextPrompt then
            raise <| TestException( sprintf "Next prompt is different from what is expected. Expect=%s, Result=%s" nextPrompt resultPrompt )
        result

