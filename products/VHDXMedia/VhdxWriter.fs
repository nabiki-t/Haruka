//=============================================================================
// Haruka Software Storage.
// VhdxChecker.fs : Implement a function to write RAW-format data to a specified location within a VHDX file.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

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
    static member GetUsedRegions ( payloads : PayloadBATEntry[] ) : uint64 [] =
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
    /// <param name="structures">
    ///  VHDX file controll structures.
    /// </param>
    /// <param name="intervals">
    ///   Array of used range offsets.
    /// </param>
    /// <returns>
    ///  Start and end positions of the used area.
    ///  The range is represented as a half-open interval.
    ///  Therefore, the end position is not included in the used region.
    /// </returns>
    static member MergeIntervals ( structures : VhdxStructures ) ( intervals : uint64 [] ) : struct ( uint64 * uint64 ) [] =
        let payloadBlockSize = structures.VDI.PayloadBlockSize |> uint64
        let initial =
            [|
                // header(1MB)
                struct( 0UL, 0x100000UL );

                // log
                let a = structures.ImmHeader.LogOffset
                let b = uint64 structures.ImmHeader.LogLength
                if a + b < a then raise <| VhdxMediaException( "Unexpected the log offset or the log length value." )
                struct( a, a + b )

                // Regions( metadata, BAT, etc. )
                for i in structures.Region.Entries do
                    let a = i.FileOffset
                    let b = uint64 i.Length
                    if a + b < a then raise <| VhdxMediaException( "Unexpected the region offset or the region length value." )
                    struct( a, a + b )

                // Sector bitmap
                for i in structures.BAT.SectorBitmap do
                    if i.SBState = BatEntryStateSB.SectorBitmapPresent then
                        if i.FileOffset + 0x100000UL < i.FileOffset then
                            raise <| VhdxMediaException( "Unexpected the sector bitmap offset value." )
                        struct( i.FileOffset, i.FileOffset + 0x100000UL ) // 1MB length

                // payload block
                for i in intervals do
                    if i + payloadBlockSize < i then
                        raise <| VhdxMediaException( "Unexpected the payload block offset value." )
                    struct( i, i + payloadBlockSize );
            |]
            |> Array.sortBy ( fun struct( a, _ ) -> a )

        let struct( s, e ) = initial.[0]
        Array.unfold ( fun struct( idx, ws1, we1 ) ->
            let rec loop ( idx2 : int ) ( ws2 : uint64 ) ( we2 : uint64 ) =
                if idx2 < initial.Length then
                    let struct( s, e ) = initial.[ idx2 ]
                    if s <= we2 then
                        loop ( idx2 + 1 ) ( ws2 ) ( max we2 e )
                    else
                        Some( struct( ws2, we2 ), struct( idx2 + 1, s, e ) )
                elif idx2 = initial.Length then
                    Some( struct( ws2, we2 ), struct( idx2 + 1, 0UL, 0UL ) )
                else
                    None
            loop idx ws1 we1
        ) struct( 1, s, e )

    /// <summary>
    ///  Identify unused areas.
    /// </summary>
    /// <param name="structures">
    ///  VHDX file structures data.
    /// </param>
    /// <param name="maxPBCount">
    ///  Specify the maximum number of payload blocks to allocate.
    /// </param>
    /// <returns>
    ///  The start position of the unused area.
    ///  The size of a single unused area is the payload block length.
    /// </returns>
    static member BuildFreeList ( structures : VhdxStructures ) ( maxPBCount : uint64 ) : uint64 [] =

        let mb = 1048576UL
        let payloadBlockSize = uint64 structures.VDI.PayloadBlockSize
        if payloadBlockSize = 0UL then
            raise <| VhdxMediaException( "Unexpected the payload block size value." )

        let fileSize = structures.LastFileSize
        let chunkCount = Functions.CeilDiv fileSize mb

        // The maximum size of a virtual disk image is 64 TB.
        // While it is expected that the physical file size will not significantly exceed this, there is no guarantee.
        if chunkCount > uint64 Int32.MaxValue * 8UL then
            raise <| VhdxMediaException( "The file is too large to build the free-space bitmap." )

        let bitmap = PooledBuffer.RentAndInit ( Functions.CeilDiv chunkCount 8UL |> int )
        try
            let markUsed ( start : uint64 ) ( finish : uint64 ) : unit =
                if finish > start && start < fileSize then
                    let firstChunk = start / mb
                    let finishChunk = min chunkCount ( Functions.CeilDiv finish mb )
                    for chunk in firstChunk .. finishChunk - 1UL do
                        let byteIndex = int ( chunk / 8UL )
                        let bitMask = 1uy <<< int ( chunk % 8UL )
                        bitmap.Array.[ byteIndex ] <- bitmap.Array.[ byteIndex ] ||| bitMask

            let markRange ( start : uint64 ) ( length : uint64 ) ( errorMessage : string ) : unit =
                let finish = start + length
                if finish < start then
                    raise <| VhdxMediaException( errorMessage )
                markUsed start finish

            // Header
            markUsed 0UL mb

            // Log
            markRange structures.ImmHeader.LogOffset ( uint64 structures.ImmHeader.LogLength ) "Unexpected the log offset or the log length value."

            // Regions (metadata, BAT, etc.)
            for region in structures.Region.Entries do
                markRange region.FileOffset ( uint64 region.Length ) "Unexpected the region offset or the region length value."

            // Sector bitmap
            for entry in structures.BAT.SectorBitmap do
                if entry.SBState = BatEntryStateSB.SectorBitmapPresent then
                    markRange entry.FileOffset mb "Unexpected the sector bitmap offset value."

            // Payload blocks
            for entry in structures.BAT.Payloads do
                match entry.State with
                | PayloadFullyPresent
                | PayloadPartiallyPresent ->
                    markRange entry.FileOffset payloadBlockSize "Unexpected the payload block offset value."
                | _ -> ()

            let rec findUsedChunk ( chunk : uint64 ) ( endChunk : uint64 ) : uint64 voption =
                if chunk < endChunk then
                    let byteIndex = int ( chunk / 8UL )
                    let bitMask = 1uy <<< int ( chunk % 8UL )
                    if ( bitmap.[byteIndex] &&& bitMask ) <> 0uy then
                        ValueSome chunk
                    else
                        findUsedChunk ( chunk + 1UL ) endChunk
                else
                    ValueNone

            let rec findNextFree ( candidate : uint64 ) : struct( uint64 * uint64 ) voption =
                if candidate > fileSize || payloadBlockSize > fileSize - candidate then
                    ValueNone
                else
                    let endOffset = candidate + payloadBlockSize
                    let firstChunk = candidate / mb
                    let endChunk = Functions.CeilDiv endOffset mb
                    match findUsedChunk firstChunk endChunk with
                    | ValueNone ->
                        ValueSome struct ( candidate, endOffset )
                    | ValueSome usedChunk ->
                        let nextChunk = usedChunk + 1UL
                        let nextCandidate =
                            if nextChunk > UInt64.MaxValue / mb then
                                fileSize
                            else
                                min fileSize ( nextChunk * mb )
                        if nextCandidate <= candidate then
                            ValueNone
                        else
                            findNextFree nextCandidate

            Array.unfold ( fun ( candidate, allocated ) ->
                if allocated >= maxPBCount then
                    None
                else
                    match findNextFree candidate with
                    | ValueSome struct ( freeBlock, nextCandidate ) ->
                        Some ( freeBlock, ( nextCandidate, allocated + 1UL ) )
                    | ValueNone ->
                        None
            ) ( 0UL, 0UL )
        finally
            bitmap.Return()

    /// <summary>
    ///  If any payload blocks that will be needed in the future are not yet allocated,
    ///  those payload blocks will be newly allocated.
    ///  This function only updates the BAT entry in memory.
    /// </summary>
    /// <param name="structures">
    ///  VHDX structures data. The value of BAT is updated.
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
    static member AllocatePayloadBlock ( structures : VhdxStructures ) ( lba : BLKCNT64_T ) ( cnt : BLKCNT64_T ) : struct ( HashSet<SEC4K_T> * uint64 ) =
        let hasParent = structures.VDI.HasParent
        let pbSize = uint64 structures.VDI.PayloadBlockSize
        let allocPBStat = 
            if hasParent then
                PayloadPartiallyPresent
            else
                PayloadFullyPresent;
        let updated4KSecs = HashSet<SEC4K_T>()
        let maxRequirePBCnt =
            let w = pbSize / Blocksize.toUInt64 structures.VDI.LogicalSectorSize
            ( Functions.CeilDiv ( uint64 cnt ) w ) + 1UL

        // build free list
        let freeList = VhdxWriter.BuildFreeList structures maxRequirePBCnt |> Array.toList

        // allocate space and update BAT entry
        let rec loop ( wcnt : BLKCNT64_T ) ( restFreeList : uint64 list ) ( gfs : uint64 ) : uint64 =
            if wcnt < cnt then
                let ( nextFL, nextgfs ) =
                    VhdxWriter.UpdatePBForAllocate structures ( lba + wcnt ) restFreeList gfs updated4KSecs
                loop ( wcnt + blkcnt_me.ofUInt64 1UL ) nextFL nextgfs
            else
                gfs
        let requiredFileSize = loop blkcnt_me.zero64 freeList structures.LastFileSize

        struct ( updated4KSecs, requiredFileSize )

    /// <summary>
    ///  Allocate payload blocks to enable data writing to the specified LBA.
    /// </summary>
    /// <param name="structures">
    ///  VHDX structures data. The value of BAT is updated.
    /// </param>
    /// <param name="lba">
    ///  LBA of the location where data is to be written.
    /// </param>
    /// <param name="restFreeList">
    ///  A list of free areas available for allocation.
    /// </param>
    /// <param name="gfs">
    ///  The minimum file size required at this point in time.
    /// </param>
    /// <param name="updated4KSecs">
    ///  List of updated 4K sectors.
    /// </param>
    /// <returns>
    ///  Returns a pair consisting of a list of free areas and the requested file size.
    /// </returns>
    static member UpdatePBForAllocate
        ( structures : VhdxStructures )
        ( lba : BLKCNT64_T )
        ( restFreeList : uint64 list )
        ( gfs : uint64 )
        ( updated4KSecs : HashSet<SEC4K_T> )
        : ( uint64 list * uint64 ) =

        let pbSize = uint64 structures.VDI.PayloadBlockSize
        let allocPBStat = if structures.VDI.HasParent then PayloadPartiallyPresent else PayloadFullyPresent;
        let struct( pbidx, _ ) = VhdxCommons.LBAtoPayloadBlockIndex lba structures
        let pb = structures.BAT.Payloads.[ int32 pbidx ]

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
            structures.BAT.Payloads.[ int32 pbidx ] <-
                {
                    pb with
                        State = allocPBStat;
                        FileOffset = fileoffset;
                }

            // calculate update 4K sector index
            let fpos = structures.BAT.BATRegionOffset + ( 8UL * pb.BatEntryIndex )
            let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
            updated4KSecs.Add secidx |> ignore

            // To next LBA block
            nextFL, nextgfs

        | PayloadFullyPresent
        | PayloadPartiallyPresent ->
            // Already allocated
            restFreeList, gfs

    /// <summary>
    ///  Mark the sector bitmap corresponding to the area to be updated as used.
    /// </summary>
    /// <param name="structures">
    ///  VHDX structures data. The value of BAT is updated.
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
        ( structures : VhdxStructures )
        ( updatedPB4K : HashSet<SEC4K_T> )
        ( lba : BLKCNT64_T )
        ( cnt : BLKCNT64_T )
        : Dictionary< SEC4K_T, ArraySegment<byte> > =

        let updatedSB4K = Dictionary< SEC4K_T, ArraySegment<byte> >()
        for i in 0UL .. blkcnt_me.toUInt64 cnt - 1UL do
            let wcnt = blkcnt_me.ofUInt64 i
            VhdxWriter.UpdateSBForAllocate structures ( lba + wcnt ) updatedPB4K updatedSB4K |> ignore
        updatedSB4K

    /// <summary>
    ///  Mark the sector corresponding to the specified LBA as used in the sector bitmap.
    /// </summary>
    /// <param name="structures">
    ///  VHDX structures data. The value of BAT is updated.
    /// </param>
    /// <param name="lba">
    ///  LBA newly put into use.
    /// </param>
    /// <param name="updatedPB4K">
    ///  Payload Block Update Information.
    /// </param>
    /// <param name="updatedSB4K">
    ///  Sector bitmap update information.
    /// </param>
    /// <returns>
    ///  Returns false if the specified LBA was already marked as used.
    /// </returns>
    static member UpdateSBForAllocate ( structures : VhdxStructures ) ( lba : BLKCNT64_T ) ( updatedPB4K : HashSet<SEC4K_T> ) ( updatedSB4K : Dictionary< SEC4K_T, ArraySegment<byte> > ) : bool =
        let bat = structures.BAT
        let struct( sbIdx, bytePos, bitPos ) = VhdxCommons.LBAtoSectorBitmapIndex lba structures
        let sbEntry = bat.SectorBitmap.[ int32 sbIdx ]
        let sb = sbEntry.Bitmap
        let sbFileOffset = sbEntry.FileOffset
        let bitValue = ( sb.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy

        if bitValue <> 0uy then
            // If it is already marked as in use, there is nothing to do.
            false
        else
            // Set the bit flag to indicate that it is in use.
            sb.[ int32 bytePos ] <- sb.[ int32 bytePos ] ||| ( 1uy <<< ( int32 bitPos ) )

            // Record information on updated 4K sectors.
            let s = ( sbFileOffset + ( uint64 bytePos ) ) / 4096UL |> sec4k_me.ofUInt64
            updatedSB4K.TryAdd( s, ArraySegment( sb, 0, 4096 ) ) |> ignore

            if sb.[ int32 bytePos ] <> 0xFFuy then
                () // Unless the current byte is 0xFF, the payload BAT entry will not become PayloadFullyPresent.
            else
                // Determine whether all sector bitmaps belonging to the payload block BAT entry associated with the LBA in question have been set to 1.

                // Bitmap byte length corresponding to one PB
                let sbBytesCntPerPB = 1048576UL / bat.ChunkRatio |> int32

                // The starting position of the bitmap corresponding to the PB BAT entry to which the LBA in question belongs.
                let startSBPosAtCurPB = ( int32 bytePos / sbBytesCntPerPB ) * sbBytesCntPerPB
                
                let span = ReadOnlySpan<byte>( sb, startSBPosAtCurPB, sbBytesCntPerPB )
                if span.IndexOfAnyExcept( 0xFFuy ) <> -1 then
                    () // There are still unused logical sectors.
                else
                    let struct( pbidx, _ ) = VhdxCommons.LBAtoPayloadBlockIndex lba structures
                    bat.Payloads.[ int32 pbidx ] <-
                        {
                            bat.Payloads.[ int32 pbidx ] with
                                State = PayloadFullyPresent;
                        }

                    // calculate update 4K sector index
                    let fpos = bat.BATRegionOffset + ( 8UL * bat.Payloads.[ int32 pbidx ].BatEntryIndex )
                    let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
                    updatedPB4K.Add secidx |> ignore
            true

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
        ByteFunc.WriteU64LE buf offset d

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
        ByteFunc.WriteU64LE buf offset d

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

        for i in 0UL .. count2 - 1UL do
            let idx = sBATIdx + i
            if ( idx + 1UL ) % ( chunkRatio + 1UL ) = 0UL then
                let sbidx = idx / ( chunkRatio + 1UL )
                VhdxWriter.SectorBitmapEntryToBytes buffer ( uint32 i * 8u ) bat.SectorBitmap.[ int32 sbidx ]
            else
                let pbidx =
                    let w = idx / ( chunkRatio + 1UL )
                    let w2 = w * chunkRatio
                    let w3 = idx % ( chunkRatio + 1UL )
                    w2 + w3
                    |> int32
                if pbidx < bat.Payloads.Length then
                    VhdxWriter.PayloadBlockEntryToBytes buffer ( uint32 i * 8u ) bat.Payloads.[ pbidx ]
        buffer

    /// <summary>
    ///  Output the updated BATEntry
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="structures">
    ///  VHDX file structures data.
    /// </param>
    /// <param name="verhd">
    ///  Latest header values.
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
    static member WriteUpdatedBAT ( fa : FileAccessor ) ( structures : VhdxStructures ) ( verhd : VhdxMutableHeader ) ( sec4Ks : SEC4K_T[] ) ( reqFileSize : uint64 ) : Task<VhdxMutableHeader> =
        task {
            let logEntryUnit = VhdxCorrupter.Max4KSectorCountFromLogCapacity structures.ImmHeader.LogLength |> int
            let cycleCount = Functions.CeilDiv sec4Ks.Length logEntryUnit

            let ps = PseudoSeqStat< struct( int32 * VhdxMutableHeader ), VhdxMutableHeader >( struct( 0, verhd ) )
            for struct( cycle, wverhd ) in ps do
                if cycle < cycleCount then
                    let widx = cycle * logEntryUnit
                    let wcnt = min logEntryUnit ( sec4Ks.Length - widx )
                    let newLogGuid = Guid.NewGuid()
                    let currentFileSize = fa.GetFileSize()

                    // Write log entry
                    let listSec4K_BatData =
                        List.init wcnt ( fun j ->
                            let sector4KNumber = sec4Ks.[ widx + j ]
                            let data = VhdxWriter.CreateBATEntryTableFrom4KSectorNumber structures.BAT sector4KNumber
                            struct ( sector4KNumber, data )
                        )
                    let logEntries =
                        VhdxCorrupter.CreateLogEntry listSec4K_BatData 0u 1UL newLogGuid currentFileSize reqFileSize
                    do! VhdxCorrupter.WriteLogEntry fa structures.ImmHeader 0u List.empty logEntries

                    // Update header( Update LogGuid )
                    let! wverhd1 = VhdxCommons.UpdateHeader fa structures.ImmHeader { wverhd with LogGuid = Guid.NewGuid() }

                    // Set file size
                    if currentFileSize < reqFileSize then
                        do! fa.SetFileSize( reqFileSize )

                    // Write BAT data to file
                    for struct ( sec4k, batData ) in listSec4K_BatData do
                        do! fa.Write ( uint64 sec4k * 4096UL ) ( ArraySegment batData )

                    // Update header ( Set LogGuid to zero )
                    let! wverhd2 = VhdxCommons.UpdateHeader fa structures.ImmHeader { wverhd1 with LogGuid = Guid() }

                    ps.Continue( struct( cycle + 1, wverhd2 ) )
                else
                    ps.Break wverhd
            return ps.LastValue |> _.Value
        }

    /// <summary>
    ///  Output the updated sector bitmap data.
    /// </summary>
    /// <param name="fa">
    ///  File accessor for the VHDX file.
    /// </param>
    /// <param name="structures">
    ///  The VHDX file structures data.
    /// </param>
    /// <param name="verhd">
    ///  Latest header values.
    /// </param>
    /// <param name="sec4Ks">
    ///  The updated sector bitmap and the 4K sector number to which the output will be located.
    /// </param>
    /// <returns>
    ///  Next header sequence number.
    /// </returns>
    static member WriteUpdatedSB ( fa : FileAccessor ) ( structures : VhdxStructures ) ( verhd : VhdxMutableHeader ) ( sec4Ks : struct( SEC4K_T * ArraySegment<byte> ) [] ) : Task<VhdxMutableHeader> =
        task {
            let logEntryUnit =
                VhdxCorrupter.Max4KSectorCountFromLogCapacity structures.ImmHeader.LogLength
                |> int
            let cycleCount = Functions.CeilDiv sec4Ks.Length logEntryUnit

            let ps = PseudoSeqStat< struct ( int32 * VhdxMutableHeader ), VhdxMutableHeader >( struct( 0, verhd ) )
            for struct ( cycle, wverhd ) in ps do
                if cycle < cycleCount then
                    let widx = cycle * logEntryUnit
                    let wcnt = min logEntryUnit ( sec4Ks.Length - widx )
                    let newLogGuid = Guid.NewGuid()
                    let currentFileSize = structures.LastFileSize

                    // Write log entry
                    let listSec4K_SBData =
                            sec4Ks.[ widx .. widx + wcnt - 1 ]
                            |> Array.map ( fun struct( s, d ) -> struct( s, d.ToArray() ) )
                            |> Array.toList
                    let logEntries = VhdxCorrupter.CreateLogEntry listSec4K_SBData 0u 1UL newLogGuid currentFileSize currentFileSize
                    do! VhdxCorrupter.WriteLogEntry fa structures.ImmHeader 0u List.empty logEntries

                    // Update header( Update LogGuid )
                    let! wverhd1 = VhdxCommons.UpdateHeader fa structures.ImmHeader { wverhd with LogGuid = Guid.NewGuid() }

                    // Write updated structures data to file
                    for struct ( sec4k, sbData ) in listSec4K_SBData do
                        do! fa.Write ( uint64 sec4k * 4096UL ) ( ArraySegment sbData )

                    // Update header ( Set LogGuid to zero )
                    let! wverhd2 = VhdxCommons.UpdateHeader fa structures.ImmHeader { wverhd1 with LogGuid = Guid.NewGuid() }
                    ps.Continue( struct( cycle + 1, wverhd2 ) )
                else
                    ps.Break wverhd
            return ps.LastValue |> _.Value
        }
   
    /// <summary>
    ///  Write the RAW data to the allocated payload block.
    /// </summary>
    /// <param name="rawfs">
    ///  File stream for the RAW data file.
    /// </param>
    /// <param name="vhdxfs">
    ///  File accessor for the HDX file.
    /// </param>
    /// <param name="lba">
    ///  Location where RAW data is written.
    /// </param>
    /// <param name="structures">
    ///  The VHDX file structures data.
    /// </param>
    static member OutputRawData ( rawfs : FileStream ) ( vhdxfs : FileAccessor ) ( lba : BLKCNT64_T ) ( structures : VhdxStructures ) : Task =
        task {
            let sectorSize = structures.VDI.LogicalSectorSize |> Blocksize.toUInt64
            let rawDataSec = uint64 rawfs.Length / sectorSize
            let buffer = Array.zeroCreate< byte >( int32 sectorSize )
            rawfs.Seek( 0L, SeekOrigin.Begin ) |> ignore

            for i in 0UL .. rawDataSec - 1UL do
                let curlba = blkcnt_me.ofUInt64 i + lba
                let struct( badIndex, offsetInBat ) = VhdxCommons.LBAtoPayloadBlockIndex curlba structures
                let pbStartPos = structures.BAT.Payloads.[ int32 badIndex ].FileOffset
                let offsetInPB = ( uint64 offsetInBat ) * sectorSize
                rawfs.ReadExactly( buffer, 0, int32 sectorSize )
                do! vhdxfs.Write ( pbStartPos + offsetInPB ) ( ArraySegment buffer )
        }

    /// <summary>
    ///  Write RAW data to VHDX file.
    /// </summary>
    /// <param name="vhdxFile">
    ///  VHDX file what to be updated.
    /// </param>
    /// <param name="rawFileName">
    ///  RAW data file what to be wrote.
    /// </param>
    /// <param name="lba">
    ///  Address in virtual disk whete raw data to be wrote.
    /// </param>
    static member Write ( vhdxFile : FileAccessor ) ( rawFileName : string ) ( lba : BLKCNT64_T ) : Task =
        task {
            let! structures = VhdxReader.ReadVhdx vhdxFile
            let sectorSize = structures.VDI.LogicalSectorSize |> Blocksize.toUInt64
            let vdsb = structures.VDI.VirtualDiskSize
            let vdss = vdsb / uint64 sectorSize |> blkcnt_me.ofUInt64
            use rawfs = new FileStream( rawFileName, FileMode.Open, FileAccess.Read, FileShare.None )
            let rawDataLength = rawfs.Length |> uint64
            let rawDataSec = rawDataLength / uint64 sectorSize |> blkcnt_me.ofUInt64

            if ( blkcnt_me.toUInt64 rawDataSec * uint64 sectorSize ) <> rawDataLength then
                raise <| VhdxMediaException( rawFileName, "RAW data length must be multiple of sector size." )
            if lba > vdss then
                let msg = sprintf "LBA(%d) must be less than or equals virtual disk total sector count(%d)." lba vdss
                raise <| VhdxMediaException( vhdxFile.FileName, msg )
            if rawDataSec > vdss then
                let msg = sprintf "RAW data sector count(%d) must be less than or equals virtual disk total sector count(%d)." rawDataSec vdss
                raise <| VhdxMediaException( vhdxFile.FileName, msg )
            if rawDataSec + lba > vdss || rawDataSec + lba < rawDataSec then
                let msg = sprintf "RAW data sector count(%d) + LBA(%d) must be less than or equals virtual disk total sector count(%d)." rawDataSec lba vdss
                raise <| VhdxMediaException( vhdxFile.FileName, msg )

            // Flash log entries
            let! verhd1 =
                if structures.Log.Length > 0 then
                    VhdxChecker.FlushLog vhdxFile structures
                else
                    Task.FromResult structures.LoadedVarHeader

            // Update FileWriteGuid and DataWriteGuid
            let! verhd2 = VhdxCommons.UpdateFileWriteGuidAndDataWriteGuid vhdxFile structures verhd1

            // Allocate Payload block. structures2 will be updated.
            let struct( updatedPB4K, requiredFileSize ) =
                VhdxWriter.AllocatePayloadBlock structures lba rawDataSec

            // Update sector bitmap.
            let updated4KSecsForSB =
                if structures.VDI.HasParent then
                    VhdxWriter.UpdateSectorBitmap structures updatedPB4K lba rawDataSec
                    |> Seq.map ( fun itr -> struct( itr.Key, itr.Value ) )
                    |> Seq.toArray
                else
                    Array.empty
            let updated4KSecsForBAT = Seq.toArray updatedPB4K

            // Output the updated BATEntry
            let! verhd3 = VhdxWriter.WriteUpdatedBAT vhdxFile structures verhd2 updated4KSecsForBAT requiredFileSize
 
            // Output sector bitmap.
            let! verhd4 =
                if updated4KSecsForSB.Length > 0 then
                    VhdxWriter.WriteUpdatedSB vhdxFile structures verhd3 updated4KSecsForSB
                else
                    Task.FromResult verhd3

            // Output RAW data
            do! VhdxWriter.OutputRawData rawfs vhdxFile lba structures
        }

