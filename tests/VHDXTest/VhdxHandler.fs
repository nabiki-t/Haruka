namespace VhdxLibrary

open System
open System.IO
open System.Text
open System.Xml


/// <summary>
///  Defines functions for handling VHDX files.
/// </summary>
type VhdxHandler() =

    /// <summary>
    ///  Write VHDX header data
    /// </summary>
    /// <param name="fs">
    ///  File stream for VHDX file.
    /// </param>
    /// <param name="header">
    ///  Header data to be wrote.
    /// </param>
    /// <returns>
    ///  Sequence number to be used when the header is updated next time.
    /// </returns>
    static member UpdateHeader ( fs : FileStream ) ( header : VhdxHeader ) : uint64 =

        let hdrBuf1 : byte[] = Array.zeroCreate 4096
        GlbFunc.WriteUInt32BE hdrBuf1 0u header.Signature
        GlbFunc.WriteUInt32LE hdrBuf1 4u 0u
        GlbFunc.WriteUInt64LE hdrBuf1 8u header.SequenceNumber
        GlbFunc.WriteGuid hdrBuf1 16u header.FileWriteGuid
        GlbFunc.WriteGuid hdrBuf1 32u header.DataWriteGuid
        GlbFunc.WriteGuid hdrBuf1 48u header.LogGuid
        GlbFunc.WriteUInt16LE hdrBuf1 64u header.LogVersion
        GlbFunc.WriteUInt16LE hdrBuf1 66u header.Version
        GlbFunc.WriteUInt32LE hdrBuf1 68u header.LogLength
        GlbFunc.WriteUInt64LE hdrBuf1 72u header.LogOffset
        let checkSum = Crc32C.Compute hdrBuf1
        GlbFunc.WriteUInt32LE hdrBuf1 4u checkSum

        // Update old header
        let oldHeaderOffset = 0x30000UL - header.Offset
        fs.Seek( int64 oldHeaderOffset, SeekOrigin.Begin ) |> ignore
        fs.Write( hdrBuf1 )
        fs.Flush()

        // Update new header
        GlbFunc.WriteUInt32LE hdrBuf1 4u 0u
        GlbFunc.WriteUInt64LE hdrBuf1 8u ( header.SequenceNumber + 1UL )
        let checkSum2 = Crc32C.Compute hdrBuf1
        GlbFunc.WriteUInt32LE hdrBuf1 4u checkSum2
        fs.Seek( int64 header.Offset, SeekOrigin.Begin ) |> ignore
        fs.Write( hdrBuf1 )
        fs.Flush()

        header.SequenceNumber + 2UL

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
    static member ResolvLBA( lba : BLKCNT64_T ) ( meta : VhdxMetadata[] ) : struct( int32 * uint64 ) voption =
        let rec loop ( idx : int32 ) =
            if idx < meta.Length then
                let pbSize =
                    meta.[idx].VirtualDiskInfo.PayloadBlockSize |> uint64       // Payload Block Size
                let logiSecSize =
                    Blocksize.toUInt64 meta.[idx].VirtualDiskInfo.LogicalSectorSize // Logical Sector Size
                let chunkRatio =
                    meta.[idx].BatEntries.ChunkRatio |> uint64                  // Chunk Ratio
                let secCntInPB = pbSize / logiSecSize                           // Number of sectors in a payload block.
                let pbIdx = ( blkcnt_me.toUInt64 lba ) / secCntInPB             // Payload block index
                let secIdxInPB = ( blkcnt_me.toUInt64 lba ) % secCntInPB        // Sector index within payload block
                let sbIdx = pbIdx / chunkRatio                                  // Index of sector bitmap BAT entries
                let pbIdxInSB = pbIdx % chunkRatio                              // Index of payload blocks within a sector bitmap BAT entry
                let byteIdxInSB =
                    ( pbIdxInSB * secCntInPB / 8UL) + ( secIdxInPB / 8UL )      // Byte position within a sector bitmap BAT entry
                let bitIdx = secIdxInPB % 8UL                                   // Bit position within a byte
                let pbEntry = meta.[idx].BatEntries.Payloads.[ int32 pbIdx ]      // Payload BAT Entry

                match pbEntry.State with
                | PayloadNotPresent ->
                    // The data to be accessed resides in the parent file.
                    loop ( idx + 1 )

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
                    let sb = meta.[idx].BatEntries.SectorBitmap[ int32 sbIdx ].Bitmap
                    let bitValue = ( sb.[ int32 byteIdxInSB ] >>> ( int32 bitIdx ) ) &&& 1uy
                    if bitValue = 1uy then
                        // The data to be accessed resides in this file.
                        let posInFile = pbEntry.FileOffset + secIdxInPB * logiSecSize
                        struct( idx, posInFile ) |> ValueSome
                    else
                        // The data to be accessed resides in the parent file.
                        loop ( idx + 1 )
            else
                // No block allocation
                ValueNone
        loop 0

    /// <summary>
    ///  From the LBA, calculate the payload block BAT entry index and the sector index within the payload block.
    /// </summary>
    /// <param name="lba">
    ///  LBA used for location identification.
    /// </param>
    /// <param name="metadata">
    ///  VHDX file metadata.
    /// </param>
    /// <returns>
    ///  Pair of payload block BAT entry index and sector index within the payload block.
    /// </returns>
    static member LBAtoPayloadBlockIndex ( lba : BLKCNT64_T ) ( metadata : VhdxMetadata ) : struct( uint32 * BLKCNT32_T ) =
        let pbsize = metadata.VirtualDiskInfo.PayloadBlockSize
        let secsize = metadata.VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt32
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
    /// <param name="metadata">
    ///  VHDX file metadata.
    /// </param>
    /// <returns>
    ///  Pair of the sector bitmap BAT entry index and bit position within the payload block.
    /// </returns>
    static member LBAtoSectorBitmapIndex ( lba : BLKCNT64_T ) ( metadata : VhdxMetadata ) : struct( uint32 * uint32 ) =
        let sbindex = ( blkcnt_me.toUInt64 lba ) / 8388608UL |> uint32
        let bitpos = ( blkcnt_me.toUInt64 lba ) % 8388608UL |> uint32
        struct( sbindex, bitpos )

    /// <summary>
    /// Retrieve all metadata, including the parent VHDX file.
    /// </summary>
    /// <param name="filePath">
    ///  VHDX file name.
    /// </param>
    /// <returns>
    ///  Retrieved VHDX metadata.
    /// </returns>
    static member ReadAllMetadata( filePath : string ) : ( string * VhdxMetadata )[] =
        printfn "================================================================"
        printfn "Load all VHDX files, including the parent file."
        printfn "File name : %s" filePath

        let rec loop ( fn : string ) ( expDWG : Guid option ) ( acc : ( string * VhdxMetadata ) list ) =

            printfn "---------"
            printfn "Parent VHDX loading : %s" fn
            if expDWG.IsSome then
                printfn "Expected DataWriteGuid : %s" ( expDWG.Value.ToString "D" )

            // Read metadata
            let meta = VhdxReader.ReadVhdx fn
            let hasParent = meta.VirtualDiskInfo.HasParent
            let pl = meta.VirtualDiskInfo.ParentLocator

            // Check Data Write Guid
            if expDWG.IsSome && meta.Header.DataWriteGuid <> expDWG.Value then
                raise <| Exception "Data Write Guid does not match"

            // Check if a File Write GUID with the same one already exists.
            for ( _, itr ) in acc do
                if itr.Header.FileWriteGuid = meta.Header.FileWriteGuid then
                    raise <| Exception "The same file is specified as the parent VHDX file."

            if not hasParent then
                // If there is no parent file, add the current file to the list and finish.
                printfn "Processing terminated as there are no more parents."
                ( fn, meta ) :: acc
            else
                // The expected value of the next parent's DataWriteGuid.
                let parentDataWriteGuid = meta.VirtualDiskInfo.ParentLocator.[ "parent_linkage" ] |> Guid

                // Identify the parent file name
                let parentFileName =
                    let r1, v1 = pl.TryGetValue "relative_path"
                    let r2, v2 = pl.TryGetValue "volume_path"
                    let r3, v3 = pl.TryGetValue "absolute_win32_path"
                    if r1 then
                        Path.Combine( [| Path.GetDirectoryName fn; v1 |] )
                    elif r2 then
                        v2
                    elif r3 then
                        v3
                    else
                        raise <| Exception "Unable to identify the parent VHDX file name."

                // Read next parent VHDX file.
                loop parentFileName ( Some parentDataWriteGuid ) ( ( fn, meta ) :: acc )

        let rv =
            loop filePath None []
            |> List.rev
            |> List.toArray

        // Verify that the metadata matches.
        for i = 1 to rv.Length - 1 do
            let vdi0 = ( snd rv.[0] ).VirtualDiskInfo
            let vdix = ( snd rv.[i] ).VirtualDiskInfo
            if vdi0.VirtualDiskSize <> vdix.VirtualDiskSize then
                raise <| Exception( sprintf "The virtual disk size of the parent (%d) does not match." i )
            if vdi0.VirtualDiskId <> vdix.VirtualDiskId then
                raise <| Exception( sprintf "The virtual disk ID of the parent (%d) does not match." i )
            if vdi0.LogicalSectorSize <> vdix.LogicalSectorSize then
                raise <| Exception( sprintf "The logical sector size of the parent (%d) does not match." i )
            if vdi0.PhysicalSectorSize <> vdix.PhysicalSectorSize then
                raise <| Exception( sprintf "The physical sector size of the parent (%d) does not match." i )
        
        printfn " ReadAllMetadata success"
        printfn " Read file count : %d" rv.Length
        printfn "================================================================"

        rv

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

        let rec loop ( cnt : uint64 ) =
            if cnt < fsizemb then
                Random.Shared.NextBytes buf
                fs.Write buf
                loop ( cnt + 1UL )
            else
                ()
        loop 0UL

        fs.Flush()
        fs.Close()
        fs.Dispose()

    /// <summary>
    ///  Compare the contents of two RAW files.
    /// </summary>
    /// <param name="fname1">
    ///  RAW file 1.
    /// </param>
    /// <param name="fname2">
    ///  RAW file 2.
    /// </param>
    /// <returns>
    ///  Returns true if the contents match, or false otherwise.
    /// </returns>
    static member CompareRAW_RAW ( fname1 : string ) ( fname2 : string ) : bool =
        use fs1 = File.OpenRead fname1
        use fs2 = File.OpenRead fname2
        if fs1.Length <> fs2.Length then
            false
        else
            let buf1 = Array.zeroCreate<byte> 1048576
            let buf2 = Array.zeroCreate<byte> 1048576
            let rec loop ( pos : int64 ) =
                if pos < fs1.Length then
                    let wlen = min 1048576L ( fs1.Length - pos ) |> int
                    fs1.ReadExactly buf1
                    fs2.ReadExactly buf2
                    if buf1 <> buf2 then
                        false
                    else
                        loop ( pos + int64 wlen )
                else
                    true
            loop 0L

    /// <summary>
    ///  Compare the contents of VHDX file and RAW file.
    /// </summary>
    /// <param name="fname1">
    ///  VHDX file 1.
    /// </param>
    /// <param name="fname2">
    ///  RAW file 2.
    /// </param>
    /// <returns>
    ///  Returns true if the contents match, or false otherwise.
    /// </returns>
    static member CompareVHDX_RAW ( fname1 : string ) ( fname2 : string ) : bool =
        let vfiles, metadatas =
            VhdxHandler.ReadAllMetadata fname1
            |> Array.unzip
            |> fun ( f, m ) -> ( f |> Array.map File.OpenRead, m )
        use fs2 = File.OpenRead fname2
        try
            let sectorSize = metadatas.[0].VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt64
            let virtualDiskSize = metadatas.[0].VirtualDiskInfo.VirtualDiskSize
            let sectorCount = virtualDiskSize / sectorSize |> blkcnt_me.ofUInt64
            if fs2.Length <> int64 virtualDiskSize then
                false
            else
                let buf1 = Array.zeroCreate<byte>( int32 sectorSize )
                let buf2 = Array.zeroCreate<byte>( int32 sectorSize )

                let rec loop ( cnt : BLKCNT64_T ) =
                    if cnt < sectorCount then
                        fs2.ReadExactly buf2

                        match VhdxHandler.ResolvLBA cnt metadatas with
                        | ValueSome ( struct( fileidx, offset ) ) ->
                            vfiles.[ fileidx ].Seek( int64 offset, SeekOrigin.Begin ) |> ignore
                            vfiles.[ fileidx ].ReadExactly buf1
                        | ValueNone ->
                            Array.fill buf1 0 ( int32 sectorSize ) 0uy
                        if buf1 <> buf2 then
                            false
                        else
                            loop ( cnt + blkcnt_me.ofUInt64 1UL )
                    else
                        true
                loop blkcnt_me.zero64
        finally
            vfiles
            |> Array.iter _.Dispose()
           
    /// <summary>
    ///  Compare the contents of two VHDX files.
    /// </summary>
    /// <param name="fname1">
    ///  VHDX file 1.
    /// </param>
    /// <param name="fname2">
    ///  VHDX file 2.
    /// </param>
    /// <returns>
    ///  Returns true if the contents match, or false otherwise.
    /// </returns>
    static member CompareVHDX_VHDX ( fname1 : string ) ( fname2 : string ) : bool =
        // read metadata for fname1
        let vfiles1, metadatas1 =
            VhdxHandler.ReadAllMetadata fname1
            |> Array.unzip
            |> fun ( f, m ) -> ( f |> Array.map File.OpenRead, m )

        // read metadata for fname2
        let vfiles2, metadatas2 =
            VhdxHandler.ReadAllMetadata fname2
            |> Array.unzip
            |> fun ( f, m ) -> ( f |> Array.map File.OpenRead, m )
        try
            let sectorSize1 = metadatas1.[0].VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt64
            let virtualDiskSize1 = metadatas1.[0].VirtualDiskInfo.VirtualDiskSize
            let sectorSize2 = metadatas2.[0].VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt64
            let virtualDiskSize2 = metadatas2.[0].VirtualDiskInfo.VirtualDiskSize
            let sectorCount = virtualDiskSize1 / sectorSize1 |> blkcnt_me.ofUInt64

            if sectorSize1 <> sectorSize2 || virtualDiskSize1 <> virtualDiskSize2 then
                // sector size or disk size mismatch.
                false
            else
                let buf1 = Array.zeroCreate<byte>( int32 sectorSize1 )
                let buf2 = Array.zeroCreate<byte>( int32 sectorSize1 )

                let rec loop ( cnt : BLKCNT64_T ) =
                    if cnt < sectorCount then

                        // read file1
                        match VhdxHandler.ResolvLBA cnt metadatas1 with
                        | ValueSome ( struct( fileidx1, offset1 ) ) ->
                            vfiles1.[ fileidx1 ].Seek( int64 offset1, SeekOrigin.Begin ) |> ignore
                            vfiles1.[ fileidx1 ].ReadExactly buf1
                        | ValueNone ->
                            Array.fill buf1 0 ( int32 sectorSize1 ) 0uy

                        // read file2
                        match VhdxHandler.ResolvLBA cnt metadatas2 with
                        | ValueSome ( struct( fileidx2, offset2 ) ) ->
                            vfiles2.[ fileidx2 ].Seek( int64 offset2, SeekOrigin.Begin ) |> ignore
                            vfiles2.[ fileidx2 ].ReadExactly buf2
                        | ValueNone ->
                            Array.fill buf2 0 ( int32 sectorSize1 ) 0uy

                        if buf1 <> buf2 then
                            false
                        else
                            loop ( cnt + blkcnt_me.ofUInt64 1UL )
                    else
                        true
                loop blkcnt_me.zero64

        finally
            vfiles1
            |> Array.iter _.Dispose()
            vfiles2
            |> Array.iter _.Dispose()
