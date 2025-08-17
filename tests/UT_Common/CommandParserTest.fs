namespace Haruka.Test.UT.Commons

open System
open System.IO
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

type testVerb =
    | Exit
    | Login
    | Logout

type CommandParser_Test1() =

    let GenCommandStream ( txt : string ) =
        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )
        ws.WriteLine( txt )
        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let rs = new StreamReader( ms )
        ms, ws, rs

    let DisposeCommandStream (  ms : MemoryStream ) ( ws : StreamWriter ) ( rs : StreamReader ) =
        ms.Dispose()
        ws.Dispose()
        rs.Dispose()

    let exitAccCmd ( namedArgs : ( string * CRValidateType )[] ) ( valuelessArgs : string[] ) ( namelessArgs : CRValidateType[] ) =
        [|{
            Command= [| "EXIT" |];
            Varb = testVerb.Exit;
            NamedArgs = namedArgs;
            ValuelessArgs = valuelessArgs;
            NamelessArgs = namelessArgs;
        }|]

    let genAccCmd2 ( cmds : string[] ) ( v : testVerb ) ( namelessArgs : CRValidateType[] ) =
        {
            Command= cmds;
            Varb = v;
            NamedArgs = Array.empty;
            ValuelessArgs = Array.empty;
            NamelessArgs = namelessArgs;
        }

    [<Fact>]
    member _.DiviteInputString_001() =
        let st = new StringTable( "" )
        let accCommands =  exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 999u ) |]
        let r = CommandParser.FromString st accCommands "exit 123"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint32( 123u ) ))
        

    [<Fact>]
    member _.DiviteInputString_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/n", CRV_String( 256 ) ) |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /n a^ a"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( "a a" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.DiviteInputString_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/n", CRV_String( 256 ) ) |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /n a^^a"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( "a^a" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.DiviteInputString_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/n", CRV_String( 256 ) ) |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /n abb^"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( "abb^" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.SearchCommand_001() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1" |] testVerb.Exit Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa1"
        Assert.True(( r.Varb = testVerb.Exit ))

    [<Fact>]
    member _.SearchCommand_002() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "A" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1 a"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_003() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1 bb1"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_004() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "A" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "B" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA"; |] testVerb.Logout Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa"
        Assert.True(( r.Varb = testVerb.Logout ))

    [<Fact>]
    member _.SearchCommand_005() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA"; |] testVerb.Logout [| CRV_String( 2 ) |];
        |]
        let r = CommandParser.FromString st accCommands "aa bb"
        Assert.True(( r.Varb = testVerb.Logout ))

    [<Fact>]
    member _.SearchCommand_006() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "A" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "B"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_007() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "A" |] testVerb.Exit Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_008() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "BB1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "CC1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_009() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "BB1"; "b" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "CC1"; "c"; |] testVerb.Logout Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa"
        Assert.True(( r.Varb = testVerb.Exit ))

    [<Fact>]
    member _.SearchCommand_010() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; |] testVerb.Exit [| CRV_String( 2 )|];
            genAccCmd2 [| "BB1"; "b" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "CC1"; "c"; |] testVerb.Logout Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa bb"
        Assert.True(( r.Varb = testVerb.Exit ))

    [<Fact>]
    member _.SearchCommand_011() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_012() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "A" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "B" |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_013() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1 cc1"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_014() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; "B" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1 bb1"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_015() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1 bb1 c"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_016() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "CC1"; |] testVerb.Login Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa1 cc1"
        Assert.True(( r.Varb = testVerb.Login ))

    [<Fact>]
    member _.SearchCommand_017() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "CC1"; |] testVerb.Login [| CRV_String( 2 )|];
        |]
        let r = CommandParser.FromString st accCommands "aa1 cc1 dd"
        Assert.True(( r.Varb = testVerb.Login ))

    [<Fact>]
    member _.SearchCommand_018() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "CC1"; "D"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa1 cc1"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_019() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "CC1"; "D"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa dd"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_020() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB2"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa bb"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_021() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; "D"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa bb"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_022() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "CC1"; |] testVerb.Login Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa bb"
        Assert.True(( r.Varb = testVerb.Exit ))

    [<Fact>]
    member _.SearchCommand_023() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit [| CRV_String( 2 ) |];
            genAccCmd2 [| "AA1"; "CC1"; |] testVerb.Login Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa bb dd"
        Assert.True(( r.Varb = testVerb.Exit ))

    [<Fact>]
    member _.SearchCommand_024() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa bb"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_025() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; "C"; |] testVerb.Exit Array.empty;
            genAccCmd2 [| "AA1"; "BB1"; "D"; |] testVerb.Login Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa bb"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_026() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA"; |] testVerb.Exit Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.SearchCommand_027() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA"; |] testVerb.Exit Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa bb"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_028() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA"; |] testVerb.Exit [| CRV_String( 2 ) |];
        |]
        let r = CommandParser.FromString st accCommands "aa bb"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( "bb" ) ))

    [<Fact>]
    member _.SearchCommand_029() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit Array.empty;
        |]
        let r = CommandParser.FromString st accCommands "aa bb"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.SearchCommand_030() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1"; |] testVerb.Exit Array.empty;
        |]
        try
            let _ = CommandParser.FromString st accCommands "aa bb cc"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SearchCommand_031() =
        let st = new StringTable( "" )
        let accCommands = [|
            genAccCmd2 [| "AA1"; "BB1" |] testVerb.Exit [| CRV_String( 2 ) |];
        |]
        let r = CommandParser.FromString st accCommands "aa bb cc"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( "cc" ) ))

    [<Fact>]
    member _.RecognizeCommand_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty
        try
            let _ = CommandParser.FromStringArray st accCommands Array.empty
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "No command strings" ))
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty
        let r = CommandParser.FromString st accCommands "eXIt"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /y"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.[ "/y" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/y", CRV_int32( 0, 1 ) ) |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /y 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.[ "/y" ] = EV_int32( 1 ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_005() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 10 ) |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( "aaa" ) ))

    [<Fact>]
    member _.RecognizeCommand_006() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); ( "/b", CRV_int32( 0, 1 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 0 /b 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( 0 ) ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_int32( 1 ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_007() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); |] [| "/b" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 0 /b"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( 0 ) ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_008() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); |] Array.empty [| CRV_int32( 0, 1 ) |]
        let r = CommandParser.FromString st accCommands "exit /a 0 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( 0 ) ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_int32( 1 ) ))

    [<Fact>]
    member _.RecognizeCommand_009() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); |] [| "/b" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /b /a 0"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( 0 ) ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_010() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/b"; "/c" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /c /b"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_NoValue ))
        Assert.True(( r.NamedArgs.[ "/c" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_011() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/b"; |] [| CRV_int32( 0, 1 ) |]
        let r = CommandParser.FromString st accCommands "exit /b 0"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_int32( 0 ) ))

    [<Fact>]
    member _.RecognizeCommand_012() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); |] Array.empty [| CRV_int32( 0, 1 ) |]
        let r = CommandParser.FromString st accCommands "exit 0 /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32 1 ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_int32( 0 ) ))

    [<Fact>]
    member _.RecognizeCommand_013() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/b" |] [| CRV_int32( 0, 1 ) |]
        let r = CommandParser.FromString st accCommands "exit 0 /b"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_int32( 0 ) ))

    [<Fact>]
    member _.RecognizeCommand_014() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 1 ); CRV_String( 10 ) |]
        let r = CommandParser.FromString st accCommands "exit 0 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 2 ))
        Assert.True(( r.NamelessArgs.[0] = EV_int32( 0 ) ))
        Assert.True(( r.NamelessArgs.[1] = EV_String( "1" ) ))

    [<Fact>]
    member _.RecognizeCommand_015() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); ( "/b", CRV_String( 10 ) ); ( "/c", CRV_int32( 0, 1 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /b aaa /c 0 /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( 1 ) ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_String( "aaa" ) ))
        Assert.True(( r.NamedArgs.[ "/c" ] = EV_int32( 0 ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_016() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/a"; "/b"; "/c" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /c /a /b"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_NoValue ))
        Assert.True(( r.NamedArgs.[ "/b" ] = EV_NoValue ))
        Assert.True(( r.NamedArgs.[ "/c" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_017() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 1 ); CRV_String( 10 ); CRV_String( 10 ); |]
        let r = CommandParser.FromString st accCommands "exit 1 aaa bbb"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 3 ))
        Assert.True(( r.NamelessArgs.[0] = EV_int32( 1 ) ))
        Assert.True(( r.NamelessArgs.[1] = EV_String( "aaa" ) ))
        Assert.True(( r.NamelessArgs.[2] = EV_String( "bbb" ) ))

    [<Fact>]
    member _.RecognizeCommand_018() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit /a"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_LAST_ARG_VAL_MISSING" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_019() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 1 ); CRV_String( 10 ); CRV_String( 10 ); |]
        try
            let _ = CommandParser.FromString st accCommands "exit 0 aa"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_020() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 1 ); CRV_String( 10 ); CRV_String( 10 ); |]
        try
            let _ = CommandParser.FromString st accCommands "exit 0 aa bb cc"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_021() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit 0"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_022() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 1 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 0 /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32 1 ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_023() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /y /y /y"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/y" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_024() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 10 ) ); |] [| "/y" |] Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa /y /a bbb /y"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "bbb" ) ))
        Assert.True(( r.NamedArgs.[ "/y" ] = EV_NoValue ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_025() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int32( 0, 1 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( 1 ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_026() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int32( 0, 1 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_027() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint32( 0u, 1u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_uint32( 1u ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_028() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint32( 0u, 1u ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_029() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int64( 0L, 1L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int64( 1L ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_030() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int64( 0L, 1L ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_031() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint64( 0UL, 1UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_uint64( 1UL ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_032() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint64( 0UL, 1UL ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_033() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_String( 1 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a a"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "a" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_034() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_String( 1 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_035() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_Regex( Regex( "a" ) ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a a"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "a" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_036() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_Regex( Regex( "a" ) ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.RecognizeCommand_037() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 0"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_LUN( lun_me.zero ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.RecognizeCommand_038() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_LUN ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -2147483649" )>]
    [<InlineData( "exit /a -2147483648" )>]
    [<InlineData( "exit /a -101" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 2147483648" )>]
    [<InlineData( "exit /a 2147483647" )>]
    member _.ValidateValue_int32_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( -100, 100 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a -100", -100 )>]
    [<InlineData( "exit /a 0", 0 )>]
    [<InlineData( "exit /a 100", 100 )>]
    member _.ValidateValue_int32_002( ecmd : string, eval : int ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( -100, 100 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -2147483649" )>]
    [<InlineData( "exit /a -2147483648" )>]
    [<InlineData( "exit /a -101" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 2147483648" )>]
    [<InlineData( "exit /a 2147483647" )>]
    member _.ValidateValue_int32_m_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int32( -100, 100 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a -100", -100 )>]
    [<InlineData( "exit /a 0", 0 )>]
    [<InlineData( "exit /a 100", 100 )>]
    member _.ValidateValue_int32_m_002( ecmd : string, eval : int ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int32( -100, 100 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int32( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -1" )>]
    [<InlineData( "exit /a 0" )>]
    [<InlineData( "exit /a 9" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 4294967295" )>]
    [<InlineData( "exit /a 4294967296" )>]
    member _.ValidateValue_uint32_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint32( 10u, 100u ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 10", 10u )>]
    [<InlineData( "exit /a 50", 50u )>]
    [<InlineData( "exit /a 100", 100u )>]
    member _.ValidateValue_uint32_002( ecmd : string, eval : uint32 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint32( 10u, 100u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_uint32( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -1" )>]
    [<InlineData( "exit /a 0" )>]
    [<InlineData( "exit /a 9" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 4294967295" )>]
    [<InlineData( "exit /a 4294967296" )>]
    member _.ValidateValue_uint32_m_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint32( 10u, 100u ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 10", 10u )>]
    [<InlineData( "exit /a 50", 50u )>]
    [<InlineData( "exit /a 100", 100u )>]
    member _.ValidateValue_uint32_m_002( ecmd : string, eval : uint32 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint32( 10u, 100u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_uint32( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -9223372036854775809" )>]
    [<InlineData( "exit /a -9223372036854775808" )>]
    [<InlineData( "exit /a 9" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 9223372036854775807" )>]
    [<InlineData( "exit /a 9223372036854775808" )>]
    member _.ValidateValue_int64_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int64( 10L, 100L ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 10", 10L )>]
    [<InlineData( "exit /a 50", 50L )>]
    [<InlineData( "exit /a 100", 100L )>]
    member _.ValidateValue_int64_002( ecmd : string, eval : int64 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int64( 10L, 100L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int64( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -9223372036854775809" )>]
    [<InlineData( "exit /a -9223372036854775808" )>]
    [<InlineData( "exit /a 9" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 9223372036854775807" )>]
    [<InlineData( "exit /a 9223372036854775808" )>]
    member _.ValidateValue_int64_m_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int64( 10L, 100L ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 10", 10L )>]
    [<InlineData( "exit /a 50", 50L )>]
    [<InlineData( "exit /a 100", 100L )>]
    member _.ValidateValue_int64_m_002( ecmd : string, eval : int64 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_int64( 10L, 100L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_int64( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -1" )>]
    [<InlineData( "exit /a 0" )>]
    [<InlineData( "exit /a 9" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 18446744073709551615" )>]
    [<InlineData( "exit /a 18446744073709551616" )>]
    member _.ValidateValue_uint64_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint64( 10UL, 100UL ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 10", 10UL )>]
    [<InlineData( "exit /a 50", 50UL )>]
    [<InlineData( "exit /a 100", 100UL )>]
    member _.ValidateValue_uint64_002( ecmd : string, eval : uint64 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint64( 10UL, 100UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_uint64( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a aaa" )>]
    [<InlineData( "exit /a 1.1" )>]
    [<InlineData( "exit /a -1" )>]
    [<InlineData( "exit /a 0" )>]
    [<InlineData( "exit /a 9" )>]
    [<InlineData( "exit /a 101" )>]
    [<InlineData( "exit /a 18446744073709551615" )>]
    [<InlineData( "exit /a 18446744073709551616" )>]
    member _.ValidateValue_uint64_m_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint64( 10UL, 100UL ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 10", 10UL )>]
    [<InlineData( "exit /a 50", 50UL )>]
    [<InlineData( "exit /a 100", 100UL )>]
    member _.ValidateValue_uint64_m_002( ecmd : string, eval : uint64 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_uint64( 10UL, 100UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_uint64( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a 123456" )>]
    [<InlineData( "exit /a 123^ 56" )>]
    [<InlineData( "exit /a 123^^56" )>]
    member _.ValidateValue_String_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a a", "a" )>]
    [<InlineData( "exit /a 12345", "12345" )>]
    [<InlineData( "exit /a 12^ 45", "12 45" )>]
    [<InlineData( "exit /a 12^^45", "12^45" )>]
    member _.ValidateValue_String_002( ecmd : string, eval : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a 123456" )>]
    [<InlineData( "exit /a 123^ 56" )>]
    [<InlineData( "exit /a 123^^56" )>]
    member _.ValidateValue_String_m_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_String( 5 ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a a", "a" )>]
    [<InlineData( "exit /a 12345", "12345" )>]
    [<InlineData( "exit /a 12^ 45", "12 45" )>]
    [<InlineData( "exit /a 12^^45", "12^45" )>]
    member _.ValidateValue_String_m_002( ecmd : string, eval : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.ValidateValue_Regex_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_Regex( new Regex "aaa.*bbb" ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit /a cd"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.ValidateValue_Regex_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_Regex( new Regex "aaa.*bbb" ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaaXXbbb"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "aaaXXbbb" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.ValidateValue_Regex_m_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_Regex( new Regex "aaa.*bbb" ) ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands "exit /a cd"
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.ValidateValue_Regex_m_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_Regex( new Regex "aaa.*bbb" ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaaXXbbb"
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "aaaXXbbb" ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a a" )>]
    [<InlineData( "exit /a 0a00" )>]
    [<InlineData( "exit /a 00-11" )>]
    [<InlineData( "exit /a -1" )>]
    [<InlineData( "exit /a 18446744073709551616" )>]
    [<InlineData( "exit /a 0x10000000000000000" )>]
    member _.ValidateValue_LUN_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_LUN ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 0", 0UL )>]
    [<InlineData( "exit /a 18446744073709551615", 0xFFFFFFFFFFFFFFFFUL )>]
    [<InlineData( "exit /a 0x0", 0UL )>]
    [<InlineData( "exit /a 0xFFFFFFFFFFFFFFFF", 0xFFFFFFFFFFFFFFFFUL )>]
    [<InlineData( "exit /a 0X1122", 0x1122UL )>]
    member _.ValidateValue_LUN_002( ecmd : string, eval : uint64 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_LUN( lun_me.fromPrim eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Theory>]
    [<InlineData( "exit /a a" )>]
    [<InlineData( "exit /a 0a00" )>]
    [<InlineData( "exit /a 00-11" )>]
    [<InlineData( "exit /a -1" )>]
    [<InlineData( "exit /a 18446744073709551616" )>]
    [<InlineData( "exit /a 0x10000000000000000" )>]
    member _.ValidateValue_LUN_m_001( ecmd : string ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_LUN ); |] Array.empty Array.empty
        try
            let _ = CommandParser.FromString st accCommands ecmd
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "exit /a 0", 0UL )>]
    [<InlineData( "exit /a 18446744073709551615", 0xFFFFFFFFFFFFFFFFUL )>]
    [<InlineData( "exit /a 0x0", 0UL )>]
    [<InlineData( "exit /a 0xFFFFFFFFFFFFFFFF", 0xFFFFFFFFFFFFFFFFUL )>]
    [<InlineData( "exit /a 0X1122", 0x1122UL )>]
    member _.ValidateValue_LUN_m_002( ecmd : string, eval : uint64 ) =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRVM_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands ecmd
        Assert.True(( r.Varb = testVerb.Exit ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_LUN( lun_me.fromPrim eval ) ))
        Assert.True(( r.NamelessArgs.Length = 0 ))

    [<Fact>]
    member _.DefaultNamedInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedInt32 "/a" 99 ) = 1 ))

    [<Fact>]
    member _.DefaultNamedInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedInt32 "/b" 99 ) = 99 ))

    [<Fact>]
    member _.DefaultNamedInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.DefaultNamedInt32 "/b" 99 ) = 99 ))

    [<Fact>]
    member _.NamedInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedInt32 "/a" ) = Some 1 ))

    [<Fact>]
    member _.NamedInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 0, 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedInt32 "/b" ) = None ))

    [<Fact>]
    member _.NamedInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.NamedInt32 "/b" ) = None ))

    [<Fact>]
    member _.DefaultNamedUInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint32( 0u, 10u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedUInt32 "/a" 99u ) = 1u ))

    [<Fact>]
    member _.DefaultNamedUInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint32( 0u, 10u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedUInt32 "/b" 99u ) = 99u ))

    [<Fact>]
    member _.DefaultNamedUInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a bbb"
        Assert.True(( ( r.DefaultNamedUInt32 "/b" 99u ) = 99u ))

    [<Fact>]
    member _.NamedUInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint32( 0u, 10u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedUInt32 "/a" ) = Some 1u ))

    [<Fact>]
    member _.NamedUInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint32( 0u, 10u ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedUInt32 "/b" ) = None ))

    [<Fact>]
    member _.NamedUInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.NamedUInt32 "/b" ) = None ))

    [<Fact>]
    member _.DefaultNamedInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int64( 0L, 10L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedInt64 "/a" 99L ) = 1L ))

    [<Fact>]
    member _.DefaultNamedInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int64( 0L, 10L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedInt64 "/b" 99L ) = 99L ))

    [<Fact>]
    member _.DefaultNamedInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a bbb"
        Assert.True(( ( r.DefaultNamedInt64 "/b" 99L ) = 99L ))

    [<Fact>]
    member _.NamedInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int64( 0L, 10L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedInt64 "/a" ) = Some 1L ))

    [<Fact>]
    member _.NamedInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int64( 0L, 10L ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedInt64 "/b" ) = None ))

    [<Fact>]
    member _.NamedInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.NamedInt64 "/b" ) = None ))

    [<Fact>]
    member _.DefaultNamedUInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint64( 0UL, 10UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedUInt64 "/a" 99UL ) = 1UL ))

    [<Fact>]
    member _.DefaultNamedUInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint64( 0UL, 10UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedUInt64 "/b" 99UL ) = 99UL ))

    [<Fact>]
    member _.DefaultNamedUInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a bbb"
        Assert.True(( ( r.DefaultNamedUInt64 "/b" 99UL ) = 99UL ))

    [<Fact>]
    member _.NamedUInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint64( 0UL, 10UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedUInt64 "/a" ) = Some 1UL ))

    [<Fact>]
    member _.NamedUInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_uint64( 0UL, 10UL ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedUInt64 "/b" ) = None ))

    [<Fact>]
    member _.NamedUInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.NamedUInt64 "/b" ) = None ))


    [<Fact>]
    member _.DefaultNamedString_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.DefaultNamedString "/a" "bbb" ) = "aaa" ))

    [<Fact>]
    member _.DefaultNamedString_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.DefaultNamedString "/b" "bbb" ) = "bbb" ))

    [<Fact>]
    member _.DefaultNamedString_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 10, 15 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 12"
        Assert.True(( ( r.DefaultNamedString "/b" "bbb" ) = "bbb" ))

    [<Fact>]
    member _.NamedString_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.NamedString "/a" ) = Some "aaa" ))

    [<Fact>]
    member _.NamedString_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 10 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.NamedString "/b" ) = None ))

    [<Fact>]
    member _.NamedString_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_int32( 10, 15 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 14"
        Assert.True(( ( r.NamedString "/b" ) = None ))

    [<Fact>]
    member _.DefaultNamedLUN_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedLUN "/a" ( lun_me.fromPrim 99UL ) ) = ( lun_me.fromPrim 1UL ) ))

    [<Fact>]
    member _.DefaultNamedLUN_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.DefaultNamedLUN "/b" ( lun_me.fromPrim 99UL ) ) = ( lun_me.fromPrim 99UL ) ))

    [<Fact>]
    member _.DefaultNamedLUN_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a aaa"
        Assert.True(( ( r.DefaultNamedLUN "/b" ( lun_me.fromPrim 99UL ) ) = ( lun_me.fromPrim 99UL ) ))

    [<Fact>]
    member _.NamedLUN_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedLUN "/a" ) = Some( lun_me.fromPrim 1UL ) ))

    [<Fact>]
    member _.NamedLUN_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_LUN ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a 1"
        Assert.True(( ( r.NamedLUN "/b" ) = None ))

    [<Fact>]
    member _.NamedLUN_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd [| ( "/a", CRV_String( 5 ) ); |] Array.empty Array.empty
        let r = CommandParser.FromString st accCommands "exit /a bbb"
        Assert.True(( ( r.NamedLUN "/b" ) = None ))

    [<Fact>]
    member _.DefaultNamelessInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 3 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessInt32 0 99 ) = 1 ))

    [<Fact>]
    member _.DefaultNamelessInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 3 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessInt32 -1 99 ) = 99 ))

    [<Fact>]
    member _.DefaultNamelessInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 3 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessInt32 2 99 ) = 99 ))

    [<Fact>]
    member _.DefaultNamelessInt32_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessInt32 0 99 ) = 99 ))

    [<Fact>]
    member _.NamelessInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 3 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessInt32 0 ) = Some 1 ))

    [<Fact>]
    member _.NamelessInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 3 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessInt32 -1 ) = None ))

    [<Fact>]
    member _.NamelessInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 0, 3 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessInt32 2 ) = None ))

    [<Fact>]
    member _.NamelessInt32_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessInt32 0 ) = None ))

    [<Fact>]
    member _.DefaultNamelessUInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 3u ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessUInt32 0 99u ) = 1u ))

    [<Fact>]
    member _.DefaultNamelessUInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 3u ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessUInt32 -1 99u ) = 99u ))

    [<Fact>]
    member _.DefaultNamelessUInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 3u ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessUInt32 2 99u ) = 99u ))

    [<Fact>]
    member _.DefaultNamelessUInt32_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessUInt32 0 99u ) = 99u ))

    [<Fact>]
    member _.NamelessUInt32_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 3u ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessUInt32 0 ) = Some 1u ))

    [<Fact>]
    member _.NamelessUInt32_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 3u ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessUInt32 -1 ) = None ))

    [<Fact>]
    member _.NamelessUInt32_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint32( 0u, 3u ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessUInt32 2 ) = None ))

    [<Fact>]
    member _.NamelessUInt32_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessUInt32 0 ) = None ))

    [<Fact>]
    member _.DefaultNamelessInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int64( 0L, 3L ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessInt64 0 99L ) = 1L ))

    [<Fact>]
    member _.DefaultNamelessInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int64( 0L, 3L ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessInt64 -1 99L ) = 99L ))

    [<Fact>]
    member _.DefaultNamelessInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int64( 0L, 3L ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessInt64 2 99L ) = 99L ))

    [<Fact>]
    member _.DefaultNamelessInt64_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessInt64 0 99L ) = 99L ))

    [<Fact>]
    member _.NamelessInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int64( 0L, 3L ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessInt64 0 ) = Some 1L ))

    [<Fact>]
    member _.NamelessInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int64( 0L, 3L ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessInt64 -1 ) = None ))

    [<Fact>]
    member _.NamelessInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int64( 0L, 3L ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessInt64 2 ) = None ))

    [<Fact>]
    member _.NamelessInt64_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessInt64 0 ) = None ))

    [<Fact>]
    member _.DefaultNamelessUInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint64( 0UL, 3UL ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessUInt64 0 99UL ) = 1UL ))

    [<Fact>]
    member _.DefaultNamelessUInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint64( 0UL, 3UL ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessUInt64 -1 99UL ) = 99UL ))

    [<Fact>]
    member _.DefaultNamelessUInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint64( 0UL, 3UL ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessUInt64 2 99UL ) = 99UL ))

    [<Fact>]
    member _.DefaultNamelessUInt64_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessUInt64 0 99UL ) = 99UL ))

    [<Fact>]
    member _.NamelessUInt64_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint64( 0UL, 3UL ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessUInt64 0 ) = Some 1UL ))

    [<Fact>]
    member _.NamelessUInt64_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint64( 0UL, 3UL ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessUInt64 -1 ) = None ))

    [<Fact>]
    member _.NamelessUInt64_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_uint64( 0UL, 3UL ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessUInt64 2 ) = None ))

    [<Fact>]
    member _.NamelessUInt64_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 4 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessUInt64 0 ) = None ))

    [<Fact>]
    member _.DefaultNamelessString_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessString 0 "GGG" ) = "aaa" ))

    [<Fact>]
    member _.DefaultNamelessString_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessString -1 "GGG" ) = "GGG" ))

    [<Fact>]
    member _.DefaultNamelessString_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessString 2 "GGG" ) = "GGG" ))

    [<Fact>]
    member _.DefaultNamelessString_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 1, 2 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessString 0 "GGG" ) = "GGG" ))

    [<Fact>]
    member _.NamelessString_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessString 0 ) = Some "aaa" ))

    [<Fact>]
    member _.NamelessString_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessString -1 ) = None ))

    [<Fact>]
    member _.NamelessString_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessString 2 ) = None ))

    [<Fact>]
    member _.NamelessString_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_int32( 1, 2 ); |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessString 0 ) = None ))

    [<Fact>]
    member _.DefaultNamelessLUN_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_LUN; |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessLUN 0 lun_me.zero ) = ( lun_me.fromPrim 1UL ) ))

    [<Fact>]
    member _.DefaultNamelessLUN_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_LUN; |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessLUN -1 lun_me.zero ) = lun_me.zero ))

    [<Fact>]
    member _.DefaultNamelessLUN_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_LUN; |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.DefaultNamelessLUN 2 lun_me.zero ) = lun_me.zero ))

    [<Fact>]
    member _.DefaultNamelessLUN_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.DefaultNamelessLUN 0 lun_me.zero ) = lun_me.zero ))

    [<Fact>]
    member _.NamelessLUN_001() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_LUN; |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessLUN 0 ) = Some( lun_me.fromPrim 1UL ) ))

    [<Fact>]
    member _.NamelessLUN_002() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_LUN; |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessLUN -1 ) = None ))

    [<Fact>]
    member _.NamelessLUN_003() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_LUN; |]
        let r = CommandParser.FromString st accCommands "exit 1"
        Assert.True(( ( r.NamelessLUN 2 ) = None ))

    [<Fact>]
    member _.NamelessLUN_004() =
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty Array.empty [| CRV_String( 5 ); |]
        let r = CommandParser.FromString st accCommands "exit aaa"
        Assert.True(( ( r.NamelessLUN 0 ) = None ))
