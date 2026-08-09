//=============================================================================
// Haruka Software Storage.
// VhdxToRaw.fs : Implement a function to extract the contents of a VHDX file as raw data.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.Text
open System.IO
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// Convert VHDX file to raw file.
type VhdxToRaw() =

    static member OutputByHexdump ( outfile : FileStream ) ( v : byte[] ) ( digits : uint64 ) ( n : uint64 ) : unit =
        let sb = StringBuilder()
        for i = 0 to v.Length - 1 do
            let p = uint64 i + n
            let f = p % digits
            if f = 0UL then
                sb.AppendFormat( "{0:X16}  {1:X2} ", p, v.[i] ) |> ignore
            elif f = digits - 1UL then
                sb.AppendFormat( "{0:X2}{1}", v.[i], Environment.NewLine ) |> ignore
            else
                sb.AppendFormat( "{0:X2} ", v.[i] ) |> ignore
        let b = System.Text.Encoding.UTF8.GetBytes( sb.ToString() )
        outfile.Write b

    /// <summary>
    ///  Convert VHDX file to raw file.
    /// </summary>
    /// <param name="inputPath">
    ///  Input VHDX file path name.
    /// </param>
    /// <param name="outputPath">
    ///  Output RAW file path name.
    /// </param>
    /// <param name="outputPath">
    ///  When outputting as a hexadecimal dump, specify the number of digits..
    /// </param>
    static member Convert ( fa : FileAccessor ) ( outputPath : string ) ( hexdump : uint64 option ) : Task =
        task {
            printfn "========================================================"
            printfn "Convert to RAW format."
            printfn "Input file : %s" fa.FileName
            printfn "Output file : %s" outputPath
            printfn "Output digits : %d" ( if hexdump.IsSome then hexdump.Value else 0UL )

            // Read VHDX file structures and open files.
            let! allStructures = VhdxReader.ReadAllStructures fa
            let vFiles, vMD = allStructures |> Array.unzip
            if vFiles.Length <= 0 then
                raise <| Exception "Missing input files."

            File.Delete outputPath
            use outfile = new FileStream( outputPath, FileMode.Create, FileAccess.Write, FileShare.None )

            let cidx = vFiles.Length - 1
            let curstr = vMD.[cidx]
            let hasParent = curstr.VDI.HasParent
            let pbBlockSize = curstr.VDI.PayloadBlockSize
            let blockSize = curstr.VDI.LogicalSectorSize |> Blocksize.toUInt32
            let zeroBuffer = Array.zeroCreate<byte>( int32 pbBlockSize )
            let readPBBuf = Array.zeroCreate<byte>( int32 pbBlockSize )
            let readSecBuf = Array.zeroCreate<byte>( blockSize |> int32 )

            // Calculate number of sectors in a payload block.
            let secCntInPB = pbBlockSize / ( blockSize ) |> int32

            for pbIdx = 0 to curstr.BAT.Payloads.Length - 1 do
                let pbItr = curstr.BAT.Payloads.[ pbIdx ]
                match pbItr.State with
                | PayloadUndefined
                | PayloadZero
                | PayloadUnapped ->
                    // Assume that all values ​​are 0.
                    printfn "Payload block %d : All zeros" pbIdx
                    match hexdump with
                    | Some x ->
                        VhdxToRaw.OutputByHexdump outfile zeroBuffer x ( uint64 pbIdx * uint64 pbBlockSize )
                    | None ->
                        outfile.Write( zeroBuffer )

                | PayloadFullyPresent ->
                    // All data is recorded in the input file.
                    printfn "Payload block %d : Recorded in the input file" pbIdx
                    do! vFiles.[cidx].ReadWithPseudoLimit curstr.LastFileSize pbItr.FileOffset ( ArraySegment readPBBuf )
                    match hexdump with
                    | Some x ->
                        VhdxToRaw.OutputByHexdump outfile readPBBuf x ( uint64 pbIdx * uint64 pbBlockSize )
                    | None ->
                        outfile.Write( readPBBuf )

                | PayloadNotPresent
                | PayloadPartiallyPresent ->
                    // The sector bitmap needs to be inspected.
                    printfn "Payload block %d : Copy sector by sector" pbIdx
                    for secIdxInPB = 0 to secCntInPB - 1 do
                        let lba = uint64 ( pbIdx * secCntInPB + secIdxInPB ) |> blkcnt_me.ofUInt64
                        match VhdxCommons.ResolvLBA lba vMD with
                        | ValueSome( struct( fsidx2, fpos ) ) ->
                            do! vFiles.[fsidx2].ReadWithPseudoLimit vMD.[fsidx2].LastFileSize fpos ( ArraySegment readSecBuf )
                        | _ ->
                            Array.fill readSecBuf 0 readSecBuf.Length 0uy
                        match hexdump with
                        | Some x ->
                            VhdxToRaw.OutputByHexdump outfile readSecBuf x ( uint64 lba * uint64 blockSize )
                        | None ->
                            outfile.Write( readSecBuf )

            vFiles |> Array.iter ( fun itr -> itr.Close() )
            outfile.Flush()
            outfile.Close()
        }

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
    /// <param name="fa">
    ///  VHDX file 1.
    /// </param>
    /// <param name="fname2">
    ///  RAW file 2.
    /// </param>
    /// <returns>
    ///  Returns true if the contents match, or false otherwise.
    /// </returns>
    static member CompareVHDX_RAW ( fa : FileAccessor ) ( fname2 : string ) : Task<bool> =
        task {
            let! allVHDXMetadata = VhdxReader.ReadAllStructures fa
            let vfiles, metadatas =
                allVHDXMetadata
                |> Array.unzip

            use fs2 = File.OpenRead fname2
            try
                let sectorSize = metadatas.[0].VDI.LogicalSectorSize |> Blocksize.toUInt64
                let virtualDiskSize = metadatas.[0].VDI.VirtualDiskSize
                let sectorCount = virtualDiskSize / sectorSize |> blkcnt_me.ofUInt64
                if fs2.Length <> int64 virtualDiskSize then
                    return false
                else
                    let buf1 = Array.zeroCreate<byte>( int32 sectorSize )
                    let buf2 = Array.zeroCreate<byte>( int32 sectorSize )

                    let loop ( cnt : BLKCNT64_T ) : Task<struct( bool * BLKCNT64_T )> =
                        task {
                            if cnt < sectorCount then
                                fs2.ReadExactly buf2
                                match VhdxCommons.ResolvLBA cnt metadatas with
                                | ValueSome ( struct( fileidx, offset ) ) ->
                                    do! vfiles.[ fileidx ].ReadWithPseudoLimit metadatas.[ fileidx ].LastFileSize offset ( ArraySegment buf1 )
                                | ValueNone ->
                                    Array.fill buf1 0 ( int32 sectorSize ) 0uy
                                if buf1 <> buf2 then
                                    return struct( false, cnt )
                                else
                                    return struct( true, cnt + blkcnt_me.ofUInt64 1UL )
                            else
                                return struct( false, cnt )
                        }
                    let! r = Functions.loopAsyncWithState loop blkcnt_me.zero64
                    return ( r = sectorCount )
            finally
                vfiles
                |> Array.iter _.Close()
        }
           
    /// <summary>
    ///  Compare the contents of two VHDX files.
    /// </summary>
    /// <param name="fa1">
    ///  VHDX file 1.
    /// </param>
    /// <param name="fa2">
    ///  VHDX file 2.
    /// </param>
    /// <returns>
    ///  Returns true if the contents match, or false otherwise.
    /// </returns>
    static member CompareVHDX_VHDX ( fa1 : FileAccessor ) ( fa2 : FileAccessor ) : Task<bool> =
        task {
            // read metadata for fname1
            let! allVHDXMetadata1 = VhdxReader.ReadAllStructures fa1
            let vfiles1, metadatas1 = allVHDXMetadata1 |> Array.unzip

            // read metadata for fname2
            let! allVHDXMetadata2 = VhdxReader.ReadAllStructures fa2
            let vfiles2, metadatas2 = allVHDXMetadata2 |> Array.unzip

            try
                let sectorSize1 = metadatas1.[0].VDI.LogicalSectorSize |> Blocksize.toUInt64
                let virtualDiskSize1 = metadatas1.[0].VDI.VirtualDiskSize
                let sectorSize2 = metadatas2.[0].VDI.LogicalSectorSize |> Blocksize.toUInt64
                let virtualDiskSize2 = metadatas2.[0].VDI.VirtualDiskSize
                let sectorCount = virtualDiskSize1 / sectorSize1 |> blkcnt_me.ofUInt64

                if sectorSize1 <> sectorSize2 || virtualDiskSize1 <> virtualDiskSize2 then
                    // sector size or disk size mismatch.
                    return false
                else
                    let buf1 = Array.zeroCreate<byte>( int32 sectorSize1 )
                    let buf2 = Array.zeroCreate<byte>( int32 sectorSize1 )

                    let loop ( cnt : BLKCNT64_T ) : Task<struct( bool * BLKCNT64_T )> =
                        task {
                            if cnt < sectorCount then

                                // read file1
                                match VhdxCommons.ResolvLBA cnt metadatas1 with
                                | ValueSome ( struct( fileidx1, offset1 ) ) ->
                                    do! vfiles1.[ fileidx1 ].ReadWithPseudoLimit metadatas1.[ fileidx1 ].LastFileSize offset1 ( ArraySegment buf1 )
                                | ValueNone ->
                                    Array.fill buf1 0 ( int32 sectorSize1 ) 0uy

                                // read file2
                                match VhdxCommons.ResolvLBA cnt metadatas2 with
                                | ValueSome ( struct( fileidx2, offset2 ) ) ->
                                    do! vfiles2.[ fileidx2 ].ReadWithPseudoLimit metadatas1.[ fileidx2 ].LastFileSize offset2 ( ArraySegment buf1 )
                                | ValueNone ->
                                    Array.fill buf2 0 ( int32 sectorSize1 ) 0uy

                                if buf1 <> buf2 then
                                    return struct( false, cnt )
                                else
                                    return struct( true, cnt + blkcnt_me.ofUInt64 1UL )
                            else
                                return struct( false, cnt )
                        }
                    let! r = Functions.loopAsyncWithState loop blkcnt_me.zero64
                    return ( r = sectorCount )

            finally
                vfiles1
                |> Array.iter _.Close()
                vfiles2
                |> Array.iter _.Close()
        }
