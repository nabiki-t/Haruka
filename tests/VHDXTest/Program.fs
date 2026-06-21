
module main

open System
open System.IO
open VhdxLibrary

let help() =
    printfn "Usage:"
    printfn "  VHDXTest read <vhdx-file> <xml-output>"
    printfn "  VHDXTest toraw <vhdx-file> <raw-output>"
    printfn "  VHDXTest tovhdx <raw-file> <vhdx-output> --f [--log <log-size>] [--p <payload-block-size>] [--s <sectore-size>]"
    printfn "  VHDXTest write <vhdx-file> <raw-file> --l <lba>"
    printfn "  VHDXTest corrupt <vhdx-input> <vhdx-output> --s <sector-indices> [--x <xml-output>]"
    printfn "  VHDXTest snapshot <parent-file> <vhdx-output> [--log <log-size>] [--p <payload-block-size>]"
    printfn "  VHDXTest create <vhdx-output> --f --v <virtual-disk-size> [--log <log-size>] [--p <payload-block-size>] [--s <sectore-size>]"
    printfn "  VHDXTest check <vhdx-input>"
    printfn ""
    printfn "Commands:"
    printfn "  read     Analyze the VHDX file and output the metadata as XML."
    printfn "  toraw    Extract a disk image in RAW format from an existing VHDX file."
    printfn "  tovhdx   Create a VHDX file from an existing RAW image file."
    printfn "  write    Write RAW data to a specified LBA position in a VHDX file."
    printfn "  corrupt  Update existing VHDX files and fill specified 4K sectors with random numbers."
    printfn "  snapshot Create a child VHDX file using an existing VHDX file as the parent."
    printfn "  create   Create a new VHDX file."
    printfn "  check    Replay unprocessed log."
    printfn ""
    printfn "Options"
    printfn "  tovhdx :"
    printfn "  --f                      Create it as a fixed VHDX file."
    printfn "  --log <log-size>         Log area length. In MB. Specify a number between 1 and 8."
    printfn "  --p <payload-block-size> Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2."
    printfn "  --s <sectore-size>       Sector size. Specify either 512 or 4096."
    printfn "  corrupt command :"
    printfn "  --s <idx1,idx2,...>      Index of 4KB sectors filled with random numbers."
    printfn "  --x <file>               Output updated metadata as XML."
    printfn "  snapshot command :"
    printfn "  --log <log-size>         Log area length. In MB. Specify a number between 1 and 8."
    printfn "  --p <payload-block-size> Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2."
    printfn "  create command :"
    printfn "  --f                      Create it as a fixed VHDX file."
    printfn "  --v <virtual-disk-size>  Virtual disk size. In MB. Specify a number between 1 and 67,108,864."
    printfn "  --log <log-size>         Log area length. In MB. Specify a number between 1 and 8."
    printfn "  --p <payload-block-size> Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2."
    printfn "  --s <sectore-size>       Sector size. Specify either 512 or 4096."
    printfn ""
    printfn "Examples:"
    printfn "  VHDXTest read disk.vhdx output.xml"
    printfn "  VHDXTest corrupt disk.vhdx output.vhdx --sectors 0,1,2"
    printfn "  VHDXTest corrupt disk.vhdx output.vhdx --sectors 8,16,24 --xml updated.xml"

type ToVHDXOptions =
    {
        Fixed   : bool
        LogSize : uint32 option
        Payload : uint32 option
        SectorSize : uint32 option
    }

type CorruptOptions =
    {
        Sectors : int64 list
        XmlOut  : string option
    }

type SnapshotOptions =
    {
        LogSize : uint32 option
        Payload : uint32 option
    }

type CreateOptions =
    {
        Fixed   : bool
        VirtualSize : uint64
        LogSize : uint32 option
        Payload : uint32 option
        SectorSize : uint32 option
    }

type Command =
    | Read of vhdx:string * xml:string
    | ToRaw of vhdx:string * raw:string
    | ToVHDX of raw:string * vhdx:string * opts:ToVHDXOptions
    | Write of vhdx:string * raw:string * lba:uint64
    | Corrupt of input:string * output:string * opts:CorruptOptions
    | Snapshot of parent:string * output:string * opts:SnapshotOptions
    | Create of output:string * opts:CreateOptions
    | Check of input:string

