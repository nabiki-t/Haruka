namespace VhdxLibrary

open System
open System.Text
open System.IO
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons


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
            let! allStructures = VhdxHandler.ReadAllStructures fa
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
                        match VhdxHandler.ResolvLBA lba vMD with
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

