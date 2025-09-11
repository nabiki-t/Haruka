//=============================================================================
// Haruka Software Storage.
// MediaCreateProc.fs : Implement MediaCreateProc class to manage the process 
//                 of generating media files.

//=============================================================================
// Namespace declaration

namespace Haruka.Controller

//=============================================================================
// Import declaration

open System
open System.IO
open System.Diagnostics
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition

type MC_PROGRESS =
    | NotStarted
    | ProgressCreation of byte
    | Recovery of byte
    | NormalEnd of DateTime
    | AbnormalEnd of DateTime

//=============================================================================
// Class implementation

/// <summary>
///  Manage the process of generating media files.
/// </summary>
/// <param name="m_MediaInfo">
///  process arguments.
/// </param>
/// <param name="m_ConfPath">
///  The current directory of the child process.
/// </param>
/// <param name="m_InitMediaPath">
///  The executable file name of the program to be launched as a child process.
/// </param>
type MediaCreateProc(
        m_MediaInfo : HarukaCtrlerCtrlReq.T_MediaType,
        m_ConfPath : string,
        m_InitMediaPath : string
    ) =

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// subprocess arguments
    let m_ProcArgs =
        match m_MediaInfo with
        | HarukaCtrlerCtrlReq.U_PlainFile( x ) ->
            sprintf "IM PLAINFILE /f \"%s\" /s %d /x" x.FileName x.FileSize

    /// path name string value, that returns at GetInitMediaStatus request
    let m_PathName =
        match m_MediaInfo with
        | HarukaCtrlerCtrlReq.U_PlainFile( x ) ->
            x.FileName

    /// file type string value, that returns at GetInitMediaStatus request
    /// (Max 32 chars)
    let m_FileTypeStr =
        match m_MediaInfo with
        | HarukaCtrlerCtrlReq.U_PlainFile( x ) ->
            "PlainFile"

    /// process identifier generator
    static let mutable m_Counter = 0UL

    /// process identifier
    /// Note that the m_ObjID is used for log output and is not guaranteed to be unique.
    let m_ProcIdentfier = Interlocked.Increment( &m_Counter )

    /// progress
    let mutable m_Progress = MC_PROGRESS.NotStarted

    /// created files
    let m_CreatedFile = new List<string>()

    /// error messages
    let m_ErrorMessages = new List<string>()

    /// created time
    let m_CreatedTime = DateTime.UtcNow

    /// <summary>
    ///  Procedure for stdout of media creation process.
    /// </summary>
    /// <param name="s">
    ///  stdout of media creation process.
    /// </param>
    /// <returns>
    ///  If the child process has terminated, it returns false. Otherwise true.
    /// </returns>
    /// <remarks>
    ///  This procedure writes messages to log and updates internal status.
    /// </remarks>
    let procMediaCreateStdout ( s : StreamReader ) : Task<bool> =
        task {
            let! line = s.ReadLineAsync()
            if line = null then
                return false
            else
                try
                    let opt = InitMediaMessage.ReaderWriter.LoadString line
                    match opt.LineType with
                    | InitMediaMessage.U_Start( _ ) ->
                        HLogger.Trace( LogID.I_INITMEDIA_PROC_START_MSG, fun g -> g.Gen0 m_ObjID )
                    | InitMediaMessage.U_CreateFile( x ) ->
                        HLogger.Trace( LogID.I_INITMEDIA_PROC_CREATE_FILE, fun g -> g.Gen1( m_ObjID, x ) )
                        m_CreatedFile.Add x
                    | InitMediaMessage.U_Progress( x ) ->
                        HLogger.Trace( LogID.V_INITMEDIA_PROC_PROGRESS, fun g -> g.Gen1( m_ObjID, ( sprintf "%d" x ) ) )
                        m_Progress <- MC_PROGRESS.ProgressCreation x
                    | InitMediaMessage.U_End( x ) ->
                        HLogger.Trace( LogID.I_INITMEDIA_PROC_END_MSG, fun g -> g.Gen1( m_ObjID, x ) )
                    | InitMediaMessage.U_ErrorMessage( x ) ->
                        HLogger.Trace( LogID.W_INITMEDIA_PROC_ERROR_MSG, fun g -> g.Gen1( m_ObjID, x ) )
                        if m_ErrorMessages.Count < Constants.INITMEDIA_MAX_ERRMSG_COUNT then
                            m_ErrorMessages.Add x
                    return true
                with
                | _ ->
                    HLogger.Trace( LogID.W_INITMEDIA_UNEXPECTED_MSG, fun g -> g.Gen1( m_ObjID, line ) )
                    return true
        }

    /// <summary>
    ///  Procedure for stderr of media creation process.
    /// </summary>
    /// <param name="s">
    ///  stderr of media creation process.
    /// </param>
    /// <returns>
    ///  If the child process has terminated, it returns false. Otherwise true.
    /// </returns>
    /// <remarks>
    ///  This procedure writes messages to log.
    /// </remarks>
    let procMediaCreateStderr ( s : StreamReader ) : Task<bool> =
        task {
            let! line = s.ReadLineAsync()
            if line = null then
                return false
            else
                HLogger.Trace( LogID.W_INITMEDIA_PROC_STDERR, fun g -> g.Gen1( m_ObjID, line ) )
                return true
        }

    /// <summary>
    ///  Procedure for 
    /// </summary>
    /// <param name="ec">
    ///  Exit code of media creation process.
    /// </param>
    let procOnExitMediaCreate ( ec : int ) : unit =
        if ec = 0 then
            HLogger.Trace( LogID.I_INITMEDIA_PROC_NORMAL_END, fun g -> g.Gen0 m_ObjID )
            m_Progress <- MC_PROGRESS.NormalEnd( DateTime.UtcNow )
        else
            HLogger.Trace( LogID.W_INITMEDIA_PROC_ABNORMAL_END, fun g -> g.Gen1( m_ObjID, ( sprintf "%d" ec ) ) )
            m_Progress <- MC_PROGRESS.Recovery 0uy
            // Delete all created files.
            for i = 0 to m_CreatedFile.Count - 1 do
                let fname = m_CreatedFile.[i]
                m_Progress <- MC_PROGRESS.Recovery ( byte ( i * 100 / m_CreatedFile.Count ) )
                HLogger.Trace( LogID.I_INITMEDIA_PROC_TRY_DEL_FILE, fun g -> g.Gen1( m_ObjID, fname ) )
                try
                    File.Delete fname
                    HLogger.Trace( LogID.I_INITMEDIA_PROC_DLETED_FILE, fun g -> g.Gen1( m_ObjID, fname ) )
                with
                | :? DirectoryNotFoundException ->
                    HLogger.Trace( LogID.I_INITMEDIA_PROC_DLETED_FILE, fun g -> g.Gen1( m_ObjID, fname ) )
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException 
                | :? IOException as x ->
                    HLogger.Trace( LogID.W_INITMEDIA_PROC_FAIL_DEL_FILE, fun g -> g.Gen2( m_ObjID, fname, x.Message ) )
            m_Progress <- MC_PROGRESS.AbnormalEnd( DateTime.UtcNow )

    /// Proces object
    let m_Proc =
        let p =
            new Process(
                StartInfo = ProcessStartInfo(
                    FileName = m_InitMediaPath,
                    Arguments = m_ProcArgs,
                    CreateNoWindow = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = m_ConfPath
                ),
                EnableRaisingEvents = true
            )

        // Add exited prodecure
        p.Exited.Add ( fun _ -> procOnExitMediaCreate p.ExitCode )

        // ptart subprocess
        HLogger.Trace( LogID.I_TRY_START_INITMEDIA_PROC, fun g -> g.Gen1( m_ObjID, m_ProcArgs ) )
        try
            if p.Start() then
                HLogger.Trace( LogID.I_INITMEDIA_PROC_STARTED, fun g ->
                    let pidStr = p.Id |> sprintf "%d"
                    g.Gen1( m_ObjID, pidStr )
                )

                // start procedure for stdout messages
                Functions.StartTask ( fun () ->
                    Functions.loopAsync ( fun () -> procMediaCreateStdout p.StandardOutput )
                )
                
                // start procedure for stderr messages
                Functions.StartTask ( fun () ->
                    Functions.loopAsync ( fun () -> procMediaCreateStderr p.StandardError )
                )

                Some p
            else
                HLogger.Trace( LogID.W_FAILED_START_INITMEDIA_PROC, fun g -> g.Gen0 m_ObjID )
                None
        with
        | _ ->
            HLogger.Trace( LogID.W_FAILED_START_INITMEDIA_PROC, fun g -> g.Gen0 m_ObjID )
            None

    do
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, "MediaCreateProc", "" ) )

    /// Get progress
    abstract Progress : MC_PROGRESS
    default _.Progress = m_Progress

    /// Get created time
    abstract CreatedTime : DateTime
    default _.CreatedTime = m_CreatedTime

    /// Kill subprocess
    abstract Kill : unit -> unit
    default _.Kill() : unit =
        if m_Proc.IsSome && ( not m_Proc.Value.HasExited ) then
            HLogger.Trace( LogID.I_INITMEDIA_PROC_KILLING, fun g ->
                let pidStr = m_Proc.Value.Id |> sprintf "%d"
                g.Gen1( m_ObjID, pidStr )
            )
            m_Proc.Value.Kill true

    abstract SubprocessHasTerminated : bool
    default _.SubprocessHasTerminated =
        if m_Proc.IsNone then
            true
        else m_Proc.Value.HasExited

    /// get process identifier
    abstract ProcIdentfier : uint64
    default _.ProcIdentfier = m_ProcIdentfier

    /// get error messages
    abstract ErrorMessages : string list
    default _.ErrorMessages = m_ErrorMessages |> Seq.toList

    /// get created files
    abstract CreatedFile : string list
    default _.CreatedFile = m_CreatedFile |> Seq.toList

    /// get path name string
    abstract PathName : string
    default _.PathName = m_PathName

    /// get file type string
    abstract FileTypeStr : string
    default _.FileTypeStr = m_FileTypeStr
