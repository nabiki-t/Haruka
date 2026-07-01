
module main

open System
open System.IO
open VhdxLibrary

let help() =
    printfn "Usage:"
    printfn "  VHDXTest read <vhdx-input> <xml-output>"
    printfn "  VHDXTest toraw <vhdx-input> <raw-output>"
    printfn "  VHDXTest tovhdx <raw-input> <vhdx-output> [--f] [--log <log-size>] [--p <payload-block-size>] [--s <sectore-size>]"
    printfn "  VHDXTest corrupt <vhdx-input> <vhdx-output> --s <sector-indices>"
    printfn "  VHDXTest snapshot <parent-vhdx-input> <vhdx-output> [--log <log-size>] [--p <payload-block-size>]"
    printfn "  VHDXTest create <vhdx-output> [--v <virtual-disk-size>] [--f] [--log <log-size>] [--p <payload-block-size>] [--s <sectore-size>]"
    printfn "  VHDXTest check <vhdx-input>"
    printfn "  VHDXTest write <raw-input> <vhdx-update> [--l <logical-block-address>] [--int <step>]"
    printfn "  VHDXTest random <raw-output> [--v <file-size>]"
    printfn "  VHDXTest compare <input1> <intput2> [--t1 <input1-type>] [--t2 <input2-type>]"
    printfn ""
    printfn "Commands:"
    printfn "  read     Analyze the VHDX file and output the metadata as XML."
    printfn "  toraw    Extract a disk image in RAW format from an existing VHDX file."
    printfn "  tovhdx   Create a VHDX file from an existing RAW image file."
    printfn "  corrupt  Update existing VHDX files and fill specified 4K sectors with random numbers."
    printfn "  snapshot Create a child VHDX file using an existing VHDX file as the parent."
    printfn "  create   Create a new VHDX file."
    printfn "  check    Replay unprocessed log."
    printfn "  write    Write raw data to VHDX file."
    printfn "  random   Create a file filled with random bytes."
    printfn "  compare  Compare the contents of two files to see if they match."
    printfn ""
    printfn "Options"
    printfn "  tovhdx :"
    printfn "  --f                         Create it as a fixed VHDX file."
    printfn "  --log <log-size>            Log area length. In MB. Specify a number between 1 and 8. Default is 1MB."
    printfn "  --p <payload-block-size>    Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2. Default is 1MB."
    printfn "  --s <sectore-size>          Sector size. Specify either 512 or 4096. Default is 512 bytes."
    printfn "  corrupt command :"
    printfn "  --s <idx1,idx2,...>         Index of 4KB sectors filled with random numbers."
    printfn "  snapshot command :"
    printfn "  --log <log-size>            Log area length. In MB. Specify a number between 1 and 8. Default is 1MB."
    printfn "  --p <payload-block-size>    Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2. Default is 1MB."
    printfn "  create command :"
    printfn "  --f                         Create it as a fixed VHDX file."
    printfn "  --v <virtual-disk-size>     Virtual disk size. In MB. Specify a number between 1 and 67,108,864. Default is 64MB."
    printfn "  --log <log-size>            Log area length. In MB. Specify a number between 1 and 8. Default is 1MB."
    printfn "  --p <payload-block-size>    Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2. Default is 1MB."
    printfn "  --s <sectore-size>          Sector size. Specify either 512 or 4096. Default is 512 bytes."
    printfn "  write command :"
    printfn "  --l <logical-block-address> The logical block address on the virtual disk where data is to be written. Default is 0."
    printfn "  --int <step>                The stage at which processing is interrupted. 1 to 8."
    printfn "  random command :"
    printfn "  --v <file-size>             File size in MB. Default is 64MB."
    printfn "  compare command :"
    printfn "  --t1 <input1-type>          Specify whether the file type of the file designated in input1 is 'raw' or 'vhdx'."
    printfn "                              If omitted, it is determined from the file name."
    printfn "  --t2 <input2-type>          Specify whether the file type of the file designated in input2 is 'raw' or 'vhdx'."
    printfn "                              If omitted, it is determined from the file name."
    printfn ""

type ToVHDXOptions =
    {
        Fixed   : bool
        LogSize : uint32
        Payload : uint32
        SectorSize : Blocksize
    }

type CreateOptions =
    {
        Fixed   : bool
        VirtualSize : uint64
        LogSize : uint32
        Payload : uint32
        SectorSize : Blocksize
    }

type FileType =
    | FT_RAW
    | FT_VHDX

type Command =
    | Read of vhdx:string * xml:string
    | ToRaw of vhdx:string * raw:string
    | ToVHDX of raw:string * vhdx:string * opts:ToVHDXOptions
    | Corrupt of input:string * output:string * sectors:SEC4K_T list
    | Snapshot of parent:string * output:string * LogSize:uint32 * Payload:uint32
    | Create of output:string * opts:CreateOptions
    | Check of input:string
    | Write of raw:string * vhdx:string * lba:BLKCNT64_T * ex:int
    | Random of raw:string * fsize:uint64
    | Compare of file1:string * f1type:FileType * file2:string * f2type:FileType

