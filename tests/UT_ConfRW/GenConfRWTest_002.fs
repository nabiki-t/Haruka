//=============================================================================
// Haruka Software Storage.
// GenConfRWTest_002.fs : Test cases for GenConfRW module.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.GenConfRW

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test.UT

//=============================================================================
// Class implementation

type GenConfRW_Test_002 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    member _.GenTestFName ( s : string ) : string =
        Functions.AppendPathName ( Path.GetTempPath() ) s

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R019_1></R019_1></Test>" )>]
    [<InlineData( "<Test><R019_1><D1>0</D1></R019_1></Test>" )>]
    [<InlineData( "<Test><R019_1><D2>0</D2></R019_1></Test>" )>]
    [<InlineData( "<Test><R019_1><D1>0</D1><D2>0</D2><D3>0</D3></R019_1></Test>" )>]
    [<InlineData( "<Test><R019_1><D1>0</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2></R019_1></Test>" )>]
    [<InlineData( "<Test><R019_1><D1>0</D1><D2>0</D2></R019_1><R019_1><D1>1</D1><D2>1</D2></R019_1></Test>" )>]
    member _.Record_001 ( ts : string ) =
        try
            ConfRW_003_001.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Record_002 () =
        let s = [|
            "<Test><R019_1><D1>0</D1><D2>1</D2></R019_1></Test>";
            "<Test><R019_1><D1>2</D1><D2>3</D2><D2>4</D2></R019_1></Test>";
            "<Test><R019_1><D1>5</D1><D2>6</D2><D2>7</D2><D2>8</D2></R019_1></Test>";
        |]
        let exr = [|
            { ConfRW_003_001.R019_1 = { D1 = 0; D2 = [ 1; ] }; };
            { ConfRW_003_001.R019_1 = { D1 = 2; D2 = [ 3; 4; ] }; };
            { ConfRW_003_001.R019_1 = { D1 = 5; D2 = [ 6; 7; 8; ] }; };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_001.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R020_1></R020_1></Test>" )>]
    [<InlineData( "<Test><R020_1><D1>0</D1></R020_1></Test>" )>]
    [<InlineData( "<Test><R020_1><D1>0</D1><D2>0</D2><D3>0</D3></R020_1></Test>" )>]
    [<InlineData( "<Test><R020_1><D1>0</D1><D1>0</D1></R020_1></Test>" )>]
    [<InlineData( "<Test><R020_1><D1>0</D1><D2>0</D2></R020_1><R020_1><D1>0</D1><D2>0</D2></R020_1><R020_1><D1>0</D1><D2>0</D2></R020_1><R020_1><D1>0</D1><D2>0</D2></R020_1></Test>" )>]
    member _.Record_003 ( ts : string ) =
        try
            ConfRW_003_002.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Record_004 () =
        let s = [|
            "<Test><R020_1><D1>0</D1><D2>0</D2></R020_1></Test>"
            "<Test><R020_1><D1>1</D1><D2>1</D2></R020_1><R020_1><D1>2</D1><D2>2</D2></R020_1></Test>"
            "<Test><R020_1><D1>3</D1><D2>3</D2></R020_1><R020_1><D1>4</D1><D2>4</D2></R020_1><R020_1><D1>5</D1><D2>5</D2></R020_1></Test>"
        |]
        let exr = [|
            { ConfRW_003_002.R020_1 = [ { D1 = 0; D2 = 0; }; ] };
            { ConfRW_003_002.R020_1 = [ { D1 = 1; D2 = 1; }; { D1 = 2; D2 = 2; }; ] };
            { ConfRW_003_002.R020_1 = [ { D1 = 3; D2 = 3; }; { D1 = 4; D2 = 4; }; { D1 = 5; D2 = 5; }; ] };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_002.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R021_1></R021_1></Test>" )>]
    [<InlineData( "<Test><R021_1><D1>0</D1></R021_1></Test>" )>]
    [<InlineData( "<Test><R021_1><D2>0</D2></R021_1></Test>" )>]
    [<InlineData( "<Test><R021_1><D1>0</D1><D2>0</D2><D3>0</D3></R021_1></Test>" )>]
    [<InlineData( "<Test><R021_1><D1>0</D1><D2>0</D2></R021_1><R021_1><D1>0</D1><D2>0</D2></R021_1></Test>" )>]
    member _.Record_005 ( ts : string ) =
        try
            ConfRW_003_003.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Record_006 () =
        let s = [|
            "<Test></Test>"
            "<Test><R021_1><D1>0</D1><D2>0</D2></R021_1></Test>"
        |]
        let exr = [|
            { ConfRW_003_003.R021_1 = None };
            { ConfRW_003_003.R021_1 = Some( { D1 = 0; D2 = 0; } ) };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_003.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><R022_1></R022_1></Test>" )>]
    [<InlineData( "<Test><R022_1><D1>0</D1></R022_1></Test>" )>]
    [<InlineData( "<Test><R022_1><D1>0</D1><D2>0</D2></R022_1></Test>" )>]
    [<InlineData( "<Test><R022_1><D1>0</D1><R022_2></R022_2><D2>0</D2></R022_1></Test>" )>]
    [<InlineData( "<Test><R022_1><D1>0</D1><R022_2><D1_1>0</D1_1></R022_2><D2>0</D2></R022_1></Test>" )>]
    [<InlineData( "<Test><R022_1><D1>0</D1><R022_2><D1_2>0</D1_2></R022_2><D2>0</D2></R022_1></Test>" )>]
    [<InlineData( "<Test><R022_1><D1>0</D1><R022_2><D1_1>0</D1_1><D1_2>0</D1_2></R022_2><R022_2><D1_1>0</D1_1><D1_2>0</D1_2></R022_2><D2>0</D2></R022_1></Test>" )>]
    member _.Record_007 ( ts : string ) =
        try
            ConfRW_003_004.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Record_008 () =
        let s = [|
            "<Test><R022_1><D1>0</D1><R022_2><D1_1>1</D1_1><D1_2>2</D1_2></R022_2><D2>3</D2></R022_1></Test>"
        |]
        let exr = [|
            { ConfRW_003_004.R022_1 = { D1 = 0; R022_2 = { D1_1 = 1; D1_2 = 2; }; D2 = 3; } };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_004.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><R023_1></R023_1></Test>" )>]
    [<InlineData( "<Test><R023_1><D1>0</D1></R023_1></Test>" )>]
    [<InlineData( "<Test><R023_1><D1>0</D1><D2>0</D2></R023_1></Test>" )>]
    [<InlineData( "<Test><R023_1><D1>0</D1><R023_2></R023_2><D2>0</D2></R023_1></Test>" )>]
    [<InlineData( "<Test><R023_1><D1>0</D1><R023_2><D1_1>0</D1_1><D1_2>0</D1_2></R023_2><D2>0</D2></R023_1></Test>" )>]
    member _.Record_009 ( ts : string ) =
        try
            ConfRW_003_005.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Record_010 () =
        let s = [|
            "<Test><R023_1><D1>0</D1><R023_2><D1_1>1</D1_1></R023_2><D2>2</D2></R023_1></Test>"
            "<Test><R023_1><D1>3</D1><R023_2><D1_2>abc</D1_2></R023_2><D2>5</D2></R023_1></Test>"
        |]
        let exr = [|
            { ConfRW_003_005.R023_1 = { D1 = 0; R023_2 = ConfRW_003_005.T_R023_2.U_D1_1( 1 ); D2 = 2; } };
            { ConfRW_003_005.R023_1 = { D1 = 3; R023_2 = ConfRW_003_005.T_R023_2.U_D1_2( "abc" ); D2 = 5; } };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_005.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Record_011 () =
        let s = [|
            { ConfRW_003_001.R019_1 = { D1 = 0; D2 = [ 1; ] }; };
            { ConfRW_003_001.R019_1 = { D1 = 2; D2 = [ 3; 4; ] }; };
            { ConfRW_003_001.R019_1 = { D1 = 5; D2 = [ 6; 7; 8; ] }; };
        |]
        let exr = [|
            "<Test><R019_1><D1>0</D1><D2>1</D2></R019_1></Test>";
            "<Test><R019_1><D1>2</D1><D2>3</D2><D2>4</D2></R019_1></Test>";
            "<Test><R019_1><D1>5</D1><D2>6</D2><D2>7</D2><D2>8</D2></R019_1></Test>";
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_001.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Record_012 () =
        let s = [|
            { ConfRW_003_001.R019_1 = { D1 = 0; D2 = [] }; };
            { ConfRW_003_001.R019_1 = { D1 = 5; D2 = [ 6; 7; 8; 9; ] }; };
        |]
        for i = 0 to s.Length - 1 do
            try
                let r = ConfRW_003_001.ReaderWriter.ToString s.[i] 
                Assert.Fail __LINE__
            with
            | :? Xunit.Sdk.FailException -> reraise();
            | _->
                ()

    [<Fact>]
    member _.Record_013 () =
        let s = [|
            { ConfRW_003_002.R020_1 = [ { D1 = 0; D2 = 0; }; ] };
            { ConfRW_003_002.R020_1 = [ { D1 = 1; D2 = 1; }; { D1 = 2; D2 = 2; }; ] };
            { ConfRW_003_002.R020_1 = [ { D1 = 3; D2 = 3; }; { D1 = 4; D2 = 4; }; { D1 = 5; D2 = 5; }; ] };
        |]
        let exr = [|
            "<Test><R020_1><D1>0</D1><D2>0</D2></R020_1></Test>"
            "<Test><R020_1><D1>1</D1><D2>1</D2></R020_1><R020_1><D1>2</D1><D2>2</D2></R020_1></Test>"
            "<Test><R020_1><D1>3</D1><D2>3</D2></R020_1><R020_1><D1>4</D1><D2>4</D2></R020_1><R020_1><D1>5</D1><D2>5</D2></R020_1></Test>"
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_002.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Record_014 () =
        let s = [|
            { ConfRW_003_002.R020_1 = [] };
            { ConfRW_003_002.R020_1 = [ { D1 = 3; D2 = 3; }; { D1 = 4; D2 = 4; }; { D1 = 5; D2 = 5; }; { D1 = 5; D2 = 5; }; ] };
        |]
        for i = 0 to s.Length - 1 do
            try
                let _ = ConfRW_003_002.ReaderWriter.ToString s.[i] 
                Assert.Fail __LINE__
            with
            | :? ConfRWException as x ->
                Assert.True(( x.Message = "Element count restriction error. R020_1" ))
            | _->
                Assert.Fail __LINE__

    [<Fact>]
    member _.Record_015 () =
        let s = [|
            { ConfRW_003_003.R021_1 = None };
            { ConfRW_003_003.R021_1 = Some( { D1 = 0; D2 = 0; } ) };
        |]
        let exr = [|
            "<Test></Test>"
            "<Test><R021_1><D1>0</D1><D2>0</D2></R021_1></Test>"
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_003.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Record_016 () =
        let s = [|
            { ConfRW_003_004.R022_1 = { D1 = 0; R022_2 = { D1_1 = 1; D1_2 = 2; }; D2 = 3; } };
        |]
        let exr = [|
            "<Test><R022_1><D1>0</D1><R022_2><D1_1>1</D1_1><D1_2>2</D1_2></R022_2><D2>3</D2></R022_1></Test>"
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_004.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Record_017 () =
        let s = [|
            { ConfRW_003_005.R023_1 = { D1 = 0; R023_2 = ConfRW_003_005.T_R023_2.U_D1_1( 1 ); D2 = 2; } };
            { ConfRW_003_005.R023_1 = { D1 = 3; R023_2 = ConfRW_003_005.T_R023_2.U_D1_2( "abc" ); D2 = 5; } };
        |]
        let exr = [|
            "<Test><R023_1><D1>0</D1><R023_2><D1_1>1</D1_1></R023_2><D2>2</D2></R023_1></Test>"
            "<Test><R023_1><D1>3</D1><R023_2><D1_2>abc</D1_2></R023_2><D2>5</D2></R023_1></Test>"
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_005.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R024_1></R024_1></Test>" )>]
    [<InlineData( "<Test><R024_1><D1_1>0</D1_1><D1_2>aa</D1_2></R024_1></Test>" )>]
    [<InlineData( "<Test><R024_1><D1_1>0</D1_1><D1_1>0</D1_1></R024_1></Test>" )>]
    [<InlineData( "<Test><R024_1><D1_2>aa</D1_2><D1_2>aa</D1_2></R024_1></Test>" )>]
    [<InlineData( "<Test><R024_1><D1_1>0</D1_1></R024_1><R024_1><D1_2>aa</D1_2></R024_1></Test>" )>]
    member _.Selection_001 ( ts : string ) =
        try
            ConfRW_003_006.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Selection_002 () =
        let s = [|
            "<Test><R024_1><D1_1>0</D1_1></R024_1></Test>";
            "<Test><R024_1><D1_2>aaa</D1_2></R024_1></Test>";
        |]
        let exr = [|
            { ConfRW_003_006.R024_1 = ConfRW_003_006.T_R024_1.U_D1_1( 0 ) };
            { ConfRW_003_006.R024_1 = ConfRW_003_006.T_R024_1.U_D1_2( "aaa" ) };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_006.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><R025_1></R025_1></Test>" )>]
    [<InlineData( "<Test><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_1>0</D1_1></R025_1></Test>" )>]
    member _.Selection_003 ( ts : string ) =
        try
            ConfRW_003_007.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Selection_004 () =
        let s = [|
            "<Test><R025_1><D1_1>0</D1_1></R025_1></Test>"
            "<Test><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_2>aaa</D1_2></R025_1></Test>"
            "<Test><R025_1><D1_2>bbb</D1_2></R025_1><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_2>bbb</D1_2></R025_1></Test>"
        |]
        let exr = [|
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ] };
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ConfRW_003_007.T_R025_1.U_D1_2( "aaa" ); ] };
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_2( "bbb" ); ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ConfRW_003_007.T_R025_1.U_D1_2( "bbb" ); ] };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_007.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R026_1></R026_1></Test>" )>]
    [<InlineData( "<Test><R026_1><D1_1>0</D1_1></R026_1><R026_1><D1_1>0</D1_1></R026_1></Test>" )>]
    member _.Selection_005 ( ts : string ) =
        try
            ConfRW_003_008.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Selection_006 () =
        let s = [|
            "<Test></Test>";
            "<Test><R026_1><D1_1>0</D1_1></R026_1></Test>";
            "<Test><R026_1><D1_2>aaa</D1_2></R026_1></Test>";
        |]
        let exr = [|
            { ConfRW_003_008.R026_1 = None };
            { ConfRW_003_008.R026_1 = Some( ConfRW_003_008.T_R026_1.U_D1_1( 0 ); ) };
            { ConfRW_003_008.R026_1 = Some( ConfRW_003_008.T_R026_1.U_D1_2( "aaa" ); ) };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_008.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R027_1></R027_1></Test>" )>]
    [<InlineData( "<Test><R027_1><D1>0</D1><R027_2><D1_1>0</D1_1></R027_2><D2>1</D2></R027_1></Test>" )>]
    [<InlineData( "<Test><R027_1><D1>0</D1></R027_1><R027_1><R027_2><D1_1>0</D1_1></R027_2></R027_1></Test>" )>]
    member _.Selection_007 ( ts : string ) =
        try
            ConfRW_003_009.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Selection_008 () =
        let s = [|
            "<Test><R027_1><D1>0</D1></R027_1></Test>";
            "<Test><R027_1><R027_2><D1_1>0</D1_1></R027_2></R027_1></Test>";
            "<Test><R027_1><R027_2><D1_2>0</D1_2></R027_2></R027_1></Test>";
            "<Test><R027_1><D2>0</D2></R027_1></Test>";
        |]
        let exr = [|
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_D1( 0 ); };
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_R027_2( ConfRW_003_009.T_R027_2.U_D1_1( 0 ) ); };
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_R027_2( ConfRW_003_009.T_R027_2.U_D1_2( 0 ) ); };
            { ConfRW_003_009.R027_1 = ConfRW_003_009.U_D2( 0 ); };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_009.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test><R028_1></R028_1></Test>" )>]
    [<InlineData( "<Test><R028_1><D1>0</D1><R028_2><D1_1>0</D1_1><D1_1>0</D1_1></R028_2><D2>1</D2></R028_1></Test>" )>]
    [<InlineData( "<Test><R028_1><D1>0</D1><D2>1</D2></R028_1></Test>" )>]
    member _.Selection_009 ( ts : string ) =
        try
            ConfRW_003_010.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Selection_010 () =
        let s = [|
            "<Test><R028_1><D1>0</D1></R028_1></Test>";
            "<Test><R028_1><R028_2><D1_1>0</D1_1><D1_2>0</D1_2></R028_2></R028_1></Test>";
            "<Test><R028_1><D2>1</D2></R028_1></Test>";
        |]
        let exr = [|
            { ConfRW_003_010.R028_1 = ConfRW_003_010.T_R028_1.U_D1( 0 ); };
            { ConfRW_003_010.R028_1 = ConfRW_003_010.T_R028_1.U_R028_2( { D1_1 = 0; D1_2 = 0 } ); };
            { ConfRW_003_010.R028_1 = ConfRW_003_010.T_R028_1.U_D2( 1 ); };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_010.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Selection_011 () =
        let s = [|
            { ConfRW_003_006.R024_1 = ConfRW_003_006.T_R024_1.U_D1_1( 0 ) };
            { ConfRW_003_006.R024_1 = ConfRW_003_006.T_R024_1.U_D1_2( "aaa" ) };
        |]
        let exr = [|
            "<Test><R024_1><D1_1>0</D1_1></R024_1></Test>";
            "<Test><R024_1><D1_2>aaa</D1_2></R024_1></Test>";
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_006.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Selection_012 () =
        let s = [|
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ] };
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ConfRW_003_007.T_R025_1.U_D1_2( "aaa" ); ] };
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_2( "bbb" ); ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ConfRW_003_007.T_R025_1.U_D1_2( "bbb" ); ] };
        |]
        let exr = [|
            "<Test><R025_1><D1_1>0</D1_1></R025_1></Test>"
            "<Test><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_2>aaa</D1_2></R025_1></Test>"
            "<Test><R025_1><D1_2>bbb</D1_2></R025_1><R025_1><D1_1>0</D1_1></R025_1><R025_1><D1_2>bbb</D1_2></R025_1></Test>"
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_007.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Selection_013 () =
        let s = [|
            { ConfRW_003_007.R025_1 = [] };
            { ConfRW_003_007.R025_1 = [ ConfRW_003_007.T_R025_1.U_D1_2( "bbb" ); ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ConfRW_003_007.T_R025_1.U_D1_2( "bbb" ); ConfRW_003_007.T_R025_1.U_D1_1( 0 ); ] };
        |]
        for i = 0 to s.Length - 1 do
            try
                let _ = ConfRW_003_007.ReaderWriter.ToString s.[i]
                Assert.Fail __LINE__
            with
            | :? ConfRWException as x ->
                Assert.True(( x.Message = "Element count restriction error. R025_1" ))
            | _ ->
                Assert.Fail __LINE__

    [<Fact>]
    member _.Selection_014 () =
        let s = [|
            { ConfRW_003_008.R026_1 = None };
            { ConfRW_003_008.R026_1 = Some( ConfRW_003_008.T_R026_1.U_D1_1( 0 ); ) };
            { ConfRW_003_008.R026_1 = Some( ConfRW_003_008.T_R026_1.U_D1_2( "aaa" ); ) };
        |]
        let exr = [|
            "<Test></Test>";
            "<Test><R026_1><D1_1>0</D1_1></R026_1></Test>";
            "<Test><R026_1><D1_2>aaa</D1_2></R026_1></Test>";
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_008.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Selection_015 () =
        let s = [|
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_D1( 0 ); };
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_R027_2( ConfRW_003_009.T_R027_2.U_D1_1( 0 ) ); };
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_R027_2( ConfRW_003_009.T_R027_2.U_D1_2( 0 ) ); };
            { ConfRW_003_009.R027_1 = ConfRW_003_009.T_R027_1.U_D2( 0 ); };
        |]
        let exr = [|
            "<Test><R027_1><D1>0</D1></R027_1></Test>";
            "<Test><R027_1><R027_2><D1_1>0</D1_1></R027_2></R027_1></Test>";
            "<Test><R027_1><R027_2><D1_2>0</D1_2></R027_2></R027_1></Test>";
            "<Test><R027_1><D2>0</D2></R027_1></Test>";
        |]

        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_009.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Selection_016 () =
        let s = [|
            { ConfRW_003_010.R028_1 = ConfRW_003_010.T_R028_1.U_D1( 0 ); };
            { ConfRW_003_010.R028_1 = ConfRW_003_010.U_R028_2( { D1_1 = 0; D1_2 = 0 } ); };
            { ConfRW_003_010.R028_1 = ConfRW_003_010.T_R028_1.U_D2( 1 ); };
        |]
        let exr = [|
            "<Test><R028_1><D1>0</D1></R028_1></Test>";
            "<Test><R028_1><R028_2><D1_1>0</D1_1><D1_2>0</D1_2></R028_2></R028_1></Test>";
            "<Test><R028_1><D2>1</D2></R028_1></Test>";
        |]

        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_010.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><D3></D3><D4><D4_1></D4_1><D4_2></D4_2></D4><D5><D5_1></D5_1></D5></Test>" )>]
    [<InlineData( "<Test><D3><D1>0</D1></D3><D4><D4_1><D1>0</D1></D4_1><D4_2><D1>0</D1></D4_2></D4><D5><D5_1><D1>0</D1></D5_1></D5></Test>" )>]
    [<InlineData( "<Test><D3><D2>0</D2></D3><D4><D4_1><D2>0</D2></D4_1><D4_2><D2>0</D2></D4_2></D4><D5><D5_1><D2>0</D2></D5_1></D5></Test>" )>]
    member _.Typedef_001 ( ts : string ) =
        try
            ConfRW_003_011.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Typedef_002 () =
        let s = [|
          "<Test>
             <D3>  <D1>0</D1>
                   <D2>1</D2>
             </D3>
             <D4>  <D4_1>  <D1>0</D1>
                           <D2>1</D2>
                   </D4_1>
                   <D4_2>  <D1>0</D1>
                           <D2>1</D2>
                   </D4_2>
             </D4>
             <D5>  <D5_1>  <D1>0</D1>
                           <D2>1</D2>
                   </D5_1>
             </D5>
            </Test>";
          "<Test>
             <D3>  <D1>0</D1>
                   <D2>1</D2>
             </D3>
             <D4>  <D4_1>  <D1>0</D1>
                           <D2>1</D2>
                   </D4_1>
                   <D4_2>  <D1>0</D1>
                           <D2>1</D2>
                   </D4_2>
             </D4>
             <D5>  <D5_2>  <D1>0</D1>
                           <D2>1</D2>
                   </D5_2>
             </D5>
            </Test>";
        |]
        let exr = [|
            { ConfRW_003_011.D3 = { D1=0; D2=1 }; ConfRW_003_011.D4 = { D4_1 = { D1=0; D2=1 }; D4_2 = { D1=0; D2=1 }; }; ConfRW_003_011.D5 = ConfRW_003_011.U_D5_1( { D1=0; D2=1 } ) };
            { ConfRW_003_011.D3 = { D1=0; D2=1 }; ConfRW_003_011.D4 = { D4_1 = { D1=0; D2=1 }; D4_2 = { D1=0; D2=1 }; }; ConfRW_003_011.D5 = ConfRW_003_011.U_D5_2( { D1=0; D2=1 } ) };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_011.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Typedef_003 () =
        let s = [|
            { ConfRW_003_011.D3 = { D1=0; D2=1 }; ConfRW_003_011.D4 = { D4_1 = { D1=0; D2=1 }; D4_2 = { D1=0; D2=1 }; }; ConfRW_003_011.D5 = ConfRW_003_011.U_D5_1( { D1=0; D2=1 } ) };
            { ConfRW_003_011.D3 = { D1=0; D2=1 }; ConfRW_003_011.D4 = { D4_1 = { D1=0; D2=1 }; D4_2 = { D1=0; D2=1 }; }; ConfRW_003_011.D5 = ConfRW_003_011.U_D5_2( { D1=0; D2=1 } ) };
        |]
        let exr = [|
          "<Test><D3><D1>0</D1><D2>1</D2></D3><D4><D4_1><D1>0</D1><D2>1</D2></D4_1><D4_2><D1>0</D1><D2>1</D2></D4_2></D4><D5><D5_1><D1>0</D1><D2>1</D2></D5_1></D5></Test>";
          "<Test><D3><D1>0</D1><D2>1</D2></D3><D4><D4_1><D1>0</D1><D2>1</D2></D4_1><D4_2><D1>0</D1><D2>1</D2></D4_2></D4><D5><D5_2><D1>0</D1><D2>1</D2></D5_2></D5></Test>";
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_011.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><D7><D1>0</D1><D2>1</D2><D2>1</D2></D7><D9><D9_1><D3>0</D3><D4>1</D4></D9_1></D9></Test>" )>]
    [<InlineData( "<Test><D7><D1>0</D1><D1>0</D1><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2></D7><D9><D9_1><D3>0</D3><D4>1</D4></D9_1></D9></Test>" )>]
    [<InlineData( "<Test><D7><D1>0</D1><D1>0</D1><D2>1</D2></D7><D9><D9_1><D3>0</D3><D4>1</D4></D9_1></D9></Test>" )>]
    [<InlineData( "<Test><D7><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2><D2>1</D2><D2>1</D2></D7><D9><D9_1><D3>0</D3><D4>1</D4></D9_1></D9></Test>" )>]
    [<InlineData( "<Test><D7><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2></D7><D9><D9_1><D3>0</D3><D4>1</D4></D9_1><D9_2><D5>0</D5></D9_2></D9></Test>" )>]
    [<InlineData( "<Test><D7><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2></D7><D9><D9_1><D4>1</D4></D9_1><D9_2><D5>0</D5><D6>0</D6></D9_2></D9></Test>" )>]
    member _.Typedef_004 ( ts : string ) =
        try
            ConfRW_003_012.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Typedef_005 () =
        let s = [|
          "<Test>
             <D7>  <D1>0</D1><D1>0</D1>
                   <D2>1</D2><D2>1</D2>
             </D7>
             <D9>  <D9_1>  <D3>0</D3>
                           <D4>1</D4>
                   </D9_1>
             </D9>
            </Test>";
          "<Test>
             <D7>  <D1>0</D1><D1>0</D1><D1>0</D1>
                   <D2>1</D2><D2>1</D2><D2>1</D2>
             </D7>
             <D9>  <D9_2>  <D5>1</D5>
                   </D9_2>
             </D9>
            </Test>";
          "<Test>
             <D7>  <D1>0</D1><D1>0</D1><D1>0</D1>
                   <D2>1</D2><D2>1</D2><D2>1</D2>
             </D7>
             <D9>  <D9_2>  <D6>1</D6>
                   </D9_2>
             </D9>
            </Test>";
        |]
        let exr = [|
            { ConfRW_003_012.D7 = { D1=[ 0; 0; ]; D2=[ 1; 1; ] }; ConfRW_003_012.D9=ConfRW_003_012.T_D9.U_D9_1( { D3=0; D4=1; } ) };
            { ConfRW_003_012.D7 = { D1=[ 0; 0; 0; ]; D2=[ 1; 1; 1; ] }; ConfRW_003_012.D9=ConfRW_003_012.T_D9.U_D9_2( ConfRW_003_012.T_T030_3.U_D5( 1 ) ) };
            { ConfRW_003_012.D7 = { D1=[ 0; 0; 0; ]; D2=[ 1; 1; 1; ] }; ConfRW_003_012.D9=ConfRW_003_012.T_D9.U_D9_2( ConfRW_003_012.T_T030_3.U_D6( 1 ) ) };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_012.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Typedef_006 () =
        let s = [|
            { ConfRW_003_012.D7 = { D1=[ 0; 0; ]; D2=[ 1; 1; ] }; ConfRW_003_012.D9=ConfRW_003_012.T_D9.U_D9_1( { D3=0; D4=1; } ) };
            { ConfRW_003_012.D7 = { D1=[ 0; 0; 0; ]; D2=[ 1; 1; 1; ] }; ConfRW_003_012.D9=ConfRW_003_012.T_D9.U_D9_2( ConfRW_003_012.T_T030_3.U_D5( 1 ) ) };
            { ConfRW_003_012.D7 = { D1=[ 0; 0; 0; ]; D2=[ 1; 1; 1; ] }; ConfRW_003_012.D9=ConfRW_003_012.T_D9.U_D9_2( ConfRW_003_012.T_T030_3.U_D6( 1 ) ) };
        |]
        let exr = [|
          "<Test><D7><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2></D7><D9><D9_1><D3>0</D3><D4>1</D4></D9_1></D9></Test>";
          "<Test><D7><D1>0</D1><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2><D2>1</D2></D7><D9><D9_2><D5>1</D5></D9_2></D9></Test>";
          "<Test><D7><D1>0</D1><D1>0</D1><D1>0</D1><D2>1</D2><D2>1</D2><D2>1</D2></D7><D9><D9_2><D6>1</D6></D9_2></D9></Test>";
        |]

        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_012.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2><D3>0</D3><R031_1><D4>0</D4><D5><D3>0</D3><R031_1><D4>0</D4></R031_1></D5></R031_1></D2></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2><D3>0</D3></D2></Test>" )>]
    member _.Typedef_007 ( ts : string ) =
        try
            ConfRW_003_013.ReaderWriter.LoadString ts |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.Typedef_008 () =
        let s = [|
          "<Test>
             <D1>0</D1>
             <D2>
               <D3>0</D3>
               <R031_1>
                 <D4>0</D4>
               </R031_1>
             </D2>
            </Test>";
          "<Test>
             <D1>0</D1>
             <D2>
               <D3>0</D3>
               <R031_1>
                 <D5>
                   <D3>0</D3>
                   <R031_1>
                     <D4>0</D4>
                   </R031_1>
                 </D5>
               </R031_1>
             </D2>
            </Test>";
          "<Test>
             <D1>0</D1>
             <D2>
               <D3>0</D3>
               <R031_1>
                 <D5>
                   <D3>0</D3>
                   <R031_1>
                     <D5>
                       <D3>0</D3>
                       <R031_1>
                         <D4>0</D4>
                       </R031_1>
                     </D5>
                   </R031_1>
                 </D5>
               </R031_1>
             </D2>
            </Test>";
        |]
        let exr : ConfRW_003_013.T_Test[] = [|
            { D1 = 0; D2 = { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D4( 0 ) } };
            { D1 = 0; D2 = { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D5( { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D4( 0 ) } ) } };
            { D1 = 0; D2 = { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D5( { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D5( { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D4( 0 ) } ) } ) } };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_013.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Typedef_009 () =
        let s : ConfRW_003_013.T_Test[] = [|
            { D1 = 0; D2 = { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D4( 0 ) } };
            { D1 = 0; D2 = { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D5( { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D4( 0 ) } ) } };
            { D1 = 0; D2 = { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D5( { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D5( { D3 = 0; R031_1 = ConfRW_003_013.T_R031_1.U_D4( 0 ) } ) } ) } };
        |]
        let exr = [|
          "<Test><D1>0</D1><D2><D3>0</D3><R031_1><D4>0</D4></R031_1></D2></Test>";
          "<Test><D1>0</D1><D2><D3>0</D3><R031_1><D5><D3>0</D3><R031_1><D4>0</D4></R031_1></D5></R031_1></D2></Test>";
          "<Test><D1>0</D1><D2><D3>0</D3><R031_1><D5><D3>0</D3><R031_1><D5><D3>0</D3><R031_1><D4>0</D4></R031_1></D5></R031_1></D5></R031_1></D2></Test>";
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_013.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Typedef_011 () =
        let s = [|
          "<Test>
             <D1>0</D1>
             <D2>
               <R032_1>
                 <D3>0</D3>
               </R032_1>
             </D2>
            </Test>";
          "<Test>
             <D1>0</D1>
             <D2>
               <R032_1>
                 <D4>
                   <R032_2>
                     <D5>0</D5>
                   </R032_2>
                 </D4>
               </R032_1>
             </D2>
            </Test>";
          "<Test>
             <D1>0</D1>
             <D2>
               <R032_1>
                 <D4>
                   <R032_2>
                     <D6>
                       <R032_1>
                        <D3>0</D3>
                       </R032_1>
                     </D6>
                   </R032_2>
                 </D4>
               </R032_1>
             </D2>
            </Test>";
          "<Test>
             <D1>0</D1>
             <D2>
               <R032_1>
                 <D4>
                   <R032_2>
                     <D6>
                       <R032_1>
                        <D4>
                          <R032_2>
                            <D5>0</D5>
                          </R032_2>
                        </D4>
                       </R032_1>
                     </D6>
                   </R032_2>
                 </D4>
               </R032_1>
             </D2>
            </Test>";
        |]
        let exr : ConfRW_003_014.T_Test[] = [|
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D3( 0 ) } };
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D5( 0 ) } ) } };
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D6( { R032_1 = ConfRW_003_014.T_R032_1.U_D3( 0 ) } ) } ) } };
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D6( { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D5( 0 ) } ) } ) } ) } };
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_014.ReaderWriter.LoadString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.Typedef_012 () =
        let s : ConfRW_003_014.T_Test[] = [|
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D3( 0 ) } };
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D5( 0 ) } ) } };
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D6( { R032_1 = ConfRW_003_014.T_R032_1.U_D3( 0 ) } ) } ) } };
            { D1 = 0; D2 = { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D6( { R032_1 = ConfRW_003_014.T_R032_1.U_D4( { R032_2 = ConfRW_003_014.T_R032_2.U_D5( 0 ) } ) } ) } ) } };
        |]
        let exr = [|
          "<Test><D1>0</D1><D2><R032_1><D3>0</D3></R032_1></D2></Test>";
          "<Test><D1>0</D1><D2><R032_1><D4><R032_2><D5>0</D5></R032_2></D4></R032_1></D2></Test>";
          "<Test><D1>0</D1><D2><R032_1><D4><R032_2><D6><R032_1><D3>0</D3></R032_1></D6></R032_2></D4></R032_1></D2></Test>";
          "<Test><D1>0</D1><D2><R032_1><D4><R032_2><D6><R032_1><D4><R032_2><D5>0</D5></R032_2></D4></R032_1></D6></R032_2></D4></R032_1></D2></Test>";
        |]
        for i = 0 to s.Length - 1 do
            let r = ConfRW_003_014.ReaderWriter.ToString s.[i] 
            Assert.True(( r = exr.[i] ))

    [<Fact>]
    member _.RefConstants_Count_001 () =
        let s = new StringBuilder()
        s.Append "<Test>" |> ignore
        s.Append( String.replicate ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) "<C>0</C>" ) |> ignore
        s.Append "<D>0</D>" |> ignore
        s.Append "</Test>" |> ignore

        try
            ConfRW_004_001.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ ->
            ()

    [<Fact>]
    member _.RefConstants_Count_002 () =
        let s = new StringBuilder()
        s.Append "<Test>" |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<C>0</C>" ) |> ignore
        s.Append "<D>0</D>" |> ignore
        s.Append "</Test>" |> ignore

        try
            let v = ConfRW_004_001.ReaderWriter.LoadString ( s.ToString() )
            Assert.True(( v.C.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
            Assert.True(( v.D = [ 0 ] ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Count_003 () =
        let s = new StringBuilder()
        s.Append "<Test>" |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<C>0</C>" ) |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<D>0</D>" ) |> ignore
        s.Append "</Test>" |> ignore

        try
            let v = ConfRW_004_001.ReaderWriter.LoadString ( s.ToString() )
            Assert.True(( v.C.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
            Assert.True(( v.D.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Count_004 () =
        let s = new StringBuilder()
        s.Append "<Test>" |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<C>0</C>" ) |> ignore
        s.Append( String.replicate ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) "<D>0</D>" ) |> ignore
        s.Append "</Test>" |> ignore

        try
            ConfRW_004_001.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ ->
            ()

    [<Fact>]
    member _.RefConstants_Count_005 () =
        let v : ConfRW_004_001.T_Test = {
            C = List.replicate ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) 0;
            D = [ 0 ];
        }
        try
            ConfRW_004_001.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ ->
            ()

    [<Fact>]
    member _.RefConstants_Count_006 () =
        let v : ConfRW_004_001.T_Test = {
            C = List.replicate Constants.MAX_TARGET_DEVICE_COUNT 0;
            D = [ 0 ];
        }

        let s = new StringBuilder()
        s.Append "<Test>" |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<C>0</C>" ) |> ignore
        s.Append "<D>0</D>" |> ignore
        s.Append "</Test>" |> ignore

        try
            let rs = ConfRW_004_001.ReaderWriter.ToString v
            Assert.True(( rs = s.ToString() ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Count_007 () =
        let v : ConfRW_004_001.T_Test = {
            C = List.replicate Constants.MAX_TARGET_DEVICE_COUNT 0;
            D = List.replicate Constants.MAX_TARGET_DEVICE_COUNT 0;
        }

        let s = new StringBuilder()
        s.Append "<Test>" |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<C>0</C>" ) |> ignore
        s.Append( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "<D>0</D>" ) |> ignore
        s.Append "</Test>" |> ignore

        try
            let rs = ConfRW_004_001.ReaderWriter.ToString v
            Assert.True(( rs = s.ToString() ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Count_008 () =
        let v : ConfRW_004_001.T_Test = {
            C = List.replicate Constants.MAX_TARGET_DEVICE_COUNT 0;
            D = List.replicate ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) 0;
        }

        try
            ConfRW_004_001.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ ->
            ()

    [<Fact>]
    member _.RefConstants_Value_string_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_string_min>" |> ignore
        s.Append ( String.replicate ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) "0" ) |> ignore
        s.Append "</D_string_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_string_min>" |> ignore
        s.Append ( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "0" ) |> ignore
        s.Append "</D_string_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_string_min( x ) ->
                Assert.True(( x.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_string_min( String.replicate ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) "0" )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_string_min( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "0" )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_string_min>" |> ignore
        s.Append ( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "0" ) |> ignore
        s.Append "</D_string_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_string_max>" |> ignore
        s.Append ( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "0" ) |> ignore
        s.Append "</D_string_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_string_max( x ) ->
                Assert.True(( x.Length = Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_string_max>" |> ignore
        s.Append ( String.replicate ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) "0" ) |> ignore
        s.Append "</D_string_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_string_max( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "0" )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_string_max>" |> ignore
        s.Append ( String.replicate Constants.MAX_TARGET_DEVICE_COUNT "0" ) |> ignore
        s.Append "</D_string_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_string_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_string_max( String.replicate ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) "0" )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_byte_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_byte_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_byte_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_byte_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_byte_min( x ) ->
                Assert.True(( x = sbyte Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_byte_min( sbyte ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_byte_min( sbyte Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_byte_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_byte_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_byte_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_byte_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_byte_max( x ) ->
                Assert.True(( x = sbyte Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_byte_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_byte_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_byte_max( sbyte Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_byte_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_byte_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_byte_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_byte_max( sbyte ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedByte_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_unsignedByte_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedByte_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedByte_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedByte_min( x ) ->
                Assert.True(( x = byte Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedByte_min( byte ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedByte_min( byte Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedByte_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedByte_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedByte_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedByte_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedByte_max( x ) ->
                Assert.True(( x = byte Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedByte_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_unsignedByte_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedByte_max( byte Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedByte_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedByte_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedByte_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedByte_max( byte ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_int_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_int_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_int_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_int_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_int_min( x ) ->
                Assert.True(( x = int Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_int_min( int ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_int_min( int Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_int_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_int_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_int_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_int_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_int_max( x ) ->
                Assert.True(( x = int Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_int_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_int_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_int_max( int Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_int_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_int_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_int_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_int_max( int ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedInt_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_unsignedInt_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedInt_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedInt_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedInt_min( x ) ->
                Assert.True(( x = uint Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedInt_min( uint ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedInt_min( uint Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedInt_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedInt_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedInt_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedInt_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedInt_max( x ) ->
                Assert.True(( x = uint Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedInt_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_unsignedInt_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedInt_max( uint Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedInt_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedInt_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedInt_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedInt_max( uint ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_long_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_long_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_long_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_long_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_long_min( x ) ->
                Assert.True(( x = int64 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_long_min( int64 ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_long_min( int64 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_long_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_long_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_long_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_long_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_long_max( x ) ->
                Assert.True(( x = int64 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_long_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_long_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_long_max( int64 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_long_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_long_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_long_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_long_max( int64 ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedLong_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_unsignedLong_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedLong_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedLong_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedLong_min( x ) ->
                Assert.True(( x = uint64 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedLong_min( uint64 ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedLong_min( uint64 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedLong_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedLong_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedLong_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedLong_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedLong_max( x ) ->
                Assert.True(( x = uint64 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedLong_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_unsignedLong_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedLong_max( uint64 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedLong_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedLong_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedLong_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedLong_max( uint64 ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_short_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_short_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_short_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_short_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_short_min( x ) ->
                Assert.True(( x = int16 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_short_min( int16 ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_short_min( int16 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_short_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_short_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_short_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_short_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_short_max( x ) ->
                Assert.True(( x = int16 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_short_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_short_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_short_max( int16 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_short_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_short_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_short_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_short_max( int16 ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedShort_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_unsignedShort_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedShort_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedShort_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedShort_min( x ) ->
                Assert.True(( x = uint16 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedShort_min( uint16 ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedShort_min( uint16 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedShort_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedShort_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedShort_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedShort_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_unsignedShort_max( x ) ->
                Assert.True(( x = uint16 Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedShort_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_unsignedShort_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedShort_max( uint16 Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_unsignedShort_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_unsignedShort_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_unsignedShort_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_unsignedShort_max( uint16 ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_double_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_double_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_double_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_double_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_double_min( x ) ->
                Assert.True(( x = float Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_double_min( float ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_double_min( float Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_double_min>" |> ignore
        s.Append ( sprintf "%f" ( float Constants.MAX_TARGET_DEVICE_COUNT ) ) |> ignore
        s.Append "</D_double_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_double_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_double_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_double_max( x ) ->
                Assert.True(( x = float Constants.MAX_TARGET_DEVICE_COUNT ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_double_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_double_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_double_max( float Constants.MAX_TARGET_DEVICE_COUNT )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_double_max>" |> ignore
        s.Append ( sprintf "%f" ( float Constants.MAX_TARGET_DEVICE_COUNT ) ) |> ignore
        s.Append "</D_double_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_double_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_double_max( float ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_001 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_TPGT_T_min>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) |> ignore
        s.Append "</D_TPGT_T_min></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_002 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_TPGT_T_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_TPGT_T_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_TPGT_T_min( x ) ->
                Assert.True(( x = tpgt_me.fromPrim( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_003 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_TPGT_T_min( tpgt_me.fromPrim( uint16 ( Constants.MAX_TARGET_DEVICE_COUNT - 1 ) ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_004 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_TPGT_T_min( tpgt_me.fromPrim( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_TPGT_T_min>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_TPGT_T_min></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_005 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_TPGT_T_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_TPGT_T_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() )
            match r.C with
            | ConfRW_004_002.U_D_TPGT_T_max( x ) ->
                Assert.True(( x = tpgt_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) ))
            | _ ->
                Assert.Fail("")
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_006 () =
        let s = new StringBuilder()
        s.Append "<Test><C><D_TPGT_T_max>" |> ignore
        s.Append ( sprintf "%d" ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) |> ignore
        s.Append "</D_TPGT_T_max></C></Test>" |> ignore

        try
            ConfRW_004_002.ReaderWriter.LoadString ( s.ToString() ) |> ignore
            Assert.Fail("")
        with
        | :? System.Xml.Schema.XmlSchemaValidationException ->
            ()
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_007 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_TPGT_T_max( tpgt_me.fromPrim( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) )
        }
        let s = new StringBuilder()
        s.Append "<Test><C><D_TPGT_T_max>" |> ignore
        s.Append ( sprintf "%d" Constants.MAX_TARGET_DEVICE_COUNT ) |> ignore
        s.Append "</D_TPGT_T_max></C></Test>" |> ignore

        try
            let r = ConfRW_004_002.ReaderWriter.ToString v
            Assert.True(( s.ToString() = r ))
        with
        | _ ->
            Assert.Fail("")

    [<Fact>]
    member _.RefConstants_Value_TPGT_T_008 () =
        let v : ConfRW_004_002.T_Test = {
            C = ConfRW_004_002.T_C.U_D_TPGT_T_max( tpgt_me.fromPrim( uint16 ( Constants.MAX_TARGET_DEVICE_COUNT + 1 ) ) )
        }

        try
            ConfRW_004_002.ReaderWriter.ToString v |> ignore
            Assert.Fail("")
        with
        | :? ConfRWException ->
            ()
        | _ ->
            Assert.Fail("")


