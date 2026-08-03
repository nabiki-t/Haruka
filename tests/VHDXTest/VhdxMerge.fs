namespace VhdxLibrary

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons

/// <summary>
///  Delete a snapshot.
/// </summary>
type VhdxMerge() =

    /// <summary>
    ///  Copy the payload data of the specified area.
    /// </summary>
    /// <param name="srcFa">
    ///  The FileAccessor for the source VHDX file
    /// </param>
    /// <param name="srcStr">
    ///  The VHDX file structures data for the source VHDX file.
    /// </param>
    /// <param name="dstFa">
    ///  The FileAccessor for the destination VHDX file
    /// </param>
    /// <param name="dstStr">
    ///  The VHDX file structures data for the destination VHDX file.
    /// </param>
    /// <param name="lba">
    ///  Start sector number of the area to be copied.
    /// </param>
    /// <param name="cnt">
    ///  The length of the area to be copied. The value of cnt is at most 524,288.
    /// </param>
    /// <remarks>
    ///  It is assumed that the area specified by lba and cnt is a contiguous area on the physical medium.
    /// </remarks>
    static member CopyData ( srcFa : FileAccessor ) ( srcStr : VhdxStructures ) ( dstFa : FileAccessor ) ( dstStr : VhdxStructures ) ( lba : BLKCNT64_T ) ( cnt : BLKCNT32_T ) : Task =
        task {
            let buf = PooledBuffer.Rent 1048576
            let blocksize = Blocksize.toUInt32 srcStr.VDI.LogicalSectorSize
            let lbPerChunk = 1048576u / blocksize    // 2048か256
            let chunkCount = ( uint32 cnt + lbPerChunk - 1u ) / lbPerChunk
            let copyLength = uint64 cnt * uint64 blocksize

            printfn "  CopyData( lba=%d, cnt=%d )" lba cnt
            printfn "  source file sizse : %d" ( srcFa.GetFileSize() )
            printfn "  destination file sizse : %d" ( dstFa.GetFileSize() )
            printfn "  blocksize : %d" blocksize
            printfn "  lbPerChunk : %d" lbPerChunk
            printfn "  chunkCount : %d" chunkCount
            printfn "  copyLength : %d" copyLength

            let srcStartBytePos =
                let struct( idx, off ) = VhdxHandler.LBAtoPayloadBlockIndex lba srcStr
                let pboffset = srcStr.BAT.Payloads.[ int idx ].FileOffset
                let inpboff = uint64 off * uint64 blocksize
                ( pboffset + inpboff )

            let dstStartBytePos =
                let struct( idx, off ) = VhdxHandler.LBAtoPayloadBlockIndex lba dstStr
                let pboffset = dstStr.BAT.Payloads.[ int idx ].FileOffset
                let inpboff = uint64 off * uint64 blocksize
                ( pboffset + inpboff )

            printfn "  srcStartBytePos : %d" srcStartBytePos
            printfn "  dstStartBytePos : %d" dstStartBytePos

            let s = seq {
                for i in [ 0UL .. uint64 chunkCount - 1UL ] do
                    let currentA = i * 1048576UL
                    let currentB = min 1048576UL ( copyLength - currentA )
                    yield ( currentA, currentB )
            }
            for ( start, length ) in s do
                do! srcFa.Read ( srcStartBytePos + start ) ( ArraySegment( buf.Array, 0, int length ) )
                do! dstFa.Write ( dstStartBytePos + start ) ( ArraySegment( buf.Array, 0, int length ) )
        }

    /// <summary>
    ///  Output the updated metadata to the VHDX file via the log.
    /// </summary>
    /// <param name="fa">
    ///  File accessor to the VHDX file.
    /// </param>
    /// <param name="structures">
    ///  Updated the VHDX file structures data.
    /// </param>
    static member UpdateMetadata ( fa : FileAccessor ) ( structures : VhdxStructures ) : Task =
        task {
            // Get recorded position of the metadata region.
            let metadataFileOffset, metadataLength = 
                let e =
                    structures.Region.Entries
                    |> List.find ( fun itr -> itr.Guid = VhdxCommon.REGENT_TYPE_METADATA )
                e.FileOffset, e.Length
            let sec4kCount = metadataLength / 4096u

            // Read old metadata bytes.
            let oldMetadataBytes = Array.zeroCreate< byte > ( int metadataLength )
            do! fa.Read metadataFileOffset ( ArraySegment oldMetadataBytes )

            // Create bytes array of the updated metadata.
            let metadataTableBytes, metadataItemsBytes = VhdxCreator.CreateMetadataBytes structures.VDI
            let newMetadataBytes = Array.zeroCreate< byte > ( int metadataLength )
            Array.blit metadataTableBytes 0 newMetadataBytes 0 metadataTableBytes.Length
            Array.blit metadataItemsBytes 0 newMetadataBytes 65536 metadataItemsBytes.Length

            // Calculate differences in 4KB increments and generate log data.
            let update4KSecs =
                [|
                    for blockIdx = 0 to int sec4kCount - 1 do
                        let offset = blockIdx * 4096
                        let oldSpan = ReadOnlySpan<byte>( oldMetadataBytes, offset, 4096 )
                        let newSpan = ReadOnlySpan<byte>( newMetadataBytes, offset, 4096 )
                        if not ( oldSpan.SequenceEqual newSpan ) then
                            let idx = sec4k_me.ofUInt64 ( uint64 blockIdx + ( metadataFileOffset / 4096UL ) )
                            let segment = ArraySegment<byte>( newMetadataBytes, offset, 4096 )
                            struct( idx, segment )
                |]

            // Update metadata while going through the log.
            let! _ = VhdxWriter.WriteUpdatedSB fa structures update4KSecs
            ()
        }

    /// <summary>
    ///  To consolidate the updates, allocate the necessary disk space.
    /// </summary>
    /// <param name="dmeta">
    ///  The VHDX structures data for the VHDX file to be deleted.
    /// </param>
    /// <param name="mmeta">
    ///  The VHDX structures data for the VHDX file that aggregates update content.
    /// </param>
    /// <param name="startLba">
    ///  Position where processing should start.
    /// </param>
    /// <param name="updateLbaBuf">
    ///  A buffer that records the logical blocks for which data should be copied from the dmeta VHDX file to the mmeta VHDX file.
    ///  A pair consisting of the start position of the copy range and the number of contiguous logical blocks is recorded.
    ///  -1 is recorded in the unused range.
    /// </param>
    /// <returns>
    ///  Returns the following set of values.
    ///  * Required file size
    ///  * Range of updated payload blocks
    ///  * Processed LBA + 1 (the LBA where processing should resume)
    /// </returns>
    static member UpdateBATForDeleteRoot
        ( dstr : VhdxStructures )
        ( mstr : VhdxStructures )
        ( startLba : BLKCNT64_T )
        ( updateLbaBuf : struct( BLKCNT64_T * BLKCNT32_T )[] )
        : ( uint64 * HashSet<SEC4K_T> * BLKCNT64_T ) =

        let d_PayloadBlockLBACount =
            ( uint64 dstr.VDI.PayloadBlockSize ) / ( Blocksize.toUInt64 dstr.VDI.LogicalSectorSize )
            |> blkcnt_me.ofUInt64
        let m_PayloadBlockLBACount =
            ( uint64 mstr.VDI.PayloadBlockSize ) / ( Blocksize.toUInt64 mstr.VDI.LogicalSectorSize )
            |> blkcnt_me.ofUInt64
        let d_VirtualDiskLBACount =
            dstr.VDI.VirtualDiskSize / ( Blocksize.toUInt64 dstr.VDI.LogicalSectorSize ) |> blkcnt_me.ofUInt64

        printfn "  UpdateBATForDeleteRoot"
        printfn "  delete target logical sector count per payload block : %d" d_PayloadBlockLBACount
        printfn "  delete target logical sector count : %d" d_VirtualDiskLBACount
        printfn "  update target logical sector count per payload block : %d" m_PayloadBlockLBACount

        // initialize return buffer
        for i = 0 to updateLbaBuf.Length - 1 do
            updateLbaBuf.[i] <- struct( blkcnt_me.ofUInt64 UInt64.MaxValue, blkcnt_me.ofUInt32 UInt32.MaxValue )

        let updatedPB4K = HashSet<SEC4K_T>()
        let freeList = VhdxWriter.BuildFreeList mstr

        // Update BAT
        let rec loop1 ( lba1 : BLKCNT64_T, ubufcnt1 : int, restFreeList1 : uint64 list, gfs1 : uint64 )  : uint64 * BLKCNT64_T =
            if lba1 < d_VirtualDiskLBACount && ubufcnt1 < updateLbaBuf.Length then

                printfn "  loop1( lba1=%d, ubufcnt1=%d, gfs1=%d )" lba1 ubufcnt1 gfs1

                let struct( pdidx, pblbaoff ) = VhdxHandler.LBAtoPayloadBlockIndex lba1 dstr
                let lbastart = ( uint64 pdidx ) * d_PayloadBlockLBACount + ( pblbaoff |> blkcnt_me.toUInt32 |> uint64 |> blkcnt_me.ofUInt64 )
                let lbaend = ( uint64 pdidx + 1UL ) * d_PayloadBlockLBACount

                printfn "  pdidx=%d, lbastart=%d, lbaend=%d" pdidx lbastart lbaend
                printfn "  Payload State=%s" ( dstr.BAT.Payloads.[ int pdidx ].State.ToString() )

                match dstr.BAT.Payloads.[ int pdidx ].State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadUnapped ->
                    // Nothing to do.
                    let struct( a, _ ) = updateLbaBuf.[ ubufcnt1 ]
                    if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                        loop1( lbaend, ubufcnt1, restFreeList1, gfs1 )
                    else
                        loop1( lbaend, ubufcnt1 + 1, restFreeList1, gfs1 )

                | PayloadZero               // Since it is explicitly set to 0, that value must also be reflected in the target of the update.
                | PayloadPartiallyPresent   // Under normal circumstances, this state is impossible.
                | PayloadFullyPresent ->
                    // Allocate space in LBA units.
                    let rec loop2 ( lba2 : BLKCNT64_T ) ( ubufcnt2 : int ) ( restFreeList2 : uint64 list ) ( gfs2 : uint64 ) : ( BLKCNT64_T * int * uint64 list * uint64 ) =
                        if ubufcnt2 >= updateLbaBuf.Length then
                            lba2, ubufcnt2, restFreeList2, gfs2
                        elif lba2 >= lbaend  then
                            // If a range spans across payload blocks, it is not treated as a contiguous region.
                            lba2, ( ubufcnt2 + 1 ), restFreeList2, gfs2
                        else
                            let ( nextRFL, nextGFS2 ) = VhdxWriter.UpdatePBForAllocate mstr lba2 restFreeList2 gfs2 updatedPB4K
                            let struct( sbIdx, bytePos, bitPos ) = VhdxHandler.LBAtoSectorBitmapIndex lba2 mstr
                            let sbEntry = mstr.BAT.SectorBitmap.[ int32 sbIdx ].Bitmap
                            let bitValue = ( sbEntry.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy

                            if bitValue = 0uy then
                                // The value needs to be copied to this LBA.
                                let struct( a, b ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.ofUInt32 1u )
                                else
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( a, b + blkcnt_me.ofUInt32 1u )

                                // Although the sector bitmap will be discarded later,
                                // it must be marked as "used" here to facilitate the process of zero-clearing unused areas.
                                // There is no need to output the sector bitmap updates to the file.
                                sbEntry.[ int32 bytePos ] <- sbEntry.[ int32 bytePos ] ||| ( 1uy <<< ( int32 bitPos ) )

                                loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ubufcnt2 nextRFL nextGFS2
                            else
                                let struct( a, _ ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ubufcnt2 nextRFL nextGFS2
                                else
                                    loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ( ubufcnt2 + 1 ) nextRFL nextGFS2

                    loop2 lbastart ubufcnt1 restFreeList1 gfs1
                    |> loop1

            else
                gfs1, lba1

        let requiredFileSize, nextlba = loop1( startLba, 0, freeList, mstr.LastFileSize )
        requiredFileSize, updatedPB4K, nextlba

    /// <summary>
    ///  For payload blocks in the PartiallyPresent state,
    ///  zero-clear the unused areas and change the state to FullyPresent.
    /// </summary>
    /// <param name="fa">
    ///  File accessor object for the VHDX file.
    /// </param>
    /// <param name="structures">
    ///  The VHDX structures data for the VHDX file.
    /// </param>
    static member FillPartiallyPayload ( fa : FileAccessor ) ( structures : VhdxStructures ) : Task =
        task {
            let blocksize = Blocksize.toUInt64 structures.VDI.LogicalSectorSize
            let payloadBlockLBACount =
                ( uint64 structures.VDI.PayloadBlockSize ) / blocksize
                |> blkcnt_me.ofUInt64
            let zerobuf = Array.zeroCreate<byte> ( int blocksize )
            let updatedPB4K = HashSet<SEC4K_T>()

            printfn "  FillPartiallyPayload"
            printfn "  update target logical sector count per payload block : %d" payloadBlockLBACount

            for pdidx = 0 to structures.BAT.Payloads.Length - 1 do
                let lbastart = ( uint64 pdidx ) * payloadBlockLBACount
                let lbaend = ( uint64 pdidx + 1UL ) * payloadBlockLBACount

                printfn "  pdidx=%d, lbastart=%d, lbaend=%d" pdidx lbastart lbaend
                printfn "  Payload State=%s" ( structures.BAT.Payloads.[ pdidx ].State.ToString() )

                match structures.BAT.Payloads.[ pdidx ].State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadUnapped
                | PayloadZero
                | PayloadFullyPresent ->
                    // Nothing to do.
                    ()

                | PayloadPartiallyPresent ->
                    structures.BAT.Payloads.[ pdidx ] <- {
                        structures.BAT.Payloads.[ pdidx ] with
                            State = PayloadFullyPresent;
                    }
                    let fpos = structures.BAT.BATRegionOffset + ( 8UL * structures.BAT.Payloads.[ pdidx ].BatEntryIndex )
                    let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
                    updatedPB4K.Add( secidx ) |> ignore

                    // Zero out unused portions at the LBA unit.
                    for wlba in [ uint64 lbastart .. uint64 lbaend - 1UL ] do
                        let bitValue =
                            let struct( sbIdx, bytePos, bitPos ) = VhdxHandler.LBAtoSectorBitmapIndex ( blkcnt_me.ofUInt64 wlba ) structures
                            let sbEntry = structures.BAT.SectorBitmap.[ int32 sbIdx ]
                            ( sbEntry.Bitmap.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy
                        if bitValue = 0uy then
                            let pos = structures.BAT.Payloads.[ pdidx ].FileOffset + ( wlba - uint64 lbastart ) * blocksize
                            do! fa.Write pos ( ArraySegment zerobuf )

            // unallocate sector bitmaps
            for i = 0 to structures.BAT.SectorBitmap.Length - 1 do
                let pb = structures.BAT.SectorBitmap.[i]
                structures.BAT.SectorBitmap.[i] <- {
                    pb with
                        SBState = BatEntryStateSB.SectorBitmapNotPresent;
                        FileOffset = 0UL;
                        Bitmap = [||];
                }
                let fpos = structures.BAT.BATRegionOffset + ( 8UL * pb.BatEntryIndex )
                let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
                updatedPB4K.Add secidx |> ignore
            let updated4KSecsForBAT = Seq.toArray updatedPB4K

            // Output updated BAT entry
            let! _ = VhdxWriter.WriteUpdatedBAT fa structures updated4KSecsForBAT structures.LastFileSize 0
            ()
        }

    /// <summary>
    ///  Delete the root node and convert its child into a dynamic VHDX file.
    /// </summary>
    static member DeleteRoot ( structures : ( FileAccessor * VhdxStructures )[] ) : Task =
        task {
            let ( dfa, dstr ) = structures.[0]
            let ( mfa, mstr ) = structures.[1]

            printfn "=== DeleteRoot ==="
            printfn "delete target file name : %s" dfa.FileName
            printfn "update target file name : %s" mfa.FileName

            let updateLbaBuf = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 4096
            let d_VirtualDiskLBACount =
                dstr.VDI.VirtualDiskSize / ( Blocksize.toUInt64 dstr.VDI.LogicalSectorSize ) |> blkcnt_me.ofUInt64

            // Flash log entries
            if mstr.Log.Length > 0 then
                printfn "=== Need to replay log. ==="
                do! VhdxChecker.Check mfa
                printfn "=== Replay log complete. ==="
            let structures1 =
                if mstr.Log.Length > 0 then
                    { mstr with Header.SequenceNumber = mstr.Header.SequenceNumber + 2UL }
                else
                    mstr

            // Update FileWriteGuid and DataWriteGuid
            printfn "=== Update FileWriteGuid and DataWriteGuid. ==="
            let! structures2 = VhdxHandler.UpdateFileWriteGuidAndDataWriteGuid mfa structures1

            // Allocate area and copy data
            printfn "=== Allocate and copy ==="
            let! _, structures3 =
                Functions.loopAsyncWithState ( fun ( lba, structures4 ) -> task {
                    printfn "  DeleteRoot loop(lba=%d)" lba

                    let requiredFileSize, updatedPB4K, nextLba =
                        VhdxMerge.UpdateBATForDeleteRoot dstr structures4 lba updateLbaBuf
                    let updated4KSecsForBAT = Seq.toArray updatedPB4K

                    printfn "  requiredFileSize=%d" requiredFileSize

                    // Output BAT entries.
                    let! nextsn5 = VhdxWriter.WriteUpdatedBAT mfa structures4 updated4KSecsForBAT requiredFileSize 0
                    let structures5 = { structures4 with Header.SequenceNumber = nextsn5; }

                    // Copy payload data.
                    for struct( start, cnt ) in updateLbaBuf do
                        if start <> blkcnt_me.ofUInt64 UInt64.MaxValue then
                            do! VhdxMerge.CopyData dfa dstr mfa structures5 start cnt

                    return struct( ( nextLba < d_VirtualDiskLBACount ), ( nextLba, structures5 ) )

                } ) ( blkcnt_me.zero64, structures2 )

            // Update parent locator.
            let structures7 = {
                structures3 with
                    Header.SequenceNumber = structures3.Header.SequenceNumber + 2UL;
                    VDI.ParentLocator = Map.empty;
                    VDI.HasParent = false;
            }
            do! VhdxMerge.UpdateMetadata mfa structures7

            // Convert PayloadPartiallyPresent payload block to PayloadFullyPresent.
            do! VhdxMerge.FillPartiallyPayload mfa structures7
        }

    /// <summary>
    ///  To consolidate the updates, allocate the necessary disk space.
    /// </summary>
    /// <param name="dmeta">
    ///  The VHDX structures data for the VHDX file to be deleted.
    /// </param>
    /// <param name="mmeta">
    ///  The VHDX structures data for the VHDX file that aggregates update content.
    /// </param>
    /// <param name="startLba">
    ///  Position where processing should start.
    /// </param>
    /// <param name="updateLbaBuf">
    ///  A buffer that records the logical blocks for which data should be copied from the dmeta VHDX file to the mmeta VHDX file.
    ///  A pair consisting of the start position of the copy range and the number of contiguous logical blocks is recorded.
    ///  -1 is recorded in the unused range.
    /// </param>
    /// <returns>
    ///  Returns the following set of values.
    ///  * Required file size
    ///  * Range of updated payload blocks
    ///  * The range of the updated sector bitmap and the updated bitmap
    ///  * Processed LBA + 1 (the LBA where processing should resume)
    /// </returns>
    static member UpdateBATForMergeIntermediate
        ( dstr : VhdxStructures )
        ( mstr : VhdxStructures )
        ( startLba : BLKCNT64_T )
        ( updateLbaBuf : struct( BLKCNT64_T * BLKCNT32_T )[] )
        : ( uint64 * HashSet<SEC4K_T> * Dictionary< SEC4K_T, ArraySegment<byte> > * BLKCNT64_T ) =

        let d_PayloadBlockLBACount =
            ( uint64 dstr.VDI.PayloadBlockSize ) / ( Blocksize.toUInt64 dstr.VDI.LogicalSectorSize )
            |> blkcnt_me.ofUInt64
        let m_PayloadBlockLBACount =
            ( uint64 mstr.VDI.PayloadBlockSize ) / ( Blocksize.toUInt64 mstr.VDI.LogicalSectorSize )
            |> blkcnt_me.ofUInt64
        let d_VirtualDiskLBACount =
            dstr.VDI.VirtualDiskSize / ( Blocksize.toUInt64 dstr.VDI.LogicalSectorSize ) |> blkcnt_me.ofUInt64

        printfn "  UpdateBATForMergeIntermediate"
        printfn "  delete target logical sector count per payload block : %d" d_PayloadBlockLBACount
        printfn "  delete target logical sector count : %d" d_VirtualDiskLBACount
        printfn "  update target logical sector count per payload block : %d" m_PayloadBlockLBACount

        // initialize return buffer
        for i = 0 to updateLbaBuf.Length - 1 do
            updateLbaBuf.[i] <- struct( blkcnt_me.ofUInt64 UInt64.MaxValue, blkcnt_me.ofUInt32 UInt32.MaxValue )

        let updatedSB4K = Dictionary< SEC4K_T, ArraySegment<byte> >()
        let updatedPB4K = HashSet<SEC4K_T>()
        let freeList = VhdxWriter.BuildFreeList mstr

        // Update BAT
        let rec loop1 ( lba1 : BLKCNT64_T, ubufcnt1 : int, restFreeList1 : uint64 list, gfs1 : uint64 )  : uint64 * BLKCNT64_T =
            if lba1 < d_VirtualDiskLBACount && ubufcnt1 < updateLbaBuf.Length then

                printfn "  loop1( lba1=%d, ubufcnt1=%d, gfs1=%d )" lba1 ubufcnt1 gfs1

                let struct( pdidx, pblbaoff ) = VhdxHandler.LBAtoPayloadBlockIndex lba1 dstr
                let lbastart = ( uint64 pdidx ) * d_PayloadBlockLBACount + ( pblbaoff |> blkcnt_me.toUInt32 |> uint64 |> blkcnt_me.ofUInt64 )
                let lbaend = ( uint64 pdidx + 1UL ) * d_PayloadBlockLBACount

                printfn "  pdidx=%d, lbastart=%d, lbaend=%d" pdidx lbastart lbaend
                printfn "  Payload State=%s" ( dstr.BAT.Payloads.[ int pdidx ].State.ToString() )

                match dstr.BAT.Payloads.[ int pdidx ].State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadUnapped ->
                    // Nothing to do.
                    let struct( a, b ) = updateLbaBuf.[ ubufcnt1 ]
                    if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                        loop1( lbaend, ubufcnt1, restFreeList1, gfs1 )
                    else
                        loop1( lbaend, ubufcnt1 + 1, restFreeList1, gfs1 )

                | PayloadZero  // Since it is explicitly set to 0, that value must also be reflected in the target of the update.
                | PayloadFullyPresent ->
                    // Allocate space in LBA units.
                    let rec loop2 ( lba2 : BLKCNT64_T ) ( ubufcnt2 : int ) ( restFreeList2 : uint64 list ) ( gfs2 : uint64 ) : ( BLKCNT64_T * int * uint64 list * uint64 ) =
                        if ubufcnt2 >= updateLbaBuf.Length then
                            lba2, ubufcnt2, restFreeList2, gfs2
                        elif lba2 >= lbaend  then
                            // If a range spans across payload blocks, it is not treated as a contiguous region.
                            lba2, ( ubufcnt2 + 1 ), restFreeList2, gfs2
                        else
                            let ( nextRFL, nextGFS2 ) = VhdxWriter.UpdatePBForAllocate mstr lba2 restFreeList2 gfs2 updatedPB4K
                            if VhdxWriter.UpdateSBForAllocate mstr lba2 updatedPB4K updatedSB4K then
                                // The value needs to be copied to this LBA.
                                let struct( a, b ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.ofUInt32 1u )
                                else
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( a, b + blkcnt_me.ofUInt32 1u )
                                loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ubufcnt2 nextRFL nextGFS2
                            else
                                let struct( a, _ ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ubufcnt2 nextRFL nextGFS2
                                else
                                    loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ( ubufcnt2 + 1 ) nextRFL nextGFS2

                    loop2 lbastart ubufcnt1 restFreeList1 gfs1
                    |> loop1

                | PayloadPartiallyPresent ->
                    // Check the target VHDX file for used areas on an LBA basis.
                    let rec loop2 ( lba2 : BLKCNT64_T ) ( ubufcnt2 : int ) ( restFreeList2 : uint64 list ) ( gfs2 : uint64 ) : ( BLKCNT64_T * int * uint64 list * uint64 ) =
                        if ubufcnt2 >= updateLbaBuf.Length then
                            lba2, ubufcnt2, restFreeList2, gfs2
                        elif lba2 >= lbaend  then
                            // If a range spans across payload blocks, it is not treated as a contiguous region.
                            lba2, ( ubufcnt2 + 1 ), restFreeList2, gfs2
                        else
                            let struct( dsbidx, bytePos, bitPos ) = VhdxHandler.LBAtoSectorBitmapIndex lba2 dstr
                            let dsbState = ( dstr.BAT.SectorBitmap.[ int32 dsbidx ].Bitmap.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy
                            if dsbState = 1uy then
                                // The logical sector in question is in use.
                                let ( nextRFL, nextGFS2 ) = VhdxWriter.UpdatePBForAllocate mstr lba2 restFreeList2 gfs2 updatedPB4K
                                if VhdxWriter.UpdateSBForAllocate mstr lba2 updatedPB4K updatedSB4K then
                                    // The value needs to be copied to this LBA.
                                    let struct( a, b ) = updateLbaBuf.[ ubufcnt2 ]
                                    if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                        updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.ofUInt32 1u )
                                    else
                                        updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, b + blkcnt_me.ofUInt32 1u )
                                loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ubufcnt2 nextRFL nextGFS2
                            else
                                // Since the logical sector in question is unused, there is no need to allocate it to the target VHDX.
                                loop2 ( lba2 + blkcnt_me.ofUInt64 1UL ) ( ubufcnt2 + 1 ) restFreeList2 gfs2
                    loop2 lbastart ubufcnt1 restFreeList1 gfs1
                    |> loop1
            else
                gfs1, lba1

        let requiredFileSize, nextlba = loop1( startLba, 0, freeList, mstr.LastFileSize )
        requiredFileSize, updatedPB4K, updatedSB4K, nextlba

    /// <summary>
    ///  Delete the intermediate VHDX file and merge the differences into its child.
    /// </summary>
    /// <param name="structures">
    ///  All of VHDX files targeted for snapshot deletion.
    /// </param>
    /// <param name="delidx">
    ///  An index for identifying snapshots to be deleted.
    /// </param>
    static member MergeIntermediate ( structures : ( FileAccessor * VhdxStructures )[] ) ( delidx : int32 ) : Task =
        task {
            let ( dfa, dstr ) = structures.[ delidx ]
            let ( mfa, mstr ) = structures.[ delidx + 1 ]

            printfn "=== MergeIntermediate ==="
            printfn "delete target file name : %s" dfa.FileName
            printfn "update target file name : %s" mfa.FileName

            let updateLbaBuf = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 4096
            let d_VirtualDiskLBACount =
                dstr.VDI.VirtualDiskSize / ( Blocksize.toUInt64 dstr.VDI.LogicalSectorSize ) |> blkcnt_me.ofUInt64

            // Flash log entries
            if mstr.Log.Length > 0 then
                printfn "=== Need to replay log. ==="
                do! VhdxChecker.Check mfa
                printfn "=== Replay log complete. ==="
            let structures1 =
                if mstr.Log.Length > 0 then
                    { mstr with Header.SequenceNumber = mstr.Header.SequenceNumber + 2UL }
                else
                    mstr

            // Update FileWriteGuid and DataWriteGuid
            printfn "=== Update FileWriteGuid and DataWriteGuid. ==="
            let! structures2 = VhdxHandler.UpdateFileWriteGuidAndDataWriteGuid mfa structures1

            // Allocate area and copy data
            printfn "=== Allocate and copy ==="
            let! _, structures3 =
                Functions.loopAsyncWithState ( fun ( lba, structures4 ) -> task {
                    printfn "  MergeIntermediate loop(lba=%d)" lba

                    let requiredFileSize, updatedPB4K, updatedSB4K, nextLba =
                        VhdxMerge.UpdateBATForMergeIntermediate dstr structures4 lba updateLbaBuf
                    let updated4KSecsForSB =
                        updatedSB4K
                        |> Seq.map ( fun itr -> struct( itr.Key, itr.Value ) )
                        |> Seq.toArray
                    let updated4KSecsForBAT = Seq.toArray updatedPB4K

                    // Output BAT entries.
                    let! nextsn5 = VhdxWriter.WriteUpdatedBAT mfa structures4 updated4KSecsForBAT requiredFileSize 0
                    let structures5 = { structures4 with Header.SequenceNumber = nextsn5; }

                    // Output sector bitmap.
                    let! nextsn6 = VhdxWriter.WriteUpdatedSB mfa structures5 updated4KSecsForSB
                    let structures6 = { structures5 with Header.SequenceNumber = nextsn6; }

                    // Copy payload data.
                    for struct( start, cnt ) in updateLbaBuf do
                        if start <> blkcnt_me.ofUInt64 UInt64.MaxValue then
                            do! VhdxMerge.CopyData dfa dstr mfa structures6 start cnt

                    return struct( ( nextLba < d_VirtualDiskLBACount ), ( nextLba, structures6 ) )

                } ) ( blkcnt_me.zero64, structures2 )

            // Update parent locator.
            let structures7 = {
                structures3 with
                    Header.SequenceNumber = structures3.Header.SequenceNumber + 2UL;
                    VDI.ParentLocator = dstr.VDI.ParentLocator;
            }
            do! VhdxMerge.UpdateMetadata mfa structures7

        }

    /// <summary>
    ///  Delete a snapshot in a differencing VHDX file.
    /// </summary>
    /// <param name="childvhdx">
    ///  VHDX file targeted for snapshot deletion.
    /// </param>
    /// <param name="ancestor">
    ///  An index for identifying snapshots to be deleted.
    /// </param>
    static member Merge ( childvhdx : FileAccessor ) ( ancestor : int32 ) : Task =
        task {
            let! structures = VhdxHandler.ReadAllStructures childvhdx

            printfn "================================================================"
            printfn "Merge VHDX file."
            printfn "leaf node file name : %s" childvhdx.FileName
            printfn "delete target index : %d" ancestor

            // Leaf-node descendants cannot be deleted (simply deleting the files is sufficient).
            if ancestor = structures.Length - 1 then
                raise <| Exception "Leaf nodes cannot be deleted."

            if ancestor = 0 then
                do! VhdxMerge.DeleteRoot structures
            else
                do! VhdxMerge.MergeIntermediate structures ancestor

            for ( fa, _ ) in structures do
                fa.Close()

            // Delete specified file.
            let delTargetFA = structures.[ ancestor ] |> fst
            File.Delete delTargetFA.FileName
        }