let parseArgs ( argv : string[] ) : Command =
    if argv.Length = 0 then
        failwith "No command has been specified."

    let cmd = argv.[0]
    let rest = argv.[ 1 .. ]

    let rec parseOptions ( map : Map< string, string option > ) ( args : string list ) =
        match args with
        | [] -> map
        | "--s" :: v :: tail ->
            parseOptions ( map.Add( "s", Some v ) ) tail
        | "--x" :: v :: tail ->
            parseOptions ( map.Add( "x", Some v ) ) tail
        | "--log" :: v :: tail ->
            parseOptions ( map.Add( "log", Some v ) ) tail
        | "--p" :: v :: tail ->
            parseOptions ( map.Add( "p", Some v ) ) tail
        | "--v" :: v :: tail ->
            parseOptions ( map.Add( "v", Some v ) ) tail
        | "--l" :: v :: tail ->
            parseOptions ( map.Add( "l", Some v ) ) tail
        | "--int" :: v :: tail ->
            parseOptions ( map.Add( "int", Some v ) ) tail
        | "--f" :: tail ->
            parseOptions ( map.Add( "f", None ) ) tail
        | "--s" :: tail ->
            failwith "--s value is missing."
        | "--x" :: tail ->
            failwith "--x value is missing."
        | "--log" :: tail ->
            failwith "--log value is missing."
        | "--p" :: tail ->
            failwith "--p value is missing."
        | "--v" :: tail ->
            failwith "--v value is missing."
        | "--l" :: tail ->
            failwith "--l value is missing."
        | "--int" :: tail ->
            failwith "--int value is missing."
        | x :: _ ->
            failwithf "Unknown option : %s" x

    match cmd.ToUpper() with
    | "READ" ->
        if rest.Length <> 2 then
            failwith "The argument for the read command is invalid."
        Command.Read( rest.[0], rest.[1] )

    | "TORAW" ->
        if rest.Length <> 2 then
            failwith "The argument for the toraw command is invalid."
        Command.ToRaw( rest.[0], rest.[1] )

    | "TOVHDX" ->
        if rest.Length <> 2 then
            failwith "The argument for the tovhdx command is invalid."
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty
        let options = {
            Fixed =
                opts.ContainsKey "f";
            LogSize =
                opts.TryFind "log"
                |> Option.bind id
                |> Option.map uint32
                |> Option.defaultValue 1u;
            Payload =
                opts.TryFind "p"
                |> Option.bind id
                |> Option.map uint32
                |> Option.defaultValue 1u;
            SectorSize =
                opts.TryFind "s"
                |> Option.bind id
                |> Option.map uint32
                |> Option.defaultValue 512u
                |> ( fun v -> 
                    if v = 512u then
                        BS_512
                    elif v = 4096u then
                        BS_4096
                    else
                        failwith "Sector size must be 512 or 4096."
                );
        }
        Command.ToVHDX( rest.[0], rest.[1], options )

    | "CORRUPT" ->
        if rest.Length < 2 then
            failwith "The argument for the corrupt command is missing."
        let input = rest.[0]
        let output = rest.[1]
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty

        let sectors =
            match opts.TryFind "s" with
            | Some ( Some v ) ->
                v.Split(',')
                |> Array.map ( fun s -> uint64 s |> sec4k_me.ofUInt64 )
                |> Array.toList
            | _ ->
                failwith "--s <idx1,idx2,...> must be specified."

        Command.Corrupt( input, output, sectors )

    | "SNAPSHOT" ->
        if rest.Length < 2 then
            failwith "The argument for the snapshot command is missing."
        let parent = rest.[0]
        let output = rest.[1]
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty

        let logSize =
            opts.TryFind "log"
            |> Option.bind id
            |> Option.map uint32
            |> Option.defaultValue 1u
        let payload =
            opts.TryFind "p"
            |> Option.bind id
            |> Option.map uint32
            |> Option.defaultValue 1u

        Command.Snapshot( parent, output, logSize, payload )

    | "CREATE" ->
        if rest.Length < 1 then
            failwith "The argument for the create command is missing."
        let opts =
            rest
            |> List.ofArray
            |> List.skip 1
            |> parseOptions Map.empty
        let options = {
            Fixed =
                opts.ContainsKey "f";
            VirtualSize =
                opts.TryFind "v"
                |> Option.bind id
                |> Option.map uint64
                |> Option.defaultValue 64UL;
            LogSize =
                opts.TryFind "log"
                |> Option.bind id
                |> Option.map uint32
                |> Option.defaultValue 1u;
            Payload =
                opts.TryFind "p"
                |> Option.bind id
                |> Option.map uint32
                |> Option.defaultValue 1u;
            SectorSize =
                opts.TryFind "s"
                |> Option.bind id
                |> Option.map uint32
                |> Option.defaultValue 512u
                |> ( fun v -> 
                    if v = 512u then
                        BS_512
                    elif v = 4096u then
                        BS_4096
                    else
                        failwith "Sector size must be 512 or 4096."
                );
        }
        Command.Create( rest.[0], options )

    | "CHECK" ->
        if rest.Length <> 1 then
            failwith "The argument for the check command is invalid."
        Command.Check( rest.[0] )

    | "WRITE" ->
        if rest.Length < 2 then
            failwith "The argument for the write command is invalid."
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty

        let lba =
            opts.TryFind "l"
            |> Option.bind id
            |> Option.map uint64
            |> Option.defaultValue 0UL
            |> blkcnt_me.ofUInt64
        let exint =
            opts.TryFind "int"
            |> Option.bind id
            |> Option.map int
            |> Option.defaultValue 0

        Command.Write( rest.[1], rest.[0], lba, exint )

    | "RANDOM" ->
        if rest.Length < 1 then
            failwith "The argument for the random command is invalid."
        let opts =
            rest
            |> List.ofArray
            |> List.skip 1
            |> parseOptions Map.empty

        let fsizemb =
            opts.TryFind "v"
            |> Option.bind id
            |> Option.map uint64
            |> Option.defaultValue 64UL

        Command.Random( rest.[0], fsizemb )

    | "COMPARE" ->
        if rest.Length < 2 then
            failwith "The argument for the random compare is invalid."
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty

        let ft1 =
            match opts.TryFind "t1" with
            | Some ( Some v ) ->
                match v.ToUpperInvariant() with
                | "RAW" -> FileType.FT_RAW
                | "VHDX" -> FileType.FT_VHDX
                | _ ->
                    failwith ( sprintf "--t1 %s is unknown." v )
            | _ ->
                if rest.[0].ToUpperInvariant().EndsWith( ".VHDX" ) then
                    FileType.FT_VHDX
                else
                    FileType.FT_RAW
        let ft2 =
            match opts.TryFind "t2" with
            | Some ( Some v ) ->
                match v.ToUpperInvariant() with
                | "RAW" -> FileType.FT_RAW
                | "VHDX" -> FileType.FT_VHDX
                | _ ->
                    failwith ( sprintf "--t2 %s is unknown." v )
            | _ ->
                if rest.[1].ToUpperInvariant().EndsWith( ".VHDX" ) then
                    FileType.FT_VHDX
                else
                    FileType.FT_RAW

        Command.Compare( rest.[0], ft1, rest.[1], ft2 )

    | _ ->
        failwithf "Unknown command : %s" cmd

