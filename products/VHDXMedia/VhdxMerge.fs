//=============================================================================
// Haruka Software Storage.
// VhdxMerge.fs : Implement a function to delete snapshots of differencing VHDX files.
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
            if srcStr.VDI.LogicalSectorSize <> dstStr.VDI.LogicalSectorSize then
                raise <| VhdxMediaException( "The block lengths of the source and destination differ." )

            let srcMediaBlkCnt = Functions.CalcBlockCount64 srcStr.VDI.VirtualDiskSize srcStr.VDI.LogicalSectorSize
            if not ( Functions.CheckAccessRangeBlock lba ( blkcnt_me.toBLKCNT64_T cnt ) srcMediaBlkCnt ) then
                raise <| VhdxMediaException( "The source region is outside the bounds of the media." )

            let dstMediaBlkCnt = Functions.CalcBlockCount64 dstStr.VDI.VirtualDiskSize dstStr.VDI.LogicalSectorSize
            if not ( Functions.CheckAccessRangeBlock lba ( blkcnt_me.toBLKCNT64_T cnt ) dstMediaBlkCnt ) then
                raise <| VhdxMediaException( "The destination region is outside the bounds of the media." )

            if cnt > blkcnt_me.zero32 then
                let blocksize = Blocksize.toUInt32 srcStr.VDI.LogicalSectorSize
                let lbPerChunk = 1048576u / blocksize    // 2048 or 256
                let chunkCount = Functions.CeilDiv ( uint32 cnt ) lbPerChunk
                let copyLength = uint64 cnt * uint64 blocksize

                let srcStartBytePos =
                    let struct( idx, off ) = VhdxCommons.LBAtoPayloadBlockIndex lba srcStr

                    let payloadBlockCount = srcStr.VDI.PayloadBlockSize / blocksize |> blkcnt_me.ofUInt32
                    if payloadBlockCount - off < cnt then
                        raise <| VhdxMediaException( "The source region is not contained within a single payload." )

                    let pb = srcStr.BAT.Payloads.[ int idx ]
                    if pb.State = BatEntryStatePB.PayloadZero then
                        ValueNone
                    elif pb.State = BatEntryStatePB.PayloadFullyPresent || pb.State = BatEntryStatePB.PayloadPartiallyPresent then
                        ValueSome ( pb.FileOffset + uint64 off * uint64 blocksize )
                    else
                        raise <| VhdxMediaException( "The source payload block is not allocated." )

                let dstStartBytePos =
                    let struct( idx, off ) = VhdxCommons.LBAtoPayloadBlockIndex lba dstStr

                    let payloadBlockCount = dstStr.VDI.PayloadBlockSize / blocksize |> blkcnt_me.ofUInt32
                    if payloadBlockCount - off < cnt then
                        raise <| VhdxMediaException( "The destination region is not contained within a single payload." )

                    let pb = dstStr.BAT.Payloads.[ int idx ]
                    if pb.State <> BatEntryStatePB.PayloadFullyPresent && pb.State <> BatEntryStatePB.PayloadPartiallyPresent then
                        raise <| VhdxMediaException( "The destination payload block is not allocated." )

                    ( pb.FileOffset + uint64 off * uint64 blocksize )

                let s = seq {
                    for i in 0UL .. uint64 chunkCount - 1UL do
                        let currentA = i * 1048576UL
                        let currentB = min 1048576UL ( copyLength - currentA )
                        yield struct ( currentA, currentB )
                }

                let buf = PooledBuffer.Rent 1048576
                for struct ( start, length ) in s do
                    if srcStartBytePos.IsSome then
                        do! srcFa.Read ( srcStartBytePos.Value + start ) ( ArraySegment( buf.Array, 0, int length ) )
                    else
                        Array.fill buf.Array 0 ( int length ) 0uy
                    do! dstFa.Write ( dstStartBytePos + start ) ( ArraySegment( buf.Array, 0, int length ) )
                buf.Return()
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
    /// <param name="verhd">
    ///  Latest header values.
    /// </param>
    /// <returns>
    ///  Updated header values.
    /// </returns>
    static member UpdateMetadata ( fa : FileAccessor ) ( structures : VhdxStructures ) ( verhd : VhdxMutableHeader ) : Task<VhdxMutableHeader> =
        task {
            // Get recorded position of the metadata region.
            let metadataFileOffset, metadataLength = 
                let e =
                    structures.Region.Entries
                    |> List.find ( fun itr -> itr.Guid = VhdxCommons.REGENT_TYPE_METADATA )
                e.FileOffset, e.Length
            if metadataLength <= 0u then
                raise <| VhdxMediaException( "Unexpected metadata region length." )
            let sec4kCount = metadataLength / 4096u

            // Read old metadata bytes.
            let oldMetadataBytes = PooledBuffer.Rent ( int32 metadataLength )
            do! fa.Read metadataFileOffset oldMetadataBytes.ArraySegment

            // Create bytes array of the updated metadata.
            let metadataTableBytes, metadataItemsBytes = VhdxCreator.CreateMetadataBytes structures.VDI
            let newMetadataBytes = PooledBuffer.RentAndInit ( int32 metadataLength )
            Array.blit metadataTableBytes 0 newMetadataBytes.Array 0 metadataTableBytes.Length
            Array.blit metadataItemsBytes 0 newMetadataBytes.Array 65536 metadataItemsBytes.Length

            // Calculate differences in 4KB increments and generate log data.
            let update4KSecs =
                [|
                    for blockIdx = 0 to int sec4kCount - 1 do
                        let offset = blockIdx * 4096
                        let oldSpan = ReadOnlySpan<byte>( oldMetadataBytes.Array, offset, 4096 )
                        let newSpan = ReadOnlySpan<byte>( newMetadataBytes.Array, offset, 4096 )
                        if not ( oldSpan.SequenceEqual newSpan ) then
                            let idx = sec4k_me.ofUInt64 ( uint64 blockIdx + ( metadataFileOffset / 4096UL ) )
                            let segment = newMetadataBytes.GetArraySegment offset 4096
                            struct( idx, segment )
                |]

            // Update metadata while going through the log.
            let! r = VhdxWriter.WriteUpdatedSB fa structures verhd update4KSecs
            oldMetadataBytes.Return()
            newMetadataBytes.Return()
            return r
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

        if Array.isEmpty updateLbaBuf then
            raise <| VhdxMediaException( "updateLbaBuf must not be empty." )
        if dstr.VDI.LogicalSectorSize <> mstr.VDI.LogicalSectorSize then
            raise <| VhdxMediaException( "Logical Sector Size unmatch." )
        if dstr.VDI.VirtualDiskSize <> mstr.VDI.VirtualDiskSize then
            raise <| VhdxMediaException( "Virtual Disk Size unmatch." )
        if dstr.VDI.PayloadBlockSize <= 0u || mstr.VDI.PayloadBlockSize <= 0u then
            raise <| VhdxMediaException( "Unexpected payload block size." )
        if dstr.VDI.HasParent then
            raise <| VhdxMediaException( "A root VHDX file must not have a parent." )
        if not mstr.VDI.HasParent then
            raise <| VhdxMediaException( "A child VHDX file must have a parent." )

        let d_PayloadBlockLBACount = Functions.CalcBlockCount64 ( uint64 dstr.VDI.PayloadBlockSize ) dstr.VDI.LogicalSectorSize
        let m_PayloadBlockLBACount = Functions.CalcBlockCount64 ( uint64 mstr.VDI.PayloadBlockSize ) mstr.VDI.LogicalSectorSize
        let d_VirtualDiskLBACount = Functions.CalcBlockCount64 dstr.VDI.VirtualDiskSize dstr.VDI.LogicalSectorSize

        if d_VirtualDiskLBACount <= startLba then
            raise <| VhdxMediaException( "The value of startLba is out of range." )

        // initialize return buffer
        Array.fill updateLbaBuf 0 updateLbaBuf.Length struct( blkcnt_me.ofUInt64 UInt64.MaxValue, blkcnt_me.ofUInt32 UInt32.MaxValue )

        let updatedPB4K = HashSet<SEC4K_T>()
        let enumFreeList =
            VhdxWriter.BuildFreeList mstr mstr.BAT.PayloadBlockCount
            |> Array.toSeq
            |> _.GetEnumerator()

        // Update BAT
        let rec loop1 struct ( lba1 : BLKCNT64_T, ubufcnt1 : int, gfs1 : uint64 )  : struct( uint64 * BLKCNT64_T ) =
            if lba1 < d_VirtualDiskLBACount && ubufcnt1 < updateLbaBuf.Length then

                let struct( pdidx, _ ) = VhdxCommons.LBAtoPayloadBlockIndex lba1 dstr
                let lbaend =
                    let struct( m_pdidx, _ ) = VhdxCommons.LBAtoPayloadBlockIndex lba1 mstr
                    ( uint64 pdidx + 1UL ) * d_PayloadBlockLBACount
                    |> min ( ( uint64 m_pdidx + 1UL ) * m_PayloadBlockLBACount )
                    |> min  d_VirtualDiskLBACount

                match dstr.BAT.Payloads.[ int pdidx ].State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadUnapped ->
                    // Nothing to do.
                    let struct( a, _ ) = updateLbaBuf.[ ubufcnt1 ]
                    if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                        loop1( lbaend, ubufcnt1, gfs1 )
                    else
                        loop1( lbaend, ubufcnt1 + 1, gfs1 )

                | PayloadZero               // Since it is explicitly set to 0, that value must also be reflected in the target of the update.
                | PayloadPartiallyPresent   // Under normal circumstances, this state is impossible.
                | PayloadFullyPresent ->
                    // Allocate space in LBA units.
                    let rec loop2 ( lba2 : BLKCNT64_T ) ( ubufcnt2 : int ) ( gfs2 : uint64 ) : struct ( BLKCNT64_T * int * uint64 ) =
                        if ubufcnt2 >= updateLbaBuf.Length then
                            struct( lba2, ubufcnt2, gfs2 )
                        elif lba2 >= lbaend  then
                            // If a range spans across payload blocks, it is not treated as a contiguous region.
                            struct( lba2, ( ubufcnt2 + 1 ), gfs2 )
                        else
                            let nextGFS2 = VhdxWriter.UpdatePBForAllocate mstr lba2 enumFreeList gfs2 updatedPB4K
                            let struct( sbIdx, bytePos, bitPos ) = VhdxCommons.LBAtoSectorBitmapIndex lba2 mstr
                            let sbEntry = mstr.BAT.SectorBitmap.[ int32 sbIdx ].Bitmap
                            if Array.isEmpty sbEntry then
                                raise <| VhdxMediaException( "Sector bitmap is not allocated." )
                            let bitValue = ( sbEntry.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy

                            if bitValue = 0uy then
                                // The value needs to be copied to this LBA.
                                let struct( a, b ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.one32 )
                                else
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( a, blkcnt_me.Incr b )

                                // Although the sector bitmap will be discarded later,
                                // it must be marked as "used" here to facilitate the process of zero-clearing unused areas.
                                // There is no need to output the sector bitmap updates to the file.
                                sbEntry.[ int32 bytePos ] <- sbEntry.[ int32 bytePos ] ||| ( 1uy <<< ( int32 bitPos ) )

                                loop2 ( blkcnt_me.Incr lba2 ) ubufcnt2 nextGFS2
                            else
                                let struct( a, _ ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    loop2 ( blkcnt_me.Incr lba2 ) ubufcnt2 nextGFS2
                                else
                                    loop2 ( blkcnt_me.Incr lba2 ) ( ubufcnt2 + 1 ) nextGFS2

                    loop2 lba1 ubufcnt1 gfs1
                    |> loop1

            else
                gfs1, lba1

        let struct( requiredFileSize, nextlba ) = loop1 struct ( startLba, 0, mstr.LastFileSize )
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
    /// <param name="verhd">
    ///  Latest header values.
    /// </param>
    /// <returns>
    ///  Updated header values.
    /// </returns>
    static member FillPartiallyPayload ( fa : FileAccessor ) ( structures : VhdxStructures ) ( verhd : VhdxMutableHeader ) : Task<VhdxMutableHeader> =
        task {
            if structures.VDI.PayloadBlockSize <= 0u then
                raise <| VhdxMediaException( "Unexpected payload block size." )
            if not structures.VDI.HasParent then
                raise <| VhdxMediaException( "It must be a differencing VHDX file." )
            if Array.exists ( _.Bitmap >> Array.isEmpty ) structures.BAT.SectorBitmap then
                raise <| VhdxMediaException( "Sector bitmap is not allocated." )

            let blocksize = Blocksize.toUInt64 structures.VDI.LogicalSectorSize
            let virtualDiskLBACount = Functions.CalcBlockCount64 structures.VDI.VirtualDiskSize structures.VDI.LogicalSectorSize
            let payloadBlockLBACount = Functions.CalcBlockCount64 ( uint64 structures.VDI.PayloadBlockSize ) structures.VDI.LogicalSectorSize
            let zerobuf = Array.zeroCreate<byte> ( int blocksize )
            let updatedPB4K = HashSet<SEC4K_T>()

            for pdidx = 0 to structures.BAT.Payloads.Length - 1 do
                let lbastart = ( uint64 pdidx ) * payloadBlockLBACount
                let lbaend = min virtualDiskLBACount ( ( uint64 pdidx + 1UL ) * payloadBlockLBACount )

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
                    for wlba in uint64 lbastart .. uint64 lbaend - 1UL do
                        let bitValue =
                            let struct( sbIdx, bytePos, bitPos ) = VhdxCommons.LBAtoSectorBitmapIndex ( blkcnt_me.ofUInt64 wlba ) structures
                            let sbEntry = structures.BAT.SectorBitmap.[ int32 sbIdx ]
                            ( sbEntry.Bitmap.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy
                        if bitValue = 0uy then
                            let pos = structures.BAT.Payloads.[ pdidx ].FileOffset + ( wlba - uint64 lbastart ) * blocksize
                            do! fa.WriteArray pos zerobuf

            // unallocate sector bitmaps
            // In principle, a sector bitmap must always be allocated for a differencing VHDX file.
            // However, to convert it into a dynamic VHDX file, the sector bitmap is set to unallocated here.
            structures.BAT.SectorBitmap
            |> Array.iteri ( fun idx itr ->
                structures.BAT.SectorBitmap.[idx] <- {
                    itr with
                        SBState = BatEntryStateSB.SectorBitmapNotPresent;
                        FileOffset = 0UL;
                        Bitmap = Array.empty
                }
                let fpos = structures.BAT.BATRegionOffset + ( 8UL * itr.BatEntryIndex )
                let secidx = fpos / 4096UL |> sec4k_me.ofUInt64
                updatedPB4K.Add secidx |> ignore
            )
            let updated4KSecsForBAT = Seq.toArray updatedPB4K

            // Output updated BAT entry
            return! VhdxWriter.WriteUpdatedBAT fa structures verhd updated4KSecsForBAT fa.FileSize
        }

    /// <summary>
    ///  Delete the root node and convert its child into a dynamic VHDX file.
    /// </summary>
    static member DeleteRoot ( structures : ( FileAccessor * VhdxStructures )[] ) : Task =
        task {
            if structures.Length < 2 then
                raise <| VhdxMediaException( "The VHDX file for the root to be deleted does not exist." )

            let ( dfa, dstr ) = structures.[0]
            let ( mfa, mstr ) = structures.[1]

            let updateLbaBuf = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 4096
            let d_VirtualDiskLBACount = Functions.CalcBlockCount64 dstr.VDI.VirtualDiskSize dstr.VDI.LogicalSectorSize

            // Flash log entries
            let! verhd1 =
                if mstr.Log.Length > 0 then
                    VhdxChecker.FlushLog mfa mstr
                else
                    Task.FromResult mstr.LoadedVarHeader

            // Update FileWriteGuid and DataWriteGuid
            let! verhd2 = VhdxCommons.UpdateFileWriteGuidAndDataWriteGuid mfa mstr verhd1

            // Allocate area and copy data
            let ps = PseudoSeqStat< ( BLKCNT64_T * VhdxMutableHeader ), VhdxMutableHeader >( ( blkcnt_me.zero64, verhd2 ) )
            for ( lba, verhd4 ) in ps do
                let requiredFileSize, updatedPB4K, nextLba =
                    VhdxMerge.UpdateBATForDeleteRoot dstr mstr lba updateLbaBuf
                let updated4KSecsForBAT = Seq.toArray updatedPB4K

                // Output BAT entries.
                let! verhd5 = VhdxWriter.WriteUpdatedBAT mfa mstr verhd4 updated4KSecsForBAT requiredFileSize

                // Copy payload data.
                for struct( start, cnt ) in updateLbaBuf do
                    if start <> blkcnt_me.ofUInt64 UInt64.MaxValue then
                        do! VhdxMerge.CopyData dfa dstr mfa mstr start cnt

                if nextLba < d_VirtualDiskLBACount then
                    ps.Continue( nextLba, verhd5 )
                else
                    ps.Break( verhd5 )
            let verhd3 = ps.LastValue |> _.Value

            // Update parent locator.
            let structures2 = {
                mstr with
                    VDI.ParentLocator = Map.empty;
                    VDI.HasParent = false;
            }
            let verhd6 = {
                verhd3 with
                    SequenceNumber = verhd3.SequenceNumber + 2UL;
            }
            let! verhd7 = VhdxMerge.UpdateMetadata mfa structures2 verhd6

            // Convert PayloadPartiallyPresent payload block to PayloadFullyPresent.
            let! _ = VhdxMerge.FillPartiallyPayload mfa structures2 verhd7
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
    ///  * The range of the updated sector bitmap and the updated bitmap
    ///  * Processed LBA + 1 (the LBA where processing should resume)
    /// </returns>
    static member UpdateBATForMergeIntermediate
        ( dstr : VhdxStructures )
        ( mstr : VhdxStructures )
        ( startLba : BLKCNT64_T )
        ( updateLbaBuf : struct( BLKCNT64_T * BLKCNT32_T )[] )
        : ( uint64 * HashSet<SEC4K_T> * Dictionary< SEC4K_T, ArraySegment<byte> > * BLKCNT64_T ) =

        let d_PayloadBlockLBACount = Functions.CalcBlockCount64 ( uint64 dstr.VDI.PayloadBlockSize ) dstr.VDI.LogicalSectorSize
        let m_PayloadBlockLBACount = Functions.CalcBlockCount64 ( uint64 mstr.VDI.PayloadBlockSize ) mstr.VDI.LogicalSectorSize
        let d_VirtualDiskLBACount = Functions.CalcBlockCount64 dstr.VDI.VirtualDiskSize dstr.VDI.LogicalSectorSize

        // initialize return buffer
        for i = 0 to updateLbaBuf.Length - 1 do
            updateLbaBuf.[i] <- struct( blkcnt_me.ofUInt64 UInt64.MaxValue, blkcnt_me.ofUInt32 UInt32.MaxValue )

        let updatedSB4K = Dictionary< SEC4K_T, ArraySegment<byte> >()
        let updatedPB4K = HashSet<SEC4K_T>()
        let enumFreeList =
            VhdxWriter.BuildFreeList mstr mstr.BAT.PayloadBlockCount
            |> Array.toSeq
            |> _.GetEnumerator()

        // Update BAT
        let rec loop1 struct ( lba1 : BLKCNT64_T, ubufcnt1 : int, gfs1 : uint64 )  : struct( uint64 * BLKCNT64_T ) =
            if lba1 < d_VirtualDiskLBACount && ubufcnt1 < updateLbaBuf.Length then

                let struct( pdidx, pblbaoff ) = VhdxCommons.LBAtoPayloadBlockIndex lba1 dstr
                let lbaend =
                    let struct( m_pdidx, _ ) = VhdxCommons.LBAtoPayloadBlockIndex lba1 mstr
                    min ( ( uint64 pdidx + 1UL ) * d_PayloadBlockLBACount ) ( ( uint64 m_pdidx + 1UL ) * m_PayloadBlockLBACount )

                match dstr.BAT.Payloads.[ int pdidx ].State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadUnapped ->
                    // Nothing to do.
                    let struct( a, b ) = updateLbaBuf.[ ubufcnt1 ]
                    if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                        loop1 struct ( lbaend, ubufcnt1, gfs1 )
                    else
                        loop1 struct ( lbaend, ubufcnt1 + 1, gfs1 )

                | PayloadZero  // Since it is explicitly set to 0, that value must also be reflected in the target of the update.
                | PayloadFullyPresent ->
                    // Allocate space in LBA units.
                    let rec loop2 ( lba2 : BLKCNT64_T ) ( ubufcnt2 : int ) ( gfs2 : uint64 ) : struct ( BLKCNT64_T * int * uint64 ) =
                        if ubufcnt2 >= updateLbaBuf.Length then
                            struct( lba2, ubufcnt2, gfs2 )
                        elif lba2 >= lbaend  then
                            // If a range spans across payload blocks, it is not treated as a contiguous region.
                            struct( lba2, ( ubufcnt2 + 1 ), gfs2 )
                        else
                            let nextGFS2 = VhdxWriter.UpdatePBForAllocate mstr lba2 enumFreeList gfs2 updatedPB4K
                            if VhdxWriter.UpdateSBForAllocate mstr lba2 updatedPB4K updatedSB4K then
                                // The value needs to be copied to this LBA.
                                let struct( a, b ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.one32 )
                                else
                                    updateLbaBuf.[ ubufcnt2 ] <- struct( a, blkcnt_me.Incr b )
                                loop2 ( blkcnt_me.Incr lba2 ) ubufcnt2 nextGFS2
                            else
                                let struct( a, _ ) = updateLbaBuf.[ ubufcnt2 ]
                                if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                    loop2 ( blkcnt_me.Incr lba2 ) ubufcnt2 nextGFS2
                                else
                                    loop2 ( blkcnt_me.Incr lba2 ) ( ubufcnt2 + 1 ) nextGFS2

                    loop2 lba1 ubufcnt1 gfs1
                    |> loop1

                | PayloadPartiallyPresent ->
                    // Check the target VHDX file for used areas on an LBA basis.
                    let rec loop2 ( lba2 : BLKCNT64_T ) ( ubufcnt2 : int ) ( gfs2 : uint64 ) : struct ( BLKCNT64_T * int * uint64 ) =
                        if ubufcnt2 >= updateLbaBuf.Length then
                            struct( lba2, ubufcnt2, gfs2 )
                        elif lba2 >= lbaend  then
                            // If a range spans across payload blocks, it is not treated as a contiguous region.
                            struct( lba2, ( ubufcnt2 + 1 ), gfs2 )
                        else
                            let struct( dsbidx, bytePos, bitPos ) = VhdxCommons.LBAtoSectorBitmapIndex lba2 dstr
                            let dsbState = ( dstr.BAT.SectorBitmap.[ int32 dsbidx ].Bitmap.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy
                            if dsbState = 1uy then
                                // The logical sector in question is in use.
                                let nextGFS2 = VhdxWriter.UpdatePBForAllocate mstr lba2 enumFreeList gfs2 updatedPB4K
                                if VhdxWriter.UpdateSBForAllocate mstr lba2 updatedPB4K updatedSB4K then
                                    // The value needs to be copied to this LBA.
                                    let struct( a, b ) = updateLbaBuf.[ ubufcnt2 ]
                                    if a = blkcnt_me.ofUInt64 UInt64.MaxValue then
                                        updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.one32 )
                                    else
                                        updateLbaBuf.[ ubufcnt2 ] <- struct( lba2, blkcnt_me.Incr b )
                                loop2 ( blkcnt_me.Incr lba2 ) ubufcnt2 nextGFS2
                            else
                                // Since the logical sector in question is unused, there is no need to allocate it to the target VHDX.
                                loop2 ( blkcnt_me.Incr lba2 ) ( ubufcnt2 + 1 ) gfs2
                    loop2 lba1 ubufcnt1 gfs1
                    |> loop1
            else
                gfs1, lba1

        let struct( requiredFileSize, nextlba ) = loop1 struct ( startLba, 0, mstr.LastFileSize )
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

            let updateLbaBuf = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 4096
            let d_VirtualDiskLBACount = Functions.CalcBlockCount64 dstr.VDI.VirtualDiskSize dstr.VDI.LogicalSectorSize

            // Flash log entries
            let! verhd1 =
                if mstr.Log.Length > 0 then
                    VhdxChecker.FlushLog mfa mstr
                else
                    Task.FromResult mstr.LoadedVarHeader

            // Update FileWriteGuid and DataWriteGuid
            let! verhd2 = VhdxCommons.UpdateFileWriteGuidAndDataWriteGuid mfa mstr verhd1

            // Allocate area and copy data
            let ps = PseudoSeqStat< ( BLKCNT64_T * VhdxMutableHeader ), VhdxMutableHeader >( ( blkcnt_me.zero64, verhd2 ) )
            for ( lba, verhd4 ) in ps do
                let requiredFileSize, updatedPB4K, updatedSB4K, nextLba =
                    VhdxMerge.UpdateBATForMergeIntermediate dstr mstr lba updateLbaBuf
                let updated4KSecsForSB =
                    updatedSB4K
                    |> Seq.map ( fun itr -> struct( itr.Key, itr.Value ) )
                    |> Seq.toArray
                let updated4KSecsForBAT = Seq.toArray updatedPB4K

                // Output BAT entries.
                let! verhd5 = VhdxWriter.WriteUpdatedBAT mfa mstr verhd4 updated4KSecsForBAT requiredFileSize

                // Output sector bitmap.
                let! verhd6 = VhdxWriter.WriteUpdatedSB mfa mstr verhd5 updated4KSecsForSB

                // Copy payload data.
                for struct( start, cnt ) in updateLbaBuf do
                    if start <> blkcnt_me.ofUInt64 UInt64.MaxValue then
                        do! VhdxMerge.CopyData dfa dstr mfa mstr start cnt

                if nextLba < d_VirtualDiskLBACount then
                    ps.Continue( nextLba, verhd6 )
                else
                    ps.Break( verhd6 )
            let verhd3 = ps.LastValue |> _.Value

            // Update parent locator.
            let structures2 = {
                mstr with
                    VDI.ParentLocator = dstr.VDI.ParentLocator;
            }
            let verhd4 = {
                verhd3 with
                    SequenceNumber = verhd3.SequenceNumber + 2UL;
            }
            let! _ = VhdxMerge.UpdateMetadata mfa structures2 verhd4
            ()
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
            let! structures = VhdxReader.ReadAllStructures childvhdx

            // Leaf-node descendants cannot be deleted (simply deleting the files is sufficient).
            if ancestor = structures.Length - 1 then
                raise <| VhdxMediaException( "Leaf nodes cannot be deleted." )

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

