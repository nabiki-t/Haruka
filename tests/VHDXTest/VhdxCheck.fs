namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary
open System.Text

/// <summary>
///  Replay unprocessed logs.
/// </summary>
type VhdxCheck() =

    static member UpdateHeader ( fs : FileStream ) ( header : VhdxHeader ) : uint64 =
        //printfn "Update header LogGuid=%s" ( newLogGuid.ToString "D" )

        let hdrBuf1 : byte[] = Array.zeroCreate 4096
        GlbFunc.WriteUInt32BE hdrBuf1 0u header.Signature
        GlbFunc.WriteUInt32LE hdrBuf1 4u 0u
        GlbFunc.WriteUInt64LE hdrBuf1 8u header.SequenceNumber
        GlbFunc.WriteGuid hdrBuf1 16u header.FileWriteGuid
        GlbFunc.WriteGuid hdrBuf1 32u header.DataWriteGuid
        GlbFunc.WriteGuid hdrBuf1 48u header.LogGuid
        GlbFunc.WriteUInt16LE hdrBuf1 64u header.LogVersion
        GlbFunc.WriteUInt16LE hdrBuf1 66u header.Version
        GlbFunc.WriteUInt32LE hdrBuf1 68u header.LogLength
        GlbFunc.WriteUInt64LE hdrBuf1 72u header.LogOffset
        let checkSum = Crc32C.Compute hdrBuf1
        GlbFunc.WriteUInt32LE hdrBuf1 4u checkSum

        // Update old header
        let oldHeaderOffset = 0x30000UL - header.Offset
        fs.Seek( int64 oldHeaderOffset, SeekOrigin.Begin ) |> ignore
        fs.Write( hdrBuf1 )
        fs.Flush()

        // Update new header
        GlbFunc.WriteUInt32LE hdrBuf1 4u 0u
        GlbFunc.WriteUInt64LE hdrBuf1 8u ( header.SequenceNumber + 1UL )
        let checkSum2 = Crc32C.Compute hdrBuf1
        GlbFunc.WriteUInt32LE hdrBuf1 4u checkSum2
        fs.Seek( int64 header.Offset, SeekOrigin.Begin ) |> ignore
        fs.Write( hdrBuf1 )
        fs.Flush()

        header.SequenceNumber + 2UL

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
                    fs.Write( itrLE.DataSectors.[ int x.ddIndex ], 0, 4084 )
                    fs.Write( x.TrailingBytes, 0, 4 )

                | LogDescriptor.Zero x ->
                    fs.Seek( int64 x.FileOffset, SeekOrigin.Begin ) |> ignore
                    for i = 0 to int x.ZeroLength - 1 do
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
        let nextSecNum = VhdxCheck.UpdateHeader outfs hd1
        outfs.Flush()

        // replay log
        VhdxCheck.ReplayLog outfs metadata.LogInfo
        outfs.Flush()

        // update log GUID in header
        let hd2 = {
            metadata.Header with
                LogGuid = Guid();
                SequenceNumber = nextSecNum;
        }
        VhdxCheck.UpdateHeader outfs hd2 |> ignore
        outfs.Flush()

        outfs.Close()
        outfs.Dispose()