[< EntryPoint >]
let main ( argv : string[] ) : int =
    let cmd =
        try
            parseArgs argv
        with
        | _ as x ->
            printfn "%s" x.Message
            help()
            exit 1
            reraise()

    match cmd with
    | Read( infile, outfile ) ->
        let metadata = VhdxReader.ReadVhdx infile
        VhdxXmlSerializer.SerializeToFile metadata outfile

    | ToRaw( infile, outfile ) ->
        VhdxToRaw.Convert infile outfile

    | ToVHDX( rawfile, outfile, opt ) ->
        VhdxCreator.RawToVHDX rawfile outfile ( opt.LogSize * 1048576u ) ( opt.Payload * 1048576u ) opt.Fixed opt.SectorSize

    | Corrupt( infile, outfile, sectors ) ->
        VhdxCorrupter.Inject infile outfile sectors

    | Snapshot( parent, outfile, logSize, payload ) ->
        VhdxCreator.Create parent outfile ( logSize * 1048576u ) ( payload * 1048576u ) false 0UL Blocksize.BS_512

    | Create( outfile, opt ) ->
        VhdxCreator.Create "" outfile ( opt.LogSize * 1048576u ) ( opt.Payload * 1048576u ) opt.Fixed ( opt.VirtualSize * 1048576UL ) opt.SectorSize

    | Check( infile ) ->
        VhdxChecker.Check infile

    | Write( rawfile, vhdxfile, lba, ex ) ->
        VhdxWriter.Write rawfile vhdxfile lba ex

    | Random( rawfile, fsizemb ) ->
        VhdxHandler.CreateRandomFile rawfile fsizemb

    | Compare( file1, f1type, file2, f2type ) ->
        let r =
            match f1type, f2type with
            | ( FT_RAW, FT_RAW ) ->
                VhdxHandler.CompareRAW_RAW file1 file2
            | ( FT_VHDX, FT_RAW ) ->
                VhdxHandler.CompareVHDX_RAW file1 file2
            | ( FT_RAW, FT_VHDX ) ->
                VhdxHandler.CompareVHDX_RAW file2 file1
            | ( FT_VHDX, FT_VHDX ) ->
                VhdxHandler.CompareVHDX_VHDX file1 file2
        if r then
            printfn "The file contents match."
        else
            printfn "The file contents do not match."

    0
