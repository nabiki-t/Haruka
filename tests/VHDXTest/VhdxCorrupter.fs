namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary
open System.Text

/// A class that updates an existing VHDX file,
/// filling the data in the specified sectors
/// with random numbers while recording the original data as an unprocessed log.
type VhdxCorrupter() =

    /// <summary>
    ///  Generate data descriptors for log entries
    /// </summary>
    /// <param name="offset">
    /// The location in the file where the data to be updated is recorded.
    /// </param>
    /// <param name="data">
    ///  Updated data. Must be 4096 bytes.
    /// </param>
    /// <param name="sequenceNumber">
    ///  Sequence number of the log entry.
    /// </param>
    /// <returns>
    ///  Bytes array of the data sector.
    /// </returns>
    static member CreateLogEntry_DataSector ( offset : SEC4K_T ) ( data : byte[] ) ( sequenceNumber : uint64 ) : byte[] =
        let v = Array.zeroCreate<byte> 32
        let TrailingBytes = data.[ data.Length - 4 .. ]
        let LeadingBytes = data.[ 0 .. 7 ]

        printfn "    Data sector"
        printfn "    Trailing Bytes : %s" ( TrailingBytes |> Array.map ( sprintf "%02X" ) |> String.concat "" )
        printfn "    Leading Bytes : %s" ( LeadingBytes |> Array.map ( sprintf "%02X" ) |> String.concat "" )
        printfn "    File offset : %d" offset
        printfn "    Sequence number : %d" sequenceNumber

        Array.blit ( Encoding.UTF8.GetBytes "desc" ) 0 v 0 4
        Array.blit TrailingBytes 0 v 4 4
        Array.blit LeadingBytes 0 v 8 8
        GlbFunc.WriteUInt64LE v 16u ( uint64 offset * 4096UL )
        GlbFunc.WriteUInt64LE v 24u sequenceNumber
        v

    /// <summary>
    ///  Create log entry.
    /// </summary>
    /// <param name="data">
    ///  The pair of the recording location in the file and the updated data.
    /// </param>
    /// <param name="fail">
    ///  Value of tail in log entry.
    /// </param>
    /// <param name="secnum">
    ///  Sequence number value of the log entry.
    /// </param>
    /// <param name="logGuid">
    ///  Log GUID value.
    /// </param>
    /// <param name="argFFO">
    ///  Flashed file offset value of the log entry.
    /// </param>
    /// <param name="argLFO">
    ///  Last file offset value of the log entry.
    /// </param>
    /// <returns>
    ///  Created bytes array of the log log entry.
    /// </returns>
    static member CreateLogEntry
            ( data : struct ( SEC4K_T * byte[] ) list )
            ( tail : uint32 )
            ( secnum : uint64 )
            ( logGuid : Guid )
            ( argFFO : uint64 )
            ( argLFO : uint64 ) : byte[] =

        let descNum = uint32 data.Length    // Number of descriptor
        let descSecLen =                    // Bytes length of the descriptor sector.
            let a = descNum * 32u + 64u
            ( ( a + 4095u ) / 4096u ) * 4096u
        let entryLength = descSecLen + ( descNum * 4096u )   // Bytes length of the log entry.
        let logEntry = Array.zeroCreate<byte> ( int entryLength )

        printfn "=== CreateLogEntry ==="
        printfn "Number of descriptor : %d" descNum
        printfn "Bytes length of the descriptor sector : %d" descSecLen
        printfn "Bytes length of the log entry : %d" entryLength
        printfn "tail : %d" tail
        printfn "Sequence number : %d" secnum
        printfn "Log GUID : %s" ( logGuid.ToString "D" )
        printfn "Flashed file offset : %d" argFFO
        printfn "Last file offset : %d" argLFO

        // Entry header
        Array.blit ( Encoding.UTF8.GetBytes "loge" ) 0 logEntry 0 4    // Signature
        GlbFunc.WriteUInt32LE logEntry 4u 0u            // Checksum
        GlbFunc.WriteUInt32LE logEntry 8u entryLength   // Entry length
        GlbFunc.WriteUInt32LE logEntry 12u tail         // tail
        GlbFunc.WriteUInt64LE logEntry 16u secnum       // Sequence number
        GlbFunc.WriteUInt32LE logEntry 24u descNum      // Number of descriptors
        GlbFunc.WriteUInt32LE logEntry 28u 0u           // Reserved
        GlbFunc.WriteGuid logEntry 32u logGuid          // Log GUID
        GlbFunc.WriteUInt64LE logEntry 48u argFFO       // Flashed file offset
        GlbFunc.WriteUInt64LE logEntry 56u argLFO       // ast file offset

        // descriptors
        data
        |> List.map ( fun struct ( o, d ) -> VhdxCorrupter.CreateLogEntry_DataSector o d secnum )
        |> List.iteri ( fun idx itr -> Array.blit itr 0 logEntry ( 64 + idx * 32 ) 32 )

        // Data sectores
        data
        |> List.iteri ( fun idx struct ( _, itr ) ->
            let pos = descSecLen + uint32( idx * 4096 )
            Array.blit ( Encoding.UTF8.GetBytes "data" ) 0 logEntry ( int pos ) 4
            GlbFunc.WriteUInt32LE logEntry ( pos + 4u ) ( uint32 ( secnum >>> 32 ) )
            Array.blit itr 8 logEntry ( int pos + 8 ) 4084
            GlbFunc.WriteUInt32LE logEntry ( pos + 4092u ) ( uint32 secnum )
        )

        // Update checksum
        let checkSum = Crc32C.Compute logEntry
        GlbFunc.WriteUInt32LE logEntry 4u checkSum
        printfn "Checksum : 0x%08X" checkSum

        logEntry

    /// <summary>
    ///  Output log entry
    /// </summary>
    /// <param name="fs">
    ///  File stream for VHDX file.
    /// </param>
    /// <param name="metadata">
    ///  Metadata for the VHDX file.
    /// </param>
    /// <param name="offset">
    ///  Offset of log output position within the log area.
    /// </param>
    /// <param name="dummyLogEntry">
    ///  Log entry that writes dummy data.
    /// </param>
    /// <param name="rightLogEntry">
    ///  Log entries that write correct data.
    /// </param>
    static member WriteLogEntry ( fs : FileStream ) ( metadata : VhdxMetadata ) ( offset : uint32 ) ( dummyLogEntry : byte[] list ) ( rightLogEntry: byte[] ) =

        // Combine the output data into a single array.
        let totalLen =
            ( dummyLogEntry |> List.sumBy ( fun itr -> itr.Length ) ) + rightLogEntry.Length
        let v = Array.zeroCreate<byte> totalLen
        let rec loop ( idx : int ) ( pos : int ) : int =
            if idx < dummyLogEntry.Length then
                Array.blit dummyLogEntry.[idx] 0 v pos dummyLogEntry.[idx].Length
                loop ( idx + 1 ) ( pos + dummyLogEntry.[idx].Length )
            else
                pos
        Array.blit rightLogEntry 0 v ( loop 0 0 ) rightLogEntry.Length

        if offset + ( uint32 totalLen ) <= metadata.Header.LogLength then
            fs.Seek( int64 metadata.Header.LogOffset + int64 offset, SeekOrigin.Begin ) |> ignore
            fs.Write( v )
        else
            let len1 = metadata.Header.LogLength - offset   // Length of the first half
            let len2 = ( uint32 v.Length ) - len1           // Length of the second half

            // first half
            fs.Seek( int64 metadata.Header.LogOffset + int64 offset, SeekOrigin.Begin ) |> ignore
            fs.Write( v, 0, int32 len1 )

            // second half
            fs.Seek( int64 metadata.Header.LogOffset, SeekOrigin.Begin ) |> ignore
            fs.Write( v, int32 len1, int32 len2 )


    /// <summary>
    ///  Update the VHDX file, writing random numbers to the specified sectors
    ///  while recording the original data as an unprocessed log.
    /// </summary>
    /// <param name="inputPath">
    ///  Input file name.
    /// </param>
    /// <param name="outputPath">
    ///  Output file name.
    /// </param>
    /// <param name="sectorIndices">
    ///  The index number of the 4K sector where random data should be written.
    /// </param>
    static member Inject ( inputPath : string ) ( outputPath : string ) ( sectorIndices : SEC4K_T list ) : unit =

        let metadata = VhdxReader.ReadVhdx inputPath

        printfn "========================================================"
        printfn "VHDX file inconsistent injection"
        printfn "Input file : %s" inputPath
        printfn "Output file : %s" outputPath
        printfn "Update 4K sectores : %s" ( sectorIndices |> List.map ( sprintf "%d" ) |> String.concat "," )

        // Copy input file to output file.
        File.Delete outputPath
        File.Copy( inputPath, outputPath )

        use fs = new FileStream( outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None )
        let fileSize = fs.Length |> uint64
        printfn "File size : %d" fileSize

        // Update header
        let newLogGuid = Guid.NewGuid()
        let newHeader = {
            metadata.Header with
                SequenceNumber = metadata.Header.SequenceNumber + 1UL;
                FileWriteGuid = Guid.NewGuid();
                LogGuid = newLogGuid;
        }
        VhdxHandler.UpdateHeader fs newHeader |> ignore

        // Fill the log area with random numbers.
        let logSecCnt = metadata.Header.LogLength / 4096u
        let randbuf = Array.zeroCreate<byte> 4096
        fs.Seek( int64 metadata.Header.LogOffset, SeekOrigin.Begin ) |> ignore
        for i = 1 to int logSecCnt do
            Random.Shared.NextBytes randbuf
            fs.Write( randbuf )
        printfn "Log area random number writing"
        printfn "Log offset : %d" metadata.Header.LogOffset
        printfn "Log length : %d" metadata.Header.LogLength
        printfn "Log sector count : %d" logSecCnt

        // Generate dummy update data
        let dummyUpdateData = [
            for i = 0 to 3 do
                yield
                    sectorIndices
                    |> List.map ( fun itr ->
                        let rnddata = Array.zeroCreate<byte> 4096
                        Random.Shared.NextBytes rnddata
                        struct ( itr, rnddata )
                    )
        ]

        // Retrieve data from the specified 4K sectors and
        // fill in the remaining data with random numbers.
        let sectorInfo = [
            let rnddata = Array.zeroCreate<byte> 4096
            Random.Shared.NextBytes rnddata
            for itr in sectorIndices do
                let offset = int64 itr * 4096L
                printfn "Replace 4K sectore(%d, %d)" itr offset
                let readdata = Array.zeroCreate<byte> 4096
                fs.Seek( offset, SeekOrigin.Begin ) |> ignore
                fs.ReadExactly readdata
                fs.Seek( offset, SeekOrigin.Begin ) |> ignore
                fs.Write rnddata
                yield struct ( itr, readdata )
        ]

        // Determine the log writing location.
        let logOutputPos = Random.Shared.Next ( int logSecCnt ) * 4096 |> uint32

        // Generate four dummy log entries.
        let dummyLogEntry =
            dummyUpdateData
            |> Seq.mapi ( fun idx itr ->
                let secn = uint64 idx + 1UL
                VhdxCorrupter.CreateLogEntry itr logOutputPos secn newLogGuid fileSize fileSize
            )
            |> Seq.toList

        // Generate a correct log entry (1 item)
        let rightLogEntry =
            VhdxCorrupter.CreateLogEntry sectorInfo logOutputPos 5UL newLogGuid fileSize fileSize

        printfn "Write log entry"
        printfn "LogOffset = %d" metadata.Header.LogOffset
        printfn "LogLength = %d" metadata.Header.LogLength
        printfn "Log write position = %d" logOutputPos
        printfn "Generated log entry count : %d" ( dummyLogEntry.Length + 1 )

        VhdxCorrupter.WriteLogEntry fs metadata logOutputPos dummyLogEntry rightLogEntry

        fs.Flush()
        fs.Close()
