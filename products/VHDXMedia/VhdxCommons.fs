//=============================================================================
// Haruka Software Storage.
// VhdxCommons.fs : Common definitions used by the VHDX media utility
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

type VhdxCommons() =

    /// GUID representing BAT in the region table.
    static member REGENT_TYPE_BAT = Guid( "2dc27766-f623-4200-9d64-115e9bfd4a08" )

    /// GUID representing the metadata in the region table.
    static member REGENT_TYPE_METADATA = Guid( "8b7ca206-4790-4b9a-b8fe-575f050f886e" )

    /// GUID representing the file parameters in the metadata item.
    static member METADATA_FILE_PARAM = Guid( "CAA16737-FA36-4D43-B3B6-33F0AA44E76B" )

    /// GUID representing the virtual disk size in the metadata item.
    static member METADATA_VIRT_DISK_SIZE = Guid( "2FA54224-CD1B-4876-B211-5DBED83BF4B8" )

    /// GUID representing the virtual disk ID in the metadata item.
    static member METADATA_VIRT_DISK_ID = Guid( "BECA12AB-B2E6-4523-93EF-C309E000C746" )

    /// GUID representing the logical sector size in the metadata item.
    static member METADATA_LOGI_SECTOR_SIZE = Guid( "8141BF1D-A96F-4709-BA47-F233A8FAAB5F" )

    /// GUID representing the physical sector size in the metadata item.
    static member METADATA_PHY_SECTOR_SIZE = Guid( "CDA348C7-445D-4471-9CC9-E9885251C556" )

    /// GUID representing the parent locator in the metadata item.
    static member METADATA_PARENT_LOC = Guid( "A8D35F2D-B30B-454D-ABF7-D3D84834AB0C" )

    /// GUID representing the type of parent locator in the metadata item.
    static member METADATA_PARENT_LOC_VHDX = Guid( "B04AEFB7-D19E-4A81-B789-25B8E9445913" )


    /// <summary>
    ///  Verify the checksum of the header.
    /// </summary>
    /// <param name="data">
    ///  The data to be verified.
    /// </param>
    /// <param name="checksum">
    ///  Checksum value.
    /// </param>
    /// <returns>
    ///  if the data is valid, it returns true.
    /// </returns>
    static member CheckHeaderChecksum( data : byte[] ) ( checksum : uint32 ) : bool =
        let oldData = ByteFunc.ReadU32LE data 4u
        ByteFunc.WriteU32LE data 4u 0u
        let result = Crc32C.Compute data = checksum
        ByteFunc.WriteU32LE data 4u oldData
        result

    /// <summary>
    ///  Write VHDX header data
    /// </summary>
    /// <param name="fa">
    ///  File accessor for VHDX file.
    /// </param>
    /// <param name="header">
    ///  Header values loaded when the VHDX file was opened.
    /// </param>
    /// <param name="verHeader">
    ///  Latest header values.
    /// </param>
    /// <returns>
    ///  Sequence number to be used when the header is updated next time.
    /// </returns>
    static member UpdateHeader ( fa : FileAccessor ) ( header : VhdxHeader ) ( verHeader : VhdxMutableHeader ) : Task<VhdxMutableHeader> =
        task {
            let hdrBuf1 = PooledBuffer.RentAndInit 4096
            ByteFunc.WriteU32BEPB hdrBuf1 0u header.Signature
            ByteFunc.WriteU32LEPB hdrBuf1 4u 0u
            ByteFunc.WriteU64LEPB hdrBuf1 8u verHeader.SequenceNumber
            ByteFunc.WriteGuidPB hdrBuf1 16u verHeader.FileWriteGuid
            ByteFunc.WriteGuidPB hdrBuf1 32u verHeader.DataWriteGuid
            ByteFunc.WriteGuidPB hdrBuf1 48u verHeader.LogGuid
            ByteFunc.WriteU16LEPB hdrBuf1 64u header.LogVersion
            ByteFunc.WriteU16LEPB hdrBuf1 66u header.Version
            ByteFunc.WriteU32LEPB hdrBuf1 68u header.LogLength
            ByteFunc.WriteU64LEPB hdrBuf1 72u header.LogOffset
            let checkSum = Crc32C.Compute hdrBuf1.ArraySegment
            ByteFunc.WriteU32LEPB hdrBuf1 4u checkSum

            // Update old header
            let oldHeaderOffset = 0x30000UL - header.Offset
            do! fa.Write oldHeaderOffset ( hdrBuf1.ArraySegment )

            // Update new header
            ByteFunc.WriteU32LEPB hdrBuf1 4u 0u
            ByteFunc.WriteU64LEPB hdrBuf1 8u ( verHeader.SequenceNumber + 1UL )
            let checkSum2 = Crc32C.Compute hdrBuf1.ArraySegment
            ByteFunc.WriteU32LEPB hdrBuf1 4u checkSum2
            do! fa.Write header.Offset ( hdrBuf1.ArraySegment )

            PooledBuffer.Return hdrBuf1
            return {
                verHeader with
                    SequenceNumber = verHeader.SequenceNumber + 2UL
            }
        }

    /// <summary>
    ///  Identify the byte location to access based on the LBA.
    /// </summary>
    /// <param name="">
    ///  LBA value used to determine location.
    /// </param>
    /// <param name="meta">
    ///  Metadata for VHDX files.
    /// </param>
    /// <returns>
    ///  Pair of file index(​​in the array meta) and byte offset in the VHDX file.
    /// </returns>
    static member ResolvLBA( lba : BLKCNT64_T ) ( meta : VhdxStructures[] ) : struct( int32 * uint64 ) voption =
        let rec loop ( idx : int32 ) =
            if idx >= 0 then
                let pbSize =
                    meta.[idx].VDI.PayloadBlockSize |> uint64       // Payload Block Size
                let logiSecSize =
                    Blocksize.toUInt64 meta.[idx].VDI.LogicalSectorSize // Logical Sector Size
                let chunkRatio =
                    meta.[idx].BAT.ChunkRatio |> uint64                  // Chunk Ratio
                let secCntInPB = pbSize / logiSecSize                           // Number of sectors in a payload block.
                let pbIdx = ( blkcnt_me.toUInt64 lba ) / secCntInPB             // Payload block index
                let secIdxInPB = ( blkcnt_me.toUInt64 lba ) % secCntInPB        // Sector index within payload block
                let sbIdx = pbIdx / chunkRatio                                  // Index of sector bitmap BAT entries
                let pbIdxInSB = pbIdx % chunkRatio                              // Index of payload blocks within a sector bitmap BAT entry
                let byteIdxInSB =
                    ( pbIdxInSB * secCntInPB / 8UL) + ( secIdxInPB / 8UL )      // Byte position within a sector bitmap BAT entry
                let bitIdx = secIdxInPB % 8UL                                   // Bit position within a byte
                let pbEntry = meta.[idx].BAT.Payloads.[ int32 pbIdx ]      // Payload BAT Entry

                match pbEntry.State with
                | PayloadNotPresent ->
                    // The data to be accessed resides in the parent file.
                    loop ( idx - 1 )

                | PayloadUndefined
                | PayloadZero
                | PayloadUnapped ->
                    // No block allocation
                    ValueNone

                | PayloadFullyPresent ->
                    // The data to be accessed resides in this file.
                    let posInFile = pbEntry.FileOffset + secIdxInPB * logiSecSize
                    struct( idx, posInFile ) |> ValueSome

                | PayloadPartiallyPresent ->
                    // The sector bitmap BAT entries need to be examined.
                    let sb = meta.[idx].BAT.SectorBitmap[ int32 sbIdx ].Bitmap
                    let bitValue = ( sb.[ int32 byteIdxInSB ] >>> ( int32 bitIdx ) ) &&& 1uy
                    if bitValue = 1uy then
                        // The data to be accessed resides in this file.
                        let posInFile = pbEntry.FileOffset + secIdxInPB * logiSecSize
                        struct( idx, posInFile ) |> ValueSome
                    else
                        // The data to be accessed resides in the parent file.
                        loop ( idx - 1 )
            else
                // No block allocation
                ValueNone
        loop ( meta.Length - 1 )

    /// <summary>
    ///  From the LBA, calculate the payload block BAT entry index and the sector index within the payload block.
    /// </summary>
    /// <param name="lba">
    ///  LBA used for location identification.
    /// </param>
    /// <param name="structures">
    ///  VHDX file controll structures.
    /// </param>
    /// <returns>
    ///  Pair of payload block BAT entry index and sector index within the payload block.
    /// </returns>
    static member LBAtoPayloadBlockIndex ( lba : BLKCNT64_T ) ( structures : VhdxStructures ) : struct( uint32 * BLKCNT32_T ) =
        let pbsize = structures.VDI.PayloadBlockSize
        let secsize = structures.VDI.LogicalSectorSize |> Blocksize.toUInt32
        let secCntInPb = pbsize / secsize |> uint64
        let pbIndex = ( blkcnt_me.toUInt64 lba ) / secCntInPb |> uint32
        let secIndex = ( blkcnt_me.toUInt64 lba ) % secCntInPb |> uint32
        struct( pbIndex, blkcnt_me.ofUInt32 secIndex )

    /// <summary>
    ///  From the LBA, calculate sector bitmap BAT entry index and bit position within the sector bitmap.
    /// </summary>
    /// <param name="lba">
    ///  LBA used for location identification.
    /// </param>
    /// <param name="structures">
    ///  VHDX file controll structures.
    /// </param>
    /// <returns>
    ///  Pair of the sector bitmap BAT entry index and bit position within the payload block.
    /// </returns>
    static member LBAtoSectorBitmapIndex ( lba : BLKCNT64_T ) ( structures : VhdxStructures ) : struct( uint32 * uint32 * uint32 ) =
        let sbindex = ( blkcnt_me.toUInt64 lba ) / 8388608UL |> uint32
        let bitpos = ( blkcnt_me.toUInt64 lba ) % 8388608UL |> uint32
        struct( sbindex, bitpos >>> 3, bitpos &&& 7u )

    /// <summary>
    ///  Create a file filled with random bytes.
    /// </summary>
    /// <param name="fname">
    ///  Output file name.
    ///  If specified file already exists, it will be overwitten.
    /// </param>
    /// <param name="fsizemb">
    ///  File size in MB.
    /// </param>
    static member CreateRandomFile ( fname : string ) ( fsizemb : uint64 ) : unit =
        use fs = File.OpenWrite fname
        let buf = Array.zeroCreate<byte> 1048576

        for _ in 1UL .. fsizemb do
            Random.Shared.NextBytes buf
            fs.Write buf

        fs.Flush()
        fs.Close()
        fs.Dispose()

    /// <summary>
    ///  Get parent likage GUID and parent file name.
    /// </summary>
    /// <param name="structures">
    ///  VHDX file controll structures.
    /// </param>
    /// <returns>
    ///  Pair of the parent linkaged GUID value and the parent file name.
    /// </returns>
    /// <remarks>
    ///  The metadata argument must specify the metadata for the differencing VHDX file that has the parent VHDX.
    /// </remarks>
    static member GetParentFileName ( structures : VhdxStructures ) : struct( Guid * ParentLocatorType ) =
        let pl = structures.VDI.ParentLocator
        let parent_linkage = pl.[ "parent_linkage" ] |> Guid
        let plt =
            let r1, v1 = pl.TryGetValue "relative_path"
            let r2, v2 = pl.TryGetValue "volume_path"
            let r3, v3 = pl.TryGetValue "absolute_win32_path"
            if r1 then
                ParentLocatorType.RelativePath( v1 )
            elif r2 then
                ParentLocatorType.VolumePath( v2 )
            elif r3 then
                ParentLocatorType.AbsoluteWin32Path( v3 )
            else
                raise <| VhdxMediaException( structures, "Unable to identify the parent VHDX file name." )
        struct( parent_linkage, plt )
        
    /// <summary>
    ///  Update the FileWriteGuid and DataWriteGuid values ​​in the header.
    /// </summary>
    /// <param name="fa">
    ///  The fileAccessor object for the VHDX file.
    /// </param>
    /// <param name="structures">
    ///  VHDX file controll structures.
    /// </param>
    /// <returns>
    ///  Latest header values.
    /// </returns>
    static member UpdateFileWriteGuidAndDataWriteGuid ( fa : FileAccessor ) ( structures : VhdxStructures ) ( verhd : VhdxMutableHeader ) : Task<VhdxMutableHeader> =
        let verhd2 = {
            FileWriteGuid = Guid.NewGuid();
            DataWriteGuid = Guid.NewGuid();
            LogGuid = Guid();
            SequenceNumber = verhd.SequenceNumber + 1UL;
        }
        VhdxCommons.UpdateHeader fa structures.ImmHeader verhd2
