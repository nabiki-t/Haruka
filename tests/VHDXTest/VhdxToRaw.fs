namespace VhdxLibrary

open System
open System.IO
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons


/// Convert VHDX file to raw file.
type VhdxToRaw() =

    /// <summary>
    ///  Convert VHDX file to raw file.
    /// </summary>
    /// <param name="inputPath">
    ///  Input VHDX file path name.
    /// </param>
    /// <param name="outputPath">
    ///  Output RAW file path name.
    /// </param>
    static member Convert ( fa : FileAccessor ) ( outputPath : string ) : Task =
        task {
            printfn "========================================================"
            printfn "Convert to RAW format."
            printfn "Input file : %s" fa.FileName
            printfn "Output file : %s" outputPath

            // Read metadata and open files.
            let! allMetadatas = VhdxHandler.ReadAllMetadata fa
            let vFiles, vMD = allMetadatas |> Array.unzip
            if vFiles.Length <= 0 then
                raise <| Exception "Missing input files."

            File.Delete outputPath
            use outfile = new FileStream( outputPath, FileMode.Create, FileAccess.Write, FileShare.None )

            let zeroBuffer = Array.zeroCreate<byte>( int32 vMD.[0].VirtualDiskInfo.PayloadBlockSize )
            let readPBBuf = Array.zeroCreate<byte>( int32 vMD.[0].VirtualDiskInfo.PayloadBlockSize )
            let readSecBuf = Array.zeroCreate<byte>( vMD.[0].VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt32 |> int32 )

            // Calculate number of sectors in a payload block.
            let secCntInPB =
                vMD.[0].VirtualDiskInfo.PayloadBlockSize / ( vMD.[0].VirtualDiskInfo.LogicalSectorSize |> Blocksize.toUInt32 ) |> int32

            for pbIdx = 0 to vMD.[0].BatEntries.Payloads.Length - 1 do
                let pbItr = vMD.[0].BatEntries.Payloads.[ pbIdx ]
                match pbItr.State with
                | PayloadUndefined
                | PayloadZero
                | PayloadUnapped ->
                    // Assume that all values ​​are 0.
                    printfn "Payload block %d : All zeros" pbIdx
                    outfile.Write( zeroBuffer )

                | PayloadFullyPresent ->
                    // All data is recorded in the input file.
                    printfn "Payload block %d : Recorded in the input file" pbIdx
                    do! vFiles.[0].ReadWithPseudoLimit vMD.[0].LastFileSize pbItr.FileOffset ( ArraySegment readPBBuf )
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
                        outfile.Write( readSecBuf )

            vFiles |> Array.iter ( fun itr -> itr.Close() )
            outfile.Flush()
            outfile.Close()
        }

