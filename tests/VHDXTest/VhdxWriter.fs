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
    ///  Search for used areas.
    /// </summary>
    /// <param name="payloads">
    ///  Payload BAT entries
    /// </param>
    /// <returns>
    ///  Array of used range offsets
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

    /// <sumary>
    ///  Consolidate contiguous used areas.
    /// </summary>
    /// <param name="intervals">
    /// </param>
    /// <param name="payloadBlockSize">
    ///  Payload block size.
    /// </param>
    /// <returns>
    ///  used range, file offset and length in bytes.
    /// </returns>
    static member mergeIntervals ( metadata : VhdxMetadata ) ( intervals : uint64 [] ) : struct ( uint64 * uint64 ) list =
        let payloadBlockSize = metadata.VirtualDiskInfo.PayloadBlockSize |> uint64
        let initial =
            [
                // header
                struct( 0UL, 1048575UL );
                // log
                struct( metadata.Header.LogOffset, metadata.Header.LogOffset + uint64 metadata.Header.LogLength - 1UL )
                // Regions( metadata, BAT, etc. )
                for i in metadata.RegionTables.Entries do
                    struct( i.FileOffset, i.FileOffset + uint64 i.Length - 1UL )
                // Sector bitmap
                for i in metadata.BatEntries.SectorBitmap do
                    if i.SBState = BatEntryStateSB.SectorBitmapPresent then
                        struct( i.FileOffset, i.FileOffset + 1048575UL );
                // payload block
                for i in intervals do
                        struct( i, i + payloadBlockSize - 1UL );
            ]

        initial
        |> List.sortBy ( fun struct( a, _ ) -> a )
        |> List.fold ( fun acc struct( s, e ) ->
            match acc with
            | [] -> [ struct( s, e ) ]
            | ( ps, pe ) :: rest ->
                if s <= pe then
                    struct( ps, max pe e ) :: rest
                else
                    struct( s, e ) :: acc
        ) []
        |> List.rev

    /// <summary>
    ///  Identify unused areas.
    /// </summary>
    /// <param name="metadata">
    ///  Metadata for VHDX file.
    /// </param>
    /// <param name="currentFileSize">
    ///  Current VHDX file size.
    /// </param>
    /// <returns>
    ///  The start position of the unused area.
    ///  The size of a single unused area is the payload block length.
    /// </returns>
    static member BuildFreeList
        ( metadata : VhdxMetadata )
        ( currentFileSize : uint64 )
        : uint64 list =

        let payloads = metadata.BatEntries.Payloads
        let payloadBlockSize = metadata.VirtualDiskInfo.PayloadBlockSize |> uint64

        // Calculate the free space by subtracting the used area from the entire file.
        let freeRanges =
            payloads
            |> VhdxWriter.getUsedRegions
            |> ( fun d ->
                printfn "--- Allocated payloads ---"
                for i = 0 to d.Length - 1 do
                    printfn "  %d : %d" i d.[i]
                d
            )
            |> VhdxWriter.mergeIntervals metadata
            |> ( fun d ->
                printfn "--- Used regions ---"
                for i = 0 to d.Length - 1 do
                    let struct( x, y ) = d.[i]
                    printfn "  %d : %d .. %d " i x y
                d
            )
            |> List.fold ( fun struct ( prevEnd, acc ) struct ( s, e ) -> 
                let acc =
                    if prevEnd < s then
                        struct ( prevEnd, s ) :: acc
                    else
                        acc
                struct ( e, acc )
            ) ( struct ( 0UL, [] ) )
            |> ( fun struct ( lastEnd, acc ) ->
                if lastEnd < currentFileSize then
                    struct ( lastEnd, currentFileSize ) :: acc
                else
                    acc
            )
            |> List.rev

        // Split into units of PayloadBlockSize.
        freeRanges
        |> List.collect ( fun struct ( start, endPos ) ->
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
    static member AllocatePayloadBlock ( currentFileSize : uint64 ) ( metadata : VhdxMetadata ) ( lba : BLKCNT64_T ) ( cnt : BLKCNT64_T ) : struct ( HashSet<SEC4K_T> * uint64 ) =
        let hasParent = metadata.VirtualDiskInfo.HasParent
        let pbSize = uint64 metadata.VirtualDiskInfo.PayloadBlockSize
        let allocPBStat = 
            if hasParent then
                PayloadPartiallyPresent
            else
                PayloadFullyPresent;
        let updated4KSecs = HashSet<SEC4K_T>()

        printfn "--------------"
        printfn "  AllocatePayloadBlock"
        printfn "  hasParent : %b" hasParent
        printfn "  Payload Block Size : %d" pbSize
        printfn "  Allocated initially payload block status : %s" ( allocPBStat.ToString() )

        // build free list
        let freeList = VhdxWriter.BuildFreeList metadata currentFileSize

        printfn "  Free area list"
        for i in freeList do
            printfn "    %d .. %d" i ( i + pbSize - 1UL )

        // allocate space and update BAT entry
        let rec loop ( wcnt : BLKCNT64_T ) ( restFreeList : uint64 list ) ( gfs : uint64 ) : uint64 =
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

                    // calculate update 4A sector index
                    let fpos = metadata.BatEntries.BATRegionOffset + uint64 ( 8 * int pbidx )
                    let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
                    updated4KSecs.Add secidx |> ignore

                    printfn "  Allocate payload block"
                    printfn "    File offset : %d" fileoffset
                    printfn "    LBA : %d" ( lba + wcnt )
                    printfn "    Payload block index : %d" pbidx
                    printfn "    Updated 4K sector number : %d" secidx

                    // To next LBA block
                    loop ( wcnt + blkcnt_me.ofUInt64 1UL ) nextFL nextgfs

                | PayloadFullyPresent
                | PayloadPartiallyPresent ->
                    // Already allocated
                    loop ( wcnt + blkcnt_me.ofUInt64 1UL ) restFreeList gfs
            else
                gfs
        let requiredFileSize = loop blkcnt_me.zero64 freeList currentFileSize

        printfn "  All payload block allocated."
        printfn "  Required File Sizse : %d" requiredFileSize

        struct ( updated4KSecs, requiredFileSize )

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
    ///  BAT updated 4K sector numbers
    /// </returns>
    /// <remarks>
    ///  The VHDX file must have a parent file.
    /// </remarks>
    static member UpdateSectorBitmap
        ( metadata : VhdxMetadata )
        ( updatedPB4K : HashSet<SEC4K_T> )
        ( lba : BLKCNT64_T )
        ( cnt : BLKCNT64_T )
        : Dictionary< SEC4K_T, ArraySegment<byte> > =

        printfn "--------------"
        printfn "  UpdateSectorBitmap"
        printfn "  LBA : %d" lba
        printfn "  sector count : %d" cnt

        let updatedSB4K = Dictionary< SEC4K_T, ArraySegment<byte> >()
        let rec loop ( wcnt : BLKCNT64_T ) =
            if wcnt >= cnt then
                ()
            else
                let struct( sbIdx, bitmapIdx ) = VhdxHandler.LBAtoSectorBitmapIndex ( lba + wcnt ) metadata
                let sbEntry = metadata.BatEntries.SectorBitmap.[ int sbIdx ]
                let sb = sbEntry.Bitmap
                let sbFileOffset = sbEntry.FileOffset
                let bytePos = bitmapIdx >>> 3
                let bitPos = bitmapIdx &&& 7u
                let bitValue = ( sb.[ int bytePos ] >>> ( int bitPos ) ) &&& 1uy

                if bitValue <> 0uy then
                    // If it is already marked as in use, there is nothing to do.
                    loop ( wcnt + blkcnt_me.ofUInt64 1UL )
                else
                    // Set the bit flag to indicate that it is in use.
                    sb.[ int bytePos ] <- sb.[ int bytePos ] ||| ( 1uy <<< ( int bitPos ) )

                    // Record information on updated 4K sectors.
                    let s = ( sbFileOffset + ( uint64 bytePos ) ) / 4096UL |> sec4k_me.ofUInt64
                    updatedSB4K.TryAdd( s, ArraySegment( sb, 0, 4096 ) ) |> ignore

                    if sb.[ int bytePos ] <> 0xFFuy then
                        // Unless the current byte is 0xFF, the payload BAT entry will not become PayloadFullyPresent.
                        loop ( wcnt + blkcnt_me.ofUInt64 1UL )
                    else
                        // 当該LBAが属するペイロードブロックBATエントリに属する、全てのセクタービットマップが1になったか否かを判断したい。
                        let sbBytesCntPerPB = 1048576UL / metadata.BatEntries.ChunkRatio |> int         // PB1個に対応するビットマップのバイト長
                        let startSBPosAtCurPB = ( int bytePos / sbBytesCntPerPB ) * sbBytesCntPerPB     // 当該LBAが属するPB BATエントリに対応する、ビットマップの開始位置
                            
                        if ArraySegment( sb, startSBPosAtCurPB, sbBytesCntPerPB ) |> Seq.exists ( (<>) 0xFFuy ) then
                            // There are still unused logical sectors.
                            loop ( wcnt + blkcnt_me.ofUInt64 1UL )
                        else
                            let struct( pbidx, _ ) = VhdxHandler.LBAtoPayloadBlockIndex ( lba + wcnt ) metadata
                            metadata.BatEntries.Payloads.[ int pbidx ] <-
                                {
                                    metadata.BatEntries.Payloads.[ int pbidx ] with
                                        State = PayloadFullyPresent;
                                }

                            // calculate update 4K sector index
                            let fpos = metadata.BatEntries.BATRegionOffset + uint64 ( 8 * int pbidx )
                            let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
                            updatedPB4K.Add secidx |> ignore

                            loop ( wcnt + blkcnt_me.ofUInt64 1UL )

        loop blkcnt_me.zero64
        updatedSB4K

    /// <summary>
    ///  Calculate the required log region size based on the number of 4K sectores.
    /// </summary>
    /// <param name="entries">
    ///  Number of 4K sectors where update information should be recorded
    /// </param>
    /// <returns>
    ///  Required log region size.
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
    ///  Log region size. It msut be greater than 1MB and multiple of 1MB.
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
        let n2 =
            let s = VhdxWriter.LogCapacityFrom4KSecCount nApprox
            s / 4096u - k
        ( min n1 n2 )

    /// <summary>
    ///  Write PayloadBATEntry data to the buffer
    /// </summary>
    /// <param name="buf">
    ///  Buffer where data will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the buffer where the data will be written.
    /// </param>
    /// <param name="">
    ///  The payload BAT entry that should be written to the buffer.
    /// </param>
    static member PayloadBlockEntryToBytes ( buf : byte[] ) ( offset : uint32 ) ( e : PayloadBATEntry ) : unit =
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
        GlbFunc.WriteUInt64LE buf offset d

    /// <summary>
    ///  Write SectorBitmapBATEntry data to the buffer
    /// </summary>
    /// <param name="buf">
    ///  Buffer where data will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the buffer where the data will be written.
    /// </param>
    /// <param name="">
    ///  The sector bitmap BAT entry that should be written to the buffer.
    /// </param>
    static member SectorBitmapEntryToBytes ( buf : byte[] ) ( offset : uint32 ) ( e : SectorBitmapBATEntry ) : unit =
        let a =
            match e.SBState with
            | BatEntryStateSB.SectorBitmapNotPresent ->
                0UL
            | BatEntryStateSB.SectorBitmapPresent ->
                6UL
        let d = e.FileOffset ||| a
        GlbFunc.WriteUInt64LE buf offset d

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
    ///  BAT entrie data that will be write to the file.
    /// </returns>
    static member CreateBATEntryTableFrom4KSectorNumber ( bat : BatEntries ) ( secnum : SEC4K_T ) : byte[] =
        let chunkRatio = bat.ChunkRatio
        let buffer = Array.zeroCreate<byte> 4096

        // Number of BAT entries written to one 4K sector
        let count = 4096UL / 8UL

        // Index of the BAT entry that will be the starting position of the output
        let sBATIdx = ( uint64 secnum - ( bat.BATRegionOffset / 4096UL ) ) * count

        // Number of BAT entries to be output
        let count2 = ( min ( sBATIdx + count ) bat.BatEntryCount ) - sBATIdx

        printfn "--- CreateBATEntryTableFrom4KSectorNumber "
        printfn "   secnum : %d" secnum
        printfn "   count : %d" count
        printfn "   sBATIdx : %d" sBATIdx
        printfn "   count2 : %d" count2

        for i in seq{ 0UL .. count2 - 1UL } do
            let idx = sBATIdx + i
            if ( idx + 1UL ) % ( chunkRatio + 1UL ) = 0UL then
                let sbidx = idx / ( chunkRatio + 1UL )
                VhdxWriter.SectorBitmapEntryToBytes buffer ( uint32 i * 8u ) bat.SectorBitmap.[ int sbidx ]
            else
                let pbidx =
                    let w = idx / ( chunkRatio + 1UL )
                    let w2 = w * chunkRatio
                    let w3 = idx % ( chunkRatio + 1UL )
                    w2 + w3
                    |> int
                printfn "bat.Payloads.Length=%d, idx=%d, pbidx=%d, chunkRatio=%d " bat.Payloads.Length idx pbidx chunkRatio
                if pbidx < bat.Payloads.Length then
                    VhdxWriter.PayloadBlockEntryToBytes buffer ( uint32 i * 8u ) bat.Payloads.[ pbidx ]
        buffer

    /// <summary>
    ///  Output the updated BATEntry
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="metadata">
    ///  Metadata for the VHDX file.
    /// </param>
    /// <param name="sec4Ks">
    ///  4K sector number representing the updated range within the BAT entry.
    /// </param>
    /// <param name="reqFileSize">
    ///  File size required after update.
    /// </param>
    /// <returns>
    ///  Next header sequence number.
    /// </returns>
    static member WriteUpdatedBAT ( fs : FileStream ) ( metadata : VhdxMetadata ) ( sec4Ks : SEC4K_T[] ) ( reqFileSize : uint64 ) ( ex : int ) : uint64 =

        let logEntryUnit = VhdxWriter.Max4KSectorCountFromLogCapacity metadata.Header.LogLength |> int
        let logOutputPos = metadata.Header.LogOffset
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
                let widx = cycle * logEntryUnit
                let wcnt = min logEntryUnit ( sec4Ks.Length - widx )
                let newLogGuid = Guid.NewGuid()
                let currentFileSize = uint64 fs.Length

                printfn "--- loop ---"
                printfn " cycle : %d" cycle
                printfn " headerSeq : %d" headerSeq
                printfn " output 4K sectores : %d .. %d" sec4Ks.[ widx ] sec4Ks.[ ( widx +  wcnt - 1 ) ]
                printfn " currentFileSize : %d" currentFileSize

                // Write log entry
                let listSec4K_BatData =
                    List.init wcnt ( fun j ->
                        let sector4KNumber = sec4Ks.[ widx + j ]
                        let data = VhdxWriter.CreateBATEntryTableFrom4KSectorNumber metadata.BatEntries sector4KNumber
                        struct ( sector4KNumber, data )
                    )
                let logEntries =
                    VhdxCorrupter.CreateLogEntry listSec4K_BatData 0u 1UL newLogGuid currentFileSize reqFileSize
                VhdxCorrupter.WriteLogEntry fs metadata 0u [] logEntries

                if ex = 3 then
                    fs.Flush()
                    fs.Close()
                    raise <| Exception( "Stop processing based on user specification. WriteUpdatedBAT, Write log entry." )

                // Update header( Update LogGuid )
                let hd1 = {
                    metadata.Header with
                        LogGuid = newLogGuid;
                        SequenceNumber = headerSeq;
                }
                let nextsn1 = VhdxHandler.UpdateHeader fs hd1

                if ex = 4 then
                    fs.Flush()
                    fs.Close()
                    raise <| Exception( "Stop processing based on user specification. WriteUpdatedBAT, Update header( Update LogGuid )." )

                // Set file size
                if currentFileSize < reqFileSize then
                    fs.SetLength( int64 reqFileSize )
                    fs.Flush()

                if ex = 5 then
                    fs.Flush()
                    fs.Close()
                    raise <| Exception( "Stop processing based on user specification. WriteUpdatedBAT, Set file size." )

                // Write metadata to file
                listSec4K_BatData
                |> List.iter ( fun struct ( sec4k, batData ) ->
                    let fpos = int64 sec4k * 4096L
                    fs.Seek( fpos, SeekOrigin.Begin ) |> ignore
                    fs.Write( batData )
                )
                fs.Flush()

                if ex = 6 then
                    fs.Flush()
                    fs.Close()
                    raise <| Exception( "Stop processing based on user specification. WriteUpdatedBAT, Write metadata to file." )

                // Update header ( Set LogGuid to zero )
                let hd2 = {
                    metadata.Header with
                        LogGuid = Guid();
                        SequenceNumber = nextsn1;
                }
                let nextsn2 = VhdxHandler.UpdateHeader fs hd2

                if ex = 7 then
                    fs.Flush()
                    fs.Close()
                    raise <| Exception( "Stop processing based on user specification. WriteUpdatedBAT, Update header ( Set LogGuid to zero )." )

                loop ( cycle + 1 ) nextsn2
            else
                headerSeq
        loop 0 metadata.Header.SequenceNumber

    /// <summary>
    ///  Output the updated sector bitmap data.
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="metadata">
    ///  Metadata for the VHDX file.
    /// </param>
    /// <param name="sec4Ks">
    ///  The updated sector bitmap and the 4K sector number to which the output will be located.
    /// </param>
    /// <returns>
    ///  Next header sequence number.
    /// </returns>
    static member WriteUpdatedSB ( fs : FileStream ) ( metadata : VhdxMetadata ) ( sec4Ks : struct( SEC4K_T * ArraySegment<byte> ) [] ) : uint64 =

        let logEntryUnit =
            VhdxWriter.Max4KSectorCountFromLogCapacity metadata.Header.LogLength
            |> int
        let logOutputPos = metadata.Header.LogOffset
        let cycleCount = ( sec4Ks.Length + ( logEntryUnit - 1 ) ) / logEntryUnit

        printfn "====================="
        printfn " WriteUpdatedSB"
        printfn " sec4Ks.Length : %d" sec4Ks.Length
        printfn " logEntryUnit : %d" logEntryUnit
        printfn " logOutputPos : %d" logOutputPos
        printfn " cycleCount : %d" cycleCount

        let rec loop ( cycle : int ) ( headerSeq : uint64 ) =
            if cycle < cycleCount then
                let widx = cycle * logEntryUnit
                let wcnt = min logEntryUnit ( sec4Ks.Length - widx )
                let newLogGuid = Guid.NewGuid()
                let currentFileSize = uint64 fs.Length

                printfn "--- loop ---"
                printfn " cycle : %d" cycle
                printfn " headerSeq : %d" headerSeq
                printfn " output 4K sectores : %d .. %d" ( sec4Ks.[ widx ] |> ( fun struct( a, _ ) -> a ) ) ( sec4Ks.[ ( widx +  wcnt - 1 ) ] |> ( fun struct( a, _ ) -> a ) )
                printfn " currentFileSize : %d" currentFileSize

                // Write log entry
                let listSec4K_SBData =
                        sec4Ks.[ widx .. widx + wcnt - 1 ]
                        |> Array.map ( fun struct( s, d ) -> struct( s, d.ToArray() ) )
                        |> Array.toList
                let logEntries = VhdxCorrupter.CreateLogEntry listSec4K_SBData 0u 1UL newLogGuid currentFileSize currentFileSize
                VhdxCorrupter.WriteLogEntry fs metadata 0u [] logEntries

                // Update header( Update LogGuid )
                let hd1 = {
                    metadata.Header with
                        LogGuid = newLogGuid;
                        SequenceNumber = headerSeq;
                }
                let nextsn1 = VhdxHandler.UpdateHeader fs hd1

                // Write metadata to file
                listSec4K_SBData
                |> List.iter ( fun struct ( sec4k, sbData ) ->
                    let fpos = sec4k * 4096UL |> int64
                    fs.Seek( fpos, SeekOrigin.Begin ) |> ignore
                    fs.Write( sbData )
                )
                fs.Flush()

                // Update header ( Set LogGuid to zero )
                let hd2 = {
                    metadata.Header with
                        LogGuid = Guid();
                        SequenceNumber = nextsn1;
                }
                VhdxHandler.UpdateHeader fs hd2
                |> loop ( cycle + 1 )
            else
                headerSeq
        loop 0 metadata.Header.SequenceNumber
   
    /// <summary>
    ///  Write the RAW data to the allocated payload block.
    /// </summary>
    /// <param name="rawfs">
    ///  File stream for the RAW data file.
    /// </param>
    /// <param name="vhdxfs">
    ///  File stream for the HDX file.
    /// </param>
    /// <param name="lba">
    ///  Location where RAW data is written.
    /// </param>
    /// <param name="metadata">
    ///  Metadata of VHDX file.
    /// </param>
    static member OutputRawData ( rawfs : FileStream ) ( vhdxfs : FileStream ) ( lba : BLKCNT64_T ) ( metadata : VhdxMetadata ) =
        let sectorSize = metadata.VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt64
        let rawDataSec = uint64 rawfs.Length / sectorSize |> blkcnt_me.ofUInt64
        let buffer = Array.zeroCreate< byte >( int sectorSize )
        rawfs.Seek( 0L, SeekOrigin.Begin ) |> ignore

        let rec loop ( curlba : BLKCNT64_T ) =
            if curlba < lba + rawDataSec then
                let struct( badIndex, offsetInBat ) = VhdxHandler.LBAtoPayloadBlockIndex curlba metadata
                let pbStartPos = metadata.BatEntries.Payloads.[ int badIndex ].FileOffset
                let offsetInPB = ( uint64 offsetInBat ) * ( uint64 sectorSize )
                rawfs.ReadExactly( buffer, 0, int sectorSize )
                vhdxfs.Seek( int64 ( pbStartPos + offsetInPB ), SeekOrigin.Begin ) |> ignore
                vhdxfs.Write( buffer )
                loop ( curlba + blkcnt_me.ofUInt64 1UL )
            else
                ()
        loop lba


    /// <summary>
    ///  Write RAW data to VHDX file.
    /// </summary>
    /// <param name="vhdxFileName">
    ///  VHDX file what to be updated.
    /// </param>
    /// <param name="rawFileName">
    ///  RAW data file what to be wrote.
    /// </param>
    /// <param name="lba">
    ///  Address in virtual disk whete raw data to be wrote.
    /// </param>
    static member Write ( vhdxFileName : string ) ( rawFileName : string ) ( lba : BLKCNT64_T ) ( ex : int ) : unit =

        let metadata = VhdxReader.ReadVhdx vhdxFileName
        let sectorSize = metadata.VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt64
        let vdsb = metadata.VirtualDiskInfo.VirtualDiskSize
        let vdss = vdsb / uint64 sectorSize |> blkcnt_me.ofUInt64
        use rawfs = new FileStream( rawFileName, FileMode.Open, FileAccess.Read, FileShare.None )
        let rawDataLength = rawfs.Length |> uint64
        let rawDataSec = rawDataLength / uint64 sectorSize |> blkcnt_me.ofUInt64

        printfn ""
        printfn "========================================================"
        printfn "Write raw data to VHDX file"
        printfn "VHDX file name : %s" vhdxFileName
        printfn "RAW file name : %s" rawFileName
        printfn "LBA : %d" lba
        printfn "RAW data length(byte) : %d" rawDataLength
        printfn "RAW data length(sector) : %d" rawDataSec
        printfn "VHDX sector sizse : %d" sectorSize
        printfn "Virtual Disk Size(byte) : %d" vdsb
        printfn "Virtual Disk Size(sector) : %d" vdss
        printfn "========================================================"

        if ( blkcnt_me.toUInt64 rawDataSec * uint64 sectorSize ) <> rawDataLength then
            raise <| Exception( "RAW data length must be multiple of sector size." )
        if lba > vdss then
            raise <| Exception( "LBA must be less than or equals virtual disk size." )
        if rawDataSec > vdss then
            raise <| Exception( "RAW data length must be less than or equals virtual disk size." )
        if rawDataSec + lba > vdss then
            raise <| Exception( "RAW data length + LBA must be less than or equals virtual disk size." )

        // Flash log entries
        let wSecNum =
            if metadata.LogInfo.Length > 0 then
                printfn "=== Need to replay log. ==="
                VhdxChecker.Check vhdxFileName
                printfn "=== Replay log complete. ==="
                metadata.Header.SequenceNumber + 2UL
            else
                metadata.Header.SequenceNumber

        if ex = 1 then
            raise <| Exception( "Stop processing based on user specification. : Flash log entries." )

        // Open VHDX file
        use vhdxfs = new FileStream( vhdxFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None )

        // Update FileWriteGuid and DataWriteGuid
        printfn "=== Update FileWriteGuid and DataWriteGuid. ==="
        let metadata2 =
            let hd = {
                metadata.Header with
                    FileWriteGuid = Guid.NewGuid();
                    DataWriteGuid = Guid.NewGuid();
                    LogGuid = Guid();
                    SequenceNumber = wSecNum + 1UL;
            }
            printfn "  FileWriteGuid : %s" ( hd.FileWriteGuid.ToString "D" )
            printfn "  DataWriteGuid : %s" ( hd.DataWriteGuid.ToString "D" )
            printfn "  SequenceNumber : %d" ( hd.SequenceNumber )
            let nextsn = VhdxHandler.UpdateHeader vhdxfs hd
            printfn "  Next sequence number : %d" nextsn
            {
                metadata with
                    Header = {
                        metadata.Header with
                            SequenceNumber = nextsn;    // Set the next sequence number to use.
                }
            }

        if ex = 2 then
            vhdxfs.Flush()
            vhdxfs.Close()
            raise <| Exception( "Stop processing based on user specification. Update FileWriteGuid and DataWriteGuid." )

        // Allocate Payload block. metadata2 will be updated.
        printfn "=== Allocate Payload block ==="
        let struct( updatedPB4K, requiredFileSize ) =
            VhdxWriter.AllocatePayloadBlock ( uint64 vhdxfs.Length ) metadata2 lba rawDataSec

        // Update sector bitmap.
        let updated4KSecsForSB =
            if metadata2.VirtualDiskInfo.HasParent then
                printfn "=== Update sector bitmap ==="
                VhdxWriter.UpdateSectorBitmap metadata2 updatedPB4K lba rawDataSec
                |> Seq.map ( fun itr -> struct( itr.Key, itr.Value ) )
                |> Seq.toArray
            else
                Array.empty
        let updated4KSecsForBAT = Seq.toArray updatedPB4K

        // Output the updated BATEntry
        printfn "=== Output the updated BATEntry ==="
        let metadata3 =
            let nextsn = VhdxWriter.WriteUpdatedBAT vhdxfs metadata2 updated4KSecsForBAT requiredFileSize ex
            {
                metadata with
                    Header = {
                        metadata.Header with
                            SequenceNumber = nextsn;    // Set the next sequence number to use.
                    }
            }

        if ex = 8 then
            vhdxfs.Flush()
            vhdxfs.Close()
            raise <| Exception( "Stop processing based on user specification. Output the updated BATEntry." )

        // Output sector bitmap.
        let metadata4 =
            if updated4KSecsForSB.Length > 0 then
                printfn "=== Output the updated sector bitmap ==="
                let nextsn = VhdxWriter.WriteUpdatedSB vhdxfs metadata3 updated4KSecsForSB
                {
                    metadata with
                        Header = {
                            metadata.Header with
                                SequenceNumber = nextsn;    // Set the next sequence number to use.
                        }
                }
            else
                metadata3

        if ex = 9 then
            vhdxfs.Flush()
            vhdxfs.Close()
            raise <| Exception( "Stop processing based on user specification. Output sector bitmap." )

        // Output RAW data
        VhdxWriter.OutputRawData rawfs vhdxfs lba metadata4

