//=============================================================================
// Haruka Software Storage.
// VhdxCreater.fs : Implement functionality to create a new, empty VHDX file.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Threading.Tasks
open System.Text.RegularExpressions

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  Create an empty VHDX file.
/// </summary>
type VhdxCreator() =

    /// <summary>
    ///  Write file type identifier.
    /// </summary>
    /// <param name="fa">
    ///  File accessor for the VHDX file.
    /// </param>
    static member private WriteFileTypeIdentifier ( fa : FileAccessor ) : Task =
        let buf = Array.zeroCreate<byte> 520
        ByteFunc.WriteU64BE buf 0u 0x7668647866696C65UL
        let creator =
            "VHDXTest.VhdxCreator"
            |> Encoding.Unicode.GetBytes
        Array.blit creator 0 buf 8 creator.Length
        fa.Write 0UL ( ArraySegment buf )

    /// <summary>
    ///  Output region table.
    /// </summary>
    /// <param name="fa">
    ///  File accessor for the VHDX file.
    /// </param>
    /// <param name="metadataStartPos">
    ///  File offset for the location where metadata is written.
    /// </param>
    /// <param name="batRegionStartPos">
    ///  File offset, which is the starting position of the BAT region.
    /// </param>
    /// <param name="batRegionSize">
    ///  Bytes size of the BAT region.
    /// </param>
    static member private WriteRegionTable
        ( fa : FileAccessor )
        ( metadataStartPos : uint64 )
        ( batRegionStartPos : uint64 )
        ( batRegionSize : uint64 )
        : Task =

        task {
            let buf = Array.zeroCreate<byte> 65536

            // Header
            ByteFunc.WriteU32BE buf 0u 0x72656769u    // Signature
            ByteFunc.WriteU32LE buf 4u 0u             // Shechsum
            ByteFunc.WriteU32LE buf 8u 2u             // Entry count

            // Metadata
            ByteFunc.WriteGuid buf 16u VhdxCommons.REGENT_TYPE_METADATA
            ByteFunc.WriteU64LE buf 32u metadataStartPos          // Metadata region start position.
            ByteFunc.WriteU32LE buf 40u 1048576u                  // Metadata region length
            ByteFunc.WriteU32LE buf 44u 1u                        // Required

            // BAT
            ByteFunc.WriteGuid buf 48u VhdxCommons.REGENT_TYPE_BAT
            ByteFunc.WriteU64LE buf 64u batRegionStartPos         // BAT region start position.
            ByteFunc.WriteU32LE buf 72u ( uint32 batRegionSize )  // BAT region length
            ByteFunc.WriteU32LE buf 76u 1u                        // Required

            // Checksum
            let checkSum = Crc32C.Compute buf                       // Update checksum
            ByteFunc.WriteU32LE buf 4u checkSum

            do! fa.Write 196608UL ( ArraySegment buf )
            do! fa.Write 262144UL ( ArraySegment buf )
        }

    static member private VolumePathRegex = Regex( @"^\\\\\?\\Volume\{[0-9a-z]{8}-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{12}\}\\.*$", RegexOptions.IgnoreCase )

    /// <summary>
    ///  Obtain the path name type and the path name to be recorded in the VHDX file from the path name.
    /// </summary>
    /// <param name="pathName">
    ///  User specified path name.
    /// </param>
    /// <returns>
    ///  The pair of parent locator entry name and value.
    /// </returns>
    static member private GetLocatorEntryValue ( pathName : string ) : struct( string * string ) =
        if VhdxCreator.VolumePathRegex.IsMatch pathName then
            struct( "volume_path", pathName )
        elif Path.IsPathFullyQualified pathName then
            if pathName.StartsWith @"\\?\" then
                struct( "absolute_win32_path", pathName )
            else
                struct( "absolute_win32_path", @"\\?\" + pathName )
        else
            struct( "relative_path", pathName )

    /// <summary>
    ///  Create bytes array for metadata.
    /// </summary>
    /// <param name="vdi">
    ///  Virtual disk information.
    /// </param>
    /// <returns>
    ///  Created bytes array pair of the metadata header and the metadata tables.
    /// </returns>
    static member CreateMetadataBytes ( vdi : VirtualDiskInfo ) : ( byte[] * byte[] ) =

        let ms = new MemoryStream() // MemoryStream for temporarily recording metadata items

        // File parameter
        let fileParamBuf = Array.zeroCreate<byte> 8
        let fileParamStartPos = 0u
        ByteFunc.WriteU32LE fileParamBuf 0u vdi.PayloadBlockSize
        fileParamBuf.[4] <-
            ( if vdi.LeaveBlockAllocated then 1uy else 0uy ) |||
            ( if vdi.HasParent then 2uy else 0uy )
        ms.Write( fileParamBuf )

        // Virtual disk size
        let vdsParamBuf = Array.zeroCreate<byte> 8
        let vdsParamStartPos = ms.Length |> uint32
        ByteFunc.WriteU64LE vdsParamBuf 0u vdi.VirtualDiskSize
        ms.Write( vdsParamBuf )

        // Virtual disk ID
        let vdidParamBuf = Array.zeroCreate<byte> 16
        let vdidParamStartPos = ms.Length |> uint32
        ByteFunc.WriteGuid vdidParamBuf 0u vdi.VirtualDiskId
        ms.Write( vdidParamBuf )

        // Logical sector size
        let lssParamBuf = Array.zeroCreate<byte> 4
        let lssParamStartPos = ms.Length |> uint32
        ByteFunc.WriteU32LE lssParamBuf 0u ( Blocksize.toUInt32 vdi.LogicalSectorSize )
        ms.Write( lssParamBuf )

        // Physical sector size
        let pssParamBuf = Array.zeroCreate<byte> 4
        let pssParamStartPos = ms.Length |> uint32
        ByteFunc.WriteU32LE pssParamBuf 0u ( Blocksize.toUInt32 vdi.PhysicalSectorSize )
        ms.Write( pssParamBuf )

        // Parent locator
        let plParamStartPos, plParamLen =
            if vdi.HasParent then
                let parentLinkageKey = Encoding.Unicode.GetBytes "parent_linkage"
                let parentLinkageVal = vdi.ParentLocator.["parent_linkage"] |> Encoding.Unicode.GetBytes

                let hasRelativePath = vdi.ParentLocator.ContainsKey "relative_path"
                let hasVolumePath = vdi.ParentLocator.ContainsKey "volume_path"
                let hasAbsoluteWin32Path = vdi.ParentLocator.ContainsKey "absolute_win32_path"
                let keyValueCount =
                    ( if hasRelativePath then 1 else 0 ) +
                    ( if hasVolumePath then 1 else 0 ) +
                    ( if hasAbsoluteWin32Path then 1 else 0 ) + 1

                let relativePathKey, relativePathVal =
                    if hasRelativePath then
                        ( Encoding.Unicode.GetBytes "relative_path" ), ( Encoding.Unicode.GetBytes vdi.ParentLocator.[ "relative_path" ] )
                    else
                        Array.Empty(), Array.Empty()

                let volumePathKey, volumePathVal =
                    if hasVolumePath then
                        ( Encoding.Unicode.GetBytes "volume_path" ), ( Encoding.Unicode.GetBytes vdi.ParentLocator.[ "volume_path" ] )
                    else
                        Array.Empty(), Array.Empty()

                let absoluteWin32PathKey, absoluteWin32PathVal =
                    if hasAbsoluteWin32Path then
                        ( Encoding.Unicode.GetBytes "absolute_win32_path" ), ( Encoding.Unicode.GetBytes vdi.ParentLocator.[ "absolute_win32_path" ] )
                    else
                        Array.Empty(), Array.Empty()

                let parentLinkageKey_StartPos = 20 + 12 * keyValueCount
                let parentLinkageVal_StartPos = parentLinkageKey_StartPos + parentLinkageKey.Length
                let relativePathKey_StartPos = parentLinkageVal_StartPos + parentLinkageVal.Length
                let relativePathVal_StartPos = relativePathKey_StartPos + relativePathKey.Length
                let volumePathKey_StartPos = relativePathVal_StartPos + relativePathVal.Length
                let volumePathVal_StartPos = volumePathKey_StartPos + volumePathKey.Length
                let absoluteWin32PathKey_StartPos = volumePathVal_StartPos + volumePathVal.Length
                let absoluteWin32PathVal_StartPos = absoluteWin32PathKey_StartPos + absoluteWin32PathKey.Length
                let buflen = absoluteWin32PathVal_StartPos + absoluteWin32PathVal.Length
                let plParamBuf = Array.zeroCreate<byte> buflen

                // Parent locator header
                ByteFunc.WriteGuid plParamBuf 0u VhdxCommons.METADATA_PARENT_LOC_VHDX
                ByteFunc.WriteU16LE plParamBuf 18u ( uint16 keyValueCount )

                // Parent locator entry
                let v = [|
                    yield ( uint32 parentLinkageKey_StartPos, uint32 parentLinkageVal_StartPos, uint16 parentLinkageKey.Length, uint16 parentLinkageVal.Length );
                    if hasRelativePath then
                        yield ( uint32 relativePathKey_StartPos, uint32 relativePathVal_StartPos, uint16 relativePathKey.Length, uint16 relativePathVal.Length );
                    if hasVolumePath then
                        yield ( uint32 volumePathKey_StartPos, uint32 volumePathVal_StartPos, uint16 volumePathKey.Length, uint16 volumePathVal.Length );
                    if hasAbsoluteWin32Path then
                        yield ( uint32 absoluteWin32PathKey_StartPos, uint32 absoluteWin32PathVal_StartPos, uint16 absoluteWin32PathKey.Length, uint16 absoluteWin32PathVal.Length );
                |]
                for i = 0 to v.Length - 1 do
                    let ( key_StartPos, val_StartPos, key_Length, val_Length ) = v.[i]
                    let p = 20u + ( uint32 i ) * 12u
                    ByteFunc.WriteU32LE plParamBuf ( p       ) key_StartPos
                    ByteFunc.WriteU32LE plParamBuf ( p + 4u  ) val_StartPos
                    ByteFunc.WriteU16LE plParamBuf ( p + 8u  ) key_Length
                    ByteFunc.WriteU16LE plParamBuf ( p + 10u ) val_Length

                // parent_linkage
                Array.blit parentLinkageKey 0 plParamBuf parentLinkageKey_StartPos parentLinkageKey.Length
                Array.blit parentLinkageVal 0 plParamBuf parentLinkageVal_StartPos parentLinkageVal.Length

                // relative_path
                if hasRelativePath then
                    Array.blit relativePathKey 0 plParamBuf relativePathKey_StartPos relativePathKey.Length
                    Array.blit relativePathVal 0 plParamBuf relativePathVal_StartPos relativePathVal.Length

                // volume_path
                if hasVolumePath then
                    Array.blit volumePathKey 0 plParamBuf volumePathKey_StartPos volumePathKey.Length
                    Array.blit volumePathVal 0 plParamBuf volumePathVal_StartPos volumePathVal.Length

                // volume_path
                if hasAbsoluteWin32Path then
                    Array.blit absoluteWin32PathKey 0 plParamBuf absoluteWin32PathKey_StartPos absoluteWin32PathKey.Length
                    Array.blit absoluteWin32PathVal 0 plParamBuf absoluteWin32PathVal_StartPos absoluteWin32PathVal.Length

                let plParamStartPos = ms.Length |> uint32
                ms.Write( plParamBuf )
                plParamStartPos, ( uint32 buflen )
            else
                0u, 0u

        // Metadata table header
        let entryCount = if vdi.HasParent then 6 else 5
        let tableLen = 32 + 32 * entryCount
        let metadatabuf = Array.zeroCreate<byte> tableLen
        ByteFunc.WriteU64LE metadatabuf 0u 0x617461646174656DUL                     // signature
        ByteFunc.WriteU16LE metadatabuf 10u ( uint16 entryCount )                   // Entry count

        // Metadata table entry ( file parameter )
        ByteFunc.WriteGuid metadatabuf 32u VhdxCommons.METADATA_FILE_PARAM          // Item ID
        ByteFunc.WriteU32LE metadatabuf 48u ( fileParamStartPos + 65536u )          // Offset
        ByteFunc.WriteU32LE metadatabuf 52u 8u                                      // Length
        metadatabuf.[56] <- 4uy

        // Metadata table entry ( Virtual disk size )
        ByteFunc.WriteGuid metadatabuf 64u VhdxCommons.METADATA_VIRT_DISK_SIZE      // Item ID
        ByteFunc.WriteU32LE metadatabuf 80u ( vdsParamStartPos + 65536u )           // Offset
        ByteFunc.WriteU32LE metadatabuf 84u 8u                                      // Length
        metadatabuf.[88] <- 6uy

        // Metadata table entry ( Virtual disk ID )
        ByteFunc.WriteGuid metadatabuf 96u VhdxCommons.METADATA_VIRT_DISK_ID        // Item ID
        ByteFunc.WriteU32LE metadatabuf 112u ( vdidParamStartPos + 65536u )         // Offset
        ByteFunc.WriteU32LE metadatabuf 116u 16u                                    // Length
        metadatabuf.[120] <- 6uy

        // Metadata table entry ( Logical sector size )
        ByteFunc.WriteGuid metadatabuf 128u VhdxCommons.METADATA_LOGI_SECTOR_SIZE   // Item ID
        ByteFunc.WriteU32LE metadatabuf 144u ( lssParamStartPos + 65536u )          // Offset
        ByteFunc.WriteU32LE metadatabuf 148u 4u                                     // Length
        metadatabuf.[152] <- 6uy

        // Metadata table entry ( Physical sector size )
        ByteFunc.WriteGuid metadatabuf 160u VhdxCommons.METADATA_PHY_SECTOR_SIZE    // Item ID
        ByteFunc.WriteU32LE metadatabuf 176u ( pssParamStartPos + 65536u )          // Offset
        ByteFunc.WriteU32LE metadatabuf 180u 4u                                     // Length
        metadatabuf.[184] <- 6uy

        // Metadata table entry ( Parent locator )
        if vdi.HasParent then
            ByteFunc.WriteGuid metadatabuf 192u VhdxCommons.METADATA_PARENT_LOC     // Item ID
            ByteFunc.WriteU32LE metadatabuf 208u ( plParamStartPos + 65536u )       // Offset
            ByteFunc.WriteU32LE metadatabuf 212u plParamLen                         // Length
            metadatabuf.[216] <- 4uy

        ( metadatabuf, ms.ToArray() )


    /// <summary>
    ///  Write BAT
    /// </summary>
    /// <param name="fa">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="isFixed">
    ///  A-LeaveBlockAllocated flag value in the file parameter.
    /// </param>
    /// <param name="hasParent">
    ///  B-HasParent flag value in file parameter.
    /// </param>
    /// <param name="batRegionStartPos">
    ///  File offset for the location where BAT is written.
    /// </param>
    /// <param name="batEntryCount">
    ///  Number of BAT entries.
    /// </param>
    /// <param name="payloadBlockCount">
    ///  Number of payload blocks.
    /// </param>
    /// <param name="sectorBitmapCount">
    ///  Number of sector bitmap blocks.
    /// </param>
    /// <param name="batRegionSize">
    ///  Length of BAT regison.
    /// </param>
    /// <param name="payloadBlockSize">
    ///  Length of a payload block.
    /// </param>
    /// <param name="chunkRate">
    ///  Chunk rate.
    /// </param>
    static member private WriteBAT
        ( fa : FileAccessor )
        ( isFixed : bool )
        ( hasParent : bool )
        ( batRegionStartPos : uint64 )
        ( batEntryCount : uint64 )
        ( payloadBlockCount : uint64 )
        ( sectorBitmapCount : uint64 )
        ( batRegionSize : uint64 )
        ( payloadBlockSize : uint32 )
        ( chunkRate : uint64 )
        : Task =

        task {
            let entrybuf = Array.zeroCreate<byte>( int32 batEntryCount * 8 )

            let! reqFileSize = task {
                if isFixed then
                    // Fixed VHDX file.
                    // Sector bitmaps ares not allocated. All of payload blocks are allocated.
                    for i in 0UL .. batEntryCount - 1UL do
                        if ( i + 1UL ) % ( chunkRate + 1UL ) = 0UL then
                            // sector bitmat BAT entry
                            ByteFunc.WriteU64LE entrybuf ( uint32 i * 8u ) 0UL
                        else
                            // Payload BAT Entry
                            let payloadPos =
                                batRegionStartPos + batRegionSize +
                                ( uint64 i - 1UL ) * ( uint64 payloadBlockSize )
                            ByteFunc.WriteU64LE entrybuf ( uint32 i * 8u ) payloadPos
                            entrybuf.[ int32 i * 8 ] <- 6uy
                    do! fa.Write batRegionStartPos ( ArraySegment entrybuf )
                    return batRegionStartPos + batRegionSize + payloadBlockCount * ( uint64 payloadBlockSize )

                elif hasParent then
                    // Differential VHDX file

                    // Initially, no payload blocks are allocated.
                    // All of sector bitmap blocks are allocated.
                    for i in 0UL .. batEntryCount - 1UL do
                        if ( i + 1UL ) % ( chunkRate + 1UL ) = 0UL then
                            // Sector bitmap BAT entry
                            let sbPos =
                                batRegionStartPos + batRegionSize +
                                ( uint64 i / chunkRate - 1UL ) * 1048576UL
                            ByteFunc.WriteU64LE entrybuf ( uint32 i * 8u ) sbPos
                            entrybuf.[int32 i * 8] <- 6uy
                        else
                            // Payload BAT entry
                            ByteFunc.WriteU64LE entrybuf ( uint32 i * 8u ) 0UL
                    do! fa.Write batRegionStartPos ( ArraySegment entrybuf )
                    return batRegionStartPos + batRegionSize + sectorBitmapCount * 1048576UL

                else
                    // Dynamic VHDX file.

                    // No sector bitmap blocks are alocated.
                    // Initially, No payload blocks are also allocated.
                    Array.fill entrybuf 0 ( int batEntryCount ) 0uy
                    do! fa.Write batRegionStartPos ( ArraySegment entrybuf )
                    return batRegionStartPos + batRegionSize
            }

            // Set file size.
            do! fa.SetFileSize( reqFileSize )
        }

    /// <summary>
    ///  Create empty VHDX file.
    /// </summary>
    /// <param name="inputFile">
    ///  Parent VHDX file.
    ///  When creating a differential VHDX file, the parent VHDX file name must be specified.
    /// </param>
    /// <param name="outputFile">
    ///  Output VHDX file.
    /// </param>
    /// <param name="logAreaSize">
    ///  Byte length of log area.
    /// </param>
    /// <param name="payloadBlockSize">
    ///  Byte length of a payload block.
    /// </param>
    /// <param name="isFixed">
    ///  If true is specified, a fixed VHDX file will be created.
    /// If inputPath is specified, isFixed value is ignored.
    /// </param>
    /// <param name="virtualDiskSize">
    ///  Bytes length of virtual disk size.
    ///  If inputPath is specified, virtualDiskSize value is ignored.
    /// </param>
    /// <param name="sectorSize">
    ///  Bytes length of a sector size.
    ///  If inputPath is specified, sectorSize value is ignored.
    /// </param>
    static member Create
        ( inputFile : FileAccessor option )
        ( outputFile : FileAccessor )
        ( logAreaSize : uint32 )
        ( payloadBlockSize : uint32 )
        ( isFixed : bool )
        ( virtualDiskSize : uint64 )
        ( sectorSize : Blocksize ) : Task =
        task {
            // Read parent VHDX file structures
            let! parentStructures = task {
                match inputFile with
                | Some x ->
                    let! r = VhdxReader.ReadVhdx x
                    return Some r
                | None ->
                    return None
            }

            // Get DataWriteGuid value of parent VHDX file.
            let parentDataWriteGuid =
                match parentStructures with
                | Some x ->
                    x.Header.DataWriteGuid
                | None ->
                    Guid()

            // Determin virtual disk size.
            let efVirtualDiskSize =
                match parentStructures with
                | Some x ->
                    x.VDI.VirtualDiskSize
                | None ->
                    virtualDiskSize

            // Determin virtual disk ID.
            let efVirtualDiskID =
                match parentStructures with
                | Some x ->
                    x.VDI.VirtualDiskId
                | None ->
                    Guid.NewGuid()

            // Determin sector size.
            let efSectorSize =
                match parentStructures with
                | Some x ->
                    x.VDI.LogicalSectorSize
                | None ->
                    sectorSize

            if logAreaSize &&& 0x000FFFFFu <> 0u then
                raise <| VhdxMediaException( sprintf "Log are size must be multiples of 1MB. Specified sizse=%d" logAreaSize )
            if payloadBlockSize < 0x100000u ||                                  // 1MB or more
                0x10000000u < payloadBlockSize ||                               // 256MB or less
                ( payloadBlockSize &&& ( payloadBlockSize - 1u ) ) <> 0u then   // Powers of 2
                raise <| VhdxMediaException( sprintf "The payload block length must be a power of 2, ranging from 1MB to 256MB. Specified sizse=%d" payloadBlockSize )
            if 0x400000000000UL < efVirtualDiskSize then
                raise <| VhdxMediaException( sprintf "The virtual disk size must be 64TB or less. Specified sizse=%d" efVirtualDiskSize )
            if efVirtualDiskSize = 0UL then
                raise <| VhdxMediaException( "The virtual disk size must be at least 1 byte." )
            if efVirtualDiskSize % Blocksize.toUInt64 efSectorSize <> 0UL then
                raise <| VhdxMediaException( sprintf "The virtual disk size must be a multiple of the sector length. Specified sizse=%d" efVirtualDiskSize )

            let chunkSize = Blocksize.toUInt64 efSectorSize * 8388608UL
            let chunkRate = chunkSize / uint64 payloadBlockSize
            let payloadBlockCount =
                ( efVirtualDiskSize + ( uint64 payloadBlockSize - 1UL ) ) / ( uint64 payloadBlockSize )
            let sectorBitmapCount = ( payloadBlockCount + ( chunkRate - 1UL ) ) / chunkRate
            let batEntryCount =
                if inputFile.IsNone then
                    payloadBlockCount + ( payloadBlockCount - 1UL ) / chunkRate
                else
                    sectorBitmapCount * ( chunkRate + 1UL )
            let batRegionSize =
                ( batEntryCount * 64UL + 0x00000000000FFFFFUL ) &&& 0xFFFFFFFFFFF00000UL
            let metadataStartPos = 1048576UL + uint64 logAreaSize
            let batRegionStartPos = metadataStartPos + 1048576UL

            // File type identifier
            do! outputFile.SetFileSize ( batRegionStartPos + batRegionSize )
            do! VhdxCreator.WriteFileTypeIdentifier outputFile

            // Header
            let header = {
                Signature = 0x68656164u;
                Checksum = 0u;              // unused
                SequenceNumber = 1UL;
                FileWriteGuid = Guid.NewGuid();
                DataWriteGuid = Guid.NewGuid();
                LogGuid = Guid()            // log is cleared
                LogVersion = 0us;           // Always 0
                Version = 1us;              // Always 1
                LogLength = logAreaSize;
                LogOffset = 1048576UL;
                Offset = 0x10000UL;
                Index = 0;                  // unused
            }
            let! _ = VhdxCommons.UpdateHeader outputFile header

            // Region table
            do! VhdxCreator.WriteRegionTable outputFile metadataStartPos batRegionStartPos batRegionSize

            // Metadata region
            let metadataTableBytes, metadataItemsBytes =
                let hasParent = inputFile.IsSome
                let parentLocator =
                    if hasParent then
                        let struct( pathEntryKey, pathEntryValue ) = VhdxCreator.GetLocatorEntryValue inputFile.Value.FileName
                        [|
                            ( "parent_linkage", ( parentDataWriteGuid.ToString "D" ) );
                            ( pathEntryKey, pathEntryValue )
                        |]
                        |> Map
                    else
                        Map.empty
                let vdi = {
                    PayloadBlockSize = payloadBlockSize;
                    LeaveBlockAllocated = isFixed;
                    HasParent = hasParent;
                    VirtualDiskSize = efVirtualDiskSize;
                    VirtualDiskId = efVirtualDiskID;
                    LogicalSectorSize = efSectorSize;
                    PhysicalSectorSize = efSectorSize;
                    ParentLocator = parentLocator;
                }
                VhdxCreator.CreateMetadataBytes vdi
            do! outputFile.Write metadataStartPos ( ArraySegment metadataTableBytes )
            do! outputFile.Write ( metadataStartPos + 65536UL ) ( ArraySegment metadataItemsBytes )

            // BAT
            do! VhdxCreator.WriteBAT
                    outputFile isFixed inputFile.IsSome batRegionStartPos
                    batEntryCount payloadBlockCount sectorBitmapCount
                    batRegionSize payloadBlockSize chunkRate
        }

    /// <summary>
    /// Convert raw image file to VHDX file format.
    /// </summary>
    /// <param name="inputPath">
    ///  Input raw image file name.
    /// </param>
    /// <param name="outputFile">
    ///  VHDX file to be created.
    /// </param>
    /// <param name="logAreaSize">
    ///  Bytes length of log are.
    /// </param>
    /// <param name="payloadBlockSize">
    ///  Bytes length of a payload block.
    /// </param>
    /// <param name="isFixed">
    ///  If true is specified, a fixed VHDX file will be created.
    /// </param>
    /// <param name="sectorSize">
    ///  Bytes length of a sector size.
    /// </param>
    static member RawToVHDX
        ( inputPath : string )
        ( outputFile : FileAccessor )
        ( logAreaSize : uint32 )
        ( payloadBlockSize : uint32 )
        ( isFixed : bool )
        ( sectorSize : Blocksize ) : Task =

        task {
            // Get length of input raw file.
            use rawfs = new FileStream( inputPath, FileMode.Open, FileAccess.Read, FileShare.None )
            let virtualDiskSize = uint64 rawfs.Length

            // Create empty VHDX file.
            do! VhdxCreator.Create None outputFile logAreaSize payloadBlockSize isFixed virtualDiskSize sectorSize

            // If dynamic VHDX file, it must be updated the BAT entries.
            if not isFixed then
                let! structures = VhdxReader.ReadVhdx outputFile
                let metadataStartPos = 1048576UL + uint64 logAreaSize
                let batRegionStartPos = metadataStartPos + 1048576UL
                let batRegionSize =
                    ( structures.BAT.BatEntryCount * 64UL + 0x00000000000FFFFFUL ) &&& 0xFFFFFFFFFFF00000UL

                do! VhdxCreator.WriteBAT
                        outputFile true false batRegionStartPos
                        structures.BAT.BatEntryCount
                        structures.BAT.PayloadBlockCount
                        structures.BAT.SectorBitmapBlockCount
                        batRegionSize
                        payloadBlockSize
                        structures.BAT.ChunkRatio

            let! structures = VhdxReader.ReadVhdx outputFile

            // Output payloag blocks
            let buf = Array.zeroCreate<byte>( int32 payloadBlockSize )
            for i = 0 to ( int32 structures.BAT.PayloadBlockCount - 1 ) do
                let ent = structures.BAT.Payloads.[i]
                let spos = ( uint64 i ) * ( uint64 payloadBlockSize )
                let len = min ( int32 payloadBlockSize ) ( int32 ( virtualDiskSize - spos ) )
                rawfs.Seek( int64 spos, SeekOrigin.Begin ) |> ignore
                rawfs.ReadExactly( buf, 0, len )
                do! outputFile.Write ent.FileOffset ( ArraySegment( buf, 0, len ) )
        }
