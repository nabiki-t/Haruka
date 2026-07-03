//=============================================================================
// Haruka Software Storage.
// FileAccessor.fs : It provides the ability to access a single file in parallel.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks

open Haruka.Constants


//=============================================================================
// Class implementation

/// <summary>
/// Enables parallel access to a file.
/// </summary>
/// <param name="m_FileName">
///  Full path name of the file.
/// </param>
/// <param name="m_Multiplicity">
///  Maximum number of simultaneous accesses.
/// </param>
/// <param name="m_ReadOnly">
///  Specify True to open in read-only mode.
/// </param>
/// <param name="factory">
///  Specify the function to use when opening a file.
/// </param>
type FileAccessor ( m_FileName : string, m_Multiplicity : uint32, m_ReadOnly : bool, factory : ( string -> FileMode -> FileAccess -> FileShare -> Stream ) ) =

    do
        if m_Multiplicity <= 0u || m_Multiplicity > Constants.LU_MAX_MULTIPLICITY then
            raise <| ArgumentException( sprintf "m_Duplex must be less than %d" Constants.LU_MAX_MULTIPLICITY, "m_Duplex" )
        if File.Exists( m_FileName ) |> not then
            raise <| FileNotFoundException( "File not found", m_FileName )

    /// Array of streams that access files
    let streams =
        let access =
            if m_ReadOnly then
                FileAccess.Read
            else
                FileAccess.ReadWrite
        Array.init ( int32 m_Multiplicity ) ( fun _ -> factory m_FileName FileMode.Open access FileShare.ReadWrite )

    /// Store currently unused streams
    let streamQueue =
        let q = ConcurrentQueue<Stream>()
        for s in streams do q.Enqueue( s )
        q

    /// <summary>
    ///  Get the stream.
    /// </summary>
    let getFreeStream() : Stream =

        let r, fs = streamQueue.TryDequeue()
        if not r then
            sprintf "No available stream. Exceeds multiplicity %d" m_Multiplicity
            |> InvalidOperationException
            |> raise
        fs

    /// <summary>
    ///  Constructor for normal file access.
    /// </summary>
    /// <param name="argFileName">
    ///  Full path name of the file.
    /// </param>
    /// <param name="argDuplex">
    ///  Maximum number of simultaneous accesses.
    /// </param>
    /// <param name="argReadOnly">
    ///  Specify True to open in read-only mode.
    /// </param>
    new ( argFileName : string, argDuplex : uint32, argReadOnly : bool ) =
        let fact a b c d = File.Open( a, b, c, d ) :> Stream
        new FileAccessor( argFileName, argDuplex, argReadOnly, fact )

    /// <summary>
    /// Close the file
      /// </summary>
    member _.Close() : unit =
        for i = 0 to streams.Length - 1 do
            let p = Interlocked.Exchange( &streams.[i], null )
            if p <> null then
                try
                    p.Close()
                    p.Dispose()
                with _ -> ()
        streamQueue.Clear()

    /// <summary>
    ///  Read data from the file.
    /// </summary>
    /// <param name="startPos">
    ///  Position in bytes where data is read.
    /// </param>
    /// <param name="buffer">
    ///  The buffer to store the read data.
    /// </param>
    member _.Read ( startPos : uint64 ) ( buffer : ArraySegment<byte> ) : Task<unit> =
        task {
            let bytesToRead = uint64 buffer.Count
            let fs = getFreeStream()
            try
                let flength = uint64 fs.Length
                if flength < startPos || flength < bytesToRead || flength - bytesToRead < startPos then
                    raise <| ArgumentOutOfRangeException( "Out of range for file access." )

                fs.Seek( int64 startPos, SeekOrigin.Begin ) |> ignore

                let exc : Exception -> bool =
                    function | :? IOException -> false | _ -> true
                let! r = Functions.RetryAsync2 ( fun () -> task {
                                do! fs.ReadExactlyAsync buffer
                            } ) exc

                if r.IsError then
                    Functions.GetErrorValue "" r |> IOException |> raise
            finally
                streamQueue.Enqueue fs
        }

    /// <summary>
    ///  Read data to the file.
    /// </summary>
    /// <param name="startPos">
    ///  Position in bytes where data is written.
    /// </param>
    /// <param name="buffer">
    ///  The data to be written.
    /// </param>
      member _.Write ( startPos : uint64 ) ( buffer : ArraySegment<byte> ) : Task<unit> =
        task {
            if m_ReadOnly then
                raise <| InvalidOperationException( "File opened read-only; Write not allowed" )

            let bytesToWrite = uint64 buffer.Count
            let fs = getFreeStream()
            try
                let flength = uint64 fs.Length
                if flength < startPos || flength < bytesToWrite || flength - bytesToWrite < startPos then
                    raise <| ArgumentOutOfRangeException( "Out of range for file access." )

                fs.Seek( int64 startPos, SeekOrigin.Begin ) |> ignore

                let exc : Exception -> bool =
                    function | :? IOException -> false | _ -> true
                let! r =
                    Functions.RetryAsync2 ( fun () -> task {
                        do! fs.WriteAsync buffer
                        do! fs.FlushAsync()
                    } ) exc

                if r.IsError then
                    Functions.GetErrorValue "" r |> IOException |> raise
            finally
                streamQueue.Enqueue fs
        }

    /// <summary>
    ///  Set the file size in bytes.
    /// </summary>
    /// <param name="size">
    ///  File size in bytes.
    /// </param>
    member _.SetFileSize( size : uint64 ) : Task<unit> =
        task {
            if m_ReadOnly then
                raise <| InvalidOperationException( "File opened read-only; Write not allowed" )
            if ( int64 size ) < 0L then
                raise <| ArgumentOutOfRangeException( "The size value is excessive." )

            let fs = getFreeStream()
            try
                let exc : Exception -> bool =
                    function | :? IOException -> false | _ -> true
                let! r =
                    Functions.RetryAsync2 ( fun () -> task {
                        fs.SetLength( int64 size )
                        do! fs.FlushAsync()
                    } ) exc
                if r.IsError then
                    Functions.GetErrorValue "" r |> IOException |> raise
            finally
                streamQueue.Enqueue fs
        }

    /// <summary>
    ///  Get current file size in bytes.
    /// </summary>
    /// <returns>
    ///  Current file size in bytes.
    /// </returns>
    member _.GetFileSize() : uint64 =
        let fs = getFreeStream()
        try
            uint64 fs.Length
        finally
            streamQueue.Enqueue fs
