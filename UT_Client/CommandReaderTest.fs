namespace Haruka.Test.UT.Client

open System
open System.IO
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

type CommandReader_Test() =


    let GenCommandStream ( txt : string ) =
        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )
        ws.WriteLine( txt )
        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let rs = new StreamReader( ms )
        ms, ws, rs

    let exitAccCmd ( namedArgs : ( string * CRValidateType )[] ) ( valuelessArgs : string[] ) ( namelessArgs : CRValidateType[] ) =
        [|{
            Command= [| "EXIT" |];
            Varb = CommandVarb.Exit;
            NamedArgs = namedArgs;
            ValuelessArgs = valuelessArgs;
            NamelessArgs = namelessArgs;
        }|]

    let RunInputCommandMethod ( infile : TextReader ) ( accCommands :  AcceptableCommand<CommandVarb> array ) : CommandParser<CommandVarb> =
        use outs = new StreamWriter( new MemoryStream() )
        let st = new StringTable( "" )
        CommandReader.InputCommand infile outs st accCommands "--"
        |> Functions.RunTaskSynchronously

    let RunInputCommandMethod_CommandInputError ( infile : TextReader ) ( accCommands : AcceptableCommand<CommandVarb> array ) ( resultmsg : string ) =
        use outs = new StreamWriter( new MemoryStream() )
        let st = new StringTable( "" )
        task {
            try
                let! _ = CommandReader.InputCommand infile outs st accCommands "--"
                Assert.Fail __LINE__
            with
            | :? CommandInputError as x ->
                Assert.True(( x.Message.StartsWith resultmsg ))
            | _ ->
                Assert.Fail __LINE__
        }
        |> Functions.RunTaskSynchronously

    [<Fact>]
    member _.InputCommand_001() =
        let ms, ws, rs = GenCommandStream "exit"
        use outs = new StreamWriter( new MemoryStream() )
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty

        let r =
            CommandReader.InputCommand rs outs st accCommands "--"
            |> Functions.RunTaskSynchronously
        Assert.True(( r.Varb = CommandVarb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.InputCommand_002() =
        let ms, ws, rs = GenCommandStream "exit"
        use outs = new StreamWriter( new MemoryStream() )
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty

        let r =
            CommandReader.InputCommand rs outs st accCommands "--"
            |> Functions.RunTaskSynchronously
        Assert.True(( r.Varb = CommandVarb.Exit ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.InputCommand_003() =
        let ms, ws, rs = GenCommandStream "aaa"
        use outs = new StreamWriter( new MemoryStream() )
        let st = new StringTable( "" )
        let accCommands = exitAccCmd Array.empty [| "/y" |] Array.empty

        task {
            try
                let! _ = CommandReader.InputCommand rs outs st accCommands "--"
                Assert.Fail __LINE__
            with
            | :? CommandInputError as x ->
                Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
            | _ -> Assert.Fail __LINE__
        }
        |> Functions.RunTaskSynchronously
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Exit_001() =
        let ms, ws, rs = GenCommandStream "exit"
        let accCommands = [| CommandReader.CmdRule_exit |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Exit ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Exit_002() =
        let ms, ws, rs = GenCommandStream "exit /y"
        let accCommands = [| CommandReader.CmdRule_exit |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Exit ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.["/y"] = EV_NoValue ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "exit /i", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "exit 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "exit /y 0", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Exit_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_exit |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.login_001() =
        let ms, ws, rs = GenCommandStream "login /h a /p 1 /f"
        let accCommands = [| CommandReader.CmdRule_login |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Login ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.["/h"] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.["/p"] = EV_int32( 1 ) ))
        Assert.True(( r.NamedArgs.["/f"] = EV_NoValue ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.login_002() =
        let ms, ws, rs = GenCommandStream ( "login /h " + ( String.replicate 256 "a" ) + " /p 65535" )
        let accCommands = [| CommandReader.CmdRule_login |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Login ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.["/h"] = EV_String( String.replicate 256 "a" ) ))
        Assert.True(( r.NamedArgs.["/p"] = EV_int32( 65535 ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "login 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "login /h /p 1", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "login /h aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "login /p 0", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "login /p 65536", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "login /d", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "login /h", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "login /p", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    member _.login_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_login |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Logout_001() =
        let ms, ws, rs = GenCommandStream "logout"
        let accCommands = [| CommandReader.CmdRule_logout |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Logout ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Logout_002() =
        let ms, ws, rs = GenCommandStream "logout /y"
        let accCommands = [| CommandReader.CmdRule_logout |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Logout ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.["/y"] = EV_NoValue ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "logout /i", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "logout 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "logout /y 0", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Logout_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_logout |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Reload_001() =
        let ms, ws, rs = GenCommandStream "reload"
        let accCommands = [| CommandReader.CmdRule_reload |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Reload ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Reload_002() =
        let ms, ws, rs = GenCommandStream "reload /y"
        let accCommands = [| CommandReader.CmdRule_reload |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Reload ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.["/y"] = EV_NoValue ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "reload /i", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "reload 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "reload /y 0", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Reload_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_reload |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Select_001() =
        let ms, ws, rs = GenCommandStream "select 0"
        let accCommands = [| CommandReader.CmdRule_select |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Select ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint32( 0u ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Select_002() =
        let ms, ws, rs = GenCommandStream ( sprintf "select %d" ( ClientConst.MAX_CHILD_NODE_COUNT - 1 ) )
        let accCommands = [| CommandReader.CmdRule_select |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Select ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint32( uint ClientConst.MAX_CHILD_NODE_COUNT - 1u ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "select", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "select 1 2", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "select -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "select /f", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.Select_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_select |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Select_004() =
        let ms, ws, rs = GenCommandStream ( sprintf "select %d" ClientConst.MAX_CHILD_NODE_COUNT )
        let accCommands = [| CommandReader.CmdRule_select |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.UnSelect_001() =
        let ms, ws, rs = GenCommandStream "unselect"
        let accCommands = [| CommandReader.CmdRule_unselect |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.UnSelect ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.UnSelect_002() =
        let ms, ws, rs = GenCommandStream "unselect /p 0"
        let accCommands = [| CommandReader.CmdRule_unselect |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.UnSelect ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/p"] = EV_uint32( 0u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.UnSelect_003() =
        let ms, ws, rs = GenCommandStream ( sprintf "unselect /p %d" ( ClientConst.MAX_CHILD_NODE_COUNT - 1 ) )
        let accCommands = [| CommandReader.CmdRule_unselect |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.UnSelect ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/p"] = EV_uint32( uint ClientConst.MAX_CHILD_NODE_COUNT - 1u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "unselect 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "unselect /p", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "unselect /p -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "unselect /f", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.UnSelect_004 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_unselect |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.UnSelect_005() =
        let ms, ws, rs = GenCommandStream ( sprintf "unselect /p %d" ClientConst.MAX_CHILD_NODE_COUNT )
        let accCommands = [| CommandReader.CmdRule_unselect |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.List_001() =
        let ms, ws, rs = GenCommandStream "list"
        let accCommands = [| CommandReader.CmdRule_list |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.List ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "list 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "list /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "list /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.List_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_list |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.ListParent_001() =
        let ms, ws, rs = GenCommandStream "listparent"
        let accCommands = [| CommandReader.CmdRule_listparent |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.ListParent ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "listparent 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "listparent /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "listparent /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.ListParent_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_listparent |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Pwd_001() =
        let ms, ws, rs = GenCommandStream "pwd"
        let accCommands = [| CommandReader.CmdRule_pwd |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Pwd ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "pwd 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "pwd /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "pwd /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Pwd_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_pwd |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Values_001() =
        let ms, ws, rs = GenCommandStream "values"
        let accCommands = [| CommandReader.CmdRule_values |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Values ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "values 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "values /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "values /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Values_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_values |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Set_001() =
        let ms, ws, rs = GenCommandStream "set a b"
        let accCommands = [| CommandReader.CmdRule_set |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Set ))
        Assert.True(( r.NamelessArgs.Length = 2 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( r.NamelessArgs.[1] = EV_String( "b" ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Set_002() =
        let astr = String.replicate 256 "a"
        let bstr = String.replicate 65536 "a"
        let ms, ws, rs = GenCommandStream ( "set " + astr + " " + bstr )
        let accCommands = [| CommandReader.CmdRule_set |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Set ))
        Assert.True(( r.NamelessArgs.Length = 2 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( astr ) ))
        Assert.True(( r.NamelessArgs.[1] = EV_String( bstr ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Set_003() =
        let astr = String.replicate 257 "a"
        let ms, ws, rs = GenCommandStream ( "set " + astr + " b" )
        let accCommands = [| CommandReader.CmdRule_set |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Set_004() =
        let bstr = String.replicate 65537 "a"
        let ms, ws, rs = GenCommandStream ( "set a " + bstr )
        let accCommands = [| CommandReader.CmdRule_set |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "set 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "set /p -1 6", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "set", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Set_005 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_set |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Validate_001() =
        let ms, ws, rs = GenCommandStream "validate"
        let accCommands = [| CommandReader.CmdRule_validate |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Validate ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "validate 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "validate /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "validate /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Validate_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_validate |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Publish_001() =
        let ms, ws, rs = GenCommandStream "publish"
        let accCommands = [| CommandReader.CmdRule_publish |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Publish ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "publish 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "publish /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "publish /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Publish_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_publish |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Nop_001() =
        let ms, ws, rs = GenCommandStream "nop"
        let accCommands = [| CommandReader.CmdRule_nop |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Nop ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "nop 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "nop /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "nop /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Nop_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_nop |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.StatusAll_001() =
        let ms, ws, rs = GenCommandStream "statusall"
        let accCommands = [| CommandReader.CmdRule_statusall |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.StatusAll ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "statusall 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "statusall /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "statusall /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.StatusAll_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_statusall |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetDevice_001() =
        let ms, ws, rs = GenCommandStream "create"
        let accCommands = [| CommandReader.CmdRule_create_TargetDevice |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_TargetDevice ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetDevice_002() =
        let ms, ws, rs = GenCommandStream "create /n a"
        let accCommands = [| CommandReader.CmdRule_create_TargetDevice |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_TargetDevice ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/n"] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetDevice_003() =
        let astr = String.replicate Constants.MAX_DEVICE_NAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( "create /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_TargetDevice |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_TargetDevice ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/n"] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetDevice_004() =
        let astr = String.replicate ( Constants.MAX_DEVICE_NAME_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( "create /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_TargetDevice |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create /n", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create /n a 0", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Create_TargetDevice_005 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_TargetDevice |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Status_001() =
        let ms, ws, rs = GenCommandStream "status"
        let accCommands = [| CommandReader.CmdRule_status |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Status ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "status 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "status /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "status /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Status_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_status |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Delete_001() =
        let ms, ws, rs = GenCommandStream "delete"
        let accCommands = [| CommandReader.CmdRule_delete |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Delete ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Delete_002() =
        let ms, ws, rs = GenCommandStream "delete /i 0"
        let accCommands = [| CommandReader.CmdRule_delete |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Delete ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/i" ]= EV_uint32( 0u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Delete_003() =
        let ms, ws, rs = GenCommandStream ( sprintf "delete /i %d" ( ClientConst.MAX_CHILD_NODE_COUNT - 1 ) )
        let accCommands = [| CommandReader.CmdRule_delete |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Delete ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/i" ]= EV_uint32( uint ClientConst.MAX_CHILD_NODE_COUNT - 1u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Delete_004() =
        let ms, ws, rs = GenCommandStream ( sprintf "delete /i %d" ClientConst.MAX_CHILD_NODE_COUNT )
        let accCommands = [| CommandReader.CmdRule_delete |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "delete 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "delete /i", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "delete /i 0 1", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "delete /i -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "delete /u", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Delete_005 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_delete |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Start_001() =
        let ms, ws, rs = GenCommandStream "start"
        let accCommands = [| CommandReader.CmdRule_start |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Start ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "start 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "start /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "start /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Start_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_start |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Kill_001() =
        let ms, ws, rs = GenCommandStream "kill"
        let accCommands = [| CommandReader.CmdRule_kill |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Kill ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "kill 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "kill /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "kill /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Kill_005 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_kill |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_001() =
        let ms, ws, rs = GenCommandStream "setlogparam"
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_002() =
        let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /s %d" Constants.LOGPARAM_MIN_SOFTLIMIT )
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/s" ] = EV_uint32( Constants.LOGPARAM_MIN_SOFTLIMIT ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_003() =
        let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /s %d" Constants.LOGPARAM_MAX_SOFTLIMIT )
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/s" ] = EV_uint32( Constants.LOGPARAM_MAX_SOFTLIMIT ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_004() =
        if Constants.LOGPARAM_MIN_SOFTLIMIT > 0u then
            let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /s %d" ( Constants.LOGPARAM_MIN_SOFTLIMIT - 1u ) )
            let accCommands = [| CommandReader.CmdRule_setlogparam |]
            RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
            GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_005() =
        if Constants.LOGPARAM_MAX_SOFTLIMIT < UInt32.MaxValue then
            let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /s %d" ( Constants.LOGPARAM_MAX_SOFTLIMIT + 1u ) )
            let accCommands = [| CommandReader.CmdRule_setlogparam |]
            RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
            GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_006() =
        let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /h %d" Constants.LOGPARAM_MIN_HARDLIMIT )
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/h" ] = EV_uint32( Constants.LOGPARAM_MIN_HARDLIMIT ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_007() =
        let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /h %d" Constants.LOGPARAM_MAX_HARDLIMIT )
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/h" ] = EV_uint32( Constants.LOGPARAM_MAX_HARDLIMIT ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_008() =
        if Constants.LOGPARAM_MIN_HARDLIMIT > 0u then
            let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /h %d" ( Constants.LOGPARAM_MIN_HARDLIMIT - 1u ) )
            let accCommands = [| CommandReader.CmdRule_setlogparam |]
            RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
            GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_009() =
        if Constants.LOGPARAM_MAX_HARDLIMIT < UInt32.MaxValue then
            let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /h %d" ( Constants.LOGPARAM_MAX_HARDLIMIT + 1u ) )
            let accCommands = [| CommandReader.CmdRule_setlogparam |]
            RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
            GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_010() =
        for li in LogLevel.Values do
            let ms, ws, rs = GenCommandStream ( sprintf "setlogparam /l %s" ( LogLevel.toString li ) )
            let accCommands = [| CommandReader.CmdRule_setlogparam |]
            let r = RunInputCommandMethod rs accCommands
            Assert.True(( r.Varb = CommandVarb.SetLogParam ))
            Assert.True(( r.NamelessArgs.Length = 0 ))
            Assert.True(( r.NamedArgs.Count = 1 ))
            Assert.True(( r.NamedArgs.[ "/l" ] = EV_String( LogLevel.toString li ) ))
            GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetLogParam_011() =
        let param = sprintf "setlogparam /s %d /h %d /l %s"
                        Constants.LOGPARAM_DEF_SOFTLIMIT
                        Constants.LOGPARAM_DEF_HARDLIMIT
                        ( LogLevel.toString LogLevel.LOGLEVEL_INFO )
        let ms, ws, rs = GenCommandStream param
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/s" ] = EV_uint32( Constants.LOGPARAM_DEF_SOFTLIMIT ) ))
        Assert.True(( r.NamedArgs.[ "/h" ] = EV_uint32( Constants.LOGPARAM_DEF_HARDLIMIT ) ))
        Assert.True(( r.NamedArgs.[ "/l" ] = EV_String( LogLevel.toString LogLevel.LOGLEVEL_INFO ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "setlogparam 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "setlogparam /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "setlogparam /s", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setlogparam /s aaa", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "setlogparam /h", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setlogparam /h aaa", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "setlogparam /l", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setlogparam /l aaa", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.SetLogParam_012 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_setlogparam |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.GetLogParam_001() =
        let ms, ws, rs = GenCommandStream "getlogparam"
        let accCommands = [| CommandReader.CmdRule_getlogparam |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.GetLogParam ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "getlogparam 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "getlogparam /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "getlogparam /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.GetLogParam_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_getlogparam |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_001() =
        let ms, ws, rs = GenCommandStream "create networkportal"
        let accCommands = [| CommandReader.CmdRule_addportal |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_NetworkPortal ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_002() =
        let ms, ws, rs = GenCommandStream "create networkportal /a a"
        let accCommands = [| CommandReader.CmdRule_addportal |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_NetworkPortal ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_003() =
        let astr = String.replicate Constants.MAX_TARGET_ADDRESS_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( "create networkportal /a " + astr )
        let accCommands = [| CommandReader.CmdRule_addportal |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_NetworkPortal ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_004() =
        let ms, ws, rs = GenCommandStream "create networkportal /p 1"
        let accCommands = [| CommandReader.CmdRule_addportal |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_NetworkPortal ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_005() =
        let ms, ws, rs = GenCommandStream "create networkportal /p 65535"
        let accCommands = [| CommandReader.CmdRule_addportal |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_NetworkPortal ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/p" ] = EV_uint32( 65535u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_006() =
        let ms, ws, rs = GenCommandStream "create networkportal /a aaa /p 1000"
        let accCommands = [| CommandReader.CmdRule_addportal |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_NetworkPortal ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( "aaa" ) ))
        Assert.True(( r.NamedArgs.[ "/p" ] = EV_uint32( 1000u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddPortal_007() =
        let astr = String.replicate ( Constants.MAX_TARGET_ADDRESS_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( "create networkportal /a " + astr )
        let accCommands = [| CommandReader.CmdRule_addportal |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create networkportal 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create networkportal /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create networkportal /a", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create networkportal /p", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create networkportal /p aaa", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "create networkportal /p 0", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "create networkportal /p 65536", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.AddPortal_008 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        use outs = new StreamWriter( new MemoryStream() )
        let st = new StringTable( "" )
        let accCommands = [| CommandReader.CmdRule_addportal |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetGroup_001() =
        let ms, ws, rs = GenCommandStream "create targetgroup"
        let accCommands = [| CommandReader.CmdRule_create_TargetGroup |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_TargetGroup ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetGroup_002() =
        let ms, ws, rs = GenCommandStream "create targetgroup /n a"
        let accCommands = [| CommandReader.CmdRule_create_TargetGroup |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_TargetGroup ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetGroup_003() =
        let astr = String.replicate Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( "create targetgroup /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_TargetGroup |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_TargetGroup ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_TargetGroup_004() =
        let astr = String.replicate ( Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( "create targetgroup /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_TargetGroup |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create targetgroup 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create targetgroup /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create targetgroup /n", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create targetgroup /n 0 1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Create_TargetGroup_005 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_TargetGroup |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Add_IPWhiteList_001() =
        let ms, ws, rs = GenCommandStream "add IPWhiteList"
        let accCommands = [| CommandReader.CmdRule_add_IPWhiteList |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_IPWhiteList ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "/fadr" )>]
    [<InlineData( "/fmask" )>]
    [<InlineData( "/t" )>]
    member _.Add_IPWhiteList_002 ( testarg : string ) =
        let astr = String.replicate 48 "a"
        let ms, ws, rs = GenCommandStream ( "add IPWhiteList " + testarg + " " + astr )
        let accCommands = [| CommandReader.CmdRule_add_IPWhiteList |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_IPWhiteList ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ testarg ] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "/fadr" )>]
    [<InlineData( "/fmask" )>]
    [<InlineData( "/t" )>]
    member _.Add_IPWhiteList_003 ( testarg : string ) =
        let astr = String.replicate 49 "a"
        let ms, ws, rs = GenCommandStream ( "add IPWhiteList " + testarg + " " + astr )
        let accCommands = [| CommandReader.CmdRule_add_IPWhiteList |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "add IPWhiteList 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "add IPWhiteList /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "add IPWhiteList /fadr", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add IPWhiteList /fmask", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add IPWhiteList /t", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add IPWhiteList /fadr a b", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "add IPWhiteList /fmask a b", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "add IPWhiteList /t a b", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Add_IPWhiteList_004 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_add_IPWhiteList |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Clear_IPWhiteList_001() =
        let ms, ws, rs = GenCommandStream "clear IPWhiteList"
        let accCommands = [| CommandReader.CmdRule_clear_IPWhiteList |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Clear_IPWhiteList ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "clear IPWhiteList 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "clear IPWhiteList /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "clear IPWhiteList /x 0", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Clear_IPWhiteList_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_clear_IPWhiteList |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Load_001() =
        let ms, ws, rs = GenCommandStream "load"
        let accCommands = [| CommandReader.CmdRule_load |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Load ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "load 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "load /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "load /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Load_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_load |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.UnLoad_001() =
        let ms, ws, rs = GenCommandStream "unload"
        let accCommands = [| CommandReader.CmdRule_unload |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.UnLoad ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "unload 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "unload /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "unload /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.UnLoad_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_unload |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Activate_001() =
        let ms, ws, rs = GenCommandStream "activate"
        let accCommands = [| CommandReader.CmdRule_activate |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Activate ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "activate 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "activate /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "activate /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Activate_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_activate |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Inactivate_001() =
        let ms, ws, rs = GenCommandStream "inactivate"
        let accCommands = [| CommandReader.CmdRule_inactivate |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Inactivate ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "inactivate 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "inactivate /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "inactivate /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Inactivate_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_inactivate |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Target_001() =
        let ms, ws, rs = GenCommandStream "create"
        let accCommands = [| CommandReader.CmdRule_create_Target |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_Target ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Target_002() =
        let ms, ws, rs = GenCommandStream "create /n a"
        let accCommands = [| CommandReader.CmdRule_create_Target |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_Target ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Target_003() =
        let astr = String.replicate Constants.ISCSI_TEXT_MAX_ISCSI_NAME_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( "create /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_Target |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_Target ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Target_004() =
        let astr = String.replicate ( Constants.ISCSI_TEXT_MAX_ISCSI_NAME_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( "create /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_Target |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Target_005() =
        let ms, ws, rs = GenCommandStream "create /n ***"
        let accCommands = [| CommandReader.CmdRule_create_Target |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create /n", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create /n 0 1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Create_Target_006 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_Target |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_001() =
        let astr = String.replicate Constants.MAX_USER_NAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( sprintf "setchap /iu %s /ip a /tu a /tp a" astr )
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 4 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( astr ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/tu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/tp" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_002() =
        let astr = String.replicate Constants.MAX_USER_NAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( sprintf "setchap /iu a /ip a /tu %s /tp a" astr )
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 4 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/tu" ] = EV_String( astr ) ))
        Assert.True(( r.NamedArgs.[ "/tp" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "/ip a /tu a /tp a /iu" )>]
    [<InlineData( "/iu a /ip a /tp a /tu" )>]
    member _.SetChap_003 ( wsname : string ) =
        let astr = String.replicate ( Constants.MAX_USER_NAME_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( sprintf "setchap %s %s" wsname astr )
        let accCommands = [| CommandReader.CmdRule_setchap |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_004() =
        let astr = String.replicate Constants.MAX_PASSWORD_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( sprintf "setchap /iu a /ip %s /tu a /tp a" astr )
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 4 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( astr ) ))
        Assert.True(( r.NamedArgs.[ "/tu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/tp" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_005() =
        let astr = String.replicate Constants.MAX_PASSWORD_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( sprintf "setchap /iu a /ip a /tu a /tp %s" astr )
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 4 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/tu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/tp" ] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]


    [<Theory>]
    [<InlineData( "/iu a /tu a /tp a /ip" )>]
    [<InlineData( "/iu a /ip a /tu a /tp" )>]
    member _.SetChap_006 ( wsname : string ) =
        let astr = String.replicate ( Constants.MAX_PASSWORD_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( sprintf "setchap %s %s" wsname astr )
        let accCommands = [| CommandReader.CmdRule_setchap |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_007() =
        let ms, ws, rs = GenCommandStream "setchap /iu a /ip b /tu c /tp d"
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 4 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( "b" ) ))
        Assert.True(( r.NamedArgs.[ "/tu" ] = EV_String( "c" ) ))
        Assert.True(( r.NamedArgs.[ "/tp" ] = EV_String( "d" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_008() =
        let ms, ws, rs = GenCommandStream "setchap /iu a /ip b /tu c"
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( "b" ) ))
        Assert.True(( r.NamedArgs.[ "/tu" ] = EV_String( "c" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SetChap_009() =
        let ms, ws, rs = GenCommandStream "setchap /iu a /ip b /tp d"
        let accCommands = [| CommandReader.CmdRule_setchap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SetChap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/iu" ] = EV_String( "a" ) ))
        Assert.True(( r.NamedArgs.[ "/ip" ] = EV_String( "b" ) ))
        Assert.True(( r.NamedArgs.[ "/tp" ] = EV_String( "d" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "setchap 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "setchap /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "setchap /tu a /tp a /ip a /iu", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setchap /tu a /tp a /ip a /iu ***", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "setchap /tu a /tp a /iu a /ip", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setchap /tu a /tp a /iu a /ip ***", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "setchap /ip a /iu a /tp a /tu", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setchap /ip a /iu a /tp a /tu ***", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "setchap /ip a /iu a /tu a /tp", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "setchap /ip a /iu a /tu a /tp ***", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "setchap /ip a /tu a /tp a", "CMDERR_MISSING_MANDATORY_ARG" )>]
    [<InlineData( "setchap /iu a /tu a /tp a", "CMDERR_MISSING_MANDATORY_ARG" )>]
    [<InlineData( "setchap", "CMDERR_MISSING_MANDATORY_ARG" )>]
    member _.SetChap_010 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_setchap |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.UnsetAuth_001() =
        let ms, ws, rs = GenCommandStream "unsetauth"
        let accCommands = [| CommandReader.CmdRule_unsetauth |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.UnsetAuth ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "unsetauth 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "unsetauth /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "unsetauth /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.UnsetAuth_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_unsetauth |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_LU_001() =
        let ms, ws, rs = GenCommandStream "create"
        let accCommands = [| CommandReader.CmdRule_create_LU |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_LU ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_LU_002() =
        let ms, ws, rs = GenCommandStream "create /l 0"
        let accCommands = [| CommandReader.CmdRule_create_LU |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_LU ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/l" ] = EV_LUN( lun_me.fromPrim 0UL ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_LU_003() =
        let ms, ws, rs = GenCommandStream "create /n a"
        let accCommands = [| CommandReader.CmdRule_create_LU |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_LU ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( "a" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_LU_004() =
        let astr = String.replicate Constants.MAX_LU_NAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( "create /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_LU |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_LU ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/n" ] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_LU_005() =
        let astr = String.replicate ( Constants.MAX_LU_NAME_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( "create /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_LU |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create /l", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create /l ***", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "create /n", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    member _.Create_LU_006 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_LU |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Attach_001() =
        let ms, ws, rs = GenCommandStream "attach"
        let accCommands = [| CommandReader.CmdRule_attach |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Attach ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Attach_002() =
        let ms, ws, rs = GenCommandStream "attach /l 0"
        let accCommands = [| CommandReader.CmdRule_attach |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Attach ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/l" ] = EV_LUN( lun_me.fromPrim 0UL ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "attach 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "attach /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "attach /l", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "attach /l ***", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.Attach_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_attach |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Detach_001() =
        let ms, ws, rs = GenCommandStream "detach"
        let accCommands = [| CommandReader.CmdRule_detach |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Detach ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Detach_002() =
        let ms, ws, rs = GenCommandStream "detach /l 0"
        let accCommands = [| CommandReader.CmdRule_detach |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Detach ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.[ "/l" ] = EV_LUN( lun_me.fromPrim 0UL ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "detach 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "detach /x", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "detach /l", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "detach /l ***", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.Detach_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_detach |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Media_PlainFile_002() =
        let astr = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( "create plainfile /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_Media_PlainFile |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_Media_PlainFile ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/n"] = EV_String( astr ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Media_PlainFile_003() =
        let astr = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( "create plainfile /n " + astr )
        let accCommands = [| CommandReader.CmdRule_create_Media_PlainFile |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create plainfile", "CMDERR_MISSING_MANDATORY_ARG" )>]
    [<InlineData( "create plainfile a", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create plainfile 0 1", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create plainfile /n", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    member _.Create_Media_PlainFile_004 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_Media_PlainFile |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "0", 0UL )>]
    [<InlineData( "18446744073709551615", 18446744073709551615UL )>]
    member _.Create_Media_MemBuffer_001 ( cmdstr : string ) ( iv : uint64 ) =
        let ms, ws, rs = GenCommandStream ( "create membuffer /s " + cmdstr )
        let accCommands = [| CommandReader.CmdRule_create_Media_MemBuffer |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_Media_MemBuffer ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/s"] = EV_uint64( iv ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create membuffer", "CMDERR_MISSING_MANDATORY_ARG" )>]
    [<InlineData( "create membuffer a", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create membuffer 0 1", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create membuffer /s", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "create membuffer /s ***", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "create membuffer /s -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "create membuffer /s 18446744073709551616", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.Create_Media_MemBuffer_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_Media_MemBuffer |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_Media_Debug_001 () =
        let ms, ws, rs = GenCommandStream ( "create debug" )
        let accCommands = [| CommandReader.CmdRule_create_Media_Debug |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Create_Media_Debug ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "create debug a", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create debug /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "create debug /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Create_Media_Debug_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_create_Media_Debug |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_initmedia_PlainFile_001() =
        let fname = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a"
        let ms, ws, rs = GenCommandStream ( sprintf "initmedia plainfile %s 1" fname )
        let accCommands = [| CommandReader.CmdRule_initmedia_PlainFile |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.InitMedia_PlainFile ))
        Assert.True(( r.NamelessArgs.Length = 2 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( fname ) ))
        Assert.True(( r.NamelessArgs.[1] = EV_int64( 1L ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_initmedia_PlainFile_002() =
        let fname = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
        let ms, ws, rs = GenCommandStream ( sprintf "initmedia plainfile %s 1" fname )
        let accCommands = [| CommandReader.CmdRule_initmedia_PlainFile |]
        RunInputCommandMethod_CommandInputError rs accCommands "CMDERR_INVALID_ARG_VALUE"
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Create_initmedia_PlainFile_003() =
        let ms, ws, rs = GenCommandStream "initmedia plainfile a 9223372036854775807"
        let accCommands = [| CommandReader.CmdRule_initmedia_PlainFile |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.InitMedia_PlainFile ))
        Assert.True(( r.NamelessArgs.Length = 2 ))
        Assert.True(( r.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( r.NamelessArgs.[1] = EV_int64( 9223372036854775807L ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "initmedia plainfile", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "initmedia plainfile a", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "initmedia plainfile a 1 2", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "initmedia plainfile a 0", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "initmedia plainfile a 9223372036854775808", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "initmedia plainfile a b", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.Create_initmedia_PlainFile_004 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_initmedia_PlainFile |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.IMStatus_001() =
        let ms, ws, rs = GenCommandStream "imstatus"
        let accCommands = [| CommandReader.CmdRule_imstatus |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.IMStatus ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]
        
    [<Theory>]
    [<InlineData( "imstatus 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "imstatus /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "imstatus /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.IMStatus_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_imstatus |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.IMKill_001() =
        let ms, ws, rs = GenCommandStream "imkill 0"
        let accCommands = [| CommandReader.CmdRule_imkill |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.IMKill ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint64( 0UL ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.IMKill_002() =
        let ms, ws, rs = GenCommandStream "imkill 18446744073709551615"
        let accCommands = [| CommandReader.CmdRule_imkill |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.IMKill ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint64( 18446744073709551615UL ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "imkill", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "imkill 1 2", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "imkill -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "imkill 18446744073709551616", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "imkill /f", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.IMKill_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_imkill |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Sessions_001() =
        let ms, ws, rs = GenCommandStream "sessions"
        let accCommands = [| CommandReader.CmdRule_sessions |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Sessions ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "sessions 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "sessions /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "sessions /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Sessions_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_sessions |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SessKill_001() =
        let ms, ws, rs = GenCommandStream "sesskill 0"
        let accCommands = [| CommandReader.CmdRule_sesskill |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SessKill ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint32( 0u ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.SessKill_002() =
        let ms, ws, rs = GenCommandStream "sesskill 65535"
        let accCommands = [| CommandReader.CmdRule_sesskill |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.SessKill ))
        Assert.True(( r.NamelessArgs.Length = 1 ))
        Assert.True(( r.NamelessArgs.[0] = EV_uint32( 65535u ) ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "sesskill", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "sesskill 1 2", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "sesskill -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "sesskill 65536", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "sesskill /f", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.SessKill_003 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_sesskill |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Connections_001() =
        let ms, ws, rs = GenCommandStream "connections"
        let accCommands = [| CommandReader.CmdRule_connections |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Connections ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Connections_002() =
        let ms, ws, rs = GenCommandStream "connections /s 0"
        let accCommands = [| CommandReader.CmdRule_connections |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Connections ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/s"] = EV_uint32( 0u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Connections_003() =
        let ms, ws, rs = GenCommandStream "connections /s 65535"
        let accCommands = [| CommandReader.CmdRule_connections |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Connections ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 1 ))
        Assert.True(( r.NamedArgs.["/s"] = EV_uint32( 65535u ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "connections 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "connections /s", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "connections /s -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "connections /s 65536", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "connections /f", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Connections_004 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_connections |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.LUStatus_001() =
        let ms, ws, rs = GenCommandStream "lustatus"
        let accCommands = [| CommandReader.CmdRule_lustatus |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.LUStatus ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "lustatus 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "lustatus /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "lustatus /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.LUStatus_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_lustatus |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.LUReset_001() =
        let ms, ws, rs = GenCommandStream "lureset"
        let accCommands = [| CommandReader.CmdRule_lureset |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.LUReset ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "lureset 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "lureset /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "lureset /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.LUReset_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_lureset |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.MediaStatus_001() =
        let ms, ws, rs = GenCommandStream "mediastatus"
        let accCommands = [| CommandReader.CmdRule_mediastatus |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.MediaStatus ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "mediastatus 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "mediastatus /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "mediastatus /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.MediaStatus_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_mediastatus |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "add trap /e TestUnitReady /a ACA", "TestUnitReady", "ACA" )>]
    [<InlineData( "add trap /e ReadCapacity /a LUReset", "ReadCapacity", "LUReset" )>]
    [<InlineData( "add trap /e Read /a Count", "Read", "Count" )>]
    [<InlineData( "add trap /e Write /a Delay", "Write", "Delay" )>]
    [<InlineData( "add trap /e Format /a Delay", "Format", "Delay" )>]
    member _.AddTrap_001 ( cmdstr : string ) ( eventResult : string ) ( actionResult : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_add_trap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_Trap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 2 ))
        Assert.True(( r.NamedArgs.[ "/e" ] = EV_String( eventResult ) ))
        Assert.True(( r.NamedArgs.[ "/a" ] = EV_String( actionResult ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "add trap /e Read /a Count /slba 0", "/slba", 0UL )>]
    [<InlineData( "add trap /e Read /a Count /slba 18446744073709551615", "/slba", 18446744073709551615UL )>]
    [<InlineData( "add trap /e Read /a Count /elba 0", "/elba", 0UL )>]
    [<InlineData( "add trap /e Read /a Count /elba 18446744073709551615", "/elba", 18446744073709551615UL )>]
    member _.AddTrap_004 ( cmdstr : string ) ( argname : string ) ( varg : uint64 ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_add_trap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_Trap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ argname ] = EV_uint64( varg ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddTrap_005() =
        let ms, ws, rs = GenCommandStream "add trap /e Read /a Count /msg c"
        let accCommands = [| CommandReader.CmdRule_add_trap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_Trap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/msg" ] = EV_String( "c" ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.AddTrap_006() =
        let td = String.replicate 256 "c"
        let ms, ws, rs = GenCommandStream ( sprintf "add trap /e Write /a Delay /msg %s" td )
        let accCommands = [| CommandReader.CmdRule_add_trap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_Trap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ "/msg" ] = EV_String( td ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "add trap /e TestUnitReady /a ACA /idx -2147483648", "/idx", -2147483648 )>]
    [<InlineData( "add trap /e TestUnitReady /a ACA /idx 2147483647", "/idx", 2147483647 )>]
    [<InlineData( "add trap /e TestUnitReady /a ACA /ms 0", "/ms", 0 )>]
    [<InlineData( "add trap /e TestUnitReady /a ACA /ms 2147483647", "/ms", 2147483647 )>]
    member _.AddTrap_007 ( cmdstr : string ) ( argname : string ) ( varg : int ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_add_trap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Add_Trap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 3 ))
        Assert.True(( r.NamedArgs.[ argname ] = EV_int32( varg ) ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "add trap 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "add trap /a", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e ReadCapacity /a", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /a ACA /e", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e eee /a ACA", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a aaa", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /slba", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /elba", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /msg", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /idx", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /ms", "CMDERR_LAST_ARG_VAL_MISSING" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /slba -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /slba 18446744073709551616", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /elba -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /elba 18446744073709551616", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /idx -2147483649", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /idx 2147483648", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /ms -1", "CMDERR_INVALID_ARG_VALUE" )>]
    [<InlineData( "add trap /e ReadCapacity /a ACA /ms 2147483648", "CMDERR_INVALID_ARG_VALUE" )>]
    member _.AddTrap_008 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_add_trap |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.ClearTrap_001() =
        let ms, ws, rs = GenCommandStream "clear trap"
        let accCommands = [| CommandReader.CmdRule_clear_trap |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Clear_Trap ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "clear trap 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "clear trap /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "clear trap /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.ClearTrap_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_clear_trap |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Fact>]
    member _.Traps_001() =
        let ms, ws, rs = GenCommandStream "traps"
        let accCommands = [| CommandReader.CmdRule_traps |]
        let r = RunInputCommandMethod rs accCommands
        Assert.True(( r.Varb = CommandVarb.Traps ))
        Assert.True(( r.NamelessArgs.Length = 0 ))
        Assert.True(( r.NamedArgs.Count = 0 ))
        GlbFunc.AllDispose [ ms; ws; rs; ]

    [<Theory>]
    [<InlineData( "traps 0", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "traps /p", "CMDERR_INVALID_ARG_COUNT" )>]
    [<InlineData( "traps /p -1", "CMDERR_INVALID_ARG_COUNT" )>]
    member _.Traps_002 ( cmdstr : string ) ( msgstr : string ) =
        let ms, ws, rs = GenCommandStream cmdstr
        let accCommands = [| CommandReader.CmdRule_traps |]
        RunInputCommandMethod_CommandInputError rs accCommands msgstr
        GlbFunc.AllDispose [ ms; ws; rs; ]
