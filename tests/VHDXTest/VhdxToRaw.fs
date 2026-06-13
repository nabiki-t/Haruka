namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary
open System.Text

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
    static member Convert ( inputPath : string ) ( outputPath : string ) : unit =

        printfn "========================================================"
        printfn "Convert to RAW format."
        printfn "Input file : %s" inputPath
        printfn "Output file : %s" outputPath

        // Read metadata and open files.
        let vFiles, vMD =
            let metadata = VhdxReader.ReadAllMetadata inputPath
            let v1 =
                metadata
                |> Array.map ( fun ( itr, _ ) ->
                    new FileStream( itr, FileMode.Open, FileAccess.Read, FileShare.None )
                )
            let v2 = metadata |> Array.map snd
            ( v1, v2 )
        if vFiles.Length <= 0 then
            raise <| Exception "Missing input files."

        File.Delete outputPath
        use outfile = new FileStream( outputPath, FileMode.Create, FileAccess.Write, FileShare.None )

        let zeroBuffer = Array.zeroCreate<byte>( int vMD.[0].VirtualDiskInfo.PayloadBlockSize )
        let readPBBuf = Array.zeroCreate<byte>( int vMD.[0].VirtualDiskInfo.PayloadBlockSize )
        let readSecBuf = Array.zeroCreate<byte>( int vMD.[0].VirtualDiskInfo.LogicalSectorSize )

        // Calculate number of sectors in a payload block.
        let secCntInPB =
            int vMD.[0].VirtualDiskInfo.PayloadBlockSize / int vMD.[0].VirtualDiskInfo.LogicalSectorSize

        vMD.[0].BatEntries.Payloads
        |> Array.iteri ( fun pbIdx pbItr ->
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
                vFiles.[0].Seek( int64 pbItr.FileOffset, SeekOrigin.Begin ) |> ignore
                vFiles.[0].Read( readPBBuf ) |> ignore
                outfile.Write( readPBBuf )

            | PayloadNotPresent
            | PayloadPartiallyPresent ->
                // The sector bitmap needs to be inspected.
                printfn "Payload block %d : Copy sector by sector" pbIdx
                for secIdxInPB = 0 to secCntInPB - 1 do
                    let lba = uint64 ( pbIdx * secCntInPB + secIdxInPB )
                    let struct( fsidx2, fpos ) = VhdxReader.ResolvLBA lba vMD
                    if fpos.IsSome then
                        vFiles.[fsidx2].Seek( int64 fpos.Value, SeekOrigin.Begin ) |> ignore
                        vFiles.[fsidx2].Read( readSecBuf ) |> ignore
                    else
                        Array.fill readSecBuf 0 readSecBuf.Length 0uy
                    outfile.Write( readSecBuf )
        )

        vFiles |> Array.iter ( fun itr -> itr.Close() )
        outfile.Flush()
        outfile.Close()
