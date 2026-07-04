module main

open System
open System.Text.RegularExpressions
open VhdxLibrary

open Haruka.Constants
open Haruka.Commons

type VHDXUtilCmdType =
    | Read
    | ToRAW
    | ToVHDX
    | Corrupt
    | Snapshot
    | Create
    | Check
    | Write
    | Random
    | Compare

type CmdArgs() =

    /// display help string
    static let help() =
        printfn "Usage:"
        printfn "  VHDXTest read <vhdx-input> <xml-output>"
        printfn "  VHDXTest toraw <vhdx-input> <raw-output>"
        printfn "  VHDXTest tovhdx <raw-input> <vhdx-output> [/f] [/log <log-size>] [/p <payload-block-size>] [/s <sectore-size>]"
        printfn "  VHDXTest corrupt <vhdx-input> <vhdx-output> /s <sector-indices>"
        printfn "  VHDXTest snapshot <parent-vhdx-input> <vhdx-output> [/log <log-size>] [/p <payload-block-size>]"
        printfn "  VHDXTest create <vhdx-output> [/v <virtual-disk-size>] [/f] [/log <log-size>] [/p <payload-block-size>] [/s <sectore-size>]"
        printfn "  VHDXTest check <vhdx-input>"
        printfn "  VHDXTest write <raw-input> <vhdx-update> [/l <logical-block-address>] [/int <step>]"
        printfn "  VHDXTest random <raw-output> [/v <file-size>]"
        printfn "  VHDXTest compare <input1> <intput2> [/t1 <input1-type>] [/t2 <input2-type>]"
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
        printfn "   /f                         Create it as a fixed VHDX file."
        printfn "   /log <log-size>            Log area length. In MB. Specify a number between 1 and 8. Default is 1MB."
        printfn "   /p <payload-block-size>    Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2. Default is 1MB."
        printfn "   /s <sectore-size>          Sector size. Specify either 512 or 4096. Default is 512 bytes."
        printfn "  corrupt command :"
        printfn "   /s <idx1,idx2,...>         Index of 4KB sectors filled with random numbers."
        printfn "  snapshot command :"
        printfn "   /log <log-size>            Log area length. In MB. Specify a number between 1 and 8. Default is 1MB."
        printfn "   /p <payload-block-size>    Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2. Default is 1MB."
        printfn "  create command :"
        printfn "   /f                         Create it as a fixed VHDX file."
        printfn "   /v <virtual-disk-size>     Virtual disk size. In MB. Specify a number between 1 and 67,108,864. Default is 64MB."
        printfn "   /log <log-size>            Log area length. In MB. Specify a number between 1 and 8. Default is 1MB."
        printfn "   /p <payload-block-size>    Payload block length. In MB. Specify a number between 1 and 256 that is a power of 2. Default is 1MB."
        printfn "   /s <sectore-size>          Sector size. Specify either 512 or 4096. Default is 512 bytes."
        printfn "  write command :"
        printfn "   /l <logical-block-address> The logical block address on the virtual disk where data is to be written. Default is 0."
        printfn "   /int <step>                The stage at which processing is interrupted. 1 to 8."
        printfn "  random command :"
        printfn "   /v <file-size>             File size in MB. Default is 64MB."
        printfn "  compare command :"
        printfn "   /t1 <input1-type>          Specify whether the file type of the file designated in input1 is 'raw' or 'vhdx'."
        printfn "                              If omitted, it is determined from the file name."
        printfn "   /t2 <input2-type>          Specify whether the file type of the file designated in input2 is 'raw' or 'vhdx'."
        printfn "                              If omitted, it is determined from the file name."
        printfn ""

    /// Command arguments rules.
    static member ArgRules : AcceptableCommand<VHDXUtilCmdType> [] =
        [|
            {
                Command = [| "READ"; |];
                Varb = VHDXUtilCmdType.Read;
                NamedArgs = Array.empty;
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "TORAW"; |];
                Varb = VHDXUtilCmdType.ToRAW;
                NamedArgs = Array.empty;
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "TOVHDX"; |];
                Varb = VHDXUtilCmdType.ToVHDX;
                NamedArgs = [|
                    ( "/log", CRV_uint32( 1u, 8u ) );
                    ( "/p", CRV_Regex( Regex( "1|2|4|8|16|32|64|128|256" ) ) );
                    ( "/s", CRV_Regex( Regex( "512|4096" ) ) );
                |];
                ValuelessArgs = [| "/f" |];
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "CORRUPT"; |];
                Varb = VHDXUtilCmdType.Corrupt;
                NamedArgs = [| ( "/s", CRVM_String( 512 ) ); |]
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "SNAPSHOT"; |];
                Varb = VHDXUtilCmdType.Snapshot;
                NamedArgs = [|
                    ( "/log", CRV_uint32( 1u, 8u ) );
                    ( "/p", CRV_Regex( Regex( "1|2|4|8|16|32|64|128|256" ) ) );
                |];
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "CREATE"; |];
                Varb = VHDXUtilCmdType.Create;
                NamedArgs = [|
                    ( "/v", CRV_uint64( 1UL, 67108864UL ) );
                    ( "/log", CRV_uint32( 1u, 8u ) );
                    ( "/p", CRV_Regex( Regex( "1|2|4|8|16|32|64|128|256" ) ) );
                    ( "/s", CRV_Regex( Regex( "512|4096" ) ) );
                |];
                ValuelessArgs = [| "/f" |];
                NamelessArgs = [| CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "CHECK"; |];
                Varb = VHDXUtilCmdType.Check;
                NamedArgs = Array.empty;
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "WRITE"; |];
                Varb = VHDXUtilCmdType.Write;
                NamedArgs = [|
                    ( "/l", CRV_uint64( 0UL, UInt64.MaxValue ) );
                    ( "/int", CRV_int32( 1, 99 ) );
                |];
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "RANDOM"; |];
                Varb = VHDXUtilCmdType.Random;
                NamedArgs = [| ( "/v", CRV_uint64( 0UL, UInt64.MaxValue ) ); |];
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
            {
                Command = [| "COMPARE"; |];
                Varb = VHDXUtilCmdType.Compare;
                NamedArgs = [|
                    ( "/t1", CRV_Regex( Regex( "raw|vhdx" ) ) );
                    ( "/t2", CRV_Regex( Regex( "raw|vhdx" ) ) );
                |];
                ValuelessArgs = Array.empty;
                NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 256 ); |];
                HelpMsgName = "";
            };
        |]

    /// <summary>
    ///  Recognize arguments.
    /// </summary>
    /// <param name="argv">
    ///  arguments
    /// </param>
    /// <returns>
    ///  Recognized arguments.
    /// </returns>
    static member Recognize ( argv: string[] ) : CommandParser<VHDXUtilCmdType> =
        try
            CommandParser.FromStringArray CmdArgs.ArgRules argv
        with
        | _ as x ->
            printfn "%s" x.Message
            help()
            exit 1

