namespace VhdxLibrary

open System
open System.IO
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

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
    /// <param name="fa">
    ///  File accessor of the VHDX file.
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
    static member ReplayLog ( fa : FileAccessor ) ( log : LogEntry list ) : Task =
        task {
            let zeroData = lazy Array.zeroCreate<byte> 4096

            for itrLE in log do
                // set file length
                do! fa.SetFileSize itrLE.LastFileOffset

                for itrDE in itrLE.Descriptors do
                    match itrDE with
                    | LogDescriptor.Data x ->
                        do! fa.Write x.FileOffset ( ArraySegment x.LeadingBytes ) 
                        do! fa.Write ( x.FileOffset + 8UL ) ( ArraySegment itrLE.DataSectors.[ int32 x.ddIndex ] )
                        do! fa.Write ( x.FileOffset + 4092UL ) ( ArraySegment x.TrailingBytes ) 

                    | LogDescriptor.Zero x ->
                        let cnt = x.ZeroLength / 4096UL
                        for i in 0UL .. cnt - 1UL do
                            do! fa.Write ( x.FileOffset + i * 4096UL ) ( ArraySegment zeroData.Value )
        }

    /// <summary>
    ///  Replay unprocessed logs.
    /// </summary>
    /// <param name="vhdxFile">
    ///  Check target VHDX file.
    /// </param>
    static member Check ( vhdxFile : FileAccessor ) : Task =
        task {
            // Read VHDX metadata
            let! metadata = VhdxReader.ReadVhdx vhdxFile

            printfn "========================================================"
            printfn "Replay unprocessed log."
            printfn "Input file name : %s" vhdxFile.FileName

            // update file write GUID in header
            let hd1 = {
                metadata.Header with
                    FileWriteGuid = Guid.NewGuid();
                    SequenceNumber = metadata.Header.SequenceNumber + 1UL;
            }
            let! nextSecNum = VhdxHandler.UpdateHeader vhdxFile hd1

            // replay log
            do! VhdxChecker.ReplayLog vhdxFile metadata.LogInfo

            // update log GUID in header
            let hd2 = {
                metadata.Header with
                    LogGuid = Guid();
                    SequenceNumber = nextSecNum;
            }
            let! _ = VhdxHandler.UpdateHeader vhdxFile hd2
            ()
        }

