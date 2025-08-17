namespace Haruka.Test.UT.Controller

open System
open System.IO
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

type CmdArgs_Test () =

    [<Fact>]
    member _.CmdArgs_001() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st Array.empty
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "No command strings" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_002() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "aaa" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_SV_001() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "SV" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_SV_002() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "SV"; "a"; "b"; |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_SV_003() =
        let st = new StringTable( "" )
        let path = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a"
        let cmd = CmdArgs.Recognize st [| "SV"; path |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( path ) ))

    [<Fact>]
    member _.CmdArgs_SV_004() =
        let st = new StringTable( "" )
        try
            let path = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
            let _ = CmdArgs.Recognize st [| "SV"; path; |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_001() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "ID" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_002() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "ID"; "a"; "b"; |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_003() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "ID"; "a"; "/p" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_LAST_ARG_VAL_MISSING" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_004() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "ID"; "a"; "/a" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_LAST_ARG_VAL_MISSING" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_005() =
        let st = new StringTable( "" )
        let path = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a"
        let cmd = CmdArgs.Recognize st [| "ID"; path |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( path ) ))

    [<Fact>]
    member _.CmdArgs_ID_006() =
        let st = new StringTable( "" )
        try
            let path = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
            let _ = CmdArgs.Recognize st [| "ID"; path; |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_007() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "ID"; "a"; "/p"; "1" |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))

    [<Fact>]
    member _.CmdArgs_ID_008() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "ID"; "a"; "/p"; "65535" |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 65535u ) ))

    [<Theory>]
    [<InlineData( "-1" )>]
    [<InlineData( "0" )>]
    [<InlineData( "65536" )>]
    member _.CmdArgs_ID_009( v : string ) =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "ID"; "a"; "/p"; v;|]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_010() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "ID"; "a"; "/a"; "b" |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( "b" ) ))

    [<Fact>]
    member _.CmdArgs_ID_011() =
        let st = new StringTable( "" )
        let adr = String.replicate Constants.MAX_CTRL_ADDRESS_STR_LENGTH "b"
        let cmd = CmdArgs.Recognize st [| "ID"; "a"; "/a"; adr |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( adr ) ))

    [<Fact>]
    member _.CmdArgs_ID_012() =
        let st = new StringTable( "" )
        try
            let adr = String.replicate ( Constants.MAX_CTRL_ADDRESS_STR_LENGTH + 1 ) "b"
            let _ = CmdArgs.Recognize st [| "ID"; "a"; "/a"; adr;|]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_ID_013() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "ID"; "a"; "/p"; "1"; "/a"; "b";|]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( "b" ) ))

    [<Fact>]
    member _.CmdArgs_ID_014() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "ID"; "a"; "/p"; "1"; "/a"; "b"; "/o"; |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 3 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( "b" ) ))
        Assert.True(( cmd.NamedArgs.[ "/o" ] = EV_NoValue ))

    [<Fact>]
    member _.CmdArgs_IM_001() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_002() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM"; "A" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_UNKNOWN_COMMAND" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_001() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM"; "PLAINFILE" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_MISSING_MANDATORY_ARG" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_002() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "a"; |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_COUNT" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_003() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; "a"; "/s" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_LAST_ARG_VAL_MISSING" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_004() =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/s"; "1"; "/f" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_LAST_ARG_VAL_MISSING" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_005() =
        let st = new StringTable( "" )
        let fname = String.replicate Constants.MAX_FILENAME_STR_LENGTH "b"
        let cmd = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; fname; "/s"; "1" |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( fname ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( 1L ) ))

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_006() =
        let st = new StringTable( "" )
        try
            let fname = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "b"
            let _ = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; fname; "/s"; "1" |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_007() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; "1" |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( 1L ) ))

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_008() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; sprintf "%d" Int64.MaxValue |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( Int64.MaxValue ) ))

    [<Theory>]
    [<InlineData( "-1" )>]
    [<InlineData( "0" )>]
    [<InlineData( "9223372036854775808" )>]
    member _.CmdArgs_IM_PLAINFILE_009 ( v : string ) =
        let st = new StringTable( "" )
        try
            let _ = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; v |]
            Assert.Fail __LINE__
        with
        | :? CommandInputError as x ->
            Assert.True(( x.Message.StartsWith "CMDERR_INVALID_ARG_VALUE" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_010() =
        let st = new StringTable( "" )
        let cmd = CmdArgs.Recognize st [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; "1"; "/x" |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 3 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( 1L ) ))
        Assert.True(( cmd.NamedArgs.[ "/x" ] = EV_NoValue ))