[< EntryPoint >]
let main ( argv : string[] ) : int32 =
    let cmd = CmdArgs.Recognize argv
    match cmd.Varb with
    | Read ->
        let infile = cmd.DefaultNamelessString 0 ""
        let outfile = cmd.DefaultNamelessString 1 ""
        let metadata = VhdxReader.ReadVhdx infile
        VhdxXmlSerializer.SerializeToFile metadata outfile

    | ToRAW ->
        let infile = cmd.DefaultNamelessString 0 ""
        let outfile = cmd.DefaultNamelessString 1 ""
        VhdxToRaw.Convert infile outfile

    | ToVHDX ->
        let rawfile = cmd.DefaultNamelessString 0 ""
        let outfile = cmd.DefaultNamelessString 1 ""
        let fixedflg = cmd.NamedArgs.ContainsKey "/f"
        let logSize = cmd.DefaultNamedUInt32 "/log" 1u
        let payloadBlockSize = cmd.DefaultNamedString "/p" "1" |> uint32
        let sectorSize = cmd.DefaultNamedString "/s" "512" |> Blocksize.fromStringValue
        VhdxCreator.RawToVHDX rawfile outfile ( logSize * 1048576u ) ( payloadBlockSize * 1048576u ) fixedflg sectorSize

    | Corrupt ->
        let infile = cmd.DefaultNamelessString 0 ""
        let outfile = cmd.DefaultNamelessString 1 ""
        let sectors =
            cmd.DefaultNamedString "/s" ""
            |> _.Split( ',' )
            |> Array.map ( fun s -> uint64 s |> sec4k_me.ofUInt64 )
            |> Array.toList
        VhdxCorrupter.Inject infile outfile sectors

    | Snapshot ->
        let parent = cmd.DefaultNamelessString 0 ""
        let outfile = cmd.DefaultNamelessString 1 ""
        let logSize = cmd.DefaultNamedUInt32 "/log" 1u
        let payloadBlockSize = cmd.DefaultNamedString "/p" "1" |> uint32
        VhdxCreator.Create parent outfile ( logSize * 1048576u ) ( payloadBlockSize * 1048576u ) false 0UL Blocksize.BS_512

    | Create ->
        let outfile = cmd.DefaultNamelessString 0 ""
        let fixedflg = cmd.NamedArgs.ContainsKey "/f"
        let virtualDiskSize = cmd.DefaultNamedUInt64 "/v" 64UL
        let logSize = cmd.DefaultNamedUInt32 "/log" 1u
        let payloadBlockSize = cmd.DefaultNamedString "/p" "1" |> uint32
        let sectorSize = cmd.DefaultNamedString "/s" "512" |> Blocksize.fromStringValue
        VhdxCreator.Create "" outfile ( logSize * 1048576u ) ( payloadBlockSize * 1048576u ) fixedflg ( virtualDiskSize * 1048576UL ) sectorSize

    | Check ->
        let infile = cmd.DefaultNamelessString 0 ""
        VhdxChecker.Check infile

    | Write ->
        let rawfile = cmd.DefaultNamelessString 0 ""
        let vhdxfile = cmd.DefaultNamelessString 1 ""
        let lba = cmd.DefaultNamedUInt64 "/l" 0UL |> blkcnt_me.ofUInt64
        let ex = cmd.DefaultNamedInt32 "/int" 0
        VhdxWriter.Write rawfile vhdxfile lba ex

    | Random ->
        let rawfile = cmd.DefaultNamelessString 0 ""
        let fsizemb = cmd.DefaultNamedUInt64 "/v" 64UL
        VhdxHandler.CreateRandomFile rawfile fsizemb

    | Compare ->
        let file1 = cmd.DefaultNamelessString 0 ""
        let file2 = cmd.DefaultNamelessString 1 ""
        let f1type =
            match cmd.NamedString "/t1" with
            | Some "vhdx" -> true
            | Some "raw" -> false
            | _ -> file1.ToUpperInvariant().EndsWith( ".VHDX" )
        let f2type =
            match cmd.NamedString "/t2" with
            | Some "vhdx" -> true
            | Some "raw" -> false
            | _ -> file2.ToUpperInvariant().EndsWith( ".VHDX" )
        let r =
            match f1type, f2type with
            | ( false, false ) ->
                VhdxHandler.CompareRAW_RAW file1 file2
            | ( true, false ) ->
                VhdxHandler.CompareVHDX_RAW file1 file2
            | ( false, true ) ->
                VhdxHandler.CompareVHDX_RAW file2 file1
            | ( true, true ) ->
                VhdxHandler.CompareVHDX_VHDX file1 file2
        if r then
            printfn "The file contents match."
        else
            printfn "The file contents do not match."

    0
