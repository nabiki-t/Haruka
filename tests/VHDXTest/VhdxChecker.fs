namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary
open System.Text

/// <summary>
///  Replay unprocessed logs.
/// </summary>
type VhdxChecker() =

    /// <summary>
    ///  Read data from a specified area of ​​the file, while reflecting updates from the log.
    /// </summary>
    /// <param name="log">
    ///  log data.
    /// </param>
    /// <param name="fs">
    ///  File stream of the VHDX file.
    /// </param>
    /// <param name="offset">
    ///  The starting position of the range to attempt to acquire data.
    ///  Must be multiple of 4KB.
    /// </param>
    /// <param name="length">
    ///  Data length to retrieve. Must be multiple of 4KB.
    /// </param>
    /// <returns>
    ///  Retrieved data.
    /// </returns>
    static member ReplayLog ( fs : FileStream ) ( log : LogEntry list ) : unit =
        for itrLE in log do
            // set file length
            if uint64 fs.Length <> itrLE.LastFileOffset then
                fs.SetLength( int64 itrLE.LastFileOffset )

            for itrDE in itrLE.Descriptors do
                match itrDE with
                | LogDescriptor.Data x ->
                    fs.Seek( int64 x.FileOffset, SeekOrigin.Begin ) |> ignore
                    fs.Write( x.LeadingBytes, 0, 8 )
                    fs.Write( itrLE.DataSectors.[ int32 x.ddIndex ], 0, 4084 )
                    fs.Write( x.TrailingBytes, 0, 4 )

                | LogDescriptor.Zero x ->
                    fs.Seek( int64 x.FileOffset, SeekOrigin.Begin ) |> ignore
                    for i = 0 to int32 x.ZeroLength - 1 do
                        fs.WriteByte( 0uy )


    /// <summary>
    ///  Replay unprocessed logs.
    /// </summary>
    /// <param name="inputPath">
    ///  Input VHDX file name.
    /// </param>
    static member Check ( inputPath : string ) : unit =

        // Read VHDX metadata
        let metadata = VhdxReader.ReadVhdx inputPath

        printfn "========================================================"
        printfn "Replay unprocessed log."
        printfn "Input file name : %s" inputPath

        use outfs = new FileStream( inputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None )

        // update file write GUID in header
        let hd1 = {
            metadata.Header with
                FileWriteGuid = Guid.NewGuid();
                SequenceNumber = metadata.Header.SequenceNumber + 1UL;
        }
        let nextSecNum = VhdxHandler.UpdateHeader outfs hd1
        outfs.Flush()

        // replay log
        VhdxChecker.ReplayLog outfs metadata.LogInfo
        outfs.Flush()

        // update log GUID in header
        let hd2 = {
            metadata.Header with
                LogGuid = Guid();
                SequenceNumber = nextSecNum;
        }
        VhdxHandler.UpdateHeader outfs hd2 |> ignore
        outfs.Flush()

        outfs.Close()
        outfs.Dispose()
