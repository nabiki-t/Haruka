namespace VhdxLibrary

open System
open System.IO
open System.Text
open System.Buffers.Binary

// ============================================================================
/// class implementation of VHDX metadata reader.

type VhdxReader() =

    /// <summary>
    ///  Read file type identifier
    /// </summary>
    /// <param name="fs">
    ///  File stream for opened VHDX file.
    /// </param>
    /// <returns>
    ///  creator string.
    /// </returns>
    static let ReadFileTypeIdentifier ( fs : FileStream ) : string =

        // signature
        let sigBuf = GlbFunc.ReadBytes fs 0UL 8u
        let signature = GlbFunc.ReadUInt64BE sigBuf 0u
        printfn "File type identifier signature : 0x%016X" signature
        if signature <> 0x7668647866696C65UL then
            raise <| Exception( "File type identifier signature mismatch" )

        // Creator
        let rs =
            GlbFunc.ReadBytes fs 8UL 512u
            |> Encoding.Unicode.GetString
            |> _.Replace( "\000", "" )
            |> _.Trim()

        printfn "Creator : %s" rs
        rs

    /// <summary>
    ///  Read headers.
    /// </summary>
    /// <param name="fs">
    ///  File stream for opened VHDX file.
    /// </param>
    /// <returns>
    ///  Retrieved VHDX file headeres.
    /// </returns>
    static let ReadHeaders ( fs : FileStream ) : VhdxHeader list =
        // Read header 0 (offset=0x10000)
        let header0Buf = GlbFunc.ReadBytes fs 0x10000UL 4096u
        let header0 = {
            Signature = GlbFunc.ReadUInt32BE header0Buf 0u;
            Checksum = GlbFunc.ReadUInt32LE header0Buf 4u;
            SequenceNumber = GlbFunc.ReadUInt64LE header0Buf 8u;
            FileWriteGuid = GlbFunc.ReadGuid header0Buf 16u;
            DataWriteGuid = GlbFunc.ReadGuid header0Buf 32u;
            LogGuid = GlbFunc.ReadGuid header0Buf 48u;
            LogVersion = GlbFunc.ReadUInt16LE header0Buf 64u;
            Version = GlbFunc.ReadUInt16LE header0Buf 66u;
            LogLength = GlbFunc.ReadUInt32LE header0Buf 68u;
            LogOffset = GlbFunc.ReadUInt64LE header0Buf 72u;
            Offset = 0x10000UL;
            Index = 0;
        }

        let header0Enable =
            let c0 = GlbFunc.CheckHeaderChecksum header0Buf header0.Checksum && header0.Signature = 0x68656164u
            let c1 = header0.LogVersion = 0us
            let c2 = header0.Version = 1us
            let c3 = header0.LogLength &&& 0x000FFFFFu = 0u             // Multiples of 1MB
            let c4 = header0.LogOffset &&& 0x00000000000FFFFFUL = 0UL   // Multiples of 1MB
            let c5 = ( int header0.LogLength ) >= 0
            let c6 = ( int64 header0.LogOffset ) >= 0L
            let c7 = ( int64 header0.LogOffset ) + ( int64 header0.LogLength ) <= fs.Length
            let c8 = header0.LogOffset + ( uint64 header0.LogLength ) <= 0x0000400000000000UL   // 64TB or less
            c0 && c1 && c2 && c3 && c4 && c5 && c6 && c7 && c8

        printfn "Header 0"
        printfn "  Signature : 0x%08X" header0.Signature
        printfn "  Checksum : 0x%08X" header0.Checksum
        printfn "  Sequence Number : %d" header0.SequenceNumber
        printfn "  File Write Guid : %s" ( header0.FileWriteGuid.ToString( "D" ) )
        printfn "  Data Write Guid : %s" ( header0.DataWriteGuid.ToString( "D" ) )
        printfn "  Log Guid : %s" ( header0.LogGuid.ToString( "D" ) )
        printfn "  Log Version : %d" header0.LogVersion
        printfn "  Version : %d" header0.Version
        printfn "  Log Length : %d" header0.LogLength
        printfn "  Log Offset : %d" header0.LogOffset
        printfn "  Header 0 Offset : %d" header0.Offset
        printfn "  Validity : %s" ( if header0Enable then "valid" else "invalid" )

        // Read header 1 (offset=0x20000)
        let header1Buf = GlbFunc.ReadBytes( fs )( 0x20000UL )( 4096u )
        let header1 = {
            Signature = GlbFunc.ReadUInt32BE header1Buf 0u;
            Checksum = GlbFunc.ReadUInt32LE header1Buf 4u;
            SequenceNumber = GlbFunc.ReadUInt64LE header1Buf 8u;
            FileWriteGuid = GlbFunc.ReadGuid header1Buf 16u;
            DataWriteGuid = GlbFunc.ReadGuid header1Buf 32u;
            LogGuid = GlbFunc.ReadGuid header1Buf 48u;
            LogVersion = GlbFunc.ReadUInt16LE header1Buf 64u;
            Version = GlbFunc.ReadUInt16LE header1Buf 66u;
            LogLength = GlbFunc.ReadUInt32LE header1Buf 68u;
            LogOffset = GlbFunc.ReadUInt64LE header1Buf 72u;
            Offset = 0x20000UL;
            Index = 1;
        }
        let header1Enable =
            let c0 = GlbFunc.CheckHeaderChecksum header1Buf header1.Checksum && header1.Signature = 0x68656164u
            let c1 = header1.LogVersion = 0us
            let c2 = header1.Version = 1us
            let c3 = header1.LogLength &&& 0x000FFFFFu = 0u             // Multiples of 1MB
            let c4 = header1.LogOffset &&& 0x00000000000FFFFFUL = 0UL   // Multiples of 1MB
            let c5 = ( int header1.LogLength ) >= 0
            let c6 = ( int64 header1.LogOffset ) >= 0L
            let c7 = ( int64 header1.LogOffset ) + ( int64 header1.LogLength ) <= fs.Length
            let c8 = header1.LogOffset + ( uint64 header1.LogLength ) <= 0x0000400000000000UL   // 64TB or less
            c0 && c1 && c2 && c3 && c4 && c5 && c6 && c7 && c8

        printfn "Header 1"
        printfn "  Signature : 0x%08X" header1.Signature
        printfn "  Checksum : 0x%08X" header1.Checksum
        printfn "  Sequence Number : %d" header1.SequenceNumber
        printfn "  File Write Guid : %s" ( header1.FileWriteGuid.ToString( "D" ) )
        printfn "  Data Write Guid : %s" ( header1.DataWriteGuid.ToString( "D" ) )
        printfn "  Log Guid : %s" ( header1.LogGuid.ToString( "D" ) )
        printfn "  Log Version : %d" header1.LogVersion
        printfn "  Version : %d" header1.Version
        printfn "  Log Length : %d" header1.LogLength
        printfn "  Log Offset : %d" header1.LogOffset
        printfn "  Header 1 Offset : %d" header1.Offset
        printfn "  Validity : %s" ( if header1Enable then "valid" else "invalid" )

        // Determine which headers to use
        if header0Enable && header1Enable then
            [ header0; header1 ]
        elif header0Enable && not header1Enable then
            [ header0 ]
        elif not header0Enable && header1Enable then
            [ header1 ]
        else
            raise <| Exception( "No valid header exists." )

    /// <summary>
    ///  Read log data sector.
    /// </summary>
    /// <param name="data">
    ///  Log data of the loaded VHDX file.
    /// </param>
    /// <param name="offset">
    ///  Offset in data.
    /// </param>
    /// <param name="seqNum">
    ///  Sequence number of the log entry.
    /// </param>
    /// <returns>
    ///  Values ​​of log data sectors, excluding signature and sequence numbers.
    ///  If there is an error in the data, an array of length 0 is returned.
    /// </returns>
    static let ReadLogDataSector ( data : byte[] ) ( offset : uint32 ) ( seqNum : uint64 ) : byte[] =
        printfn "  ReadLogDataSector( offset=%d )" offset

        let signeture = GlbFunc.ReadUInt32BE data offset
        let sequenceHigh = GlbFunc.ReadUInt32LE data ( offset + 4u )
        let sequenceLow = GlbFunc.ReadUInt32LE data ( offset + 4092u )

        printfn "    Signeture : 0x%08X" signeture
        printfn "    Sequence High : %d" sequenceHigh
        printfn "    Sequence Low : %d" sequenceLow

        let signeture_Check = signeture = 0x64617461u
        if not signeture_Check then printfn "    Invalid signature"

        let sequenceHigh_Check = sequenceHigh = uint32 ( seqNum >>> 32 )
        if not sequenceHigh_Check then printfn "    Invalid sequence high"

        let sequenceLow_Check = sequenceLow = uint32 ( seqNum &&& 0xFFFFFFFFUL )
        if not sequenceLow_Check then printfn "    Invalid sequence low"

        if not signeture_Check || not sequenceHigh_Check || not sequenceLow_Check then
            [||]
        else
            data.[ int offset + 8 .. int offset + 4091 ]

    /// <summary>
    ///  Read log descriptor.
    /// </summary>
    /// <param name="data">
    ///  Log data of the loaded VHDX file.
    /// </param>
    /// <param name="offset">
    ///  Offset in data.
    /// </param>
    /// <param name="dataDescCount">
    ///  Index number of data descriptors.
    /// </param>
    /// <param name="seqNum">
    ///  Sequence number of the log entry.
    /// </param>
    /// <returns>
    ///  Retrieved log descriptor.
    ///  If there is an error in the data, None is returned.
    /// </returns>
    static let ReadLogDescriptor ( data : byte[] ) ( offset : uint32 ) ( dataDescCount : uint32 ) ( seqNum : uint64 ) : LogDescriptor option =

        printfn "  ReadLogDescriptor( offset=%d )" offset

        let signeture = GlbFunc.ReadUInt32BE data offset
        printfn "    Signeture : 0x%08X" signeture

         // Zero descriptor
        if signeture = 0x7A65726Fu then
            let zeroLength = GlbFunc.ReadUInt64LE data ( offset + 8u )
            let fileOffset = GlbFunc.ReadUInt64LE data ( offset + 16u )
            let sequenceNumber = GlbFunc.ReadUInt64LE data ( offset + 24u )

            printfn "    Zero Length : %d" zeroLength
            printfn "    File Offset : %d" fileOffset
            printfn "    Sequence Number : %d" sequenceNumber

            let zeroLength_Check = ( zeroLength &&& 0x0000000000000FFFUL ) = 0UL
            if not zeroLength_Check then printfn "    Invalid zero length"

            let fileOffset_Check = ( fileOffset &&& 0x0000000000000FFFUL ) = 0UL
            if not fileOffset_Check then printfn "    Invalid file offset"

            let sequenceNumber_Check = seqNum = sequenceNumber
            if not sequenceNumber_Check then printfn "    Invalid sequence number"

            if zeroLength_Check && fileOffset_Check && sequenceNumber_Check then
                {
                    ZeroSignature = signeture;
                    ZeroLength = zeroLength;
                    FileOffset = fileOffset;
                    SequenceNumber = sequenceNumber;
                }
                |> LogDescriptor.Zero
                |> Some
            else
                None

        // Data descriptor
        elif signeture = 0x64657363u then
            let trailingBytes = data.[ int offset + 4 .. int offset + 7 ]
            let leadingBytes = data.[ int offset + 8 .. int offset + 15 ]
            let fileOffset = GlbFunc.ReadUInt64LE data ( offset + 16u )
            let sequenceNumber = GlbFunc.ReadUInt64LE data ( offset + 24u )

            printfn "    Trailing Bytes : %s" ( trailingBytes |> Array.map ( sprintf "%02X" ) |> String.concat "," )
            printfn "    Leading Bytes : %s" ( leadingBytes |> Array.map ( sprintf "%02X" ) |> String.concat "," )
            printfn "    File Offset : %d" fileOffset
            printfn "    Sequence Number : %d" sequenceNumber

            let fileOffset_Check = ( fileOffset &&& 0x0000000000000FFFUL ) = 0UL
            if not fileOffset_Check then printfn "    Invalid file offset"

            let sequenceNumber_Check = seqNum = sequenceNumber
            if not sequenceNumber_Check then printfn "  Invalid sequence number"

            if fileOffset_Check && sequenceNumber_Check then
                {
                    DataSignature = signeture;
                    TrailingBytes = trailingBytes;
                    LeadingBytes = leadingBytes;
                    FileOffset = fileOffset;
                    SequenceNumber = sequenceNumber;
                    ddIndex = dataDescCount;
                }
                |> LogDescriptor.Data
                |> Some
            else
                None

        else
            printfn "    Unknown signature in log descriptor"
            None

    /// <summary>
    ///  Read log entry
    /// </summary>
    /// <param name="logData">
    ///  Log data of the loaded VHDX file.
    /// </param>
    /// <param name="pos">
    ///  Offset in logData.
    /// </param>
    /// <param name="headerLogGuid">
    ///  Log Guid value in the header.
    /// </param>
    /// <returns>
    ///  Retrieved log entry value, or None.
    /// </returns>
    static let ReadLogEntry ( logData : byte[] ) ( pos : uint32 ) ( headerLogGuid : Guid ) : LogEntry option =
        printfn "-----------------------"
        printfn "ReadLogEntry( pos=%d )" pos

        // The log data length should be in units of 1MB,
        // and the starting position should be in units of 4KB.
        if ( logData.Length &&& 0x000FFFFF ) <> 0 then
            raise <| Exception( "The log data length is not in units of 1MB." )
        if ( pos &&& 0x00000FFFu ) <> 0u then
            raise <| Exception( "The log sequence start position is not in 4KB units." )

        let wpos = GlbFunc.RapUInt32 pos ( uint32 logData.Length )

        // Retrieve each item in the entry header.
        let signature = GlbFunc.ReadUInt32BE logData wpos
        let checksum = GlbFunc.ReadUInt32LE logData ( wpos + 4u )
        let entryLength = GlbFunc.ReadUInt32LE logData ( wpos + 8u )
        let tail = GlbFunc.ReadUInt32LE logData ( wpos + 12u )
        let sequenceNumber = GlbFunc.ReadUInt64LE logData ( wpos + 16u )
        let descriptorCount = GlbFunc.ReadUInt32LE logData ( wpos + 24u )
        let logGuid = GlbFunc.ReadGuid logData ( wpos + 32u );
        let flushedFileOffset = GlbFunc.ReadUInt64LE logData ( wpos + 48u )
        let lastFileOffset = GlbFunc.ReadUInt64LE logData ( wpos + 56u )

        printfn "  Signature : 0x%08X" signature
        printfn "  Checksum : 0x%08X" checksum
        printfn "  Entry Length : %d" entryLength
        printfn "  Tail : 0x%08X" tail
        printfn "  Sequence Number : %d" sequenceNumber
        printfn "  Descriptor Count : %d" descriptorCount
        printfn "  Log GGuid : %s" ( logGuid.ToString "D" )
        printfn "  Flushed File Offset : %d" flushedFileOffset
        printfn "  Last File Offset : %d" lastFileOffset

        // Verify whether the signature is correct.
        let signature_Check = signature = 0x6C6F6765u
        if not signature_Check then printfn "  Invalid signature"

        // Verify whether the entry length is correct.
        let entryLength_Check =
            ( int entryLength ) >= 0 && int entryLength <= logData.Length && ( entryLength &&& 0x00000FFFu ) = 0u
        if not entryLength_Check then printfn "  Invalid entry length"

        // Verify whether the tail is correct.
        let tail_Check = ( int tail ) >= 0 && int tail < logData.Length && ( tail &&& 0x00000FFFu ) = 0u
        if not tail_Check then printfn "  Invalid tail"

        // Verify whether the number of descriptors is correct.
        let descriptorCount_Check = ( int descriptorCount ) >= 0 && ( 64u + descriptorCount * 32u ) <= uint32 entryLength
        if not descriptorCount_Check then printfn "  Invalid descriptors count"

        // Check if the log GUID is equal to the one in the header.
        let logGuid_Check = logGuid = headerLogGuid
        if not logGuid_Check then printfn "  Invalid log guid"

        // Verify whether the flushed file offset is correct.
        let flushedFileOffset_Check = ( flushedFileOffset &&& 0x00000000000FFFFFUL ) = 0UL
        if not flushedFileOffset_Check then printfn "  Invalid flushed file offset"

        // Verify whether the last file offset is correct.
        let lastFileOffset_Check = ( lastFileOffset &&& 0x00000000000FFFFFUL ) = 0UL
        if not lastFileOffset_Check then printfn "  Invalid last file offset"

        if not signature_Check || not entryLength_Check ||
                not tail_Check || not descriptorCount_Check || not logGuid_Check ||
                not flushedFileOffset_Check || not lastFileOffset_Check then
            None
        else
            // Retrieve all log entry data
            let logEntryData = Array.zeroCreate<byte>( int entryLength )
            for i = 0 to int ( entryLength / 4096u ) - 1 do
                let srcidx = GlbFunc.RapInt32 ( int wpos + i * 4096 ) 0x000FFFFF
                Array.blit logData srcidx logEntryData ( i * 4096 ) 4096

            /// Verify whether the checksum is correct.
            let checksum_Check = GlbFunc.CheckHeaderChecksum logEntryData checksum
            if not checksum_Check then
                printfn "  Invalid checksum"
                None
            else
                // Retrieve the descriptor
                let rec loop ( idx : uint32 ) ( ddcnt : uint32 ) ( r : LogDescriptor list ) : ( LogDescriptor list * uint32 ) =
                    if idx < descriptorCount then
                        let desc = ReadLogDescriptor logEntryData ( 64u + idx * 32u ) ddcnt sequenceNumber
                        match desc with
                        | None ->
                            [], 0u  // failed to retrieve the descriptor
                        | Some ( LogDescriptor.Data _ ) ->
                            loop ( idx + 1u ) ( ddcnt + 1u ) ( desc.Value :: r )
                        | Some ( LogDescriptor.Zero _ ) ->
                            loop ( idx + 1u ) ddcnt ( desc.Value :: r )
                    else
                        ( List.rev r ), ddcnt
                let logDescriptors, dataDescCount = loop 0u 0u []
                if logDescriptors.Length <> int descriptorCount then
                    printfn "Failed to retrieve the descriptor"
                    None
                else
                    // Identify the starting position of the data sector.
                    let dataSectorPos =
                        let a = 64u + descriptorCount * 32u
                        ( ( a + 4095u ) / 4096u ) * 4096u

                    // Find the number of data sectors.
                    let dataSectorCount = ( entryLength - dataSectorPos ) / 4096u

                    printfn "  starting position of the data sector : %d" dataSectorPos
                    printfn "  number of data sectors : %d" dataSectorCount

                    if dataSectorCount <> dataDescCount then
                        printfn "  The number of data sectors and the number of descriptors do not match."
                        None
                    else
                        // Retrieve the data sector
                        let dataSectores = [
                            for i = 0 to int dataSectorCount - 1 do
                                let d = ReadLogDataSector logEntryData ( dataSectorPos + uint32 i * 4096u ) sequenceNumber
                                if d.Length = 4084 then
                                    d
                        ]
                        if dataSectores.Length <> int dataSectorCount then
                            printfn "Failed ReadLogDataSector function"
                            None
                        else
                            printfn "This log entry looks correct..."
                            {
                                Signature = signature;
                                Checksum = checksum;
                                EntryLength = uint32 entryLength;
                                Tail = uint32 tail;
                                SequenceNumber = uint32 sequenceNumber;
                                DescriptorCount = uint32 descriptorCount;
                                LogGuid = logGuid;
                                FlushedFileOffset = flushedFileOffset;
                                LastFileOffset = lastFileOffset;
                                Descriptors = logDescriptors;
                                DataSectors = dataSectores;
                            }
                            |> Some

    /// <summary>
    ///  Read Active Log Sequense
    /// </summary>
    /// <param name="logData">
    ///  Log data of the loaded VHDX file.
    /// </param>
    /// <param name="headerLogGuid">
    ///  Log Guid value in the header.
    /// </param>
    /// <returns>
    ///  Retrieved log entry value list.
    /// </returns>
    static let ReadActiveLogSequense ( logData : byte[] ) ( headerLogGuid : Guid ) : LogEntry list =

        printfn "================================================================"
        printfn "Read Active Log Sequense"

        let rec getCurrentSeq ( pos : uint32 ) ( acc : LogEntry list ) =
            match ReadLogEntry logData pos headerLogGuid with
            | Some x ->
                match acc with
                | h :: _ ->
                    if h.SequenceNumber + 1u = x.SequenceNumber then
                        getCurrentSeq ( pos + x.EntryLength ) ( x :: acc )
                    else
                        printfn "The sequence numbers are not consecutive."
                        acc |> List.rev
                | [] ->
                    getCurrentSeq ( pos + x.EntryLength ) ( x :: acc )
            | None ->
                printfn "Failed to read log entry."
                acc |> List.rev

        let rec getActiveSeq ( activeSeq : LogEntry list ) ( curTail : uint32 ) : LogEntry list =
            printfn "-----------------------"
            printfn "getActiveSeq( activeSeq.Length=%d, curTail=%d )" activeSeq.Length curTail

            // Read current entry
            let curSeq = getCurrentSeq curTail []
            printfn "Retrieved entry count : %d" curSeq.Length

            // Check whether the Tail of each entry falls within the sequence length range from curTail.
            let SeqTotalLen = curSeq |> List.sumBy _.EntryLength
            let r = curSeq |> List.exists ( fun itr -> itr.Tail < curTail || itr.Tail >= curTail + SeqTotalLen )

            if r then
                printfn "The Tail of each entry is not within the sequence length range from curTail."
                printfn "  SeqTotalLen = %d" SeqTotalLen
                printfn "  curTail = %d" curTail

            let nextActiveSeq, nextCurTail =
                if curSeq.Length = 0 || r then
                    printfn "The retrieved sequence is invalid."
                    activeSeq, ( GlbFunc.RapUInt32 ( curTail + 4096u ) ( uint32 logData.Length ) )
                else
                    // The current entry appears to be correct.
                    let asSecNum =
                        if activeSeq.Length = 0 then
                            0u
                        else
                            activeSeq.[0].SequenceNumber
                    let nas =
                        printfn "Sequence number of active sequence : %d" asSecNum
                        printfn "Sequence number of retrieved sequence : %d" curSeq.[0].SequenceNumber

                        if asSecNum < curSeq.[0].SequenceNumber then
                            printfn "The retrieved sequence is valid."
                            curSeq
                        else
                            printfn "The retrieved sequence appears correct, but the sequence number is old."
                            activeSeq
                    nas, ( GlbFunc.RapUInt32 ( curTail + SeqTotalLen ) ( uint32 logData.Length ) )

            if nextCurTail < curTail then
                printfn "Search for active sequences complete."
                nextActiveSeq
            else
                getActiveSeq nextActiveSeq nextCurTail

        getActiveSeq [] 0u

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
    static let ReadBytesWithLog ( log : LogEntry list ) ( fs : FileStream ) ( offset : uint64 ) ( length : uint32 ) : byte[] =
        if offset &&& 0x0000000000000FFFUL <> 0UL then
            raise <| Exception "offset is not in units of 4KB."
        if length &&& 0x00000FFFu <> 0u then
            raise <| Exception "length is not in units of 4KB."

        if length = 0u then
            [||]
        else
            // Read from the file.
            let buf = GlbFunc.ReadBytes fs offset length
            // Reflect updates from the log.
            for itrLE in log do
                for itrDE in itrLE.Descriptors do
                    match itrDE with
                    | LogDescriptor.Data x ->
                        if offset <= x.FileOffset && x.FileOffset < offset + uint64 length then
                            let dstPos = x.FileOffset - offset |> int
                            Array.blit x.LeadingBytes 0 buf dstPos 8
                            Array.blit itrLE.DataSectors.[ int x.ddIndex ] 0 buf ( dstPos + 8 ) 4084
                            Array.blit x.TrailingBytes 0 buf ( dstPos + 4092 ) 4
                    | LogDescriptor.Zero x ->
                        let startIdx = max offset x.FileOffset
                        let endIdx = min ( offset + uint64 length ) ( x.FileOffset + uint64 x.ZeroLength )
                        if endIdx > startIdx then
                            let targetIndex = startIdx - offset |> int
                            let targetLength = endIdx - startIdx |> int
                            Array.fill buf targetIndex targetLength 0uy
            buf

    /// <summary>
    ///  Read Region Table.
    /// </summary>
    /// <param name="data">
    ///  Region table data of the loaded VHDX file.
    /// </param>
    /// <param name="fileLen">
    ///  VHDX file length.
    /// </param>
    /// <returns>
    ///  Retrieved region table data, or None.
    /// </returns>
    static let ReadRegionTable ( data : byte[] ) ( fileLen : uint64 ) : RegionTable option =

        // Interpretation of the region table header
        let signature = GlbFunc.ReadUInt32BE data 0u
        let checksum = GlbFunc.ReadUInt32LE data 4u
        let entryCount = GlbFunc.ReadUInt32LE data 8u
        let signature_Check = signature = 0x72656769u
        let checksum_Check = GlbFunc.CheckHeaderChecksum data checksum
        let entryCount_Check = 0u <= entryCount && entryCount <= 2047u

        printfn "Region table header"
        printfn "  Signature : 0x%08X" signature
        printfn "  Checksum : 0x%08X" checksum
        printfn "  Entry count : %d" entryCount

        if not signature_Check || not checksum_Check || not entryCount_Check then
            None
        else
            // Interpretation of Region table entries.
            let entryCountInt = int entryCount
            let entries =
                [
                    for i = 0 to entryCountInt - 1 do
                        let entryOffset = 16u + uint32 i * 32u
                        let guid = GlbFunc.ReadGuid data entryOffset
                        let fileOffsetBytes = GlbFunc.ReadUInt64LE data ( entryOffset + 16u )
                        let lengthBytes = GlbFunc.ReadUInt32LE data ( entryOffset + 24u )
                        let required = GlbFunc.ReadUInt32LE data ( entryOffset + 28u )

                        printfn "Region table entry(%d)" i
                        printfn "  Entry offset : 0x%08X" entryOffset
                        printfn "  Guid : %s" ( guid.ToString( "D" ) )
                        printfn "  File offset : %d" fileOffsetBytes
                        printfn "  Length : %d" lengthBytes
                        printfn "  Required : %d" required

                        let fileOffsetBytes_Check = ( fileOffsetBytes &&& 0xFFFFFUL ) = 0UL && fileOffsetBytes >= 0x100000UL
                        let lengthBytes_Check1 = ( lengthBytes &&& 0xFFFFFu ) = 0u
                        let lengthBytes_Check2 =
                            let w = fileOffsetBytes + uint64 lengthBytes
                            w <= 0x400000000000UL && w <= fileLen

                        if fileOffsetBytes_Check && lengthBytes_Check1 && lengthBytes_Check2 then
                            yield {
                                Guid = guid;
                                FileOffset = fileOffsetBytes;
                                Length = lengthBytes;
                                Required = required = 1u;
                            }
                ]

            // Check if there is any overlap in the regions.
            let entries_Check =
                entries
                |> List.sortBy _.FileOffset
                |> List.windowed 2
                |> List.exists ( fun itr ->
                    itr.[1].FileOffset < ( itr.[0].FileOffset + uint64 itr.[0].Length ) 
                )
                |> not
            if entries.Length <> entryCountInt || not entries_Check then
                None
            else
                {
                    Signature = signature;
                    Checksum = checksum;
                    EntryCount = entryCount;
                    Entries = entries;
                }
                |> Some

    /// <summary>
    ///  Read metadata area.
    /// </summary>
    /// <param name="data">
    ///  Metadata table data of the loaded VHDX file.
    /// </param>
    /// <returns>
    ///  Retrieved metadata information, or None.
    /// </returns>
    static let ReadMetadata ( data : byte[] ) : VirtualDiskInfo =

        let signature = GlbFunc.ReadUInt64BE data 0u        // signature
        let mtEntryCount = GlbFunc.ReadUInt16LE data 10u    // Entry count

        printfn "Metadata table header."
        printfn "  Signature : 0x%016X" signature
        printfn "  Entry count : %d" mtEntryCount

        if signature <> 0x6D65746164617461UL then
            raise <| Exception( "The signatures in the metadata table do not match." )
        if mtEntryCount > 2047us then
            raise <| Exception( "The number of metadata entries is invalid." )

        // Metadata entries
        let metadataItems =
            [
                for i in 0 .. int mtEntryCount - 1 do
                    let eo = 32u + uint32 i * 32u
                    let itemId = GlbFunc.ReadGuid data eo
                    let offset = GlbFunc.ReadUInt32LE data ( eo + 16u );
                    let length = GlbFunc.ReadUInt32LE data ( eo + 20u );
                    let b = data.[ int eo + 24 ]
                    let isUser = ( b &&& 0x01uy ) <> 0uy
                    let isVirtualDisk = ( b &&& 0x02uy ) <> 0uy
                    let isRequired = ( b &&& 0x04uy ) <> 0uy

                    printfn "Metadata entry(%d)" i
                    printfn "  Item ID : %s" ( itemId.ToString( "D" ) )
                    printfn "  Offset : %d" offset
                    printfn "  Length : %d" length
                    printfn "  IsUser : %b" isUser
                    printfn "  IsVirtualDisk : %b" isVirtualDisk
                    printfn "  IsRequired : %b" isRequired

                    if ( offset = 0u && length = 0u ) then  // It's OK if both the offset and length are 0.
                        yield {
                            ItemId = itemId;
                            Offset = offset;
                            Length = length;
                            IsUser = isUser;
                            IsVirtualDisk = isVirtualDisk;
                            IsRequired = isRequired;
                            Data = Array.empty;
                        }
                    elif offset < 0x10000u ||                           // The offset must be 64KB or more.
                            length < 1u ||                              // Length is 1 byte or more
                            uint32 data.Length  < offset + length ||    // The entire area belongs to the metadata area.
                            0x100000u < length then                     // Length must be 1MB or less
                        ()
                    else
                        yield {
                            ItemId = itemId;
                            Offset = offset;
                            Length = length;
                            IsUser = isUser;
                            IsVirtualDisk = isVirtualDisk;
                            IsRequired = isRequired;
                            Data = data.[ int offset .. int offset + int length - 1 ];
                        }
            ]
        if metadataItems.Length <> int mtEntryCount then
            raise <| Exception( "Invalid metadata entry" )

        let userEntCount =
            metadataItems
            |> List.sumBy ( fun itr -> if itr.IsUser then 1 else 0 )
        if 1024 < userEntCount then
            raise <| Exception( "The number of user entries is incorrect." )

        // Retrieve file parameters
        let fileParamItem =
            metadataItems
            |> List.tryFind ( fun m -> m.ItemId = GlbFunc.METADATA_FILE_PARAM )
        if fileParamItem.IsNone then
            raise <| Exception( "Metadata item(file parameter) missing" )
        if fileParamItem.Value.Length < 8u then
            raise <| Exception( "Invalid Length of metadata item(file parameter)." )
        let payloadBlockSize = GlbFunc.ReadUInt32LE fileParamItem.Value.Data 0u
        let leaveBlockAllocated = ( fileParamItem.Value.Data.[4] &&& 0x01uy ) = 0x01uy
        let hasParent = ( fileParamItem.Value.Data.[4] &&& 0x02uy ) = 0x02uy

        printfn "File parameter(block size) : %d" payloadBlockSize
        printfn "File parameter(A-LeaveBlockAllocated) : %b" leaveBlockAllocated
        printfn "File parameter(B-HasParent) : %b" hasParent

        if payloadBlockSize < 0x100000u ||      // 1MB or more
            0x10000000u < payloadBlockSize ||   // 256MB or less
            ( payloadBlockSize &&& ( payloadBlockSize - 1u ) ) <> 0u then   // Powers of 2
            raise <| Exception( "Incorrect payload block size" )

        // Retrieve the virtual disk size.
        let diskSizeItem =
            metadataItems
            |> List.tryFind ( fun m -> m.ItemId = GlbFunc.METADATA_VIRT_DISK_SIZE )
        if diskSizeItem.IsNone then
            raise <| Exception( "metadata item(virtual disk size) missing" )
        if diskSizeItem.Value.Length < 8u then
            raise <| Exception( "Length of metadata item(virtual disk size) is invalid." )
        let virtualDiskSize = GlbFunc.ReadUInt64LE diskSizeItem.Value.Data 0u

        printfn "metadata item(virtual disk size): %d" virtualDiskSize

        if 0x400000000000UL < virtualDiskSize then
            raise <| Exception( "The virtual disk size is too large." )
        if virtualDiskSize = 0UL then
            raise <| Exception( "The virtual disk size is too small." )

        // Retrieve the virtual disk ID
        let diskIDItem =
            metadataItems
            |> List.tryFind ( fun m -> m.ItemId = GlbFunc.METADATA_VIRT_DISK_ID )
        if diskIDItem.IsNone then
            raise <| Exception( "Metadata item(virtual disk ID) missing." )
        if diskIDItem.Value.Length < 16u then
            raise <| Exception( "Length of metadata item(virtual disk ID) is invalid" )
        let VirtualDiskId = GlbFunc.ReadGuid diskIDItem.Value.Data 0u
        printfn "Metadata item(virtual disk ID) : %s" ( VirtualDiskId.ToString( "D" ) )

        // Retrieve logical sector size.
        let logiSecSizeItem =
            metadataItems
            |> List.tryFind ( fun m -> m.ItemId = GlbFunc.METADATA_LOGI_SECTOR_SIZE )
        if logiSecSizeItem.IsNone then
            raise <| Exception( "Metadata item(logical sector size) missing." )
        if logiSecSizeItem.Value.Length < 4u then
            raise <| Exception( "Length of metadata item(logical sector size) is invalid." )
        let logicalSectorSize = GlbFunc.ReadUInt32LE logiSecSizeItem.Value.Data 0u

        printfn "Metadata item(logical sector size) : %d" logicalSectorSize

        if logicalSectorSize <> 512u && logicalSectorSize <> 4096u then
            raise <| Exception( "Incorrect logical sector size" )
        if virtualDiskSize % uint64 logicalSectorSize <> 0UL then
            raise <| Exception( "The virtual disk size is not a multiple of the logical sector size." )

        // Retrieve the physical sector size.
        let physSecSizeItem =
            metadataItems
            |> List.tryFind ( fun m -> m.ItemId = GlbFunc.METADATA_LOGI_SECTOR_SIZE )
        if physSecSizeItem.IsNone then
            raise <| Exception( "Metadata item(physical sector size) missing" )
        if physSecSizeItem.Value.Length < 4u then
            raise <| Exception( "Length of metadata item(physical sector size) is invalid" )
        let physicalSectorSize = GlbFunc.ReadUInt32LE physSecSizeItem.Value.Data 0u

        printfn "Metadata item(physical sector size) : %d" physicalSectorSize

        if physicalSectorSize <> 512u && physicalSectorSize <> 4096u then
            raise <| Exception( "Incorrect physical sector size" )

        // Retrieve the parent locator
        let parentLocator =
            if hasParent then
                let parLocItem =
                    metadataItems
                    |> List.tryFind ( fun m -> m.ItemId = GlbFunc.METADATA_PARENT_LOC )
                if parLocItem.IsNone then
                    raise <| Exception( "Metadata item(parent locator) missing" )
                if parLocItem.Value.Length < 20u then
                    raise <| Exception( "Length of metadata item(parent locator) is invalid" )
                let locatorType = GlbFunc.ReadGuid parLocItem.Value.Data 0u
                printfn "Metadata item(parent locator type) : %s" ( locatorType.ToString "D" )
                if locatorType <> GlbFunc.METADATA_PARENT_LOC_VHDX then
                    raise <| Exception( "The type of metadata item (parent locator) is unknown." )
                let keyValueCount = GlbFunc.ReadUInt16LE parLocItem.Value.Data 18u
                printfn "Metadata item(parent locator count) : %d" keyValueCount
                if parLocItem.Value.Length < 20u + uint32 keyValueCount * 12u then
                    raise <| Exception( "The number of metadata item(parent locator) is invalid." )
                let data = parLocItem.Value.Data
                let dlen = data.Length |> uint32
                let parLocEntry = [
                    for i = 0 to int keyValueCount - 1 do
                        let wpos = 20 + i * 12 |> uint32
                        let keyOffset = GlbFunc.ReadUInt32LE data wpos
                        let valueOffset = GlbFunc.ReadUInt32LE data ( wpos + 4u )
                        let keyLength = GlbFunc.ReadUInt16LE data ( wpos + 8u )
                        let valueLength = GlbFunc.ReadUInt16LE data ( wpos + 10u )
                        printfn "  Parent locator key offset : %d" keyOffset
                        printfn "  Parent locator value offset  : %d" valueOffset
                        printfn "  Parent locator key length : %d" keyLength
                        printfn "  Parent locator value length : %d" valueLength
                        if keyOffset = 0u || valueOffset = 0u || keyLength = 0us || valueLength = 0us ||
                            dlen < keyOffset + uint32 keyLength ||
                            dlen < valueOffset + uint32 valueLength ||
                            ( int keyOffset ) <= 0 || ( int valueOffset ) <= 0 then
                                printfn "  Invalid parent locator(%d) values. Ignore this entry." i
                        else
                            let lpkey =
                                data.[ int keyOffset .. int keyOffset + int keyLength - 1 ]
                                |> Encoding.Unicode.GetString
                                |> _.ToLower()
                            let lpval =
                                data.[ int valueOffset .. int valueOffset + int valueLength - 1 ]
                                |> Encoding.Unicode.GetString

                            printfn "  Parent locator %d : %s  %s" i lpkey lpval

                            yield ( lpkey, lpval )
                ]
                if parLocEntry.Length <> int keyValueCount then
                    raise <| Exception( "Invalid metadata item(Parent locator)" )
                let m = parLocEntry |> Map
                if m.ContainsKey "parent_linkage" |> not then
                    raise <| Exception( "Missing parent_linkage in metadata item(Parent locator)" )
                let r, _ = Guid.TryParse m.[ "parent_linkage" ]
                if not r then
                    raise <| Exception( "Invalid format of parent_linkage in metadata item(Parent locator)" )
                let pathCheck =
                    [| "relative_path"; "volume_path"; "absolute_win32_path"; |]
                    |> Array.exists m.ContainsKey
                if not pathCheck then
                    raise <| Exception( "Metadata item(parent locator) does not contain relative_path, volume_path, or absolute_win32_path." )
                m
            else
                printfn "Since there is no parent, the parent locator is not obtained."
                Map.empty

        {
            PayloadBlockSize = payloadBlockSize;
            LeaveBlockAllocated = leaveBlockAllocated;
            HasParent = hasParent;
            VirtualDiskSize = virtualDiskSize;
            VirtualDiskId = VirtualDiskId;
            LogicalSectorSize = if logicalSectorSize = 512u then BS_512 else BS_4096;
            PhysicalSectorSize = if physicalSectorSize = 512u then BS_512 else BS_4096;
            ParentLocator = parentLocator;
        }

    /// <summary>
    ///  Get payload block entries from BAT.
    /// </summary>
    /// <param name="batData">
    ///  Block allocation table data from VHDX file.
    /// </param>
    /// <param name="chunkRatio">
    ///  Chunk ratio.
    /// </param>
    /// <param name="pbIndex">
    ///  Index of payload BAT entry.
    /// </param>
    /// <returns>
    ///  Retrieved payload BAT Entry.
    /// </returns>
    static let GetPayloadBlockEntry ( batData : byte[] ) ( chunkRatio : uint64 ) ( pbIndex : uint64 ) : PayloadBATEntry =
        if pbIndex >= uint64 batData.Length then
            raise <| Exception "Index value is excessive"
        let idx = ( pbIndex / chunkRatio ) * ( chunkRatio + 1UL ) + ( pbIndex % chunkRatio )
        let entry = GlbFunc.ReadUInt64LE batData ( uint32 idx * 8u )
        let state =
            match entry &&& 0x0000000000000007UL with
            | 0UL -> BatEntryStatePB.PayloadNotPresent
            | 1UL -> BatEntryStatePB.PayloadUndefined
            | 2UL -> BatEntryStatePB.PayloadZero
            | 3UL -> BatEntryStatePB.PayloadUnapped
            | 6UL -> BatEntryStatePB.PayloadFullyPresent
            | 7UL -> BatEntryStatePB.PayloadPartiallyPresent
            | _ -> BatEntryStatePB.PayloadNotPresent
        let fileOffset = entry &&& 0xFFFFFFFFFFFFFFF8UL

        printfn "Payload BAT entry(Index) : %d" idx
        printfn "Payload BAT entry(State) : %s" ( state.ToString() )
        printfn "Payload BAT entry(Offset) : %d" fileOffset

        {
            BatEntryIndex = idx;
            State = state;
            FileOffset = fileOffset;
        }

    /// <summary>
    /// Retrieve sector bitmap BAT entries from BAT.
    /// </summary>
    /// <param name="batData">
    ///  Block allocation table data from VHDX file.
    /// </param>
    /// <param name="chunkRatio">
    ///  Chunk ratio.
    /// </param>
    /// <param name="sbbIndex">
    ///  Index of the sector bitmat entry.
    /// </param>
    /// <returns>
    /// pair of status of the sector bitmap and file offset of the sector bitmap data.
    /// </returns>
    static let GetSectorBitmapBlockEntry ( batData : byte[] ) ( chunkRatio : uint64 ) ( sbbIndex : uint64 ) : struct( uint64 * BatEntryStateSB * uint64 ) =
        if sbbIndex >= uint64 batData.Length then
            raise <| Exception "The index value of the sector bitmap BAT entry is excessive."
        let idx = sbbIndex * ( chunkRatio + 1UL ) + chunkRatio
        let entry = GlbFunc.ReadUInt64LE batData ( uint32 idx * 8u )
        let state =
            match entry &&& 0x0000000000000007UL with
            | 0UL -> BatEntryStateSB.SectorBitmapNotPresent
            | 6UL -> BatEntryStateSB.SectorBitmapPresent
            | _ -> BatEntryStateSB.SectorBitmapNotPresent
        let fileOffset = entry &&& 0xFFFFFFFFFFFFFFF8UL

        printfn "Sector bitmap BAT entry(Index) : %d" idx
        printfn "Sector bitmap BAT entry(Status) : %s" ( state.ToString() )
        printfn "Sector bitmap BAT entry(Offset) : %d" fileOffset

        struct ( idx, state, fileOffset )

    /// <summary>
    ///  Read block allocation table.
    /// </summary>
    /// <param name="logInfo">
    ///  Log information.
    /// </param>
    /// <param name="fs">
    ///  File stream of VHDX file.
    /// </param>
    /// <param name="batRegion">
    ///  Location where the BAT table is recorded.
    /// </param>
    /// <param name="virtualDiskInfo">
    ///  metadata information.
    /// </param>
    /// <returns>
    ///  Retrieved BAT entries.
    /// </returns>
    static let ReadBat
        ( logInfo : LogEntry list )
        ( fs : FileStream )
        ( batRegion : RegionEntry )
        ( virtualDiskInfo : VirtualDiskInfo )
        : BatEntries =

        let fileData = ReadBytesWithLog logInfo fs batRegion.FileOffset batRegion.Length
        let chunkSize = 0x800000UL * Blocksize.toUInt64 virtualDiskInfo.LogicalSectorSize
        let chunkRatio = chunkSize / uint64 virtualDiskInfo.PayloadBlockSize
        let payloadBlockCount = ( virtualDiskInfo.VirtualDiskSize - 1UL ) / uint64 virtualDiskInfo.PayloadBlockSize + 1UL
        let sectorBitmapBlockCount = ( payloadBlockCount - 1UL ) / chunkRatio + 1UL
        let batEntryCount =
            if not virtualDiskInfo.HasParent then
                payloadBlockCount + ( ( payloadBlockCount - 1UL ) / chunkRatio )
            else
                sectorBitmapBlockCount * ( chunkRatio + 1UL )

        printfn "  Chunk Size : %d" chunkSize
        printfn "  Chunk Ratio : %d" chunkRatio
        printfn "  Payload Block Count : %d" payloadBlockCount
        printfn "  Sector Bitmap Block Count : %d" sectorBitmapBlockCount
        printfn "  Bat Entry Count : %d" batEntryCount

        if uint64( fileData.Length / 8 ) < batEntryCount then
            raise <| Exception "The BAT entry has insufficient data length."

        // Read payload BAT entries
        let payloads = Array.zeroCreate<PayloadBATEntry>( int payloadBlockCount )
        for i = 0 to int payloadBlockCount - 1 do
            payloads.[i] <- GetPayloadBlockEntry fileData chunkRatio ( uint64 i )

        // Read sector bitmap blocks
        let sectorBitmapBlock = Array.zeroCreate<SectorBitmapBATEntry>( int sectorBitmapBlockCount )
        for i = 0 to int sectorBitmapBlockCount - 1 do
            let struct( idx, stat, pos ) = GetSectorBitmapBlockEntry fileData chunkRatio ( uint64 i )
            sectorBitmapBlock.[i] <-
                match stat with
                | BatEntryStateSB.SectorBitmapNotPresent ->
                    {
                        BatEntryIndex = idx;
                        SBState = stat;
                        FileOffset = pos;
                        Bitmap = Array.empty;
                    }
                | BatEntryStateSB.SectorBitmapPresent ->
                    {
                        BatEntryIndex = idx;
                        SBState = stat;
                        FileOffset = pos;
                        Bitmap = ReadBytesWithLog logInfo fs pos 0x100000u;
                    }

        {
            BATRegionOffset = batRegion.FileOffset;
            BATRegionLength = batRegion.Length;
            ChunkSize = chunkSize;
            ChunkRatio = chunkRatio;
            PayloadBlockCount = payloadBlockCount;
            SectorBitmapBlockCount = sectorBitmapBlockCount;
            BatEntryCount = batEntryCount;
            Payloads = payloads;
            SectorBitmap = sectorBitmapBlock;
        }

    /// <summary>
    ///  Read the VHDX file and retrieve the metadata.
    /// </summary>
    /// <param name="filePath">
    ///  VHDX file name.
    /// </param>
    /// <returns>
    ///  Retrieved metadata.
    /// </returns>
    static member ReadVhdx( filePath : string ) : VhdxMetadata =
        use fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read )

        if fs.Length < 0x30000L then
            raise <| Exception( "The VHDX file is too small." )

        // Validating the file type identifier and obtaining the creator
        printfn "================================================================"
        let creator = ReadFileTypeIdentifier fs

        // Load the header
        printfn "================================================================"
        let headers = ReadHeaders fs
        let currentHeader = headers.[ 0 ]

        // Retrieve log information (active log entries only)
        let logInfo =
            if currentHeader.LogLength > 0u && currentHeader.LogGuid <> Guid.Empty then
                let logData = GlbFunc.ReadBytes fs currentHeader.LogOffset currentHeader.LogLength
                let e = ReadActiveLogSequense logData currentHeader.LogGuid
                if e.Length = 0 then
                    raise <| Exception "No valid logs exist."

                // Verify the value of FlushedFileOffset in the last entry.
                let headFFO = ( e |> List.last ).FlushedFileOffset
                if fs.Length < int64 headFFO then
                    raise <| Exception "The file has been truncated."
                e
            else
                []

        printfn "Number of log entries retrieved : %d" logInfo.Length
        for itr in logInfo do
            printfn "***"
            printfn "  Signature : 0x%08X" itr.Signature
            printfn "  Checksum : 0x%08X" itr.Checksum
            printfn "  Entry Length : %d" itr.EntryLength
            printfn "  Tail : 0x%08X" itr.Tail
            printfn "  Descriptor Count: %d" itr.DescriptorCount
            printfn "  Log Guid : %s" ( itr.LogGuid.ToString "D" )
            printfn "  Flushed File Offset : %d" itr.FlushedFileOffset
            printfn "  Last File Offset : %d" itr.LastFileOffset
            printfn "  Log Descriptors---"
            for di in itr.Descriptors do
                match di with
                | LogDescriptor.Data( x ) ->
                    printfn "    Data Descriptor"
                    printfn "    Data Signature : 0x%08X" x.DataSignature
                    printfn "    Trailing Bytes : %s" ( x.TrailingBytes |> Array.map ( sprintf "%02X" ) |> String.concat "" )
                    printfn "    Leading Bytes : %s" ( x.LeadingBytes |> Array.map ( sprintf "%02X" ) |> String.concat "" )
                    printfn "    File Offset : %d" x.FileOffset
                    printfn "    Sequence Number : %d" x.SequenceNumber
                    printfn "    Index : %d" x.ddIndex
                | LogDescriptor.Zero( x ) ->
                    printfn "    Zero Descriptor"
                    printfn "    Zero Signature : 0x%08X" x.ZeroSignature
                    printfn "    Zero Length : %d" x.ZeroLength
                    printfn "    File Offset : %d" x.FileOffset
                    printfn "    Sequence Number : %d" x.SequenceNumber

        // Read Region table 1 0x30000
        printfn "================================================================"
        printfn "Region Table 1"
        printfn "  4K Sector Number : %d .. %d"
                    ( 0x30000UL / 4096UL )
                    ( 0x30000UL / 4096UL + 65536UL / 4096UL - 1UL )
        let regionTable1Buf = ReadBytesWithLog logInfo fs 0x30000UL 65536u
        let regionTable1 = ReadRegionTable regionTable1Buf ( uint64 fs.Length )

        // Read Region table 2 0x40000
        printfn "================================================================"
        printfn "Region Table 2"
        printfn "  4K Sector Number : %d .. %d"
                    ( 0x40000UL / 4096UL )
                    ( 0x40000UL / 4096UL + 65536UL / 4096UL - 1UL )
        let regionTable2Buf = ReadBytesWithLog logInfo fs 0x40000UL 65536u
        let regionTable2 = ReadRegionTable regionTable2Buf ( uint64 fs.Length )

        // Region Table List
        let regionTables =
            [
                if regionTable1.IsSome then
                    yield regionTable1.Value;
                if regionTable2.IsSome then
                    yield regionTable2.Value;
            ]
        let currentRegionTable =
            if regionTables.Length = 0 then
                raise <| Exception( "No valid region table exists." )
            regionTables.[0]

        // Get the locations of the metadata region and BAT region.
        let metadataRegion =
            currentRegionTable.Entries
            |> List.tryFind ( fun e -> e.Guid = GlbFunc.REGENT_TYPE_METADATA )
        if metadataRegion.IsNone then
            raise <| Exception("Metadata region not found.")

        let batRegion =
            currentRegionTable.Entries
            |> List.tryFind ( fun e -> e.Guid = GlbFunc.REGENT_TYPE_BAT )
        if batRegion.IsNone then
            raise <| Exception("BAT region not found.")

        // Read metadata region.
        printfn "================================================================"
        printfn "Metadata region"
        printfn "  4K Sector Number : %d .. %d"
                    ( metadataRegion.Value.FileOffset / 4096UL )
                    ( metadataRegion.Value.FileOffset / 4096UL + uint64 metadataRegion.Value.Length / 4096UL - 1UL )
        let metadataBuf = ReadBytesWithLog logInfo fs metadataRegion.Value.FileOffset metadataRegion.Value.Length
        let virtualDiskInfo = ReadMetadata metadataBuf

        // Read BAT
        printfn "================================================================"
        printfn "BAT"
        printfn "  4K Sector Number : %d .. %d"
                    ( batRegion.Value.FileOffset / 4096UL )
                    ( batRegion.Value.FileOffset / 4096UL + uint64 batRegion.Value.Length / 4096UL - 1UL )
        let batEntries = ReadBat logInfo fs batRegion.Value virtualDiskInfo

        if virtualDiskInfo.HasParent then
            // For differential VHDX files, if a PartiallyPresent payload BAT entry exists,
            // a corresponding sector bitmap BAT entry must also exist.
            batEntries.Payloads
            |> Array.filter ( _.State.IsPayloadPartiallyPresent )
            |> Array.iteri ( fun idx itr ->
                let j = idx / int batEntries.ChunkRatio  // Index of sector bitmap BAT entry
                if batEntries.SectorBitmap.[j].Bitmap.Length = 0 then
                    raise <| Exception "There is no sector bitmap BAT entry corresponding to the payload BAT entry for PartiallyPresent."
            )
        else
            // If there is no parent, the PartiallyPresent payload BAT entry must not exist.
            if batEntries.Payloads |> Array.exists ( _.State.IsPayloadPartiallyPresent ) then
                raise <| Exception "A fixed or dynamic VHDX file exists with a payload BAT entry for PartiallyPresent."

            // If a parent does not exist, a sector bitmap BAT entry must not exist.
            if batEntries.SectorBitmap |> Array.exists ( fun itr -> itr.Bitmap.Length > 0 ) then
                raise <| Exception "The VHDX file has either fixed or dynamic sector bitmap BAT entries assigned to it."

        {
            Creator = creator;
            Header = currentHeader;
            LogInfo = logInfo;
            RegionTables = currentRegionTable;
            VirtualDiskInfo = virtualDiskInfo;
            BatEntries = batEntries;
        }
