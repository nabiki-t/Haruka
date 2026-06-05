//=============================================================================
// Haruka Software Storage.
// CmdArgsTest.fs : Test cases for CmdArgs class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Controller

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Controller

//=============================================================================
// Class implementation

type CmdArgs_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.CmdArgs_001() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize Array.empty |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.NoCommandString, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_002() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "aaa" |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.UnknownCommand "aaa", e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_SV_001() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "SV" |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgCount, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_SV_002() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "SV"; "a"; "b"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgCount, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_SV_003() =
        let path = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a"
        let cmd = CmdArgs.Recognize [| "SV"; path |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( path ) ))

    [<Fact>]
    member _.CmdArgs_SV_004() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                let path = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
                CmdArgs.Recognize [| "SV"; path; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.NamelessPatternMismatch, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_001() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "ID" |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgCount, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_002() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "ID"; "a"; "b"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgCount, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_003() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "ID"; "a"; "/p"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.LastArgValMissing, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_004() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "ID"; "a"; "/a"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.LastArgValMissing, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_005() =
        let path = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a"
        let cmd = CmdArgs.Recognize [| "ID"; path |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( path ) ))

    [<Fact>]
    member _.CmdArgs_ID_006() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                let path = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
                CmdArgs.Recognize [| "ID"; path; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.NamelessPatternMismatch, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_007() =
        let cmd = CmdArgs.Recognize [| "ID"; "a"; "/p"; "1" |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))

    [<Fact>]
    member _.CmdArgs_ID_008() =
        let cmd = CmdArgs.Recognize [| "ID"; "a"; "/p"; "65535" |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 65535u ) ))

    [<Theory>]
    [<InlineData( "-1" )>]
    [<InlineData( "0" )>]
    [<InlineData( "65536" )>]
    member _.CmdArgs_ID_009( v : string ) =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "ID"; "a"; "/p"; v; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgValue v, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_010() =
        let cmd = CmdArgs.Recognize [| "ID"; "a"; "/a"; "b" |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( "b" ) ))

    [<Fact>]
    member _.CmdArgs_ID_011() =
        let adr = String.replicate Constants.MAX_CTRL_ADDRESS_STR_LENGTH "b"
        let cmd = CmdArgs.Recognize [| "ID"; "a"; "/a"; adr |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 1 ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( adr ) ))

    [<Fact>]
    member _.CmdArgs_ID_012() =
        let adr = String.replicate ( Constants.MAX_CTRL_ADDRESS_STR_LENGTH + 1 ) "b"            
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                
                CmdArgs.Recognize [| "ID"; "a"; "/a"; adr; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgValue adr, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_ID_013() =
        let cmd = CmdArgs.Recognize [| "ID"; "a"; "/p"; "1"; "/a"; "b";|]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( "b" ) ))

    [<Fact>]
    member _.CmdArgs_ID_014() =
        let cmd = CmdArgs.Recognize [| "ID"; "a"; "/p"; "1"; "/a"; "b"; "/o"; |]
        Assert.True(( cmd.NamelessArgs.Length = 1 ))
        Assert.True(( cmd.NamelessArgs.[0] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.Count = 3 ))
        Assert.True(( cmd.NamedArgs.[ "/p" ] = EV_uint32( 1u ) ))
        Assert.True(( cmd.NamedArgs.[ "/a" ] = EV_String( "b" ) ))
        Assert.True(( cmd.NamedArgs.[ "/o" ] = EV_NoValue ))

    [<Fact>]
    member _.CmdArgs_IM_001() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM" |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.UnknownCommand "IM", e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_002() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "A" |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.UnknownCommand "IM", e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_001() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "PLAINFILE" |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.MissingMandatoryArg, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_002() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "PLAINFILE"; "a"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgCount, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_003() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.LastArgValMissing, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_004() =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/s"; "1"; "/f"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.LastArgValMissing, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_005() =
        let fname = String.replicate Constants.MAX_FILENAME_STR_LENGTH "b"
        let cmd = CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; fname; "/s"; "1" |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( fname ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( 1L ) ))

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_006() =
        let fname = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "b"
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; fname; "/s"; "1"; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgValue fname, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_007() =
        let cmd = CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; "1" |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( 1L ) ))

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_008() =
        let cmd = CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; sprintf "%d" Int64.MaxValue |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 2 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( Int64.MaxValue ) ))

    [<Theory>]
    [<InlineData( "-1" )>]
    [<InlineData( "0" )>]
    [<InlineData( "9223372036854775808" )>]
    member _.CmdArgs_IM_PLAINFILE_009 ( v : string ) =
        let e =
            Assert.Throws< CommandInputError > ( fun () ->
                CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; v; |] |> ignore
            )
        Assert.StrictEqual( CIE_ErrorCode.InvalidArgValue v, e.ErrorCode )

    [<Fact>]
    member _.CmdArgs_IM_PLAINFILE_010() =
        let cmd = CmdArgs.Recognize [| "IM"; "PLAINFILE"; "/f"; "a"; "/s"; "1"; "/x" |]
        Assert.True(( cmd.NamelessArgs.Length = 0 ))
        Assert.True(( cmd.NamedArgs.Count = 3 ))
        Assert.True(( cmd.NamedArgs.[ "/f" ] = EV_String( "a" ) ))
        Assert.True(( cmd.NamedArgs.[ "/s" ] = EV_int64( 1L ) ))
        Assert.True(( cmd.NamedArgs.[ "/x" ] = EV_NoValue ))
