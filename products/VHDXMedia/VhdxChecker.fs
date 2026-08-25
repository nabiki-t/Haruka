//=============================================================================
// Haruka Software Storage.
// VhdxChecker.fs : Implement a function to replay uncommitted logs of a VHDX file.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks

open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  Replay unprocessed logs.
/// </summary>
type VhdxChecker() =

    /// <summary>
    ///  Read data from a specified area of ​​the file, while reflecting updates from the log.
    /// </summary>
    /// <param name="fa">
    ///  File accessor of the VHDX file.
    /// </param>
    /// <param name="log">
    ///  log data.
    /// </param>
    static member private Replay ( fa : FileAccessor ) ( log : LogEntry list ) : Task =
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
    /// <param name="vhdxFile">
    ///  VHDX file structures data.
    /// </param>
    /// <returns>
    ///  Updated mutable header values.
    /// </returns>
    static member FlushLog ( vhdxFile : FileAccessor ) ( structures : VhdxStructures ) : Task<VhdxMutableHeader> =
        task {
            // update file write GUID in header
            let verhd1 = {
                structures.LoadedVarHeader with
                    SequenceNumber = structures.LoadedVarHeader.SequenceNumber + 1UL;
                    FileWriteGuid = Guid.NewGuid();
            }
            let! nextvh = VhdxCommons.UpdateHeader vhdxFile structures.ImmHeader verhd1

            // replay log
            do! VhdxChecker.Replay vhdxFile structures.Log

            // update log GUID in header
            let verhd2 = {
                nextvh with
                    LogGuid = Guid();
            }
            return! VhdxCommons.UpdateHeader vhdxFile structures.ImmHeader verhd2
        }

    /// <summary>
    ///  Replay unprocessed logs.
    /// </summary>
    /// <param name="vhdxFile">
    ///  Check target VHDX file.
    /// </param>
    /// <returns>
    ///  Updated mutable header values.
    /// </returns>
    static member Check ( vhdxFile : FileAccessor ) : Task<VhdxMutableHeader> =
        task {
            // Read VHDX metadata
            let! structures = VhdxReader.ReadVhdx vhdxFile
            return! VhdxChecker.FlushLog vhdxFile structures
        }
