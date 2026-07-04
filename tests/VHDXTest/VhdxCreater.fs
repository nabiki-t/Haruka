namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary
open System.Text

open Haruka.Constants
open Haruka.Commons


/// <summary>
///  Create an empty VHDX file.
/// </summary>
type VhdxCreator() =

    /// <summary>
    ///  Write file type identifier.
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    static member private WriteFileTypeIdentifier ( fs : FileStream ) : unit =
        let buf = Array.zeroCreate<byte> 520
        VhdxCommon.WriteUInt64BE buf 0u 0x7668647866696C65UL
        let creator =
            "VHDXTest.VhdxCreator"
            |> Encoding.Unicode.GetBytes
        Array.blit creator 0 buf 8 creator.Length
        fs.Write( buf )

    /// <summary>
    ///  Output region table.
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
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
        ( fs : FileStream )
        ( metadataStartPos : uint64 )
        ( batRegionStartPos : uint64 )
        ( batRegionSize : uint64 )
        : unit =

        let buf = Array.zeroCreate<byte> 65536

        // Header
        VhdxCommon.WriteUInt32BE buf 0u 0x72656769u    // Signature
        VhdxCommon.WriteUInt32LE buf 4u 0u             // Shechsum
        VhdxCommon.WriteUInt32LE buf 8u 2u             // Entry count

        // Metadata
        VhdxCommon.WriteGuid buf 16u VhdxCommon.REGENT_TYPE_METADATA
        VhdxCommon.WriteUInt64LE buf 32u metadataStartPos          // Metadata region start position.
        VhdxCommon.WriteUInt32LE buf 40u 1048576u                  // Metadata region length
        VhdxCommon.WriteUInt32LE buf 44u 1u                        // Required

        // BAT
        VhdxCommon.WriteGuid buf 48u VhdxCommon.REGENT_TYPE_BAT
        VhdxCommon.WriteUInt64LE buf 64u batRegionStartPos         // BAT region start position.
        VhdxCommon.WriteUInt32LE buf 72u ( uint32 batRegionSize )  // BAT region length
        VhdxCommon.WriteUInt32LE buf 76u 1u                        // Required

        // Checksum
        let checkSum = Crc32C.Compute buf                       // Update checksum
        VhdxCommon.WriteUInt32LE buf 4u checkSum

        fs.Seek( 196608L, SeekOrigin.Begin ) |> ignore
        fs.Write( buf )
        fs.Flush()

        fs.Seek( 262144L, SeekOrigin.Begin ) |> ignore
        fs.Write( buf )
        fs.Flush()

    /// <summary>
    ///  Write metadata
    /// </summary>
    /// <param name="fs">
    ///  File stream for the VHDX file.
    /// </param>
    /// <param name="metadataStartPos">
    ///  File offset for the location where metadata is written.
    /// </param>
    /// <param name="payloadBlockSize">
    ///  Payload block size value in file parameter.
    /// </param>
    /// <param name="isFixed">
    ///  A-LeaveBlockAllocated flag value in the file parameter.
    /// </param>
    /// <param name="hasParent">
    ///  B-HasParent flag value in file parameter.
    /// </param>
    /// <param name="virtualDiskSize">
    ///  Virtual disk size value in the virtual disk size parameter.
    /// </param>
    /// <param name="virtualDiskID">
    ///  Virtual disk ID value in the virtual disk ID parameter.
    /// </param>
    /// <param name="sectorSize">
    ///  Sector size tat is used for the logical sector size and PhysicalSectorSize.
    /// </param>
    /// <param name="parentDataWriteGuid">
    ///  Value of Data write GUID on the parent virtual disk.
    /// </param>
    /// <param name="parentFileName">
    ///  File name of parent VHDX file.
    /// </param>
    static member private WriteMetadata
        ( fs : FileStream )
        ( metadataStartPos : uint64 )
        ( payloadBlockSize : uint32 )
        ( isFixed : bool )
        ( hasParent : bool )
        ( virtualDiskSize : uint64 )
        ( virtualDiskID : Guid )
        ( sectorSize : Blocksize )
        ( parentDataWriteGuid : Guid )
        ( parentFileName : string )
        : unit =

        // MemoryStream for temporarily recording metadata items
        let ms = new MemoryStream()

        // File parameter
        let fileParamBuf = Array.zeroCreate<byte> 8
        let fileParamStartPos = 0u
        VhdxCommon.WriteUInt32LE fileParamBuf 0u payloadBlockSize
        fileParamBuf.[4] <-
            ( if isFixed then 1uy else 0uy ) |||
            ( if hasParent then 2uy else 0uy )
        ms.Write( fileParamBuf )

        // Virtual disk size
        let vdsParamBuf = Array.zeroCreate<byte> 8
        let vdsParamStartPos = ms.Length |> uint32
        VhdxCommon.WriteUInt64LE vdsParamBuf 0u virtualDiskSize
        ms.Write( vdsParamBuf )

        // Virtual disk ID
        let vdidParamBuf = Array.zeroCreate<byte> 16
        let vdidParamStartPos = ms.Length |> uint32
        VhdxCommon.WriteGuid vdidParamBuf 0u virtualDiskID
        ms.Write( vdidParamBuf )

        // Logical sector size
        let lssParamBuf = Array.zeroCreate<byte> 4
        let lssParamStartPos = ms.Length |> uint32
        VhdxCommon.WriteUInt32LE lssParamBuf 0u ( Blocksize.toUInt32 sectorSize )
        ms.Write( lssParamBuf )

        // Physical sector size
        let pssParamBuf = Array.zeroCreate<byte> 4
        let pssParamStartPos = ms.Length |> uint32
        VhdxCommon.WriteUInt32LE pssParamBuf 0u ( Blocksize.toUInt32 sectorSize )
        ms.Write( pssParamBuf )

        // Parent locator
        let plParamStartPos, plParamLen =
            if hasParent then
                let parentLinkageKey =
                    "parent_linkage"
                    |> Encoding.Unicode.GetBytes
                let parentLinkageVal =
                    parentDataWriteGuid.ToString "b"
                    |> Encoding.Unicode.GetBytes
                let relativePathKey =
                    "relative_path"
                    |> Encoding.Unicode.GetBytes
                let relativePathVal =
                    parentFileName
                    |> Encoding.Unicode.GetBytes
                let parentLinkageKey_StartPos = 20 + 12 * 2
                let parentLinkageVal_StartPos =
                        parentLinkageKey_StartPos + parentLinkageKey.Length
                let relativePathKey_StartPos =
                        parentLinkageVal_StartPos + parentLinkageVal.Length
                let relativePathVal_StartPos =
                        relativePathKey_StartPos + relativePathKey.Length
                let buflen = relativePathVal_StartPos + relativePathVal.Length
                let plParamBuf = Array.zeroCreate<byte> buflen

                // Parent locator header
                VhdxCommon.WriteGuid plParamBuf 0u VhdxCommon.METADATA_PARENT_LOC_VHDX
                VhdxCommon.WriteUInt16LE plParamBuf 18u 2us

                // Parent locator entry (parent_linkage)
                VhdxCommon.WriteUInt32LE plParamBuf 20u ( uint32 parentLinkageKey_StartPos )
                VhdxCommon.WriteUInt32LE plParamBuf 24u ( uint32 parentLinkageVal_StartPos )
                VhdxCommon.WriteUInt16LE plParamBuf 28u ( uint16 parentLinkageKey.Length )
                VhdxCommon.WriteUInt16LE plParamBuf 30u ( uint16 parentLinkageVal.Length )

                // Parent locator entry (relative_path)
                VhdxCommon.WriteUInt32LE plParamBuf 32u ( uint32 relativePathKey_StartPos )
                VhdxCommon.WriteUInt32LE plParamBuf 36u ( uint32 relativePathVal_StartPos )
                VhdxCommon.WriteUInt16LE plParamBuf 40u ( uint16 relativePathKey.Length )
                VhdxCommon.WriteUInt16LE plParamBuf 42u ( uint16 relativePathVal.Length )

                // parent_linkage
                Array.blit parentLinkageKey 0 plParamBuf parentLinkageKey_StartPos parentLinkageKey.Length
                Array.blit parentLinkageVal 0 plParamBuf parentLinkageVal_StartPos parentLinkageVal.Length

                // relative_path
                Array.blit relativePathKey 0 plParamBuf relativePathKey_StartPos relativePathKey.Length
                Array.blit relativePathVal 0 plParamBuf relativePathVal_StartPos relativePathVal.Length

                let plParamStartPos = ms.Length |> uint32
                ms.Write( plParamBuf )
                plParamStartPos, ( uint32 buflen )
            else
                0u, 0u

        // Metadata table header
        let entryCount = if hasParent then 6 else 5
        let tableLen = 32 + 32 * entryCount
        let metadatabuf = Array.zeroCreate<byte> tableLen
        VhdxCommon.WriteUInt64LE metadatabuf 0u 0x617461646174656DUL      // signature
        VhdxCommon.WriteUInt16LE metadatabuf 10u ( uint16 entryCount )    // Entry count

        // Metadata table entry ( file parameter )
        VhdxCommon.WriteGuid metadatabuf 32u VhdxCommon.METADATA_FILE_PARAM           // Item ID
        VhdxCommon.WriteUInt32LE metadatabuf 48u ( fileParamStartPos + 65536u )    // Offset
        VhdxCommon.WriteUInt32LE metadatabuf 52u 8u                                // Length
        metadatabuf.[56] <- 4uy

        // Metadata table entry ( Virtual disk size )
        VhdxCommon.WriteGuid metadatabuf 64u VhdxCommon.METADATA_VIRT_DISK_SIZE       // Item ID
        VhdxCommon.WriteUInt32LE metadatabuf 80u ( vdsParamStartPos + 65536u )     // Offset
        VhdxCommon.WriteUInt32LE metadatabuf 84u 8u                                // Length
        metadatabuf.[88] <- 6uy

        // Metadata table entry ( Virtual disk ID )
        VhdxCommon.WriteGuid metadatabuf 96u VhdxCommon.METADATA_VIRT_DISK_ID         // Item ID
        VhdxCommon.WriteUInt32LE metadatabuf 112u ( vdidParamStartPos + 65536u )   // Offset
        VhdxCommon.WriteUInt32LE metadatabuf 116u 16u                              // Length
        metadatabuf.[120] <- 6uy

        // Metadata table entry ( Logical sector size )
        VhdxCommon.WriteGuid metadatabuf 128u VhdxCommon.METADATA_LOGI_SECTOR_SIZE    // Item ID
        VhdxCommon.WriteUInt32LE metadatabuf 144u ( lssParamStartPos + 65536u )    // Offset
        VhdxCommon.WriteUInt32LE metadatabuf 148u 4u                               // Length
        metadatabuf.[152] <- 6uy

        // Metadata table entry ( Physical sector size )
        VhdxCommon.WriteGuid metadatabuf 160u VhdxCommon.METADATA_PHY_SECTOR_SIZE     // Item ID
        VhdxCommon.WriteUInt32LE metadatabuf 176u ( pssParamStartPos + 65536u )    // Offset
        VhdxCommon.WriteUInt32LE metadatabuf 180u 4u                               // Length
        metadatabuf.[184] <- 6uy

        // Metadata table entry ( Parent locator )
        if hasParent then
            VhdxCommon.WriteGuid metadatabuf 192u VhdxCommon.METADATA_PARENT_LOC       // Item ID
            VhdxCommon.WriteUInt32LE metadatabuf 208u ( plParamStartPos + 65536u )  // Offset
            VhdxCommon.WriteUInt32LE metadatabuf 212u plParamLen                    // Length
            metadatabuf.[216] <- 4uy

        fs.Seek( int64 metadataStartPos, SeekOrigin.Begin ) |> ignore
        fs.Write( metadatabuf )
        fs.Seek( int64 metadataStartPos + 65536L, SeekOrigin.Begin ) |> ignore
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        ms.CopyTo( fs )

    /// <summary>
    ///  Write BAT
    /// </summary>
    /// <param name="fs">
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
        ( fs : FileStream )
        ( isFixed : bool )
        ( hasParent : bool )
        ( batRegionStartPos : uint64 )
        ( batEntryCount : uint64 )
        ( payloadBlockCount : uint64 )
        ( sectorBitmapCount : uint64 )
        ( batRegionSize : uint64 )
        ( payloadBlockSize : uint32 )
        ( chunkRate : uint64 )
        : unit =

        fs.Seek( int64 batRegionStartPos, SeekOrigin.Begin ) |> ignore
        let entrybuf = Array.zeroCreate<byte> 8

        let reqFileSize =
            if isFixed then
                // Fixed VHDX file.
                printfn "Write BAT entries for fixed VHDX file."
                for i = 1 to ( int32 batEntryCount ) do
                    if i % ( int32 chunkRate + 1 ) = 0 then
                        // sector bitmat BAT entry
                        printfn "Entry(%d) : Sector bitmap Offset=0" i
                        VhdxCommon.WriteUInt64LE entrybuf 0u 0UL
                    else
                        // Payload BAT Entry
                        let payloadPos =
                            batRegionStartPos + batRegionSize +
                            ( uint64 i - 1UL ) * ( uint64 payloadBlockSize )
                        printfn "Entry(%d) : Payload Offset=%d" i payloadPos
                        VhdxCommon.WriteUInt64LE entrybuf 0u payloadPos
                        entrybuf.[0] <- 6uy
                    fs.Write( entrybuf )

                // Sector bitmaps ares not allocated. All of payload blocks are allocated.
                batRegionStartPos + batRegionSize + payloadBlockCount * ( uint64 payloadBlockSize )

            elif hasParent then
                // Differential VHDX file
                printfn "Write BAT entries for differential VHDX file."
                for i = 1 to ( int32 batEntryCount ) do
                    if i % ( int32 chunkRate + 1 ) = 0 then
                        // Sector bitmap BAT entry
                        let sbPos =
                            batRegionStartPos + batRegionSize +
                            ( uint64 i / chunkRate - 1UL ) * 1048576UL
                        printfn "Entry(%d) : Sector bitmap Offset=%d" i sbPos
                        VhdxCommon.WriteUInt64LE entrybuf 0u sbPos
                        entrybuf.[0] <- 6uy
                    else
                        // Payload BAT entry
                        printfn "Entry(%d) : Payload Offset=0" i
                        VhdxCommon.WriteUInt64LE entrybuf 0u 0UL
                        entrybuf.[0] <- 0uy
                    fs.Write( entrybuf )

                // Initially, no payload blocks are allocated.
                // All of sector bitmap blocks are allocated.
                batRegionStartPos + batRegionSize + sectorBitmapCount * 1048576UL

            else
                // Dynamic VHDX file.
                printfn "Write BAT entries for dynamic VHDX file."
                for i = 1 to ( int32 batEntryCount ) do
                    if i % ( int32 chunkRate + 1 ) = 0 then
                        // Sector bitmap BAT entry
                        printfn "Entry(%d) : Sector bitmap Offset=0" i
                        VhdxCommon.WriteUInt64LE entrybuf 0u 0UL
                    else
                        // Payload BAT entry
                        printfn "Entry(%d) : Payload Offset=0" i
                        VhdxCommon.WriteUInt64LE entrybuf 0u 0UL
                        entrybuf.[0] <- 0uy
                    fs.Write( entrybuf )
                // No sector bitmap blocks are alocated.
                // Initially, No payload blocks are also allocated.
                batRegionStartPos + batRegionSize

        // Set file size.
        printfn "File size : %d" reqFileSize
        fs.SetLength( int64 reqFileSize )

    /// <summary>
    ///  Create empty VHDX file.
    /// </summary>
    /// <param name="inputPath">
    ///  Parent VHDX file name.
    ///  When creating a differential VHDX file, the parent VHDX file name must be specified.
    /// </param>
    /// <param name="outputPath">
    ///  Output VHDX file name.
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
        ( inputPath : string )
        ( outputPath : string )
        ( logAreaSize : uint32 )
        ( payloadBlockSize : uint32 )
        ( isFixed : bool )
        ( virtualDiskSize : uint64 )
        ( sectorSize : Blocksize ) : unit =

        // Read parent VHDX file metadata
        let parentMetadata =
            if inputPath <> "" then
                VhdxReader.ReadVhdx inputPath |> Some
            else
                None

        // Get DataWriteGuid value of parent VHDX file.
        let parentDataWriteGuid =
            match parentMetadata with
            | Some x ->
                x.Header.DataWriteGuid
            | None ->
                Guid()

        // Determin virtual disk size.
        let efVirtualDiskSize =
            match parentMetadata with
            | Some x ->
                x.VirtualDiskInfo.VirtualDiskSize
            | None ->
                virtualDiskSize

        // Determin virtual disk ID.
        let efVirtualDiskID =
            match parentMetadata with
            | Some x ->
                x.VirtualDiskInfo.VirtualDiskId
            | None ->
                Guid.NewGuid()

        // Determin sector size.
        let efSectorSize =
            match parentMetadata with
            | Some x ->
                x.VirtualDiskInfo.LogicalSectorSize
            | None ->
                sectorSize

        printfn "========================================================"
        printfn "Create empty virtual disk"
        printfn "Input file name : %s" inputPath
        printfn "Output file name : %s" outputPath
        printfn "Log area size : %d" logAreaSize
        printfn "Payload block size : %d" payloadBlockSize
        printfn "Is fixed : %b" isFixed
        printfn "DataWriteGuid of parent disk : %s" ( parentDataWriteGuid.ToString "b" )
        printfn "Virtual disk size : %d" efVirtualDiskSize
        printfn "Virtual disk ID : %s" ( efVirtualDiskID.ToString "D" )
        printfn "Sector size : %s" ( Blocksize.toStringName efSectorSize )

        if logAreaSize &&& 0x000FFFFFu <> 0u then
            raise <| Exception "Log are size must be multiples of 1MB."
        if payloadBlockSize < 0x100000u ||                                  // 1MB or more
            0x10000000u < payloadBlockSize ||                               // 256MB or less
            ( payloadBlockSize &&& ( payloadBlockSize - 1u ) ) <> 0u then   // Powers of 2
            raise <| Exception( "The payload block length must be a power of 2, ranging from 1MB to 256MB." )
        if 0x400000000000UL < efVirtualDiskSize then
            raise <| Exception( "The virtual disk size must be 64TB or less." )
        if efVirtualDiskSize = 0UL then
            raise <| Exception( "The virtual disk size must be at least 1 byte." )
        if efVirtualDiskSize % Blocksize.toUInt64 efSectorSize <> 0UL then
            raise <| Exception( "The virtual disk size must be a multiple of the sector length." )

        let chunkSize = Blocksize.toUInt64 efSectorSize * 8388608UL
        let chunkRate = chunkSize / uint64 payloadBlockSize
        let payloadBlockCount =
            ( efVirtualDiskSize + ( uint64 payloadBlockSize - 1UL ) ) / ( uint64 payloadBlockSize )
        let sectorBitmapCount = ( payloadBlockCount + ( chunkRate - 1UL ) ) / chunkRate
        let batEntryCount =
            if inputPath.Length = 0 then
                payloadBlockCount + ( payloadBlockCount - 1UL ) / chunkRate
            else
                sectorBitmapCount * ( chunkRate + 1UL )
        let batRegionSize =
            ( batEntryCount * 64UL + 0x00000000000FFFFFUL ) &&& 0xFFFFFFFFFFF00000UL
        let metadataStartPos = 1048576UL + uint64 logAreaSize
        let batRegionStartPos = metadataStartPos + 1048576UL

        printfn "Metadeta start position : %d" metadataStartPos
        printfn "Chunk size : %d" chunkSize
        printfn "Chunk ratio : %d" chunkRate
        printfn "Payload block count : %d" payloadBlockCount
        printfn "Sector bitmap block count : %d" sectorBitmapCount
        printfn "BAT entry count : %d" batEntryCount
        printfn "BAT region size : %d" batRegionSize
        printfn "BAT region start pos : %d" batRegionStartPos

        use fs = new FileStream( outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None )

        // File type identifier
        VhdxCreator.WriteFileTypeIdentifier fs

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
        VhdxHandler.UpdateHeader fs header |> ignore

        // Region table
        VhdxCreator.WriteRegionTable fs metadataStartPos batRegionStartPos batRegionSize

        // Metadata region
        let hasParent = ( inputPath.Length > 0 )
        VhdxCreator.WriteMetadata
            fs metadataStartPos payloadBlockSize isFixed hasParent efVirtualDiskSize
            efVirtualDiskID efSectorSize parentDataWriteGuid inputPath

        // BAT
        VhdxCreator.WriteBAT
            fs isFixed ( inputPath.Length > 0 ) batRegionStartPos
            batEntryCount payloadBlockCount sectorBitmapCount
            batRegionSize payloadBlockSize chunkRate

    /// <summary>
    /// Convert raw image file to VHDX file format.
    /// </summary>
    /// <param name="inputPath">
    ///  Input raw image file name.
    /// </param>
    /// <param name="outputPath">
    ///  VHDX file name to be created.
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
        ( outputPath : string )
        ( logAreaSize : uint32 )
        ( payloadBlockSize : uint32 )
        ( isFixed : bool )
        ( sectorSize : Blocksize ) : unit =

        // Get length of input raw file.
        use rawfs = new FileStream( inputPath, FileMode.Open, FileAccess.Read, FileShare.None )
        let virtualDiskSize = uint64 rawfs.Length

        printfn "========================================================"
        printfn "Convert raw image file to VHDX file."
        printfn "Input file name : %s" inputPath
        printfn "Output file name : %s" outputPath
        printfn "Log area length : %d" logAreaSize
        printfn "Payload block length : %d" payloadBlockSize
        printfn "Fix format : %b" isFixed
        printfn "Virtual disk size : %d" virtualDiskSize
        printfn "Sector length : %s" ( Blocksize.toStringName sectorSize )

        // Create empty VHDX file.
        VhdxCreator.Create "" outputPath logAreaSize payloadBlockSize isFixed virtualDiskSize sectorSize

        // If dynamic VHDX file, it must be updated the BAT entries.
        if not isFixed then
            let metadata = VhdxReader.ReadVhdx outputPath
            use outfs = new FileStream( outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None )
            let metadataStartPos = 1048576UL + uint64 logAreaSize
            let batRegionStartPos = metadataStartPos + 1048576UL
            let batRegionSize =
                ( metadata.BatEntries.BatEntryCount * 64UL + 0x00000000000FFFFFUL ) &&& 0xFFFFFFFFFFF00000UL

            VhdxCreator.WriteBAT
                outfs true false batRegionStartPos
                metadata.BatEntries.BatEntryCount
                metadata.BatEntries.PayloadBlockCount
                metadata.BatEntries.SectorBitmapBlockCount
                batRegionSize
                payloadBlockSize
                metadata.BatEntries.ChunkRatio
            outfs.Flush()
            outfs.Close()
            outfs.Dispose()

        let metadata = VhdxReader.ReadVhdx outputPath
        use outfs = new FileStream( outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None )

        // Output payloag blocks
        let buf = Array.zeroCreate<byte>( int32 payloadBlockSize )
        for i = 0 to ( int32 metadata.BatEntries.PayloadBlockCount - 1 ) do
            let ent = metadata.BatEntries.Payloads.[i]
            let spos = ( uint64 i ) * ( uint64 payloadBlockSize )
            let len = min ( int32 payloadBlockSize ) ( int32 ( virtualDiskSize - spos ) )
            rawfs.Seek( int64 spos, SeekOrigin.Begin ) |> ignore
            rawfs.ReadExactly( buf, 0, len )
            outfs.Seek( int64 ent.FileOffset, SeekOrigin.Begin ) |> ignore
            outfs.Write( buf, 0, len )

        outfs.Flush()
        outfs.Close()
        outfs.Dispose()
