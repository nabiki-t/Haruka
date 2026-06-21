namespace VhdxLibrary

open System
open System.IO
open System.Text
open System.Collections.Generic

/// <summary>
///  Write RAW data to VHDX file.
/// </summary>
type VhdxWriter() =

    /// <summary>
    ///  Extract used regions from payload BAT entries.
    /// </summary>
    /// <param name="payloads">
    ///  Payload BAT entries.
    /// </param>
    /// <returns>
    ///  Array of file offsets for allocated payload blocks, sorted in ascending order.
    /// </returns>
    static member getUsedRegions ( payloads : PayloadBATEntry[] ) : uint64 [] =
        payloads
        |> Array.choose ( fun e ->
            match e.State with
            | PayloadFullyPresent
            | PayloadPartiallyPresent ->
                Some ( e.FileOffset )
            | _ ->
                None
        )
        |> Array.sort

    /// <summary>
    ///  Merge overlapping or adjacent intervals.
    /// </summary>
    /// <param name="payloadBlockSize">
    ///  Payload block size in bytes.
    /// </param>
    /// <param name="intervals">
    ///  Array of starting offsets of payload blocks.
    /// </param>
    /// <returns>
    ///  List of merged intervals as struct tuples (start, end).
    /// </returns>
    static member mergeIntervals ( payloadBlockSize : uint64 ) ( intervals : uint64 [] ) : struct( uint64 * uint64 ) list =
        intervals
        |> Array.fold ( fun acc s ->
            let e = s + payloadBlockSize
            match acc with
            | [] -> [ struct( s, e ) ]
            | struct( ps, pe ) :: rest ->
                if s <= pe then
                    struct( ps, max pe e ) :: rest
                else
                    struct( s, e ) :: acc
        ) []
        |> List.rev

    /// <summary>
    ///  Calculate free regions available for payload block allocation.
    /// </summary>
    /// <param name="payloads">
    ///  Payload BAT entries.
    /// </param>
    /// <param name="payloadBlockSize">
    ///  Payload block size in bytes.
    /// </param>
    /// <param name="currentFileSize">
    ///  Current file size in bytes.
    /// </param>
    /// <returns>
    ///  List of file offsets where new payload blocks can be allocated.
    /// </returns>
    static member BuildFreeList
        ( payloads : PayloadBATEntry[] )
        ( payloadBlockSize : uint64 )
        ( currentFileSize : uint64 )
        : uint64 list =

        // Subtract used regions to find free regions
        let freeRanges =
            payloads
            |> VhdxWriter.getUsedRegions 
            |> VhdxWriter.mergeIntervals payloadBlockSize
            |> List.fold ( fun struct( prevEnd, acc ) struct( s, e ) -> 
                let acc =
                    if prevEnd < s then
                        struct( prevEnd, s ) :: acc
                    else
                        acc
                struct( e, acc )
            ) ( struct( 0UL, [] ) )
            |> ( fun struct( lastEnd, acc ) ->
                if lastEnd < currentFileSize then
                    struct( lastEnd, currentFileSize ) :: acc
                else
                    acc
            )
            |> List.rev

        // Split into aligned payload block units
        freeRanges
        |> List.collect ( fun struct( start, endPos ) ->
            let alignedStart =
                let s = ( ( start + 1048576UL - 1UL) / 1048576UL ) * 1048576UL
                max s start
            let alignedEnd = endPos - ( endPos % 1048576UL )

            if alignedStart >= alignedEnd then
                []
            else
                let length = alignedEnd - alignedStart
                let blockCount = length / payloadBlockSize

                List.init ( int blockCount ) ( fun i ->
                    alignedStart + uint64 i * payloadBlockSize
                )
        )

    /// <summary>
    ///  If any payload blocks that will be needed in the future are not yet allocated,
    ///  those payload blocks will be newly allocated.
    ///  This function only updates the BAT entry in memory.
    /// </summary>
    /// <param name="currentFileSize">
    ///  Currently allocated file size.
    /// </param>
    /// <param name="metadata">
    ///  VHDX metadata. The value of BAT is updated.
    /// </param>
    /// <param name="lba">
    ///  The starting position of the area where data will be written.
    ///  Offset in a virtual disk, per logical sector.
    /// </param>
    /// <param name="cnt">
    ///  The number of logical sectors in the area to be written to.
    /// </param>
    /// <returns>
    ///  Pair of BAT updated 4K sector numbers and the file size required after allocation.
    /// </returns>
    static member AllocatePayloadBlock ( currentFileSize : uint64 ) ( metadata : VhdxMetadata ) ( lba : uint64 ) ( cnt : uint64 ) : struct( uint64 [] * uint64 ) =
        let hasParent = metadata.VirtualDiskInfo.HasParent
        let pbSize = uint64 metadata.VirtualDiskInfo.PayloadBlockSize
        let allocPBStat = 
            if hasParent then
                PayloadPartiallyPresent
            else
                PayloadFullyPresent;
        let updated4KSecs = HashSet<uint64>()

        // build free list
        let freeList = VhdxWriter.BuildFreeList metadata.BatEntries.Payloads pbSize currentFileSize

        // allocate space and update BAT entry
        let rec loop ( wcnt : uint64 ) ( restFreeList : uint64 list ) ( gfs : uint64 ) : uint64 =
            if wcnt < cnt then
                let struct( pbidx, _ ) = VhdxHandler.LBAtoPayloadBlockIndex ( lba + wcnt ) metadata
                let pb = metadata.BatEntries.Payloads.[ int pbidx ]
                match pb.State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadZero
                | PayloadUnapped ->

                    // allocate free space
                    let fileoffset, nextFL, nextgfs =
                        match restFreeList with
                        | [] ->
                            gfs, [], ( gfs + pbSize )
                        | a :: b ->
                            a, b, gfs

                    // update payload block BAT entry
                    metadata.BatEntries.Payloads.[ int pbidx ] <-
                        {
                            pb with
                                State = allocPBStat;
                                FileOffset = fileoffset;
                        }

                    // calculate updated 4K sector index
                    let fpos = metadata.BatEntries.BATRegionOffset + uint64 ( 8 * int pbidx )
                    let secidx = fpos / 4096UL
                    updated4KSecs.Add secidx |> ignore

                    // To next LBA block
                    loop ( wcnt + 1UL ) nextFL nextgfs

                | PayloadFullyPresent
                | PayloadPartiallyPresent ->
                    // Already allocated
                    loop ( wcnt + 1UL ) restFreeList gfs
            else
                gfs
        let requiredFileSize = loop 0UL freeList currentFileSize
        struct( Array.ofSeq updated4KSecs, requiredFileSize )

    /// <summary>
    ///  Mark the sector bitmap corresponding to the area to be updated as used.
    /// </summary>
    /// <param name="metadata">
    ///  VHDX metadata. The value of BAT is updated.
    /// </param>
    /// <param name="lba">
    ///  The starting position of the area where data will be written.
    ///  Offset in a virtual disk, per logical sector.
    /// </param>
    /// <param name="cnt">
    ///  The number of logical sectors in the area to be written to.
    /// </param>
    /// <returns>
    ///  Array of pairs of 4K sector numbers and updated bitmap data segments.
    /// </returns>
    /// <remarks>
    ///  The VHDX file must have a parent file.
    /// </remarks>
    static member UpdateSectorBitmap ( metadata : VhdxMetadata ) ( lba : uint64 ) ( cnt : uint64 )
        : struct( uint64 * ArraySegment<byte> ) [] =

        let updated4KSecs = Dictionary< uint64, ArraySegment<byte> >()
        let rec loop ( wcnt : uint64 ) =
            if wcnt < cnt then
                let struct( sbIdx, bitmapIdx ) =
                    VhdxHandler.LBAtoSectorBitmapIndex ( lba + wcnt ) metadata
                let sbEntry = metadata.BatEntries.SectorBitmap.[ int sbIdx ]
                let sb = sbEntry.Bitmap
                let bytePos = bitmapIdx >>> 3
                let bitPos = bitmapIdx &&& 7u
                let bitValue = ( sb.[ int bytePos ] >>> ( int bitPos ) ) &&& 1uy
                if bitValue = 0uy then
                    sb.[ int bytePos ] <- sb.[ int bytePos ] ||| ( 1uy <<< ( int bitPos ) )
                    let s = ( sbEntry.FileOffset + ( uint64 bytePos ) ) / 4096UL
                    updated4KSecs.TryAdd( s, ArraySegment( sb, 0, 4096 ) ) |> ignore
                loop ( wcnt + 1UL )
            else
                ()
        loop 0UL
        updated4KSecs
        |> Seq.map ( fun itr -> struct( itr.Key, itr.Value ) )
        |> Seq.toArray

    /// <summary>
    ///  Calculate the required log region size based on the number of 4K sectors.
    /// </summary>
    /// <param name="entries">
    ///  Number of 4K sectors where update information should be recorded.
    /// </param>
    /// <returns>
    ///  Required log region size in bytes.
    /// </returns>
    static member LogCapacityFrom4KSecCount ( entries : uint32 ) : uint32 =
        let headerRaw = 64u + entries * 32u
        let headerSize = ( ( headerRaw + 4095u ) / 4096u ) * 4096u
        let dataSize = entries * 4096u
        headerSize + dataSize

    /// <summary>
    ///  Determine the upper limit of the 4K sectors that can be recorded
    ///  based on the size of the log area.
    /// </summary>
    /// <param name="capacity">
    ///  Log region size in bytes. It must be greater than 1MB and multiple of 1MB.
    /// </param>
    /// <returns>
    ///  The number of 4K entries that can be recorded using a single log entry.
    /// </returns>
    static member Max4KSectorCountFromLogCapacity ( capacity : uint32 ) : uint32 =
        let nApprox = ( capacity - 64u ) / 4128u
        let k =
            let headerRaw = 64u + 32u * nApprox
            ( headerRaw + 4095u ) / 4096u
        let n1 = ( 4096u * k - 64u ) / 32u
        let n2 = VhdxWriter.LogCapacityFrom4KSecCount nApprox / 4096u - k
        min n1 n2

    /// <summary>
    ///  Write PayloadBATEntry data to the buffer.
    /// </summary>
    /// <param name="buf">
    ///  Buffer where data will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the buffer where the data will be written.
    /// </param>
    /// <param name="e">
    ///  The payload BAT entry that should be written to the buffer.
    /// </param>
    static member PayloadBlockEntryToBytes ( buf : byte[] ) ( offset : int ) ( e : PayloadBATEntry ) : unit =
        let a =
            match e.State with
            | PayloadNotPresent ->
                0UL
            | PayloadUndefined ->
                1UL
            | PayloadZero ->
                2UL
            | PayloadUnapped ->
                3UL
            | PayloadFullyPresent ->
                6UL
            | PayloadPartiallyPresent ->
                7UL
        let d = e.FileOffset ||| a
        GlbFunc.WriteUInt64LE buf ( uint32 offset ) d

    /// <summary>
    ///  Write SectorBitmapBATEntry data to the buffer.
    /// </summary>
    /// <param name="buf">
    ///  Buffer where data will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the buffer where the data will be written.
    /// </param>
    /// <param name="e">
    ///  The sector bitmap BAT entry that should be written to the buffer.
    /// </param>
    static member SectorBitmapEntryToBytes ( buf : byte[] ) ( offset : int ) ( e : SectorBitmapBATEntry ) : unit =
        let a =
            match e.SBState with
            | SectorBitmapNotPresent ->
                0UL
            | SectorBitmapPresent ->
                6UL
        let d = e.FileOffset ||| a
        GlbFunc.WriteUInt64LE buf ( uint32 offset ) d

    /// <summary>
    ///  Create BAT table from 4K sector number.
    /// </summary>
    /// <param name="bat">
    ///  BAT entry data.
    /// </param>
    /// <param name="secnum">
    ///  4K sector number that was updated.
    /// </param>
    /// <returns>
    ///  BAT entry data that will be written to the file.
    /// </returns>
    static member CreateBATEntryTableFrom4KSectorNumber ( bat : BatEntries ) ( secnum : uint32 ) : byte[] =
        let chunkRatio = int bat.ChunkRatio
        let buffer = Array.zeroCreate<byte> 4096

        // Number of BAT entries written to one 4K sector
        let count = 4096UL / 8UL

        // Index of the BAT entry that will be the starting position of the output
        let sBATIdx = ( uint64 secnum - ( bat.BATRegionOffset / 4096UL ) ) * count

        // Number of BAT entries to be output
        let count2 = ( min ( sBATIdx + count ) bat.BatEntryCount ) - sBATIdx

        for i = 0 to int count2 - 1 do
            let idx = int sBATIdx + i
            if ( idx + 1 ) % ( chunkRatio + 1 ) = 0 then
                let sbidx = idx / ( chunkRatio + 1 )
                VhdxWriter.SectorBitmapEntryToBytes buffer ( i * 8 ) bat.SectorBitmap.[ sbidx ]
            else
                let pbidx =
                    let w = idx / ( chunkRatio + 1 )
                    let w2 = w * chunkRatio
                    let w3 = idx % ( chunkRatio + 1 )
                    w2 + w3
                VhdxWriter.PayloadBlockEntryToBytes buffer ( i * 8 ) bat.Payloads.[ pbidx ]
        buffer

    /// <summary>
    ///  Output the updated BAT entry data through log.
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="metadata">
    ///  Metadata for the VHDX file.
    /// </param>
    /// <param name="sec4Ks">
    ///  4K sector numbers representing the updated range within the BAT entry.
    /// </param>
    /// <param name="reqFileSize">
    ///  File size required after update.
    /// </param>
    /// <returns>
    ///  Next header sequence number.
    /// </returns>
    static member WriteUpdatedBAT ( fs : FileStream ) ( metadata : VhdxMetadata ) ( sec4Ks : uint64[] ) ( reqFileSize : uint64 ) : uint64 =

        let logEntryUnit =
            VhdxWriter.Max4KSectorCountFromLogCapacity metadata.Header.LogLength
            |> int
        let logOutputPos = uint32 ( metadata.Header.LogOffset )
        let cycleCount = ( sec4Ks.Length + ( logEntryUnit - 1 ) ) / logEntryUnit

        printfn "====================="
        printfn " WriteUpdatedBAT"
        printfn " sec4Ks.Length : %d" sec4Ks.Length
        printfn " reqFileSize : %d" reqFileSize
        printfn " logEntryUnit : %d" logEntryUnit
        printfn " logOutputPos : %d" logOutputPos
        printfn " cycleCount : %d" cycleCount

        let rec loop ( cycle : int ) ( headerSeq : uint64 ) =
            if cycle < cycleCount then
                let wcnt = min ( cycle + logEntryUnit ) sec4Ks.Length
                let widx = cycle * logEntryUnit
                let newLogGuid = Guid.NewGuid()
                let currentFileSize = uint64 fs.Length

                printfn "--- loop ---"
                printfn " cycle : %d" cycle
                printfn " headerSeq : %d" headerSeq
                printfn " wcnt : %d" wcnt
                printfn " widx : %d" widx
                printfn " currentFileSize : %d" currentFileSize

                // Write log entry for each 4K sector to be updated
                let logEntries =
                    List.init wcnt ( fun j -> 
                        let sequenceNumber = uint64 j + 1UL
                        let sector4KNumber = sec4Ks.[ widx + j ] |> uint32
                        let data =
                            VhdxWriter.CreateBATEntryTableFrom4KSectorNumber metadata.BatEntries sector4KNumber
                        struct( uint64 ( int64 sector4KNumber * 4096L ), data )
                    )

                // Build log entry using CreateLogEntry
                let newLogEntry =
                    VhdxCorrupter.CreateLogEntry
                        ( List.map ( fun struct( o, d ) -> ( o, d ) ) logEntries )
                        0u
                        ( headerSeq + 1UL )
                        newLogGuid
                        currentFileSize
                        currentFileSize

                // Write the log entry to file
                VhdxCorrupter.WriteLogEntry fs metadata logOutputPos [] newLogEntry

                // Update header( Update LogGuid )
                let hd1 = {
                    metadata.Header with
                        LogGuid = newLogGuid;
                        SequenceNumber = headerSeq;
                }
                let nextsn1 = VhdxHandler.UpdateHeader fs hd1

                // Set file size if needed
                if currentFileSize < reqFileSize then
                    fs.SetLength( int64 reqFileSize )
                    fs.Flush()

                // Write BAT data to file at the specified 4K sector positions
                logEntries
                |> List.iteri ( fun i struct( offset, data ) ->
                    let fpos = int64 offset
                    fs.Seek( fpos, SeekOrigin.Begin ) |> ignore
                    fs.Write( data )
                ) 
                fs.Flush()

                // Invalidate the log by clearing LogGuid
                let hd2 = {
                    metadata.Header with
                        LogGuid = Guid();
                        SequenceNumber = nextsn1;
                }
                VhdxHandler.UpdateHeader fs hd2
            else
                headerSeq
        loop 0 metadata.Header.SequenceNumber

    /// <summary>
    ///  Output the updated sector bitmap data through log.
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="metadata">
    ///  Metadata for the VHDX file.
    /// </param>
    /// <param name="sec4Ks">
    ///  The updated sector bitmap data and the 4K sector numbers where they will be written.
    /// </param>
    /// <returns>
    ///  Next header sequence number.
    /// </returns>
    static member WriteUpdatedSB ( fs : FileStream ) ( metadata : VhdxMetadata ) ( sec4Ks : struct( uint64 * ArraySegment<byte> ) [] ) : uint64 =

        let logEntryUnit =
            VhdxWriter.Max4KSectorCountFromLogCapacity metadata.Header.LogLength
            |> int
        let logOutputPos = uint32 ( metadata.Header.LogOffset )
        let cycleCount = ( sec4Ks.Length + ( logEntryUnit - 1 ) ) / logEntryUnit

        printfn "====================="
        printfn " WriteUpdatedSB"
        printfn " sec4Ks.Length : %d" sec4Ks.Length
        printfn " logEntryUnit : %d" logEntryUnit
        printfn " logOutputPos : %d" logOutputPos
        printfn " cycleCount : %d" cycleCount

        let rec loop ( cycle : int ) ( headerSeq : uint64 ) =
            if cycle < cycleCount then
                let wcnt = min ( cycle + logEntryUnit ) sec4Ks.Length
                let widx = cycle * logEntryUnit
                let newLogGuid = Guid.NewGuid()
                let currentFileSize = uint64 fs.Length

                printfn "--- loop ---"
                printfn " cycle : %d" cycle
                printfn " headerSeq : %d" headerSeq
                printfn " wcnt : %d" wcnt
                printfn " widx : %d" widx
                printfn " currentFileSize : %d" currentFileSize

                // Write log entry for each 4K sector to be updated
                let logEntries =
                    List.init wcnt ( fun j -> 
                        let sequenceNumber = uint64 j + 1UL
                        let struct( sector4KNumber, data ) = sec4Ks.[ widx + j ]
                        let offset = uint64 ( int64 sector4KNumber * 4096L )
                        let dataArray = Array.zeroCreate<byte> 4096
                        Array.blit data.Array 0 dataArray 0 data.Count
                        struct( offset, dataArray )
                    )

                // Build log entry using CreateLogEntry
                let newLogEntry =
                    VhdxCorrupter.CreateLogEntry
                        ( List.map ( fun struct( o, d ) -> ( o, d ) ) logEntries )
                        0u
                        ( headerSeq + 1UL )
                        newLogGuid
                        currentFileSize
                        currentFileSize

                // Write the log entry to file
                VhdxCorrupter.WriteLogEntry fs metadata logOutputPos [] newLogEntry

                // Update header( Update LogGuid )
                let hd1 = {
                    metadata.Header with
                        LogGuid = newLogGuid;
                        SequenceNumber = headerSeq;
                }
                let nextsn1 = VhdxHandler.UpdateHeader fs hd1

                // Write sector bitmap data to file at the specified 4K sector positions
                logEntries
                |> List.iteri ( fun i struct( offset, data ) ->
                    let fpos = int64 offset
                    fs.Seek( fpos, SeekOrigin.Begin ) |> ignore
                    fs.Write( data )
                ) 
                fs.Flush()

                // Invalidate the log by clearing LogGuid
                let hd2 = {
                    metadata.Header with
                        LogGuid = Guid();
                        SequenceNumber = nextsn1;
                }
                VhdxHandler.UpdateHeader fs hd2
            else
                headerSeq
        loop 0 metadata.Header.SequenceNumber

    /// <summary>
    ///  Write the RAW data to the allocated payload blocks.
    /// </summary>
    /// <param name="rawfs">
    ///  File stream for the RAW data file.
    /// </param>
    /// <param name="vhdxfs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="metadata">
    ///  VHDX metadata with updated BAT entries.
    /// </param>
    /// <param name="lba">
    ///  Starting LBA position in the virtual disk.
    /// </param>
    static member OutputRawData ( rawfs : FileStream ) ( vhdxfs : FileStream ) ( metadata : VhdxMetadata ) ( lba : uint64 ) ( rawDataSec : uint64 ) : unit =

        let sectorSize = int metadata.VirtualDiskInfo.LogicalSectorSize
        let buffer = Array.zeroCreate<byte> sectorSize
        rawfs.Seek( 0L, SeekOrigin.Begin ) |> ignore

        // Write each sector from the raw file to the VHDX at position lba + i
        let rec loop ( i : uint64 ) =
            if i < rawDataSec then
                let targetLba = lba + i
                let struct( badIndex, offsetInBat ) = VhdxHandler.LBAtoPayloadBlockIndex targetLba metadata
                let pbStartPos = metadata.BatEntries.Payloads.[ int badIndex ].FileOffset
                let offsetInPB = ( uint64 offsetInBat ) * ( uint64 sectorSize )
                rawfs.ReadExactly( buffer, 0, sectorSize )
                vhdxfs.Seek( int64 ( pbStartPos + offsetInPB ), SeekOrigin.Begin ) |> ignore
                vhdxfs.Write( buffer )
                loop ( i + 1UL )
            else
                ()
        loop 0UL

    /// <summary>
    ///  Write RAW data to VHDX file.
    /// </summary>
    /// <param name="vhdxFileName">
    ///  VHDX file to be updated.
    /// </param>
    /// <param name="rawFileName">
    ///  RAW data file to be written.
    /// </param>
    /// <param name="lba">
    ///  Address in virtual disk where raw data will be written.
    /// </param>
    static member Write ( vhdxFileName : string ) ( rawFileName : string ) ( lba : uint64 ) : unit =

        let metadata = VhdxReader.ReadVhdx vhdxFileName
        let sectorSize = metadata.VirtualDiskInfo.LogicalSectorSize
        let vdsb = metadata.VirtualDiskInfo.VirtualDiskSize
        let vdss = vdsb / uint64 sectorSize
        use rawfs = new FileStream( rawFileName, FileMode.Open, FileAccess.Read, FileShare.None )
        let rawDataLength = rawfs.Length |> uint64
        let rawDataSec = rawDataLength / uint64 sectorSize

        printfn "========================================================"
        printfn "Write raw data to VHDX file"
        printfn "VHDX file name : %s" vhdxFileName
        printfn "RAW file name : %s" rawFileName
        printfn "LBA : %d" lba
        printfn "RAW data length(byte) : %d" rawDataLength
        printfn "RAW data length(sector) : %d" rawDataSec
        printfn "VHDX sector size : %d" sectorSize
        printfn "Virtual Disk Size(byte) : %d" vdsb
        printfn "Virtual Disk Size(sector) : %d" vdss

        if ( rawDataSec * uint64 sectorSize ) <> rawDataLength then
            raise <| Exception( "RAW data length must be multiple of sector size." )
        if lba > vdss then
            raise <| Exception( "LBA must be less than or equals virtual disk size." )
        if rawDataSec > vdss then
            raise <| Exception( "RAW data length must be less than or equals virtual disk size." )
        if rawDataSec + lba > vdss then
            raise <| Exception( "RAW data length + LBA must be less than or equals virtual disk size." )

        // Flash log entries if any exist
        let wSecNum =
            if metadata.LogInfo.Length > 0 then
                printfn "Replay log."
                VhdxChecker.Check vhdxFileName
                metadata.Header.SequenceNumber + 2UL
            else
                metadata.Header.SequenceNumber

        // Open VHDX file
        use vhdxfs = new FileStream( vhdxFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None )

        // Update FileWriteGuid and DataWriteGuid
        let hd1 = {
            metadata.Header with
                FileWriteGuid = Guid.NewGuid();
                DataWriteGuid = Guid.NewGuid();
                SequenceNumber = wSecNum + 1UL;
        }
        let nextsn1 = VhdxHandler.UpdateHeader vhdxfs hd1

        // Allocate Payload block
        let struct( updated4KSecsForBAT, requiredFileSize ) =
            VhdxWriter.AllocatePayloadBlock ( uint64 vhdxfs.Length ) metadata lba rawDataSec

        // Update sector bitmap if the VHDX has a parent
        let updated4KSecsForSB =
            if metadata.VirtualDiskInfo.HasParent then
                VhdxWriter.UpdateSectorBitmap metadata lba rawDataSec
            else
                Array.empty

        // Output the updated BAT entry data through log
        let nextsn2 = VhdxWriter.WriteUpdatedBAT vhdxfs metadata updated4KSecsForBAT requiredFileSize

        // Output the updated sector bitmap data through log (only for differential VHDX)
        let nextsn3 =
            if metadata.VirtualDiskInfo.HasParent then
                VhdxWriter.WriteUpdatedSB vhdxfs metadata updated4KSecsForSB
            else
                nextsn2

        // Output RAW data to the allocated payload blocks
        VhdxWriter.OutputRawData rawfs vhdxfs metadata lba rawDataSec

