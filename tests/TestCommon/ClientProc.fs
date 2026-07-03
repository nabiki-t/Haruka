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
open System.Threading

open Haruka.Constants
open Haruka.Commons
open System.Text

//=============================================================================
// Class implementation


type ClientProc ( m_Proc : Process ) =

    /// <summary>
    ///  Haruka CLI client process wrapper.
    /// </summary>
    /// <param name="workPath">
    ///  Working directory path name.
    /// </param>
    new ( workPath : string ) =
        let bc = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let p = new Process(
            StartInfo = ProcessStartInfo(
                FileName = GlbFunc.clientExePath,
                Arguments = "",
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workPath
            ),
            EnableRaisingEvents = true
        )

        try
            if p.Start() |> not then
                raise <| TestException( "Failed to start client proc." )
        finally
            Thread.CurrentThread.CurrentCulture <- bc

        let prompt = GlbFunc.ReadString p.StandardOutput 4
        if prompt <> "--> " then
            raise <| TestException( sprintf "Next prompt is different from what is expected. Expect=--> , Result=%s" prompt )
        ClientProc p
           

    /// <summary>
    ///  Haruka CLI client process wrapper.
    /// </summary>
    /// <param name="address">
    ///  Specify the IP address or host name to connect to the controller.
    /// </param>
    /// <param name="portNumber">
    ///  Specify the TCP port number to connect to the controller.
    /// </param>
    /// <param name="workPath">
    ///  Working directory path name.
    /// </param>
    new ( address : string, portNumber : int32, workPath : string ) =
        let bc = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
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

        try
            if p.Start() |> not then
                raise <| TestException( "Failed to start client proc." )
        finally
            Thread.CurrentThread.CurrentCulture <- bc

        let prompt = GlbFunc.ReadString p.StandardOutput 4
        if prompt <> "CR> " then
            raise <| TestException( sprintf "Next prompt is different from what is expected. Expect=CR> , Result=%s" prompt )
        ClientProc p

    /// Whether the client process has terminated or not.
    member _.HasExited =
        m_Proc.HasExited

    /// Get the exit code of the client process.
    member _.ExitCode =
        m_Proc.ExitCode

    /// Terminate the client process.
    member _.Kill() =
        m_Proc.Kill()

    /// <summary>
    ///  Execute the command and check whether the response and next prompt are as expected.
    /// </summary>
    /// <param name="command">
    ///  Specify the command to be executed.
    /// </param>
    /// <param name="expect">
    ///  Specifies the expected response string. If no response is expected, specify a zero-length string.
    /// </param>
    /// <param name="nextPrompt">
    ///  Specifies the expected next prompt.
    /// </param>
    member this.RunCommand ( command : string ) ( expect : string ) ( nextPrompt : string ) : unit =
        let r = this.RunCommandGetResp command nextPrompt
        if not ( ( expect.Length = 0 && r.Length = 0 ) || ( expect.Length > 0 && r.Length = 1 ) ) then
            raise <| TestException( sprintf "The number of lines in the response is different from what is expected." )
        if expect.Length > 0 then
            if r.[0].StartsWith expect |> not then
                raise <| TestException( sprintf "The response is different from what is expected. Expect=%s, Result=%s" expect r.[0] )

    /// <summary>
    ///  Execute the command and check whether the next prompt are as expected.
    /// </summary>
    /// <param name="command">
    ///  Specify the command to be executed.
    /// </param>
    /// <param name="nextPrompt">
    ///  Specifies the expected next prompt.
    /// </param>
    /// <returns>
    ///  Returns the response string.
    /// </returns>
    member _.RunCommandGetResp ( command : string ) ( nextPrompt : string ) : string[] =
        m_Proc.StandardInput.WriteLine command

        let vResult = System.Collections.Generic.List<string>()
        let rec loop () : string =
            let c = m_Proc.StandardOutput.Read()
            if c = -1 then
                raise <| TestException( "Unexpected process termination" )
            if ( char c ) = ' ' then
                let lineStr = m_Proc.StandardOutput.ReadLine()
                vResult.Add ( lineStr.TrimStart() )
                loop()
            else
                let sb = StringBuilder()
                sb.Append( char c ) |> ignore
                sb.Append( GlbFunc.ReadString m_Proc.StandardOutput 3 ) |> ignore
                sb.ToString()
        let resultPrompt = loop()
        if resultPrompt <> nextPrompt then
            raise <| TestException( sprintf "Next prompt is different from what is expected. Expect=%s, Result=%s" nextPrompt resultPrompt )
        Seq.toArray vResult

    /// <summary>
    ///  Execute the command and tarminate client process.
    /// </summary>
    /// <param name="command">
    ///  Specify the command to be executed.
    /// </param>
    member _.RunCommandForTerminate ( command : string ) : unit =
        m_Proc.StandardInput.WriteLine command

        let rec loop ( cnt : int32 ) =
            if cnt < 10 then
                Thread.Sleep 10
                if not m_Proc.HasExited then
                    loop ( cnt + 1 )
            else
                raise <| TestException( sprintf "Contrary to expectations, the process continues. Command=%s" command )
        loop 0

    /// <summary>
    ///  Execute the list command and retrieve the index value of the row containing the specified string.
    /// </summary>
    /// <param name="str">
    ///  Criteria string.
    /// </param>
    /// <param name="nextPrompt">
    ///  Specifies the expected next prompt.
    /// </param>
    member this.GetIndexNumber ( str : string ) ( nextPrompt : string ) : int32 =
        let v = this.RunCommandGetResp "list" nextPrompt
        let v2 = v |> Array.filter ( fun itr -> itr.Contains str )
        if v2.Length = 0 then
            raise <| TestException "No objects meet the criteria."
        if v2.Length > 1 then
            raise <| TestException "Multiple objects apply."
        let idx =  v2.[0].Split ":" |> Array.map _.Trim()
        Int32.Parse idx.[0]

    /// <summary>
    ///  Execute the status command and retrieve the status value of the row containing the specified string.
    /// </summary>
    /// <param name="str">
    ///  Criteria string.
    /// </param>
    /// <param name="nextPrompt">
    ///  Specifies the expected next prompt.
    /// </param>
    member this.GetStatus ( str : string ) ( nextPrompt : string ) : string =
        let v = this.RunCommandGetResp "status" nextPrompt
        let v2 = v |> Array.filter ( fun itr -> itr.Contains str )
        if v2.Length = 0 then
            raise <| TestException "No objects meet the criteria."
        if v2.Length > 1 then
            raise <| TestException "Multiple objects apply."
        let idx =  v2.[0].Split ":" |> Array.map _.Trim()
        idx.[0]

    /// <summary>
    ///  Verify the status of the object.
    /// </summary>
    /// <param name="str">
    ///  Criteria string.
    /// </param>
    /// <param name="expStat">
    ///  expected status string.
    /// </param>
    /// <param name="nextPrompt">
    ///  Specifies the expected next prompt.
    /// </param>
    member this.CheckStatus ( str : string ) ( expStat : string ) ( nextPrompt : string ) : unit =
        let stat = this.GetStatus str nextPrompt
        if stat <> expStat then
            raise <| TestException ( sprintf "Status string mismatch. Expected=%s, Result=%s" expStat stat )