let parseArgs ( argv: string[] ) : Command =
    if argv.Length = 0 then
        failwith "No command has been specified."

    let cmd = argv.[0]
    let rest = argv.[ 1 .. ]

    let rec parseOptions ( map : Map< string, string option > ) ( args : string list ) =
        match args with
        | [] -> map
        | "--l" :: v :: tail ->
            parseOptions ( map.Add( "l", Some v ) ) tail
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
        | "--f" :: tail ->
            parseOptions ( map.Add( "f", None ) ) tail
        | "--l" :: tail ->
            failwith "--l value is missing."
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
        | x :: _ ->
            failwithf "Unknown option : %s" x

    match cmd with
    | "read" ->
        if rest.Length <> 2 then
            failwith "The argument for the read command is invalid."
        Command.Read( rest.[0], rest.[1] )

    | "toraw" ->
        if rest.Length <> 2 then
            failwith "The argument for the toraw command is invalid."
        Command.ToRaw( rest.[0], rest.[1] )

    | "tovhdx" ->
        if rest.Length <> 2 then
            failwith "The argument for the tovhdx command is invalid."
        let raw = rest.[0]
        let vhdx = rest.[1]
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty

        let isFixed = opts.ContainsKey "f"

        let logSize =
            opts.TryFind "log"
            |> Option.bind id
            |> Option.map uint32
        let payload =
            opts.TryFind "p"
            |> Option.bind id
            |> Option.map uint32
        let sectorSize =
            opts.TryFind "s"
            |> Option.bind id
            |> Option.map uint32

        let options = {
            Fixed = isFixed;
            LogSize = logSize;
            Payload = payload;
            SectorSize = sectorSize;
        }
        Command.ToVHDX( raw, vhdx, options )

    | "write" ->
        if rest.Length < 2 then
            failwith "The argument for the write command is missing."
        let vhdxFile = rest.[0]
        let rawFile = rest.[1]
        let opts =
            rest
            |> List.ofArray
            |> List.skip 2
            |> parseOptions Map.empty

        let lba =
            match opts.TryFind "l" with
            | Some ( Some v ) ->
                uint64 v
            | _ ->
                failwith "--l <lba> must be specified."

        Command.Write( vhdxFile, rawFile, lba )

    | "corrupt" ->
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
                |> Array.map int64
                |> Array.toList
            | _ ->
                failwith "--s <idx1,idx2,...> must be specified."

        let xmlOut =
            opts.TryFind "x"
            |> Option.bind id

        Command.Corrupt( input, output, { Sectors = sectors; XmlOut = xmlOut } )

    | "snapshot" ->
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
        let payload =
            opts.TryFind "p"
            |> Option.bind id
            |> Option.map uint32

        Command.Snapshot( parent, output, { LogSize = logSize; Payload = payload } )

    | "create" ->
        if rest.Length < 1 then
            failwith "The argument for the create command is missing."
        let output = rest.[0]
        let opts =
            rest
            |> List.ofArray
            |> List.skip 1
            |> parseOptions Map.empty

        let isFixed = opts.ContainsKey "f"

        let virtualSize =
            match opts.TryFind "v" with
            | Some ( Some v ) ->
                uint64 v
            | _ ->
                failwith "--v <virtual-disk-size> must be specified."

        let logSize =
            opts.TryFind "log"
            |> Option.bind id
            |> Option.map uint32
        let payload =
            opts.TryFind "p"
            |> Option.bind id
            |> Option.map uint32
        let sectorSize =
            opts.TryFind "s"
            |> Option.bind id
            |> Option.map uint32

        let options = {
            Fixed = isFixed;
            VirtualSize = virtualSize;
            LogSize = logSize;
            Payload = payload;
            SectorSize = sectorSize;
        }
        Command.Create( output, options )

    | "check" ->
        if rest.Length <> 1 then
            failwith "The argument for the check command is invalid."
        Command.Check( rest.[0] )

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
        let logSize = ( Option.defaultValue 1u opt.LogSize ) * 1024u * 1024u
        let payloadBlockSize = ( Option.defaultValue 1u opt.Payload ) * 1024u * 1024u
        let sectorekSize = ( Option.defaultValue 512u opt.SectorSize )
        VhdxCreator.RawToVHDX rawfile outfile logSize payloadBlockSize opt.Fixed sectorekSize

    | Write( vhdxFile, rawFile, lba ) ->
        VhdxWriter.Write vhdxFile rawFile lba

    | Corrupt( infile, outfile, opt ) ->
        let metadata = VhdxReader.ReadVhdx infile
        VhdxCorrupter.Inject metadata infile outfile opt.Sectors
        if opt.XmlOut.IsSome then
            VhdxXmlSerializer.SerializeToFile metadata opt.XmlOut.Value

    | Snapshot( parent, outfile, opt ) ->
        let logSize = ( Option.defaultValue 1u opt.LogSize ) * 1024u * 1024u
        let payloadBlockSize = ( Option.defaultValue 1u opt.Payload ) * 1024u * 1024u
        VhdxCreator.Create parent outfile logSize payloadBlockSize false 0UL 0u

    | Create( outfile, opt ) ->
        let logSize = ( Option.defaultValue 1u opt.LogSize ) * 1024u * 1024u
        let payloadBlockSize = ( Option.defaultValue 1u opt.Payload ) * 1024u * 1024u
        let vdiskSize = opt.VirtualSize * 1024UL * 1024UL
        let sectorekSize = ( Option.defaultValue 512u opt.SectorSize )
        VhdxCreator.Create "" outfile logSize payloadBlockSize opt.Fixed vdiskSize sectorekSize

    | Check( infile ) ->
        VhdxChecker.Check infile

    0
