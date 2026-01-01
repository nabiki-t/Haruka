//=============================================================================
// Haruka Software Storage.
// GenConfRWTest_001.fs : Test cases for GenConfRW module.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.GenConfRW

//=============================================================================
// Import declaration

open System
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Test.UT
open System.Net

//=============================================================================
// Class implementation

type GenConfRW_Test_001 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    member _.CreateTestDir() =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "GenConfRW_Test_001"
        GlbFunc.CreateDir w1 |> ignore
        w1

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.FileRead_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "FileRead_001.txt"
        GlbFunc.DeleteFile fname
        try
            ConfRW_001.ConfRW_UT001.LoadFile fname |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "<Test>" )>]
    [<InlineData( "<Test></Test>" )>]
    [<InlineData( "<Test><Dummy></Test>" )>]
    [<InlineData( "<Test><Dummy></Dummy></Test>" )>]
    [<InlineData( "<Test><Dummy>abc</Dummy></Test>" )>]
    [<InlineData( "<Test><Dummy>0</Dummy><Dummy>0</Dummy>/Test>" )>]
    [<InlineData( "<Test><Dummy>-2147483649</Dummy></Test>" )>]
    [<InlineData( "<Test><Dummy>2147483648</Dummy></Test>" )>]
    member this.FileRead_002 ( s : string ) =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "FileRead_002.txt"
        File.WriteAllText( fname, s )
        try
            ConfRW_001.ConfRW_UT001.LoadFile fname |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Theory>]
    [<InlineData( "<Test><Dummy>0</Dummy></Test>", 0 )>]
    [<InlineData( "<Test><Dummy>1</Dummy></Test>", 1 )>]
    [<InlineData( "<Test><Dummy>-1</Dummy></Test>", -1 )>]
    [<InlineData( "<Test><Dummy>-2147483648</Dummy></Test>", -2147483648 )>]
    [<InlineData( "<Test><Dummy>2147483647</Dummy></Test>", 2147483647 )>]
    member this.FileRead_003 ( s : string ) ( exr : int ) =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "FileRead_003.txt"
        File.WriteAllText( fname, s )
        let r = ConfRW_001.ConfRW_UT001.LoadFile fname
        Assert.True( r.Dummy = exr )
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.WriteFile_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "WriteFile_001.txt"
        GlbFunc.DeleteFile fname
        let w : ConfRW_001.T_Test = { Dummy = 99; }
        ConfRW_001.ConfRW_UT001.WriteFile fname w
        Assert.True(( File.Exists fname ))
        let re = File.ReadAllLines fname
        let exResult = [| "<Test>"; "  <Dummy>99</Dummy>"; "</Test>"; |]
        Assert.True(( re = exResult ))
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.WriteFile_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "WriteFile_002.txt"
        File.WriteAllText( fname, "abc" )
        let w : ConfRW_001.T_Test = { Dummy = 98; }
        ConfRW_001.ConfRW_UT001.WriteFile fname w
        Assert.True(( File.Exists fname ))
        let re = File.ReadAllLines fname
        let exResult = [| "<Test>"; "  <Dummy>98</Dummy>"; "</Test>"; |]
        Assert.True(( re = exResult ))
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.WriteFile_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "WriteFile_003.txt"
        try
            GlbFunc.DeleteFile fname
            GlbFunc.DeleteDir fname
        with
        | _ -> ()
        GlbFunc.CreateDir fname |> ignore
        let w : ConfRW_001.T_Test = { Dummy = 98; }
        try
            ConfRW_001.ConfRW_UT001.WriteFile fname w
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()
        GlbFunc.DeleteDir fname
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>6</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D6>0</D6></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D7>0</D7><D7>0</D7></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D8>0</D8><D8>0</D8></Test>" )>]
    member _.SingleValue_Int_001 ( s : string ) =
        try
            ConfRW_002_int.ConfRW_UT002_int.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2 )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3 )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4 )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5 )>]
    member _.SingleValue_Int_002 ( s : string ) ( exr : int ) =
        let r = ConfRW_002_int.ConfRW_UT002_int.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_Int_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0; 1; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ 0; 1; 2; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Int_003_data" )>]
    member _.SingleValue_Int_003 ( s : string ) ( exr : int list ) =
        let r = ConfRW_002_int.ConfRW_UT002_int.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_Int_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1 ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Int_004_data" )>]
    member _.SingleValue_Int_004 ( s : string ) ( exr : int option ) =
        let r = ConfRW_002_int.ConfRW_UT002_int.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_Int_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>0</D7><D8>0</D8></Test>" :> obj; 0 :> obj; 0 :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>1</D7><D8>2</D8></Test>" :> obj; 1 :> obj; 2 :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98 :> obj; Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Int_005_data" )>]
    member _.SingleValue_Int_005 ( s : string ) ( exr_D7 : int ) ( exr_D8 : int ) =
        let r = ConfRW_002_int.ConfRW_UT002_int.LoadString s
        Assert.True( r.D4 = 0 )
        Assert.True( r.D5 = 99 )
        Assert.True( r.D6 = Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_Int_006_data = [|
        [|
            ( { D1 = 2; D2 = [ 0; 1; ]; D3 = None; D4 = 0; D5 = 0; D6 = 0; D7 = 1; D8 = 2; } : ConfRW_002_int.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>1</D7><D8>2</D8></Test>" :> obj;
        |];
        [|
            ( { D1 = 3; D2 = [ 0; 1; 2; ]; D3 = None; D4 = 0; D5 = 0; D6 = 1; D7 = 2; D8 = 3; } : ConfRW_002_int.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>2</D7><D8>3</D8></Test>" :> obj;
        |];
        [|
            ( { D1 = 2; D2 = [ 0; 1; ]; D3 = Some( 5 ); D4 = 0; D5 = 0; D6 = 2; D7 = 3; D8 = 4; } : ConfRW_002_int.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>3</D7><D8>4</D8></Test>" :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Int_006_data" )>]
    member _.SingleValue_Int_006 ( s : ConfRW_002_int.T_Test ) ( exr : string )=
        let r = ConfRW_002_int.ConfRW_UT002_int.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_Int_007_data = [|
        [|
            ( { D1 = 1; D2 = [ 0; 1; ]; D3 = None; D4 = 0; D5 = 0; D6 = 0; D7 = 1; D8 = 2; } : ConfRW_002_int.T_Test ) :> obj;
        |];
        [|
            ( { D1 = 6; D2 = [ 0; 1; ]; D3 = None; D4 = 0; D5 = 0; D6 = 0; D7 = 1; D8 = 2; } : ConfRW_002_int.T_Test ) :> obj;
        |];
        [|
            ( { D1 = 2; D2 = [ 0; ]; D3 = None; D4 = 0; D5 = 0; D6 = 0; D7 = 1; D8 = 2; } : ConfRW_002_int.T_Test ) :> obj;
        |];
        [|
            ( { D1 = 2; D2 = [ 0; 1; 2; 3; ]; D3 = None; D4 = 0; D5 = 0; D6 = 0; D7 = 1; D8 = 2; } : ConfRW_002_int.T_Test ) :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Int_007_data" )>]
    member _.SingleValue_Int_007 ( s : ConfRW_002_int.T_Test ) =
        try
            ConfRW_002_int.ConfRW_UT002_int.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>-1</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>4294967296</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_UInt_001 ( s : string ) =
        try
            ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 1u )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    member _.SingleValue_UInt_002 ( s : string ) ( exr : uint ) =
        let r = ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_UInt_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0u; 1u; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ 0u; 1u; 2u; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UInt_003_data" )>]
    member _.SingleValue_UInt_003 ( s : String ) ( exr : uint list ) =
        let r = ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_UInt_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1u ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>4294967295</D3></Test>" :> obj; Some( 4294967295u ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UInt_004_data" )>]
    member _.SingleValue_UInt_004 ( s : String ) ( exr : uint option ) =
        let r = ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_UInt_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98u :> obj; uint32 Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>3</D7><D8>4</D8></Test>" :> obj; 3u :> obj; 4u :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UInt_005_data" )>]
    member _.SingleValue_UInt_005 ( s : string ) ( exr_D7 : uint ) ( exr_D8 : uint ) =
        let r = ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.LoadString s
        Assert.True( r.D4 = 0u )
        Assert.True( r.D5 = 99u )
        Assert.True( r.D6 = uint32 Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_UInt_006_data = [|
        [|
            ( { D1 = 1u; D2 = [ 0u; 1u; ]; D3 = None; D4 = 0u; D5 = 0u; D6 = 1u; D7 = 2u; D8 = 3u; } : ConfRW_002_unsignedInt.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 2u; D2 = [ 0u; 1u; 2u; ]; D3 = None; D4 = 0u; D5 = 0u; D6 = 2u; D7 = 3u; D8 = 4u; } : ConfRW_002_unsignedInt.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 1u; D2 = [ 0u; 1u; ]; D3 = Some( 5u ); D4 = 0u; D5 = 0u; D6 = 3u; D7 = 4u; D8 = 5u; } : ConfRW_002_unsignedInt.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UInt_006_data" )>]
    member _.SingleValue_UInt_006 ( s : ConfRW_002_unsignedInt.T_Test ) ( exr : string ) =
        let r = ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_UInt_007_data = [|
        [| ( { D1 = 0u; D2 = [ 0u; 1u; ]; D3 = None; D4 = 0u; D5 = 0u; D6 = 0u; D7 = 2u; D8 = 3u; } : ConfRW_002_unsignedInt.T_Test ) :> obj |];
        [| ( { D1 = 3u; D2 = [ 0u; 1u; ]; D3 = None; D4 = 0u; D5 = 0u; D6 = 0u; D7 = 2u; D8 = 3u; } : ConfRW_002_unsignedInt.T_Test ) :> obj |];
        [| ( { D1 = 2u; D2 = [ 0u; ]; D3 = None; D4 = 0u; D5 = 0u; D6 = 0u; D7 = 2u; D8 = 3u; } : ConfRW_002_unsignedInt.T_Test ) :> obj |];
        [| ( { D1 = 2u; D2 = [ 0u; 1u; 2u; 3u; ]; D3 = None; D4 = 0u; D5 = 0u; D6 = 0u; D7 = 2u; D8 = 3u; } : ConfRW_002_unsignedInt.T_Test ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UInt_007_data" )>]
    member _.SingleValue_UInt_007 ( s : ConfRW_002_unsignedInt.T_Test ) =
        try
            ConfRW_002_unsignedInt.ConfRW_UT002_unsignedInt.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>-9223372036854775809</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>9223372036854775808</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_Long_001 ( s : string ) =
        try
            ConfRW_002_long.ConfRW_UT002_long.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 1L )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2L )>]
    member _.SingleValue_Long_002 ( s : string ) ( exr : int64 ) =
        try
            let r = ConfRW_002_long.ConfRW_UT002_long.LoadString s
            Assert.True( r.D1 = exr )
        with
        | _->
            Assert.Fail __LINE__

    static member m_SingleValue_Long_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0L; 1L; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ 0L; 1L; 2L; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Long_003_data" )>]
    member _.SingleValue_Long_003 ( s : String ) ( exr : int64 list ) =
        let r = ConfRW_002_long.ConfRW_UT002_long.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_Long_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1L ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>-9223372036854775808</D3></Test>" :> obj; Some( -9223372036854775808L ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>9223372036854775807</D3></Test>" :> obj; Some( 9223372036854775807L ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Long_004_data" )>]
    member _.SingleValue_Long_004 ( s : String ) ( exr : int64 option ) =
        let r = ConfRW_002_long.ConfRW_UT002_long.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_Long_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98L :> obj; int64 Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; 4L :> obj; 5L :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Long_005_data" )>]
    member _.SingleValue_Long_005 ( s : string ) ( exr_D7 : int64 ) ( exr_D8 : int64 ) =
        let r = ConfRW_002_long.ConfRW_UT002_long.LoadString s
        Assert.True( r.D4 = 0L )
        Assert.True( r.D5 = 99L )
        Assert.True( r.D6 = int64 Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_Long_006_data = [|
        [|
            ( { D1 = 1L; D2 = [ 0L; 1L; ]; D3 = None; D4 = 0L; D5 = 0L; D6 = 1L; D7 = 2L; D8 = 3L; } : ConfRW_002_long.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 2L; D2 = [ 0L; 1L; 2L; ]; D3 = None; D4 = 0L; D5 = 0L; D6 = 2L; D7 = 3L; D8 = 4L; } : ConfRW_002_long.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 1L; D2 = [ 0L; 1L; ]; D3 = Some( 5L ); D4 = 0L; D5 = 0L; D6 = 3L; D7 = 4L; D8 = 5L; } : ConfRW_002_long.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Long_006_data" )>]
    member _.SingleValue_Long_006 ( s : ConfRW_002_long.T_Test ) ( exr : string ) =
        let r = ConfRW_002_long.ConfRW_UT002_long.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_Long_007_data = [|
        [| ( { D1 = 0L; D2 = [ 0L; 1L; ]; D3 = None; D4 = 0L; D5 = 0L; D6 = 1L; D7 = 2L; D8 = 3L; } : ConfRW_002_long.T_Test ) :> obj |];
        [| ( { D1 = 3L; D2 = [ 0L; 1L; ]; D3 = None; D4 = 0L; D5 = 0L; D6 = 1L; D7 = 2L; D8 = 3L; } : ConfRW_002_long.T_Test ) :> obj |];
        [| ( { D1 = 2L; D2 = [ 0L; ]; D3 = None; D4 = 0L; D5 = 0L; D6 = 1L; D7 = 2L; D8 = 3L; } : ConfRW_002_long.T_Test ) :> obj |];
        [| ( { D1 = 2L; D2 = [ 0L; 1L; 2L; 3L; ]; D3 = None; D4 = 0L; D5 = 0L; D6 = 1L; D7 = 2L; D8 = 3L; } : ConfRW_002_long.T_Test ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Long_007_data" )>]
    member _.SingleValue_Long_007 ( s : ConfRW_002_long.T_Test ) =
        try
            ConfRW_002_long.ConfRW_UT002_long.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>-1</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>18446744073709551616</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>1</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>1</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>1</D6></Test>" )>]
    member _.SingleValue_ULong_001 ( s : string ) =
        try
            ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 1UL )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2UL )>]
    member _.SingleValue_ULong_002 ( s : string ) ( exr : uint64 ) =
        let r = ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_ULong_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0UL; 1UL; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ 0UL; 1UL; 2UL; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ULong_003_data" )>]
    member _.SingleValue_ULong_003 ( s : String ) ( exr : uint64 list ) =
        let r = ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_ULong_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1UL ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; Some( 0UL ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>18446744073709551615</D3></Test>" :> obj; Some( 18446744073709551615UL ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ULong_004_data" )>]
    member _.SingleValue_ULong_004 ( s : String ) ( exr : uint64 option ) =
        let r = ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_ULong_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98UL :> obj; uint64 Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj; 2UL :> obj; 3UL :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ULong_005_data" )>]
    member _.SingleValue_ULong_005 ( s : string ) ( exr_D7 : uint64 ) ( exr_D8 : uint64 ) =
        let r = ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.LoadString s
        Assert.True( r.D4 = 0UL )
        Assert.True( r.D5 = 99UL )
        Assert.True( r.D6 = uint64 Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_ULong_006_data = [|
        [|
            ( { D1 = 1UL; D2 = [ 0UL; 1UL; ]; D3 = None; D4 = 0UL; D5 = 0UL; D6 = 1UL; D7 = 2UL; D8 = 3UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 2UL; D2 = [ 0UL; 1UL; 2UL; ]; D3 = None; D4 = 0UL; D5 = 0UL; D6 = 2UL; D7 = 3UL; D8 = 4UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 1UL; D2 = [ 0UL; 1UL; ]; D3 = Some( 5UL ); D4 = 0UL; D5 = 0UL; D6 = 3UL; D7 = 4UL; D8 = 5UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ULong_006_data" )>]
    member _.SingleValue_ULong_006 ( s : ConfRW_002_unsignedLong.T_Test ) ( exr : string ) =
        let r = ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_ULong_007_data = [|
        [| ( { D1 = 0UL; D2 = [ 0UL; 1UL; ]; D3 = None; D4 = 0UL; D5 = 0UL; D6 = 1UL; D7 = 2UL; D8 = 3UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj |];
        [| ( { D1 = 3UL; D2 = [ 0UL; 1UL; ]; D3 = None; D4 = 0UL; D5 = 0UL; D6 = 1UL; D7 = 2UL; D8 = 3UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj |];
        [| ( { D1 = 2UL; D2 = [ 0UL; ]; D3 = None; D4 = 0UL; D5 = 0UL; D6 = 1UL; D7 = 2UL; D8 = 3UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj |];
        [| ( { D1 = 2UL; D2 = [ 0UL; 1UL; 2UL; 3UL; ]; D3 = None; D4 = 0UL; D5 = 0UL; D6 = 1UL; D7 = 2UL; D8 = 3UL; } : ConfRW_002_unsignedLong.T_Test ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ULong_007_data" )>]
    member _.SingleValue_ULong_007 ( s : ConfRW_002_unsignedLong.T_Test ) =
        try
            ConfRW_002_unsignedLong.ConfRW_UT002_unsignedLong.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>-32769</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>32768</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_Short_001 ( s : string ) =
        try
            ConfRW_002_short.ConfRW_UT002_short.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 1s )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2s )>]
    member _.SingleValue_Short_002 ( s : string ) ( exr : int16 ) =
        let r = ConfRW_002_short.ConfRW_UT002_short.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_Short_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0s; 1s; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ 0s; 1s; 2s; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Short_003_data" )>]
    member _.SingleValue_Short_003 ( s : String ) ( exr : int16 list ) =
        let r = ConfRW_002_short.ConfRW_UT002_short.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_Short_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1s ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>-32768</D3></Test>" :> obj; Some( -32768s ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>32767</D3></Test>" :> obj; Some( 32767s ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Short_004_data" )>]
    member _.SingleValue_Short_004 ( s : String ) ( exr : int16 option ) =
        let r = ConfRW_002_short.ConfRW_UT002_short.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_Short_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98s :> obj; int16 Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; 4s :> obj; 5s :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Short_005_data" )>]
    member _.SingleValue_Short_005 ( s : string ) ( exr_D7 : int16 ) ( exr_D8 : int16 ) =
        let r = ConfRW_002_short.ConfRW_UT002_short.LoadString s
        Assert.True( r.D4 = 0s )
        Assert.True( r.D5 = 99s )
        Assert.True( r.D6 = int16 Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_Short_006_data = [|
        [|
            ( { D1 = 1s; D2 = [ 0s; 1s; ]; D3 = None; D4 = 0s; D5 = 0s; D6 = 1s; D7 = 2s; D8 = 3s; } : ConfRW_002_short.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 2s; D2 = [ 0s; 1s; 2s; ]; D3 = None; D4 = 0s; D5 = 0s; D6 = 2s; D7 = 3s; D8 = 4s; } : ConfRW_002_short.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 1s; D2 = [ 0s; 1s; ]; D3 = Some( 5s ); D4 = 0s; D5 = 0s; D6 = 3s; D7 = 4s; D8 = 5s; } : ConfRW_002_short.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Short_006_data" )>]
    member _.SingleValue_Short_006 ( s : ConfRW_002_short.T_Test ) ( exr : string ) =
        let r = ConfRW_002_short.ConfRW_UT002_short.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_Short_007_data = [|
        [| ( { D1 = 0s; D2 = [ 0s; 1s; ]; D3 = None; D4 = 0s; D5 = 0s; D6 = 1s; D7 = 2s; D8 = 3s;  } : ConfRW_002_short.T_Test ) :> obj |];
        [| ( { D1 = 3s; D2 = [ 0s; 1s; ]; D3 = None; D4 = 0s; D5 = 0s; D6 = 1s; D7 = 2s; D8 = 3s;  } : ConfRW_002_short.T_Test ) :> obj |];
        [| ( { D1 = 2s; D2 = [ 0s; ]; D3 = None; D4 = 0s; D5 = 0s; D6 = 1s; D7 = 2s; D8 = 3s;  } : ConfRW_002_short.T_Test ) :> obj |];
        [| ( { D1 = 2s; D2 = [ 0s; 1s; 2s; 3s; ]; D3 = None; D4 = 0s; D5 = 0s; D6 = 1s; D7 = 2s; D8 = 3s;  } : ConfRW_002_short.T_Test ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Short_007_data" )>]
    member _.SingleValue_Short_007 ( s : ConfRW_002_short.T_Test ) =
        try
            ConfRW_002_short.ConfRW_UT002_short.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>-1</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>65536</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_UShort_001 ( s : string ) =
        try
            ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 1us )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2us )>]
    member _.SingleValue_UShort_002 ( s : string ) ( exr : uint16 ) =
        let r = ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_UShort_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0us; 1us; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ 0us; 1us; 2us; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UShort_003_data" )>]
    member _.SingleValue_UShort_003 ( s : String ) ( exr : uint16 list ) =
        let r = ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_UShort_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1us ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; Some( 0us ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>65535</D3></Test>" :> obj; Some( 65535us ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UShort_004_data" )>]
    member _.SingleValue_UShort_004 ( s : String ) ( exr : uint16 option ) =
        let r = ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_UShort_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98us :> obj; uint16 Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; 4us :> obj; 5us :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UShort_005_data" )>]
    member _.SingleValue_UShort_005 ( s : string ) ( exr_D7 : uint16 ) ( exr_D8 : uint16 ) =
        let r = ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.LoadString s
        Assert.True( r.D4 = 0us )
        Assert.True( r.D5 = 99us )
        Assert.True( r.D6 = uint16 Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_UShort_006_data = [|
        [|
            ( { D1 = 1us; D2 = [ 0us; 1us; ]; D3 = None; D4 = 0us; D5 = 0us; D6 = 1us; D7 = 2us; D8 = 3us; } : ConfRW_002_unsignedShort.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 2us; D2 = [ 0us; 1us; 2us; ]; D3 = None; D4 = 0us; D5 = 0us; D6 = 2us; D7 = 3us; D8 = 4us; } : ConfRW_002_unsignedShort.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 1us; D2 = [ 0us; 1us; ]; D3 = Some( 5us ); D4 = 0us; D5 = 0us; D6 = 3us; D7 = 4us; D8 = 5us; } : ConfRW_002_unsignedShort.T_Test ) :> obj;
            "<Test><D1>1</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UShort_006_data" )>]
    member _.SingleValue_UShort_006 ( s : ConfRW_002_unsignedShort.T_Test ) ( exr : string ) =
        let r = ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_UShort_007_data = [|
        [| ( { D1 = 0us; D2 = [ 0us; 1us; ]; D3 = None; D4 = 0us; D5 = 0us; D6 = 1us; D7 = 2us; D8 = 3us; } : ConfRW_002_unsignedShort.T_Test ) :> obj |];
        [| ( { D1 = 3us; D2 = [ 0us; 1us; ]; D3 = None; D4 = 0us; D5 = 0us; D6 = 1us; D7 = 2us; D8 = 3us; } : ConfRW_002_unsignedShort.T_Test ) :> obj |];
        [| ( { D1 = 2us; D2 = [ 0us; ]; D3 = None; D4 = 0us; D5 = 0us; D6 = 1us; D7 = 2us; D8 = 3us; } : ConfRW_002_unsignedShort.T_Test ) :> obj |];
        [| ( { D1 = 2us; D2 = [ 0us; 1us; 2us; 3us; ]; D3 = None; D4 = 0us; D5 = 0us; D6 = 1us; D7 = 2us; D8 = 3us; } : ConfRW_002_unsignedShort.T_Test ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_UShort_007_data" )>]
    member _.SingleValue_UShort_007 ( s : ConfRW_002_unsignedShort.T_Test ) =
        try
            ConfRW_002_unsignedShort.ConfRW_UT002_unsignedShort.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0.99</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2.01</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_Float_001 ( s : string ) =
        try
            ConfRW_002_double.ConfRW_UT002_double.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 1.0 )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2.0 )>]
    member _.SingleValue_Float_002 ( s : string ) ( exr : float ) =
        let r = ConfRW_002_double.ConfRW_UT002_double.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_Float_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ 0.0; 1.0; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0.0</D2><D2>1.0</D2><D2>2.0</D2><D3>0</D3></Test>" :> obj; [ 0.0; 1.0; 2.0; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Float_003_data" )>]
    member _.SingleValue_Float_003 ( s : String ) ( exr : float list ) =
        let r = ConfRW_002_double.ConfRW_UT002_double.LoadString s
        Assert.True( r.D2 = exr )


    static member m_SingleValue_Float_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( 1.0 ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; Some( 0.0 ) :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>65535</D3></Test>" :> obj; Some( 65535.0 ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Float_004_data" )>]
    member _.SingleValue_Float_004 ( s : String ) ( exr : float option ) =
        let r = ConfRW_002_double.ConfRW_UT002_double.LoadString s
        Assert.True( r.D3 = exr )


    static member m_SingleValue_Float_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; 98.0 :> obj; float Constants.MAX_TARGET_DEVICE_COUNT :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>1</D7><D8>2</D8></Test>" :> obj; 1.0 :> obj; 2.0 :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Float_005_data" )>]
    member _.SingleValue_Float_005 ( s : string ) ( exr_D7 : float ) ( exr_D8 : float ) =
        let r = ConfRW_002_double.ConfRW_UT002_double.LoadString s
        Assert.True( r.D4 = 0.0 )
        Assert.True( r.D5 = 99.0 )
        Assert.True( r.D6 = float Constants.MAX_TARGET_DEVICE_COUNT )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_Float_006_data = [|
        [|
            ( { D1 = 1.0; D2 = [ 0.0; 1.0; ]; D3 = None; D4 = 0.0; D5 = 0.0; D6 = 1.0; D7 = 2.0; D8 = 3.0; } : ConfRW_002_double.T_Test ) :> obj;
            "<Test><D1>1.000000</D1><D2>0.000000</D2><D2>1.000000</D2><D7>2.000000</D7><D8>3.000000</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 2.0; D2 = [ 0.0; 1.0; 2.0; ]; D3 = None; D4 = 0.0; D5 = 0.0; D6 = 2.0; D7 = 3.0; D8 = 4.0; } : ConfRW_002_double.T_Test ) :> obj;
            "<Test><D1>2.000000</D1><D2>0.000000</D2><D2>1.000000</D2><D2>2.000000</D2><D7>3.000000</D7><D8>4.000000</D8></Test>" :> obj
        |];
        [|
            ( { D1 = 1.0; D2 = [ 0.0; 1.0; ]; D3 = Some( 5.0 ); D4 = 0.0; D5 = 0.0; D6 = 3.0; D7 = 4.0; D8 = 5.0; } : ConfRW_002_double.T_Test ) :> obj;
            "<Test><D1>1.000000</D1><D2>0.000000</D2><D2>1.000000</D2><D3>5.000000</D3><D7>4.000000</D7><D8>5.000000</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Float_006_data" )>]
    member _.SingleValue_Float_006 ( s : ConfRW_002_double.T_Test ) ( exr : string ) =
        let r = ConfRW_002_double.ConfRW_UT002_double.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_Float_007_data = [|
        [| ( { D1 = 0.0; D2 = [ 0.0; 1.0; ]; D3 = None; D4 = 0.0; D5 = 0.0; D6 = 1.0; D7 = 2.0; D8 = 3.0; } : ConfRW_002_double.T_Test ) :> obj |];
        [| ( { D1 = 3.0; D2 = [ 0.0; 1.0; ]; D3 = None; D4 = 0.0; D5 = 0.0; D6 = 1.0; D7 = 2.0; D8 = 3.0; } : ConfRW_002_double.T_Test ) :> obj |];
        [| ( { D1 = 2.0; D2 = [ 0.0; ]; D3 = None; D4 = 0.0; D5 = 0.0; D6 = 1.0; D7 = 2.0; D8 = 3.0; } : ConfRW_002_double.T_Test ) :> obj |];
        [| ( { D1 = 2.0; D2 = [ 0.0; 1.0; 2.0; 3.0; ]; D3 = None; D4 = 0.0; D5 = 0.0; D6 = 1.0; D7 = 2.0; D8 = 3.0; } : ConfRW_002_double.T_Test ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Float_007_data" )>]
    member _.SingleValue_Float_007 ( s : ConfRW_002_double.T_Test ) =
        try
            ConfRW_002_double.ConfRW_UT002_double.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>true</D2><D2>true</D2><D3>true</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaa</D1><D2>true</D2><D2>true</D2><D3>true</D3></Test>" )>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D3>true</D3></Test>" )>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D2>true</D2><D2>true</D2><D3>true</D3></Test>" )>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3><D3>true</D3></Test>" )>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3><D3>true</D3><D3>true</D3></Test>" )>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3><D4>true</D4></Test>" )>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3><D5>true</D5></Test>" )>]
    member _.SingleValue_Bool_001 ( s : string ) =
        try
            ConfRW_002_boolean.ConfRW_UT002_boolean.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3></Test>", true )>]
    [<InlineData( "<Test><D1>false</D1><D2>true</D2><D2>true</D2><D3>true</D3></Test>", false )>]
    member _.SingleValue_Bool_002 ( s : string ) ( exr : bool ) =
        let r = ConfRW_002_boolean.ConfRW_UT002_boolean.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_Bool_003_data = [|
        [| "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3></Test>" :> obj; [ true; true; ] :> obj |];
        [| "<Test><D1>true</D1><D2>false</D2><D2>false</D2><D2>false</D2><D3>false</D3></Test>" :> obj; [ false; false; false; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Bool_003_data" )>]
    member _.SingleValue_Bool_003 ( s : String ) ( exr : bool list ) =
        let r = ConfRW_002_boolean.ConfRW_UT002_boolean.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_Bool_004_data = [|
        [| "<Test><D1>true</D1><D2>true</D2><D2>true</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>true</D3></Test>" :> obj; Some( true ) :> obj |];
        [| "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>false</D3></Test>" :> obj; Some( false ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Bool_004_data" )>]
    member _.SingleValue_Bool_004 ( s : String ) ( exr : bool option ) =
        let r = ConfRW_002_boolean.ConfRW_UT002_boolean.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_Bool_005_data = [|
        [| "<Test><D1>true</D1><D2>true</D2><D2>true</D2></Test>" :> obj; true :> obj; |];
        [| "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D6>false</D6></Test>" :> obj; false :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Bool_005_data" )>]
    member _.SingleValue_Bool_005 ( s : string ) ( exr : bool ) =
        let r = ConfRW_002_boolean.ConfRW_UT002_boolean.LoadString s
        Assert.True( r.D4 = false )
        Assert.True( r.D5 = false )
        Assert.True( r.D6 = exr )
                
    static member m_SingleValue_Bool_006_data = [|
        [|
            ( { D1 = true; D2 = [ true; true; ]; D3 = None; D4 = true; D5 = true; D6 = true; } : ConfRW_002_boolean.T_Test ) :> obj;
            "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D6>true</D6></Test>" :> obj
        |];
        [|
            ( { D1 = false; D2 = [ false; false; false ]; D3 = None; D4 = true; D5 = true; D6 = false; } : ConfRW_002_boolean.T_Test ) :> obj;
            "<Test><D1>false</D1><D2>false</D2><D2>false</D2><D2>false</D2><D6>false</D6></Test>" :> obj
        |];
        [|
            ( { D1 = true; D2 = [ true; true; ]; D3 = Some( false ); D4 = true; D5 = true; D6 = true; } : ConfRW_002_boolean.T_Test ) :> obj;
            "<Test><D1>true</D1><D2>true</D2><D2>true</D2><D3>false</D3><D6>true</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Bool_006_data" )>]
    member _.SingleValue_Bool_006 ( s : ConfRW_002_boolean.T_Test ) ( exr : string ) =
        let r = ConfRW_002_boolean.ConfRW_UT002_boolean.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_Bool_007_data = [|
        [| ( { D1 = true; D2 = [ true; ]; D3 = None; D4 = true; D5 = true; D6 = true; } : ConfRW_002_boolean.T_Test ) :> obj |];
        [| ( { D1 = true; D2 = [ true; true; true; true; ]; D3 = None; D4 = true; D5 = true; D6 = true; } : ConfRW_002_boolean.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Bool_007_data" )>]
    member _.SingleValue_Bool_007 ( s : ConfRW_002_boolean.T_Test ) =
        try
            ConfRW_002_boolean.ConfRW_UT002_boolean.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>4294967296</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_NETPORTIDX_001 ( s : string ) =
        try
            ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_NETPORTIDX_002 ( s : string ) ( exr : uint32 ) =
        let r = ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.LoadString s
        Assert.True( r.D1 = netportidx_me.fromPrim exr )

    static member m_SingleValue_NETPORTIDX_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ netportidx_me.fromPrim 0u; netportidx_me.fromPrim 1u; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ netportidx_me.fromPrim 0u; netportidx_me.fromPrim 1u; netportidx_me.fromPrim 2u; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_NETPORTIDX_003_data" )>]
    member _.SingleValue_NETPORTIDX_003 ( s : String ) ( exr : NETPORTIDX_T list ) =
        let r = ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_NETPORTIDX_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( netportidx_me.fromPrim 1u ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_NETPORTIDX_004_data" )>]
    member _.SingleValue_NETPORTIDX_004 ( s : String ) ( exr : NETPORTIDX_T option ) =
        let r = ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_NETPORTIDX_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; netportidx_me.fromPrim 98u :> obj; netportidx_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; netportidx_me.fromPrim 4u :> obj; netportidx_me.fromPrim 5u :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_NETPORTIDX_005_data" )>]
    member _.SingleValue_NETPORTIDX_005 ( s : string ) ( exr_D7 : NETPORTIDX_T ) ( exr_D8 : NETPORTIDX_T ) =
        let r = ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.LoadString s
        Assert.True( r.D4 = netportidx_me.fromPrim 0u )
        Assert.True( r.D5 = netportidx_me.fromPrim 99u )
        Assert.True( r.D6 = netportidx_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_NETPORTIDX_006_data = [|
        [|
            ( { D1 = netportidx_me.fromPrim 2u; D2 = [ netportidx_me.fromPrim 0u; netportidx_me.fromPrim 1u; ]; D3 = None; D4 = netportidx_me.fromPrim 0u; D5 = netportidx_me.fromPrim 0u; D6 = netportidx_me.fromPrim 1u; D7 = netportidx_me.fromPrim 2u; D8 = netportidx_me.fromPrim 3u; } : ConfRW_002_NETPORTIDX_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = netportidx_me.fromPrim 3u; D2 = [ netportidx_me.fromPrim 0u; netportidx_me.fromPrim 1u; netportidx_me.fromPrim 2u; ]; D3 = None; D4 = netportidx_me.fromPrim 0u; D5 = netportidx_me.fromPrim 0u; D6 = netportidx_me.fromPrim 2u; D7 = netportidx_me.fromPrim 3u; D8 = netportidx_me.fromPrim 4u; } : ConfRW_002_NETPORTIDX_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = netportidx_me.fromPrim 2u; D2 = [ netportidx_me.fromPrim 0u; netportidx_me.fromPrim 1u; ]; D3 = Some( netportidx_me.fromPrim 5u ); D4 = netportidx_me.fromPrim 0u; D5 = netportidx_me.fromPrim 0u; D6 = netportidx_me.fromPrim 3u; D7 = netportidx_me.fromPrim 4u; D8 = netportidx_me.fromPrim 5u; } : ConfRW_002_NETPORTIDX_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_NETPORTIDX_006_data" )>]
    member _.SingleValue_NETPORTIDX_006 ( s : ConfRW_002_NETPORTIDX_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_NETPORTIDX_007_data = [|
        [| ( { D1 = netportidx_me.fromPrim 2u; D2 = [ netportidx_me.fromPrim 0u; ]; D3 = None; D4 = netportidx_me.fromPrim 0u; D5 = netportidx_me.fromPrim 0u; D6 = netportidx_me.fromPrim 1u; D7 = netportidx_me.fromPrim 2u; D8 = netportidx_me.fromPrim 3u; } : ConfRW_002_NETPORTIDX_T.T_Test ) :> obj |];
        [| ( { D1 = netportidx_me.fromPrim 2u; D2 = [ netportidx_me.fromPrim 0u; netportidx_me.fromPrim 1u; netportidx_me.fromPrim 2u; netportidx_me.fromPrim 3u; ]; D3 = None; D4 = netportidx_me.fromPrim 0u; D5 = netportidx_me.fromPrim 0u; D6 = netportidx_me.fromPrim 1u; D7 = netportidx_me.fromPrim 2u; D8 = netportidx_me.fromPrim 3u; } : ConfRW_002_NETPORTIDX_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_NETPORTIDX_007_data" )>]
    member _.SingleValue_NETPORTIDX_007 ( s : ConfRW_002_NETPORTIDX_T.T_Test ) =
        try
            ConfRW_002_NETPORTIDX_T.ConfRW_UT002_NETPORTIDX_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>4294967296</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_TNODEIDX_001 ( s : string ) =
        try
            ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_TNODEIDX_002 ( s : string ) ( exr : uint32 ) =
        let r = ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.LoadString s
        Assert.True( r.D1 = tnodeidx_me.fromPrim exr )

    static member m_SingleValue_TNODEIDX_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ tnodeidx_me.fromPrim 0u; tnodeidx_me.fromPrim 1u; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ tnodeidx_me.fromPrim 0u; tnodeidx_me.fromPrim 1u; tnodeidx_me.fromPrim 2u; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TNODEIDX_003_data" )>]
    member _.SingleValue_TNODEIDX_003 ( s : String ) ( exr : TNODEIDX_T list ) =
        let r = ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_TNODEIDX_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( tnodeidx_me.fromPrim 1u ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TNODEIDX_004_data" )>]
    member _.SingleValue_TNODEIDX_004 ( s : String ) ( exr : TNODEIDX_T option ) =
        let r = ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_TNODEIDX_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; tnodeidx_me.fromPrim 98u :> obj; tnodeidx_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; tnodeidx_me.fromPrim 4u :> obj; tnodeidx_me.fromPrim 5u :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TNODEIDX_005_data" )>]
    member _.SingleValue_TNODEIDX_005 ( s : string ) ( exr_D7 : TNODEIDX_T ) ( exr_D8 : TNODEIDX_T ) =
        let r = ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.LoadString s
        Assert.True( r.D4 = tnodeidx_me.fromPrim 0u )
        Assert.True( r.D5 = tnodeidx_me.fromPrim 99u )
        Assert.True( r.D6 = tnodeidx_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_TNODEIDX_006_data = [|
        [|
            ( { D1 = tnodeidx_me.fromPrim 2u; D2 = [ tnodeidx_me.fromPrim 0u; tnodeidx_me.fromPrim 1u; ]; D3 = None; D4 = tnodeidx_me.fromPrim 0u; D5 = tnodeidx_me.fromPrim 0u; D6 = tnodeidx_me.fromPrim 1u; D7 = tnodeidx_me.fromPrim 2u; D8 = tnodeidx_me.fromPrim 3u; } : ConfRW_002_TNODEIDX_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = tnodeidx_me.fromPrim 3u; D2 = [ tnodeidx_me.fromPrim 0u; tnodeidx_me.fromPrim 1u; tnodeidx_me.fromPrim 2u; ]; D3 = None; D4 = tnodeidx_me.fromPrim 0u; D5 = tnodeidx_me.fromPrim 0u; D6 = tnodeidx_me.fromPrim 2u; D7 = tnodeidx_me.fromPrim 3u; D8 = tnodeidx_me.fromPrim 4u; } : ConfRW_002_TNODEIDX_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = tnodeidx_me.fromPrim 2u; D2 = [ tnodeidx_me.fromPrim 0u; tnodeidx_me.fromPrim 1u; ]; D3 = Some( tnodeidx_me.fromPrim 5u ); D4 = tnodeidx_me.fromPrim 0u; D5 = tnodeidx_me.fromPrim 0u; D6 = tnodeidx_me.fromPrim 3u; D7 = tnodeidx_me.fromPrim 4u; D8 = tnodeidx_me.fromPrim 5u; } : ConfRW_002_TNODEIDX_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TNODEIDX_006_data" )>]
    member _.SingleValue_TNODEIDX_006 ( s : ConfRW_002_TNODEIDX_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_TNODEIDX_007_data = [|
        [| ( { D1 = tnodeidx_me.fromPrim 2u; D2 = [ tnodeidx_me.fromPrim 0u; ]; D3 = None; D4 = tnodeidx_me.fromPrim 0u; D5 = tnodeidx_me.fromPrim 0u; D6 = tnodeidx_me.fromPrim 1u; D7 = tnodeidx_me.fromPrim 2u; D8 = tnodeidx_me.fromPrim 3u; } : ConfRW_002_TNODEIDX_T.T_Test ) :> obj |];
        [| ( { D1 = tnodeidx_me.fromPrim 2u; D2 = [ tnodeidx_me.fromPrim 0u; tnodeidx_me.fromPrim 1u; tnodeidx_me.fromPrim 2u; tnodeidx_me.fromPrim 3u; ]; D3 = None; D4 = tnodeidx_me.fromPrim 0u; D5 = tnodeidx_me.fromPrim 0u; D6 = tnodeidx_me.fromPrim 1u; D7 = tnodeidx_me.fromPrim 2u; D8 = tnodeidx_me.fromPrim 3u; } : ConfRW_002_TNODEIDX_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TNODEIDX_007_data" )>]
    member _.SingleValue_TNODEIDX_007 ( s : ConfRW_002_TNODEIDX_T.T_Test ) =
        try
            ConfRW_002_TNODEIDX_T.ConfRW_UT002_TNODEIDX_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>4294967296</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_MEDIAIDX_001 ( s : string ) =
        try
            ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_MEDIAIDX_002 ( s : string ) ( exr : uint32 ) =
        let r = ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.LoadString s
        Assert.True( r.D1 = mediaidx_me.fromPrim exr )

    static member m_SingleValue_MEDIAIDX_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ mediaidx_me.fromPrim 0u; mediaidx_me.fromPrim 1u; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ mediaidx_me.fromPrim 0u; mediaidx_me.fromPrim 1u; mediaidx_me.fromPrim 2u; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_MEDIAIDX_003_data" )>]
    member _.SingleValue_MEDIAIDX_003 ( s : String ) ( exr : MEDIAIDX_T list ) =
        let r = ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_MEDIAIDX_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( mediaidx_me.fromPrim 1u ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_MEDIAIDX_004_data" )>]
    member _.SingleValue_MEDIAIDX_004 ( s : String ) ( exr : MEDIAIDX_T option ) =
        let r = ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_MEDIAIDX_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; mediaidx_me.fromPrim 98u :> obj; mediaidx_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; mediaidx_me.fromPrim 4u :> obj; mediaidx_me.fromPrim 5u :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_MEDIAIDX_005_data" )>]
    member _.SingleValue_MEDIAIDX_005 ( s : string ) ( exr_D7 : MEDIAIDX_T ) ( exr_D8 : MEDIAIDX_T ) =
        let r = ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.LoadString s
        Assert.True( r.D4 = mediaidx_me.fromPrim 0u; )
        Assert.True( r.D5 = mediaidx_me.fromPrim 99u; )
        Assert.True( r.D6 = mediaidx_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_MEDIAIDX_006_data = [|
        [|
            ( { D1 = mediaidx_me.fromPrim 2u; D2 = [ mediaidx_me.fromPrim 0u; mediaidx_me.fromPrim 1u; ]; D3 = None; D4 = mediaidx_me.fromPrim 0u; D5 = mediaidx_me.fromPrim 0u; D6 = mediaidx_me.fromPrim 1u; D7 = mediaidx_me.fromPrim 2u; D8 = mediaidx_me.fromPrim 3u; } : ConfRW_002_MEDIAIDX_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = mediaidx_me.fromPrim 3u; D2 = [ mediaidx_me.fromPrim 0u; mediaidx_me.fromPrim 1u; mediaidx_me.fromPrim 2u; ]; D3 = None; D4 = mediaidx_me.fromPrim 0u; D5 = mediaidx_me.fromPrim 0u; D6 = mediaidx_me.fromPrim 2u; D7 = mediaidx_me.fromPrim 3u; D8 = mediaidx_me.fromPrim 4u; } : ConfRW_002_MEDIAIDX_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = mediaidx_me.fromPrim 2u; D2 = [ mediaidx_me.fromPrim 0u; mediaidx_me.fromPrim 1u; ]; D3 = Some( mediaidx_me.fromPrim 5u ); D4 = mediaidx_me.fromPrim 0u; D5 = mediaidx_me.fromPrim 0u; D6 = mediaidx_me.fromPrim 3u; D7 = mediaidx_me.fromPrim 4u; D8 = mediaidx_me.fromPrim 5u; } : ConfRW_002_MEDIAIDX_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_MEDIAIDX_006_data" )>]
    member _.SingleValue_MEDIAIDX_006 ( s : ConfRW_002_MEDIAIDX_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_MEDIAIDX_007_data = [|
        [| ( { D1 = mediaidx_me.fromPrim 2u; D2 = [ mediaidx_me.fromPrim 0u; ]; D3 = None; D4 = mediaidx_me.fromPrim 0u; D5 = mediaidx_me.fromPrim 0u; D6 = mediaidx_me.fromPrim 1u; D7 = mediaidx_me.fromPrim 2u; D8 = mediaidx_me.fromPrim 3u; } : ConfRW_002_MEDIAIDX_T.T_Test ) :> obj |];
        [| ( { D1 = mediaidx_me.fromPrim 2u; D2 = [ mediaidx_me.fromPrim 0u; mediaidx_me.fromPrim 1u; mediaidx_me.fromPrim 2u; mediaidx_me.fromPrim 3u; ]; D3 = None; D4 = mediaidx_me.fromPrim 0u; D5 = mediaidx_me.fromPrim 0u; D6 = mediaidx_me.fromPrim 1u; D7 = mediaidx_me.fromPrim 2u; D8 = mediaidx_me.fromPrim 3u; } : ConfRW_002_MEDIAIDX_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_MEDIAIDX_007_data" )>]
    member _.SingleValue_MEDIAIDX_007 ( s : ConfRW_002_MEDIAIDX_T.T_Test ) =
        try
            ConfRW_002_MEDIAIDX_T.ConfRW_UT002_MEDIAIDX_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>65536</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_TPGT_001 ( s : string ) =
        try
            ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2us )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3us )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4us )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5us )>]
    member _.SingleValue_TPGT_002 ( s : string ) ( exr : uint16 ) =
        let r = ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.LoadString s
        Assert.True( r.D1 = tpgt_me.fromPrim exr )

    static member m_SingleValue_TPGT_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ tpgt_me.fromPrim 0us; tpgt_me.fromPrim 1us; ]:> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ tpgt_me.fromPrim 0us; tpgt_me.fromPrim 1us; tpgt_me.fromPrim 2us; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TPGT_003_data" )>]
    member _.SingleValue_TPGT_003 ( s : String ) ( exr : TPGT_T list ) =
        let r = ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_TPGT_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( tpgt_me.fromPrim 1us ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TPGT_004_data" )>]
    member _.SingleValue_TPGT_004 ( s : String ) ( exr : TPGT_T option ) =
        let r = ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_TPGT_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; tpgt_me.fromPrim 98us :> obj; tpgt_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; tpgt_me.fromPrim 4us :> obj; tpgt_me.fromPrim 5us :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TPGT_005_data" )>]
    member _.SingleValue_TPGT_005 ( s : string ) ( exr_D7 : TPGT_T ) ( exr_D8 : TPGT_T ) =
        let r = ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.LoadString s
        Assert.True( r.D4 = tpgt_me.fromPrim 0us )
        Assert.True( r.D5 = tpgt_me.fromPrim 99us )
        Assert.True( r.D6 = tpgt_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_TPGT_006_data = [|
        [|
            ( { D1 = tpgt_me.fromPrim 2us; D2 = [ tpgt_me.fromPrim 0us; tpgt_me.fromPrim 1us; ]; D3 = None; D4 = tpgt_me.fromPrim 0us; D5 = tpgt_me.fromPrim 0us; D6 = tpgt_me.fromPrim 1us; D7 = tpgt_me.fromPrim 2us; D8 = tpgt_me.fromPrim 3us; } : ConfRW_002_TPGT_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = tpgt_me.fromPrim 3us; D2 = [ tpgt_me.fromPrim 0us; tpgt_me.fromPrim 1us; tpgt_me.fromPrim 2us; ]; D3 = None; D4 = tpgt_me.fromPrim 0us; D5 = tpgt_me.fromPrim 0us; D6 = tpgt_me.fromPrim 2us; D7 = tpgt_me.fromPrim 3us; D8 = tpgt_me.fromPrim 4us; } : ConfRW_002_TPGT_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = tpgt_me.fromPrim 2us; D2 = [ tpgt_me.fromPrim 0us; tpgt_me.fromPrim 1us; ]; D3 = Some( tpgt_me.fromPrim 5us ); D4 = tpgt_me.fromPrim 0us; D5 = tpgt_me.fromPrim 0us; D6 = tpgt_me.fromPrim 3us; D7 = tpgt_me.fromPrim 4us; D8 = tpgt_me.fromPrim 5us; } : ConfRW_002_TPGT_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TPGT_006_data" )>]
    member _.SingleValue_TPGT_006 ( s : ConfRW_002_TPGT_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_TPGT_007_data = [|
        [| ( { D1 = tpgt_me.fromPrim 2us; D2 = [ tpgt_me.fromPrim 0us; ]; D3 = None; D4 = tpgt_me.fromPrim 0us; D5 = tpgt_me.fromPrim 0us; D6 = tpgt_me.fromPrim 3us; D7 = tpgt_me.fromPrim 3us; D8 = tpgt_me.fromPrim 3us; } : ConfRW_002_TPGT_T.T_Test ) :> obj |];
        [| ( { D1 = tpgt_me.fromPrim 2us; D2 = [ tpgt_me.fromPrim 0us; tpgt_me.fromPrim 1us; tpgt_me.fromPrim 2us; tpgt_me.fromPrim 3us; ]; D3 = None; D4 = tpgt_me.fromPrim 0us; D5 = tpgt_me.fromPrim 0us; D6 = tpgt_me.fromPrim 3us; D7 = tpgt_me.fromPrim 3us; D8 = tpgt_me.fromPrim 3us; } : ConfRW_002_TPGT_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TPGT_007_data" )>]
    member _.SingleValue_TPGT_007 ( s : ConfRW_002_TPGT_T.T_Test ) =
        try
            ConfRW_002_TPGT_T.ConfRW_UT002_TPGT_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>00-00</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>xx</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>GG</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>256</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    member _.SingleValue_LUN_001 ( s : string ) =
        try
            ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 0UL )>]
    [<InlineData( "<Test><D1>11</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 11UL )>]
    [<InlineData( "<Test><D1>255</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 255UL )>]
    member _.SingleValue_LUN_002 ( s : string ) ( exr : uint64 ) =
        let r = ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.LoadString s
        Assert.True( r.D1 = lun_me.fromPrim exr )

    static member m_SingleValue_LUN_003_data = [|
        [|
            "<Test><D1>0</D1><D2>0</D2><D2>11</D2></Test>" :> obj;
             [ lun_me.fromPrim 0UL; lun_me.fromPrim 11UL; ] :> obj
        |];
        [|
            "<Test><D1>0</D1><D2>0</D2><D2>11</D2><D2>22</D2></Test>" :> obj;
            [ lun_me.fromPrim 0UL; lun_me.fromPrim 11UL; lun_me.fromPrim 22UL; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LUN_003_data" )>]
    member _.SingleValue_LUN_003 ( s : String ) ( exr : LUN_T list ) =
        let r = ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_LUN_004_data = [|
        [| "<Test><D1>78</D1><D2>0</D2><D2>0</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>76</D3></Test>" :> obj; Some( lun_me.fromPrim 76UL ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LUN_004_data" )>]
    member _.SingleValue_LUN_004 ( s : String ) ( exr : LUN_T option ) =
        let r = ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_LUN_005_data = [|
        [|
            "<Test><D1>88</D1><D2>0</D2><D2>0</D2></Test>" :> obj;
            lun_me.fromPrim 98UL :> obj;
        |];
        [|
            "<Test><D1>88</D1><D2>0</D2><D2>0</D2><D6>0</D6></Test>" :> obj;
            lun_me.fromPrim 0UL :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LUN_005_data" )>]
    member _.SingleValue_LUN_005 ( s : string ) ( exr : LUN_T ) =
        let r = ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.LoadString s
        Assert.True( r.D4 = lun_me.fromPrim 0UL )
        Assert.True( r.D5 = lun_me.fromPrim 99UL )
        Assert.True( r.D6 = exr )

    static member m_SingleValue_LUN_006_data = [|
        [|
            ( { D1 = lun_me.fromPrim 88UL; D2 = [ lun_me.fromPrim 0UL; lun_me.fromPrim 11UL; ]; D3 = None; D4 = lun_me.zero; D5 = lun_me.zero; D6 = lun_me.fromPrim 1UL; } : ConfRW_002_LUN_T.T_Test ) :> obj;
            "<Test><D1>88</D1><D2>0</D2><D2>11</D2><D6>1</D6></Test>" :> obj
        |];
        [|
            ( { D1 = lun_me.fromPrim 33UL; D2 = [ lun_me.fromPrim 0UL; lun_me.fromPrim 11UL; lun_me.fromPrim 22UL; ]; D3 = None; D4 = lun_me.zero; D5 = lun_me.zero; D6 = lun_me.fromPrim 2UL; } : ConfRW_002_LUN_T.T_Test ) :> obj;
            "<Test><D1>33</D1><D2>0</D2><D2>11</D2><D2>22</D2><D6>2</D6></Test>" :> obj
        |];
        [|
            ( { D1 = lun_me.fromPrim 22UL; D2 = [ lun_me.fromPrim 0UL; lun_me.fromPrim 11UL; ]; D3 = Some( lun_me.fromPrim 55UL ); D4 = lun_me.zero; D5 = lun_me.zero; D6 = lun_me.fromPrim 3UL; } : ConfRW_002_LUN_T.T_Test ) :> obj;
            "<Test><D1>22</D1><D2>0</D2><D2>11</D2><D3>55</D3><D6>3</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LUN_006_data" )>]
    member _.SingleValue_LUN_006 ( s : ConfRW_002_LUN_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_LUN_007_data = [|
        [| ( { D1 = lun_me.fromPrim 0UL; D2 = [ lun_me.fromPrim 0UL; ]; D3 = None; D4 = lun_me.zero; D5 = lun_me.zero; D6 = lun_me.fromPrim 1UL; } : ConfRW_002_LUN_T.T_Test ) :> obj |];
        [| ( { D1 = lun_me.fromPrim 0UL; D2 = [ lun_me.fromPrim 0UL; lun_me.fromPrim 0UL; lun_me.fromPrim 0UL; lun_me.fromPrim 0UL; ]; D3 = None; D4 = lun_me.zero; D5 = lun_me.zero; D6 = lun_me.fromPrim 1UL; } : ConfRW_002_LUN_T.T_Test ) :> obj |];
        [| ( { D1 = lun_me.fromPrim 256UL; D2 = [ lun_me.fromPrim 0UL; lun_me.fromPrim 0UL; ]; D3 = None; D4 = lun_me.zero; D5 = lun_me.zero; D6 = lun_me.fromPrim 1UL; } : ConfRW_002_LUN_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LUN_007_data" )>]
    member _.SingleValue_LUN_007 ( s : ConfRW_002_LUN_T.T_Test ) =
        try
            ConfRW_002_LUN_T.ConfRW_UT002_LUN_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>18446744073709551616</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_RESVKEY_001 ( s : string ) =
        try
            ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2UL )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3UL )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4UL )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5UL )>]
    member _.SingleValue_RESVKEY_002 ( s : string ) ( exr : uint64 ) =
        let r = ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.LoadString s
        Assert.True( r.D1 = resvkey_me.fromPrim exr )

    static member m_SingleValue_RESVKEY_003_data = [|
        [|
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj;
             [ resvkey_me.fromPrim 0UL; resvkey_me.fromPrim 1UL; ] :> obj
        |];
        [|
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj;
            [ resvkey_me.fromPrim 0UL; resvkey_me.fromPrim 1UL; resvkey_me.fromPrim 2UL; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_RESVKEY_003_data" )>]
    member _.SingleValue_RESVKEY_003 ( s : String ) ( exr : RESVKEY_T list ) =
        let r = ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_RESVKEY_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( resvkey_me.fromPrim 1UL ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_RESVKEY_004_data" )>]
    member _.SingleValue_RESVKEY_004 ( s : String ) ( exr : RESVKEY_T option ) =
        let r = ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_RESVKEY_005_data = [|
        [|
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj;
            resvkey_me.fromPrim 98UL :> obj;
            resvkey_me.fromPrim ( uint64 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj;
        |];
        [|
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj;
            resvkey_me.fromPrim 4UL :> obj;
            resvkey_me.fromPrim 5UL :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_RESVKEY_005_data" )>]
    member _.SingleValue_RESVKEY_005 ( s : string ) ( exr_D7 : RESVKEY_T ) ( exr_D8 : RESVKEY_T ) =
        let r = ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.LoadString s
        Assert.True( r.D4 = resvkey_me.fromPrim 0UL )
        Assert.True( r.D5 = resvkey_me.fromPrim 99UL )
        Assert.True( r.D6 = resvkey_me.fromPrim ( uint64 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_RESVKEY_006_data = [|
        [|
            ( { D1 = resvkey_me.fromPrim 2UL; D2 = [ resvkey_me.fromPrim 0UL; resvkey_me.fromPrim 1UL; ]; D3 = None; D4 = resvkey_me.fromPrim 0UL; D5 = resvkey_me.fromPrim 0UL; D6 = resvkey_me.fromPrim 1UL; D7 = resvkey_me.fromPrim 2UL; D8 = resvkey_me.fromPrim 3UL; } : ConfRW_002_RESVKEY_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = resvkey_me.fromPrim 3UL; D2 = [ resvkey_me.fromPrim 0UL; resvkey_me.fromPrim 1UL; resvkey_me.fromPrim 2UL; ]; D3 = None; D4 = resvkey_me.fromPrim 0UL; D5 = resvkey_me.fromPrim 0UL; D6 = resvkey_me.fromPrim 2UL; D7 = resvkey_me.fromPrim 3UL; D8 = resvkey_me.fromPrim 4UL; } : ConfRW_002_RESVKEY_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = resvkey_me.fromPrim 2UL; D2 = [ resvkey_me.fromPrim 0UL; resvkey_me.fromPrim 1UL; ]; D3 = Some( resvkey_me.fromPrim 5UL ); D4 = resvkey_me.fromPrim 0UL; D5 = resvkey_me.fromPrim 0UL; D6 = resvkey_me.fromPrim 3UL; D7 = resvkey_me.fromPrim 4UL; D8 = resvkey_me.fromPrim 5UL; } : ConfRW_002_RESVKEY_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_RESVKEY_006_data" )>]
    member _.SingleValue_RESVKEY_006 ( s : ConfRW_002_RESVKEY_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_RESVKEY_007_data = [|
        [| ( { D1 = resvkey_me.fromPrim 2UL; D2 = [ resvkey_me.fromPrim 0UL; ]; D3 = None; D4 = resvkey_me.fromPrim 0UL; D5 = resvkey_me.fromPrim 0UL; D6 = resvkey_me.fromPrim 1UL; D7 = resvkey_me.fromPrim 2UL; D8 = resvkey_me.fromPrim 3UL; } : ConfRW_002_RESVKEY_T.T_Test ) :> obj |];
        [| ( { D1 = resvkey_me.fromPrim 2UL; D2 = [ resvkey_me.fromPrim 0UL; resvkey_me.fromPrim 1UL; resvkey_me.fromPrim 2UL; resvkey_me.fromPrim 3UL; ]; D3 = None; D4 = resvkey_me.fromPrim 0UL; D5 = resvkey_me.fromPrim 0UL; D6 = resvkey_me.fromPrim 1UL; D7 = resvkey_me.fromPrim 2UL; D8 = resvkey_me.fromPrim 3UL; } : ConfRW_002_RESVKEY_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_RESVKEY_007_data" )>]
    member _.SingleValue_RESVKEY_007 ( s : ConfRW_002_RESVKEY_T.T_Test ) =
        try
            ConfRW_002_RESVKEY_T.ConfRW_UT002_RESVKEY_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1></D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaaa</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" )>]
    [<InlineData( "<Test><D1>None</D1><D2>None</D2><D3>None</D3></Test>" )>]
    [<InlineData( "<Test><D1>None</D1><D2>None</D2><D2>None</D2><D2>None</D2><D2>None</D2><D3>None</D3></Test>" )>]
    [<InlineData( "<Test><D1>None</D1><D2>None</D2><D2>None</D2><D3>None</D3><D3>None</D3></Test>" )>]
    [<InlineData( "<Test><D1>None</D1><D2>None</D2><D2>None</D2><D3>None</D3><D3>None</D3><D3>None</D3></Test>" )>]
    [<InlineData( "<Test><D1>None</D1><D2>None</D2><D2>None</D2><D3>None</D3><D4>None</D4></Test>" )>]
    [<InlineData( "<Test><D1>None</D1><D2>None</D2><D2>None</D2><D3>None</D3><D5>None</D5></Test>" )>]
    member _.SingleValue_AuthMethodCandidateValue_001 ( s : string ) =
        try
            ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    static member m_SingleValue_AuthMethodCandidateValue_002_data = [|
        [| "<Test><D1>None</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_None :> obj |];
        [| "<Test><D1>CHAP</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_CHAP :> obj |];
        [| "<Test><D1>SRP</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_SRP :> obj |];
        [| "<Test><D1>KRB5</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_KRB5 :> obj |];
        [| "<Test><D1>SPKM1</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_SPKM1 :> obj |];
        [| "<Test><D1>SPKM2</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_SPKM2 :> obj |];
        [| "<Test><D1>NotUnderstood</D1><D2>None</D2><D2>None</D2><D3>None</D3></Test>" :> obj; AuthMethodCandidateValue.AMC_NotUnderstood :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_AuthMethodCandidateValue_002_data" )>]
    member _.SingleValue_AuthMethodCandidateValue_002 ( inv : string ) ( outr : AuthMethodCandidateValue ) =
        let r = ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.LoadString inv
        Assert.True( r.D1 = outr )

    static member m_SingleValue_AuthMethodCandidateValue_003_data = [|
        [|
            "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2><D3>None</D3></Test>" :> obj;
             [ AuthMethodCandidateValue.AMC_None; AuthMethodCandidateValue.AMC_CHAP; ] :> obj
        |];
        [|
            "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2><D2>SRP</D2><D3>None</D3></Test>" :> obj;
            [ AuthMethodCandidateValue.AMC_None; AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_SRP; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_AuthMethodCandidateValue_003_data" )>]
    member _.SingleValue_AuthMethodCandidateValue_003 ( s : String ) ( exr : AuthMethodCandidateValue list ) =
        let r = ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_AuthMethodCandidateValue_004_data = [|
        [| "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2><D3>CHAP</D3></Test>" :> obj; Some( AuthMethodCandidateValue.AMC_CHAP ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_AuthMethodCandidateValue_004_data" )>]
    member _.SingleValue_AuthMethodCandidateValue_004 ( s : String ) ( exr : AuthMethodCandidateValue option ) =
        let r = ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_AuthMethodCandidateValue_005_data = [|
        [|
            "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2></Test>" :> obj;
            AuthMethodCandidateValue.AMC_SPKM1 :> obj;
        |];
        [|
            "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2><D6>KRB5</D6></Test>" :> obj;
            AuthMethodCandidateValue.AMC_KRB5 :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_AuthMethodCandidateValue_005_data" )>]
    member _.SingleValue_AuthMethodCandidateValue_005 ( s : string ) ( exr : AuthMethodCandidateValue ) =
        let r = ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.LoadString s
        Assert.True( r.D4 = AuthMethodCandidateValue.AMC_None )
        Assert.True( r.D5 = AuthMethodCandidateValue.AMC_SRP )
        Assert.True( r.D6 = exr )

    static member m_SingleValue_AuthMethodCandidateValue_006_data = [|
        [|
            ( { D1 = AuthMethodCandidateValue.AMC_None; D2 = [ AuthMethodCandidateValue.AMC_None; AuthMethodCandidateValue.AMC_CHAP; ]; D3 = None; D4 = AuthMethodCandidateValue.AMC_None; D5 = AuthMethodCandidateValue.AMC_None; D6 = AuthMethodCandidateValue.AMC_KRB5; } : ConfRW_002_AuthMethodCandidateValue.T_Test ) :> obj;
            "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2><D6>KRB5</D6></Test>" :> obj
        |];
        [|
            ( { D1 = AuthMethodCandidateValue.AMC_CHAP; D2 = [ AuthMethodCandidateValue.AMC_None; AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_SRP; ]; D3 = None; D4 = AuthMethodCandidateValue.AMC_None; D5 = AuthMethodCandidateValue.AMC_None; D6 = AuthMethodCandidateValue.AMC_CHAP; } : ConfRW_002_AuthMethodCandidateValue.T_Test ) :> obj;
            "<Test><D1>CHAP</D1><D2>None</D2><D2>CHAP</D2><D2>SRP</D2><D6>CHAP</D6></Test>" :> obj
        |];
        [|
            ( { D1 = AuthMethodCandidateValue.AMC_None; D2 = [ AuthMethodCandidateValue.AMC_None; AuthMethodCandidateValue.AMC_CHAP; ]; D3 = Some( AuthMethodCandidateValue.AMC_NotUnderstood ); D4 = AuthMethodCandidateValue.AMC_None; D5 = AuthMethodCandidateValue.AMC_None; D6 = AuthMethodCandidateValue.AMC_SPKM2; } : ConfRW_002_AuthMethodCandidateValue.T_Test ) :> obj;
            "<Test><D1>None</D1><D2>None</D2><D2>CHAP</D2><D3>NotUnderstood</D3><D6>SPKM2</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_AuthMethodCandidateValue_006_data" )>]
    member _.SingleValue_AuthMethodCandidateValue_006 ( s : ConfRW_002_AuthMethodCandidateValue.T_Test ) ( exr : string ) =
        let r = ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_AuthMethodCandidateValue_007_data = [|
        [| ( { D1 = AuthMethodCandidateValue.AMC_CHAP; D2 = [ AuthMethodCandidateValue.AMC_None; ]; D3 = None; D4 = AuthMethodCandidateValue.AMC_None; D5 = AuthMethodCandidateValue.AMC_None; D6 = AuthMethodCandidateValue.AMC_KRB5; } : ConfRW_002_AuthMethodCandidateValue.T_Test ) :> obj |];
        [| ( { D1 = AuthMethodCandidateValue.AMC_CHAP; D2 = [ AuthMethodCandidateValue.AMC_None; AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_SRP; AuthMethodCandidateValue.AMC_KRB5; ]; D3 = None; D4 = AuthMethodCandidateValue.AMC_None; D5 = AuthMethodCandidateValue.AMC_None; D6 = AuthMethodCandidateValue.AMC_KRB5; } : ConfRW_002_AuthMethodCandidateValue.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_AuthMethodCandidateValue_007_data" )>]
    member _.SingleValue_AuthMethodCandidateValue_007 ( s : ConfRW_002_AuthMethodCandidateValue.T_Test ) =
        try
            ConfRW_002_AuthMethodCandidateValue.ConfRW_UT002_AuthMethodCandidateValue.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1></D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaaa</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" )>]
    [<InlineData( "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" )>]
    [<InlineData( "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" )>]
    [<InlineData( "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3><D3>NO_RESERVATION</D3></Test>" )>]
    [<InlineData( "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3><D3>NO_RESERVATION</D3><D3>NO_RESERVATION</D3></Test>" )>]
    [<InlineData( "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3><D4>NO_RESERVATION</D4></Test>" )>]
    [<InlineData( "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3><D5>NO_RESERVATION</D5></Test>" )>]
    member _.SingleValue_PR_TYPE_001 ( s : string ) =
        try
            ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    static member m_SingleValue_PR_TYPE_002_data = [|
        [| "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.NO_RESERVATION :> obj |];
        [| "<Test><D1>WRITE_EXCLUSIVE</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.WRITE_EXCLUSIVE :> obj |];
        [| "<Test><D1>EXCLUSIVE_ACCESS</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.EXCLUSIVE_ACCESS :> obj |];
        [| "<Test><D1>WRITE_EXCLUSIVE_REGISTRANTS_ONLY</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY :> obj |];
        [| "<Test><D1>WRITE_EXCLUSIVE_ALL_REGISTRANTS</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS :> obj |];
        [| "<Test><D1>EXCLUSIVE_ACCESS_REGISTRANTS_ONLY</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY :> obj |];
        [| "<Test><D1>EXCLUSIVE_ACCESS_ALL_REGISTRANTS</D1><D2>NO_RESERVATION</D2><D2>NO_RESERVATION</D2><D3>NO_RESERVATION</D3></Test>" :> obj; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_PR_TYPE_002_data" )>]
    member _.SingleValue_PR_TYPE_002 ( inv : string ) ( outr : PR_TYPE ) =
        let r = ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.LoadString inv
        Assert.True( r.D1 = outr )

    static member m_SingleValue_PR_TYPE_003_data = [|
        [|
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D3>NO_RESERVATION</D3></Test>" :> obj;
             [ PR_TYPE.NO_RESERVATION; PR_TYPE.WRITE_EXCLUSIVE; ] :> obj
        |];
        [|
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D2>EXCLUSIVE_ACCESS</D2><D3>NO_RESERVATION</D3></Test>" :> obj;
            [ PR_TYPE.NO_RESERVATION; PR_TYPE.WRITE_EXCLUSIVE; PR_TYPE.EXCLUSIVE_ACCESS; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_PR_TYPE_003_data" )>]
    member _.SingleValue_PR_TYPE_003 ( s : String ) ( exr : PR_TYPE list ) =
        let r = ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_PR_TYPE_004_data = [|
        [| "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D3>WRITE_EXCLUSIVE</D3></Test>" :> obj; Some( PR_TYPE.WRITE_EXCLUSIVE ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_PR_TYPE_004_data" )>]
    member _.SingleValue_PR_TYPE_004 ( s : String ) ( exr : PR_TYPE option ) =
        let r = ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_PR_TYPE_005_data = [|
        [|
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2></Test>" :> obj;
            PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS :> obj;
        |];
        [|
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D6>WRITE_EXCLUSIVE_ALL_REGISTRANTS</D6></Test>" :> obj;
            PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_PR_TYPE_005_data" )>]
    member _.SingleValue_PR_TYPE_005 ( s : string ) ( exr : PR_TYPE ) =
        let r = ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.LoadString s
        Assert.True( r.D4 = PR_TYPE.NO_RESERVATION )
        Assert.True( r.D5 = PR_TYPE.WRITE_EXCLUSIVE )
        Assert.True( r.D6 = exr )

    static member m_SingleValue_PR_TYPE_006_data = [|
        [|
            ( { D1 = PR_TYPE.NO_RESERVATION; D2 = [ PR_TYPE.NO_RESERVATION; PR_TYPE.WRITE_EXCLUSIVE; ]; D3 = None; D4 = PR_TYPE.NO_RESERVATION; D5 = PR_TYPE.NO_RESERVATION; D6 = PR_TYPE.EXCLUSIVE_ACCESS; } : ConfRW_002_PR_TYPE.T_Test ) :> obj;
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D6>EXCLUSIVE_ACCESS</D6></Test>" :> obj
        |];
        [|
            ( { D1 = PR_TYPE.NO_RESERVATION; D2 = [ PR_TYPE.NO_RESERVATION; PR_TYPE.WRITE_EXCLUSIVE; PR_TYPE.EXCLUSIVE_ACCESS; ]; D3 = None; D4 = PR_TYPE.NO_RESERVATION; D5 = PR_TYPE.NO_RESERVATION; D6 = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY; } : ConfRW_002_PR_TYPE.T_Test ) :> obj;
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D2>EXCLUSIVE_ACCESS</D2><D6>WRITE_EXCLUSIVE_REGISTRANTS_ONLY</D6></Test>" :> obj
        |];
        [|
            ( { D1 = PR_TYPE.NO_RESERVATION; D2 = [ PR_TYPE.NO_RESERVATION; PR_TYPE.WRITE_EXCLUSIVE; ]; D3 = Some( PR_TYPE.EXCLUSIVE_ACCESS ); D4 = PR_TYPE.NO_RESERVATION; D5 = PR_TYPE.NO_RESERVATION; D6 = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS; } : ConfRW_002_PR_TYPE.T_Test ) :> obj;
            "<Test><D1>NO_RESERVATION</D1><D2>NO_RESERVATION</D2><D2>WRITE_EXCLUSIVE</D2><D3>EXCLUSIVE_ACCESS</D3><D6>WRITE_EXCLUSIVE_ALL_REGISTRANTS</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_PR_TYPE_006_data" )>]
    member _.SingleValue_PR_TYPE_006 ( s : ConfRW_002_PR_TYPE.T_Test ) ( exr : string ) =
        let r = ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_PR_TYPE_007_data = [|
        [| ( { D1 = PR_TYPE.NO_RESERVATION; D2 = [ PR_TYPE.NO_RESERVATION; ]; D3 = None; D4 = PR_TYPE.NO_RESERVATION; D5 = PR_TYPE.NO_RESERVATION; D6 = PR_TYPE.EXCLUSIVE_ACCESS; } : ConfRW_002_PR_TYPE.T_Test ) :> obj |];
        [| ( { D1 = PR_TYPE.NO_RESERVATION; D2 = [ PR_TYPE.NO_RESERVATION; PR_TYPE.WRITE_EXCLUSIVE; PR_TYPE.EXCLUSIVE_ACCESS; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY; ]; D3 = None; D4 = PR_TYPE.NO_RESERVATION; D5 = PR_TYPE.NO_RESERVATION; D6 = PR_TYPE.EXCLUSIVE_ACCESS; } : ConfRW_002_PR_TYPE.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_PR_TYPE_007_data" )>]
    member _.SingleValue_PR_TYPE_007 ( s : ConfRW_002_PR_TYPE.T_Test ) =
        try
            ConfRW_002_PR_TYPE.ConfRW_UT002_PR_TYPE.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1></D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaaa</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" )>]
    [<InlineData( "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" )>]
    [<InlineData( "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" )>]
    [<InlineData( "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3><D3>VERBOSE</D3></Test>" )>]
    [<InlineData( "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3><D3>VERBOSE</D3><D3>VERBOSE</D3></Test>" )>]
    [<InlineData( "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3><D4>VERBOSE</D4></Test>" )>]
    [<InlineData( "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3><D5>VERBOSE</D5></Test>" )>]
    member _.SingleValue_LogLevel_001 ( s : string ) =
        try
            ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    static member m_SingleValue_LogLevel_002_data = [|
        [| "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" :> obj; LogLevel.LOGLEVEL_VERBOSE :> obj |];
        [| "<Test><D1>INFO</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" :> obj; LogLevel.LOGLEVEL_INFO :> obj |];
        [| "<Test><D1>WARNING</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" :> obj; LogLevel.LOGLEVEL_WARNING :> obj |];
        [| "<Test><D1>ERROR</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" :> obj; LogLevel.LOGLEVEL_ERROR :> obj |];
        [| "<Test><D1>FAILED</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" :> obj; LogLevel.LOGLEVEL_FAILED :> obj |];
        [| "<Test><D1>OFF</D1><D2>VERBOSE</D2><D2>VERBOSE</D2><D3>VERBOSE</D3></Test>" :> obj; LogLevel.LOGLEVEL_OFF :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LogLevel_002_data" )>]
    member _.SingleValue_LogLevel_002 ( inv : string ) ( outr : LogLevel ) =
        let r = ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.LoadString inv
        Assert.True( r.D1 = outr )

    static member m_SingleValue_LogLevel_003_data = [|
        [|
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D3>VERBOSE</D3></Test>" :> obj;
            [ LogLevel.LOGLEVEL_VERBOSE; LogLevel.LOGLEVEL_INFO; ] :> obj
        |];
        [|
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D2>WARNING</D2><D3>VERBOSE</D3></Test>" :> obj;
            [ LogLevel.LOGLEVEL_VERBOSE; LogLevel.LOGLEVEL_INFO; LogLevel.LOGLEVEL_WARNING; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LogLevel_003_data" )>]
    member _.SingleValue_LogLevel_003 ( s : String ) ( exr : LogLevel list ) =
        let r = ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_LogLevel_004_data = [|
        [| "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D3>INFO</D3></Test>" :> obj; Some( LogLevel.LOGLEVEL_INFO ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LogLevel_004_data" )>]
    member _.SingleValue_LogLevel_004 ( s : String ) ( exr : LogLevel option ) =
        let r = ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_LogLevel_005_data = [|
        [|
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2></Test>" :> obj;
            LogLevel.LOGLEVEL_VERBOSE :> obj;
        |];
        [|
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D6>ERROR</D6></Test>" :> obj;
            LogLevel.LOGLEVEL_ERROR :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LogLevel_005_data" )>]
    member _.SingleValue_LogLevel_005 ( s : string ) ( exr : LogLevel ) =
        let r = ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.LoadString s
        Assert.True( r.D4 = LogLevel.LOGLEVEL_INFO )
        Assert.True( r.D5 = LogLevel.LOGLEVEL_WARNING )
        Assert.True( r.D6 = exr )

    static member m_SingleValue_LogLevel_006_data = [|
        [|
            ( { D1 = LogLevel.LOGLEVEL_VERBOSE; D2 = [ LogLevel.LOGLEVEL_VERBOSE; LogLevel.LOGLEVEL_INFO; ]; D3 = None; D4 = LogLevel.LOGLEVEL_VERBOSE; D5 = LogLevel.LOGLEVEL_VERBOSE; D6 = LogLevel.LOGLEVEL_WARNING; } : ConfRW_002_LogLevel.T_Test ) :> obj;
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D6>WARNING</D6></Test>" :> obj
        |];
        [|
            ( { D1 = LogLevel.LOGLEVEL_VERBOSE; D2 = [ LogLevel.LOGLEVEL_VERBOSE; LogLevel.LOGLEVEL_INFO; LogLevel.LOGLEVEL_WARNING; ]; D3 = None; D4 = LogLevel.LOGLEVEL_VERBOSE; D5 = LogLevel.LOGLEVEL_VERBOSE; D6 = LogLevel.LOGLEVEL_ERROR; } : ConfRW_002_LogLevel.T_Test ) :> obj;
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D2>WARNING</D2><D6>ERROR</D6></Test>" :> obj
        |];
        [|
            ( { D1 = LogLevel.LOGLEVEL_VERBOSE; D2 = [ LogLevel.LOGLEVEL_VERBOSE; LogLevel.LOGLEVEL_INFO; ]; D3 = Some( LogLevel.LOGLEVEL_WARNING ); D4 = LogLevel.LOGLEVEL_VERBOSE; D5 = LogLevel.LOGLEVEL_VERBOSE; D6 = LogLevel.LOGLEVEL_FAILED; } : ConfRW_002_LogLevel.T_Test ) :> obj;
            "<Test><D1>VERBOSE</D1><D2>VERBOSE</D2><D2>INFO</D2><D3>WARNING</D3><D6>FAILED</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LogLevel_006_data" )>]
    member _.SingleValue_LogLevel_006 ( s : ConfRW_002_LogLevel.T_Test ) ( exr : string ) =
        let r = ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_LogLevel_007_data = [|
        [| ( { D1 = LogLevel.LOGLEVEL_VERBOSE; D2 = [ LogLevel.LOGLEVEL_VERBOSE; ]; D3 = None; D4 = LogLevel.LOGLEVEL_VERBOSE; D5 = LogLevel.LOGLEVEL_VERBOSE; D6 = LogLevel.LOGLEVEL_ERROR; } : ConfRW_002_LogLevel.T_Test ) :> obj |];
        [| ( { D1 = LogLevel.LOGLEVEL_VERBOSE; D2 = [ LogLevel.LOGLEVEL_VERBOSE; LogLevel.LOGLEVEL_INFO; LogLevel.LOGLEVEL_WARNING; LogLevel.LOGLEVEL_ERROR; ]; D3 = None; D4 = LogLevel.LOGLEVEL_VERBOSE; D5 = LogLevel.LOGLEVEL_VERBOSE; D6 = LogLevel.LOGLEVEL_FAILED; } : ConfRW_002_LogLevel.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_LogLevel_007_data" )>]
    member _.SingleValue_LogLevel_007 ( s : ConfRW_002_LogLevel.T_Test ) =
        try
            ConfRW_002_LogLevel.ConfRW_UT002_LogLevel.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>AAAA</D1><D2>a</D2><D2>a</D2><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaa$$$00</D1><D2>a</D2><D2>a</D2><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123</D1><D2>a</D2><D2>a</D2><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D2>a</D2><D2>a</D2><D2>a</D2><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D2>a</D2><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D2>a</D2><D3>a</D3><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D2>a</D2><D3>a</D3><D4>a</D4></Test>" )>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D2>a</D2><D3>a</D3><D5>a</D5></Test>" )>]
    member _.SingleValue_iSCSIName_001 ( s : string ) =
        try
            ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>a</D1><D2>a</D2><D2>a</D2><D3>a</D3></Test>", "a" )>]
    [<InlineData( "<Test><D1>aaa-bbb.ccc:ddd</D1><D2>a</D2><D2>a</D2><D3>a</D3></Test>", "aaa-bbb.ccc:ddd" )>]
    [<InlineData( "<Test><D1>0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012</D1><D2>a</D2><D2>a</D2><D3>a</D3></Test>", "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012" )>]
    member _.SingleValue_iSCSIName_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.LoadString s
        Assert.True( r.D1 = exr )

    static member m_SingleValue_iSCSIName_003_data = [|
        [|
            "<Test><D1>a</D1><D2>aaa</D2><D2>bbb</D2><D3>a</D3></Test>" :> obj;
            [ "aaa"; "bbb" ] :> obj
        |];
        [|
            "<Test><D1>a</D1><D2>aaa</D2><D2>bbb</D2><D2>ccc</D2><D3>a</D3></Test>" :> obj;
            [ "aaa"; "bbb"; "ccc"; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_iSCSIName_003_data" )>]
    member _.SingleValue_iSCSIName_003 ( s : String ) ( exr : string list ) =
        let r = ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_iSCSIName_004_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>aaabbbccc</D3></Test>" :> obj; Some( "aaabbbccc" ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_iSCSIName_004_004_data" )>]
    member _.SingleValue_iSCSIName_004 ( s : String ) ( exr : string option ) =
        let r = ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_iSCSIName_005_data = [|
        [|
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj;
            "b01" :> obj;
        |];
        [|
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D6>ccc001</D6></Test>" :> obj;
            "ccc001" :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_iSCSIName_005_data" )>]
    member _.SingleValue_iSCSIName_005 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.LoadString s
        Assert.True( r.D4 = "" )
        Assert.True( r.D5 = "a01" )
        Assert.True( r.D6 = exr )

    static member m_SingleValue_iSCSIName_006_data = [|
        [|
            ( { D1 = "aaa"; D2 = [ "bbb"; "ccc"; ]; D3 = None; D4 = ""; D5 = ""; D6 = "d01"; } : ConfRW_002_iSCSIName.T_Test ) :> obj;
            "<Test><D1>aaa</D1><D2>bbb</D2><D2>ccc</D2><D6>d01</D6></Test>" :> obj
        |];
        [|
            ( { D1 = "aaa"; D2 = [ "bbb"; "ccc"; "ddd"; ]; D3 = None; D4 = ""; D5 = ""; D6 = "d02"; } : ConfRW_002_iSCSIName.T_Test ) :> obj;
            "<Test><D1>aaa</D1><D2>bbb</D2><D2>ccc</D2><D2>ddd</D2><D6>d02</D6></Test>" :> obj
        |];
        [|
            ( { D1 = "aaa"; D2 = [ "bbb"; "ccc"; ]; D3 = Some( "ddd" ); D4 = ""; D5 = ""; D6 = "d03"; } : ConfRW_002_iSCSIName.T_Test ) :> obj;
            "<Test><D1>aaa</D1><D2>bbb</D2><D2>ccc</D2><D3>ddd</D3><D6>d03</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_iSCSIName_006_data" )>]
    member _.SingleValue_iSCSIName_006 ( s : ConfRW_002_iSCSIName.T_Test ) ( exr : string ) =
        let r = ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_iSCSIName_007_data = [|
        [| ( { D1 = "AAAbbb00"; D2 = [ "aaa"; "bbb" ]; D3 = None; D4 = ""; D5 = ""; D6 = ""; } : ConfRW_002_iSCSIName.T_Test ) :> obj |];
        [| ( { D1 = "aaa"; D2 = [ "aaa"; ]; D3 = None; D4 = ""; D5 = ""; D6 = ""; } : ConfRW_002_iSCSIName.T_Test ) :> obj |];
        [| ( { D1 = "aaa"; D2 = [ "aaa"; "aaa"; "aaa"; ]; D3 = None; D4 = ""; D5 = ""; D6 = ""; } : ConfRW_002_iSCSIName.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_iSCSIName_007_data" )>]
    member _.SingleValue_iSCSIName_007 ( s : ConfRW_002_iSCSIName.T_Test ) =
        try
            ConfRW_002_iSCSIName.ConfRW_UT002_iSCSIName.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>a</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaaaaa</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>Xbbbc</D2><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXXXy</D2><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXc</D2><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXc</D2><D3>a</D3><D3>a</D3><D3>a</D3><D3>a</D3></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXc</D2><D3>a</D3><D3>a</D3><D4>a</D4><D4>a</D4></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXc</D2><D3>a</D3><D3>a</D3><D5>a</D5></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXc</D2><D3>a</D3><D3>a</D3><D6>a</D6></Test>" )>]
    [<InlineData( "<Test><D1>aa</D1><D2>aXc</D2><D3>a</D3><D3>a</D3><D7>a</D7></Test>" )>]
    [<InlineData( "<Test><D1>&lt;&lt;&lt;&lt;&lt;&lt;</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>" )>]
    member _.SingleValue_string_001 ( s : string ) =
        try
            ConfRW_002_string.ConfRW_UT002_string.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "ab" )>]
    [<InlineData( "<Test><D1>abc</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "abc" )>]
    [<InlineData( "<Test><D1>abcd</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "abcd" )>]
    [<InlineData( "<Test><D1>abcde</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "abcde" )>]
    [<InlineData( "<Test><D1>&lt;&lt;&lt;&lt;&lt;</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "<<<<<" )>]
    [<InlineData( "<Test><D1>&gt;&amp;&quot;&apos;</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", ">&\"\'" )>]
    [<InlineData( "<Test><D1>a&#013;&#010;b</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "a\r\nb" )>]
    member _.SingleValue_string_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_string.ConfRW_UT002_string.LoadString s
        Assert.True( r.D1 = exr )

    [<Theory>]
    [<InlineData( "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>a</D3></Test>", "ac" )>]
    [<InlineData( "<Test><D1>abc</D1><D2>aXXc</D2><D3>a</D3><D3>a</D3></Test>", "aXXc" )>]
    [<InlineData( "<Test><D1>abcd</D1><D2>aYYYc</D2><D3>a</D3><D3>a</D3></Test>", "aYYYc" )>]
    [<InlineData( "<Test><D1>abcde</D1><D2>aZZZc</D2><D3>a</D3><D3>a</D3></Test>", "aZZZc" )>]
    member _.SingleValue_string_003 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_string.ConfRW_UT002_string.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_string_004_data = [|
        [| "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>b</D3></Test>" :> obj; [ "a"; "b" ] :> obj |];
        [| "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>b</D3><D3>c</D3></Test>" :> obj; [ "a"; "b"; "c"; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_string_004_data" )>]
    member _.SingleValue_string_004 ( s : String ) ( exr : string list ) =
        let r = ConfRW_002_string.ConfRW_UT002_string.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_string_005_data = [|
        [| "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>b</D3></Test>" :> obj; None :> obj |];
        [| "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>b</D3><D4>abcd</D4></Test>" :> obj; Some( "abcd" ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_string_005_data" )>]
    member _.SingleValue_string_005 ( s : String ) ( exr : string option ) =
        let r = ConfRW_002_string.ConfRW_UT002_string.LoadString s
        Assert.True( r.D4 = exr )

    static member m_SingleValue_string_006_data = [|
        [|
            "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>b</D3></Test>" :> obj;
            "b01" :> obj;
            Constants.PRODUCT_NAME :> obj;
        |];
        [|
            "<Test><D1>ab</D1><D2>ac</D2><D3>a</D3><D3>b</D3><D8>ddd</D8><D9>eee</D9></Test>" :> obj;
            "ddd" :> obj;
            "eee" :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_string_006_data" )>]
    member _.SingleValue_string_006 ( s : string ) ( exr_D8 : string ) ( exr_D9 : string ) =
        let r = ConfRW_002_string.ConfRW_UT002_string.LoadString s
        Assert.True( r.D5 = "" )
        Assert.True( r.D6 = "a01" )
        Assert.True( r.D7 = Constants.PRODUCT_NAME )
        Assert.True( r.D8 = exr_D8 )
        Assert.True( r.D9 = exr_D9 )


    static member m_SingleValue_string_007_data = [|
        [|
            ( { D1 = "aa"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj;
            "<Test><D1>aa</D1><D2>abbc</D2><D3>bbb</D3><D3>ccc</D3><D8>yyy</D8><D9>zzz</D9></Test>" :> obj
        |];
        [|
            ( { D1 = "aaa"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; "ddd"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "yyy"; D8 = "zzz"; D9 = "AAA"; } : ConfRW_002_string.T_Test ) :> obj;
            "<Test><D1>aaa</D1><D2>abbc</D2><D3>bbb</D3><D3>ccc</D3><D3>ddd</D3><D8>zzz</D8><D9>AAA</D9></Test>" :> obj
        |];
        [|
            ( { D1 = "aaaa"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; ]; D4 = Some( "ddd" ); D5 = ""; D6 = ""; D7 = "zzz"; D8 = "AAA"; D9 = "BBB"; } : ConfRW_002_string.T_Test ) :> obj;
            "<Test><D1>aaaa</D1><D2>abbc</D2><D3>bbb</D3><D3>ccc</D3><D4>ddd</D4><D8>AAA</D8><D9>BBB</D9></Test>" :> obj
        |];
        [|
            ( { D1 = "<>&\"\'"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj;
            "<Test><D1>&lt;&gt;&amp;&quot;&apos;</D1><D2>abbc</D2><D3>bbb</D3><D3>ccc</D3><D8>yyy</D8><D9>zzz</D9></Test>" :> obj
        |];
        [|
            ( { D1 = "<\rg\n>"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj;
            "<Test><D1>&lt;&#013;g&#010;&gt;</D1><D2>abbc</D2><D3>bbb</D3><D3>ccc</D3><D8>yyy</D8><D9>zzz</D9></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_string_007_data" )>]
    member _.SingleValue_string_007 ( s : ConfRW_002_string.T_Test ) ( exr : string ) =
        let r = ConfRW_002_string.ConfRW_UT002_string.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_string_008_data = [|
        [| ( { D1 = "a"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj |];
        [| ( { D1 = "abcdef"; D2 = "abbc"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj |];
        [| ( { D1 = "aaa"; D2 = "Xbbc"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj |];
        [| ( { D1 = "aaa"; D2 = "abbY"; D3 = [ "bbb"; "ccc"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj |];
        [| ( { D1 = "aaa"; D2 = "abbc"; D3 = [ "bbb"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj |];
        [| ( { D1 = "aaa"; D2 = "abbc"; D3 = [ "bbb"; "bbb"; "bbb"; "bbb"; ]; D4 = None; D5 = ""; D6 = ""; D7 = "xxx"; D8 = "yyy"; D9 = "zzz"; } : ConfRW_002_string.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_string_008_data" )>]
    member _.SingleValue_string_008 ( s : ConfRW_002_string.T_Test ) =
        try
            ConfRW_002_string.ConfRW_UT002_string.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3><D3>00000000-0000-0000-0000-000000000000</D3><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>0X0XX00Y-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000-00</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3><D4>00000000-0000-0000-0000-000000000000</D4></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3><D5>00000000-0000-0000-0000-000000000000</D5></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-00000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-0000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>00000000000000000000000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    member _.SingleValue_GUID_001 ( s : string ) =
        try
            ConfRW_002_GUID.ConfRW_UT002_GUID.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>", "00000000-0000-0000-0000-000000000000" )>]
    [<InlineData( "<Test><D1>11111111-1111-1111-1111-111111111111</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>", "11111111-1111-1111-1111-111111111111" )>]
    [<InlineData( "<Test><D1>22222222-2222-2222-2222-222222222222</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>", "22222222-2222-2222-2222-222222222222" )>]
    member _.SingleValue_GUID_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_GUID.ConfRW_UT002_GUID.LoadString s
        Assert.True( r.D1 = Guid.Parse exr )

    static member m_SingleValue_GUID_003_data = [|
        [|
            "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>11111111-1111-1111-1111-111111111111</D2><D2>22222222-2222-2222-2222-222222222222</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" :> obj;
            [ Guid.Parse "11111111-1111-1111-1111-111111111111"; Guid.Parse "22222222-2222-2222-2222-222222222222"; ] :> obj
        |];
        [|
            "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>11111111-1111-1111-1111-111111111111</D2><D2>22222222-2222-2222-2222-222222222222</D2><D2>33333333-3333-3333-3333-333333333333</D2><D3>00000000-0000-0000-0000-000000000000</D3></Test>" :> obj;
            [ Guid.Parse "11111111-1111-1111-1111-111111111111"; Guid.Parse "22222222-2222-2222-2222-222222222222"; Guid.Parse "33333333-3333-3333-3333-333333333333"; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_GUID_003_data" )>]
    member _.SingleValue_GUID_003 ( s : String ) ( exr : Guid list ) =
        let r = ConfRW_002_GUID.ConfRW_UT002_GUID.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_GUID_004_data = [|
        [| "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>00000000-0000-0000-0000-000000000000</D2><D3>22222222-2222-2222-2222-222222222222</D3></Test>" :> obj; Some( Guid.Parse "22222222-2222-2222-2222-222222222222" ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_GUID_004_data" )>]
    member _.SingleValue_GUID_004 ( s : String ) ( exr : Guid option ) =
        let r = ConfRW_002_GUID.ConfRW_UT002_GUID.LoadString s
        Assert.True( r.D3 = exr )

    [<Theory>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>11111111-1111-1111-1111-111111111111</D2></Test>", "98000000-0000-0000-0000-000000000098" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>00000000-0000-0000-0000-000000000000</D2><D2>11111111-1111-1111-1111-111111111111</D2><D6>22222222-2222-2222-2222-222222222222</D6></Test>", "22222222-2222-2222-2222-222222222222" )>]
    member _.SingleValue_GUID_005 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_GUID.ConfRW_UT002_GUID.LoadString s
        Assert.True( r.D4 = Guid.Parse "00000000-0000-0000-0000-000000000000" )
        Assert.True( r.D5 = Guid.Parse "99000000-0000-0000-0000-000000000099" )
        Assert.True( r.D6 = Guid.Parse exr )

    static member m_SingleValue_GUID_006_data = [|
        [|
            ( { D1 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D2 = [ Guid.Parse "10000000-0000-0000-0000-000000000000"; Guid.Parse "20000000-0000-0000-0000-000000000000"; ]; D3 = None; D4 = Guid.Parse "30000000-0000-0000-0000-000000000000"; D5 = Guid.Parse "40000000-0000-0000-0000-000000000000"; D6 = Guid.Parse "50000000-0000-0000-0000-000000000000"; } : ConfRW_002_GUID.T_Test ) :> obj;
            "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>10000000-0000-0000-0000-000000000000</D2><D2>20000000-0000-0000-0000-000000000000</D2><D6>50000000-0000-0000-0000-000000000000</D6></Test>" :> obj
        |];
        [|
            ( { D1 = Guid.Parse "00000000-0000-0000-0000-000000000001"; D2 = [ Guid.Parse "10000000-0000-0000-0000-000000000001"; Guid.Parse "20000000-0000-0000-0000-000000000001"; Guid.Parse "30000000-0000-0000-0000-000000000001"; ]; D3 = None; D4 = Guid.Parse "40000000-0000-0000-0000-000000000001"; D5 = Guid.Parse "50000000-0000-0000-0000-000000000001"; D6 = Guid.Parse "60000000-0000-0000-0000-000000000001"; } : ConfRW_002_GUID.T_Test ) :> obj;
            "<Test><D1>00000000-0000-0000-0000-000000000001</D1><D2>10000000-0000-0000-0000-000000000001</D2><D2>20000000-0000-0000-0000-000000000001</D2><D2>30000000-0000-0000-0000-000000000001</D2><D6>60000000-0000-0000-0000-000000000001</D6></Test>" :> obj
        |];
        [|
            ( { D1 = Guid.Parse "00000000-0000-0000-0000-000000000002"; D2 = [ Guid.Parse "10000000-0000-0000-0000-000000000002"; Guid.Parse "20000000-0000-0000-0000-000000000002"; ]; D3 = Some( Guid.Parse "30000000-0000-0000-0000-000000000002" ); D4 = Guid.Parse "40000000-0000-0000-0000-000000000002"; D5 = Guid.Parse "50000000-0000-0000-0000-000000000002"; D6 = Guid.Parse "60000000-0000-0000-0000-000000000002"; } : ConfRW_002_GUID.T_Test ) :> obj;
            "<Test><D1>00000000-0000-0000-0000-000000000002</D1><D2>10000000-0000-0000-0000-000000000002</D2><D2>20000000-0000-0000-0000-000000000002</D2><D3>30000000-0000-0000-0000-000000000002</D3><D6>60000000-0000-0000-0000-000000000002</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_GUID_006_data" )>]
    member _.SingleValue_GUID_006 ( s : ConfRW_002_GUID.T_Test ) ( exr : string ) =
        let r = ConfRW_002_GUID.ConfRW_UT002_GUID.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_GUID_007_data = [|
        [| ( { D1 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D2 = [ Guid.Parse "00000000-0000-0000-0000-000000000000"; ]; D3 = None; D4 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D5 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D6 = Guid.Parse "00000000-0000-0000-0000-000000000000"; } : ConfRW_002_GUID.T_Test ) :> obj |];
        [| ( { D1 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D2 = [ Guid.Parse "00000000-0000-0000-0000-000000000000"; Guid.Parse "00000000-0000-0000-0000-000000000000"; Guid.Parse "00000000-0000-0000-0000-000000000000"; Guid.Parse "00000000-0000-0000-0000-000000000000"; ]; D3 = None; D4 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D5 = Guid.Parse "00000000-0000-0000-0000-000000000000"; D6 = Guid.Parse "00000000-0000-0000-0000-000000000000"; } : ConfRW_002_GUID.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_GUID_007_data" )>]
    member _.SingleValue_GUID_007 ( s : ConfRW_002_GUID.T_Test ) =
        try
            ConfRW_002_GUID.ConfRW_UT002_GUID.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D3>TD_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3><D3>TD_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3><D3>TD_00000000</D3><D3>TD_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_0X0XX00Y</D3></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000-00</D3></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3><D4>TD_00000000</D4></Test>" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3><D5>TD_00000000</D5></Test>" )>]
    [<InlineData( "<Test><D1>00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3></Test>" )>]
    member _.SingleValue_TargetDeviceID_001 ( s : string ) =
        try
            ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3></Test>", "TD_00000000" )>]
    [<InlineData( "<Test><D1>TD_11111111</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3></Test>", "TD_11111111" )>]
    [<InlineData( "<Test><D1>TD_22222222</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_00000000</D3></Test>", "TD_22222222" )>]
    member _.SingleValue_TargetDeviceID_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.LoadString s
        Assert.True( r.D1 = tdid_me.fromString exr )

    static member m_SingleValue_TargetDeviceID_003_data = [|
        [|
            "<Test><D1>TD_00000000</D1><D2>TD_11111111</D2><D2>TD_22222222</D2><D3>TD_00000000</D3></Test>" :> obj;
            [ tdid_me.fromString "TD_11111111"; tdid_me.fromString "TD_22222222"; ] :> obj
        |];
        [|
            "<Test><D1>TD_00000000</D1><D2>TD_11111111</D2><D2>TD_22222222</D2><D2>TD_33333333</D2><D3>TD_00000000</D3></Test>" :> obj;
            [ tdid_me.fromString "TD_11111111"; tdid_me.fromString "TD_22222222"; tdid_me.fromString "TD_33333333"; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetDeviceID_003_data" )>]
    member _.SingleValue_TargetDeviceID_003 ( s : String ) ( exr : TDID_T list ) =
        let r = ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_TargetDeviceID_004_data = [|
        [| "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_00000000</D2><D3>TD_22222222</D3></Test>" :> obj; Some( tdid_me.fromString "TD_22222222" ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetDeviceID_004_data" )>]
    member _.SingleValue_TargetDeviceID_004 ( s : String ) ( exr : TDID_T option ) =
        let r = ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.LoadString s
        Assert.True( r.D3 = exr )

    [<Theory>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_11111111</D2></Test>", "TD_98000098" )>]
    [<InlineData( "<Test><D1>TD_00000000</D1><D2>TD_00000000</D2><D2>TD_11111111</D2><D6>TD_22222222</D6></Test>", "TD_22222222" )>]
    member _.SingleValue_TargetDeviceID_005 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.LoadString s
        Assert.True( r.D4 = tdid_me.fromString "TD_00000000" )
        Assert.True( r.D5 = tdid_me.fromString "TD_99000099" )
        Assert.True( r.D6 = tdid_me.fromString exr )

    static member m_SingleValue_TargetDeviceID_006_data = [|
        [|
            ( { D1 = tdid_me.fromString "TD_00000000"; D2 = [ tdid_me.fromString "TD_10000000"; tdid_me.fromString "TD_20000000"; ]; D3 = None; D4 = tdid_me.fromString "TD_30000000"; D5 = tdid_me.fromString "TD_40000000"; D6 = tdid_me.fromString "TD_50000000"; } : ConfRW_002_TargetDeviceID.T_Test ) :> obj;
            "<Test><D1>TD_00000000</D1><D2>TD_10000000</D2><D2>TD_20000000</D2><D6>TD_50000000</D6></Test>" :> obj
        |];
        [|
            ( { D1 = tdid_me.fromString "TD_00000001"; D2 = [ tdid_me.fromString "TD_10000001"; tdid_me.fromString "TD_20000001"; tdid_me.fromString "TD_30000001"; ]; D3 = None; D4 = tdid_me.fromString "TD_40000001"; D5 = tdid_me.fromString "TD_50000001"; D6 = tdid_me.fromString "TD_60000001"; } : ConfRW_002_TargetDeviceID.T_Test ) :> obj;
            "<Test><D1>TD_00000001</D1><D2>TD_10000001</D2><D2>TD_20000001</D2><D2>TD_30000001</D2><D6>TD_60000001</D6></Test>" :> obj
        |];
        [|
            ( { D1 = tdid_me.fromString "TD_00000002"; D2 = [ tdid_me.fromString "TD_10000002"; tdid_me.fromString "TD_20000002"; ]; D3 = Some( tdid_me.fromString "TD_30000002" ); D4 = tdid_me.fromString "TD_40000002"; D5 = tdid_me.fromString "TD_50000002"; D6 = tdid_me.fromString "TD_60000002"; } : ConfRW_002_TargetDeviceID.T_Test ) :> obj;
            "<Test><D1>TD_00000002</D1><D2>TD_10000002</D2><D2>TD_20000002</D2><D3>TD_30000002</D3><D6>TD_60000002</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetDeviceID_006_data" )>]
    member _.SingleValue_TargetDeviceID_006 ( s : ConfRW_002_TargetDeviceID.T_Test ) ( exr : string ) =
        let r = ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_TargetDeviceID_007_data = [|
        [| ( { D1 = tdid_me.fromString "TD_00000000"; D2 = [ tdid_me.fromString "TD_00000000"; ]; D3 = None; D4 = tdid_me.fromString "TD_00000000"; D5 = tdid_me.fromString "TD_00000000"; D6 = tdid_me.fromString "TD_00000000"; } : ConfRW_002_TargetDeviceID.T_Test ) :> obj |];
        [| ( { D1 = tdid_me.fromString "TD_00000000"; D2 = [ tdid_me.fromString "TD_00000000"; tdid_me.fromString "TD_00000000"; tdid_me.fromString "TD_00000000"; tdid_me.fromString "TD_00000000"; ]; D3 = None; D4 = tdid_me.fromString "TD_00000000"; D5 = tdid_me.fromString "TD_00000000"; D6 = tdid_me.fromString "TD_00000000"; } : ConfRW_002_TargetDeviceID.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetDeviceID_007_data" )>]
    member _.SingleValue_TargetDeviceID_007 ( s : ConfRW_002_TargetDeviceID.T_Test ) =
        try
            ConfRW_002_TargetDeviceID.ConfRW_UT002_TargetDeviceID.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D3>TG_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3><D3>TG_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3><D3>TG_00000000</D3><D3>TG_00000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_0X0XX00Y</D3></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000-00</D3></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3><D4>TG_00000000</D4></Test>" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3><D5>TG_00000000</D5></Test>" )>]
    [<InlineData( "<Test><D1>00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3></Test>" )>]
    member _.SingleValue_TargetGroupID_001 ( s : string ) =
        try
            ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3></Test>", "TG_00000000" )>]
    [<InlineData( "<Test><D1>TG_11111111</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3></Test>", "TG_11111111" )>]
    [<InlineData( "<Test><D1>TG_22222222</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_00000000</D3></Test>", "TG_22222222" )>]
    member _.SingleValue_TargetGroupID_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.LoadString s
        Assert.True( r.D1 = tgid_me.fromString exr )

    static member m_SingleValue_TargetGroupID_003_data = [|
        [|
            "<Test><D1>TG_00000000</D1><D2>TG_11111111</D2><D2>TG_22222222</D2><D3>TG_00000000</D3></Test>" :> obj;
            [ tgid_me.fromString "TG_11111111"; tgid_me.fromString "TG_22222222"; ] :> obj
        |];
        [|
            "<Test><D1>TG_00000000</D1><D2>TG_11111111</D2><D2>TG_22222222</D2><D2>TG_33333333</D2><D3>TG_00000000</D3></Test>" :> obj;
            [ tgid_me.fromString "TG_11111111"; tgid_me.fromString "TG_22222222"; tgid_me.fromString "TG_33333333"; ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetGroupID_003_data" )>]
    member _.SingleValue_TargetGroupID_003 ( s : String ) ( exr : TGID_T list ) =
        let r = ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_TargetGroupID_004_data = [|
        [| "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_00000000</D2><D3>TG_22222222</D3></Test>" :> obj; Some( tgid_me.fromString "TG_22222222" ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetGroupID_004_data" )>]
    member _.SingleValue_TargetGroupID_004 ( s : String ) ( exr : TGID_T option ) =
        let r = ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.LoadString s
        Assert.True( r.D3 = exr )

    [<Theory>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_11111111</D2></Test>", "TG_98000098" )>]
    [<InlineData( "<Test><D1>TG_00000000</D1><D2>TG_00000000</D2><D2>TG_11111111</D2><D6>TG_22222222</D6></Test>", "TG_22222222" )>]
    member _.SingleValue_TargetGroupID_005 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.LoadString s
        Assert.True( r.D4 = tgid_me.fromString "TG_00000000" )
        Assert.True( r.D5 = tgid_me.fromString "TG_99000099" )
        Assert.True( r.D6 = tgid_me.fromString exr )

    static member m_SingleValue_TargetGroupID_006_data = [|
        [|
            ( { D1 = tgid_me.fromString "TG_00000000"; D2 = [ tgid_me.fromString "TG_10000000"; tgid_me.fromString "TG_20000000"; ]; D3 = None; D4 = tgid_me.fromString "TG_30000000"; D5 = tgid_me.fromString "TG_40000000"; D6 = tgid_me.fromString "TG_50000000"; } : ConfRW_002_TargetGroupID.T_Test ) :> obj;
            "<Test><D1>TG_00000000</D1><D2>TG_10000000</D2><D2>TG_20000000</D2><D6>TG_50000000</D6></Test>" :> obj
        |];
        [|
            ( { D1 = tgid_me.fromString "TG_00000001"; D2 = [ tgid_me.fromString "TG_10000001"; tgid_me.fromString "TG_20000001"; tgid_me.fromString "TG_30000001"; ]; D3 = None; D4 = tgid_me.fromString "TG_40000001"; D5 = tgid_me.fromString "TG_50000001"; D6 = tgid_me.fromString "TG_60000001"; } : ConfRW_002_TargetGroupID.T_Test ) :> obj;
            "<Test><D1>TG_00000001</D1><D2>TG_10000001</D2><D2>TG_20000001</D2><D2>TG_30000001</D2><D6>TG_60000001</D6></Test>" :> obj
        |];
        [|
            ( { D1 = tgid_me.fromString "TG_00000002"; D2 = [ tgid_me.fromString "TG_10000002"; tgid_me.fromString "TG_20000002"; ]; D3 = Some( tgid_me.fromString "TG_30000002" ); D4 = tgid_me.fromString "TG_40000002"; D5 = tgid_me.fromString "TG_50000002"; D6 = tgid_me.fromString "TG_60000002"; } : ConfRW_002_TargetGroupID.T_Test ) :> obj;
            "<Test><D1>TG_00000002</D1><D2>TG_10000002</D2><D2>TG_20000002</D2><D3>TG_30000002</D3><D6>TG_60000002</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetGroupID_006_data" )>]
    member _.SingleValue_TargetGroupID_006 ( s : ConfRW_002_TargetGroupID.T_Test ) ( exr : string ) =
        let r = ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_TargetGroupID_007_data = [|
        [| ( { D1 = tgid_me.fromString "TG_00000000"; D2 = [ tgid_me.fromString "TG_00000000"; ]; D3 = None; D4 = tgid_me.fromString "TG_00000000"; D5 = tgid_me.fromString "TG_00000000"; D6 = tgid_me.fromString "TG_00000000"; } : ConfRW_002_TargetGroupID.T_Test ) :> obj |];
        [| ( { D1 = tgid_me.fromString "TG_00000000"; D2 = [ tgid_me.fromString "TG_00000000"; tgid_me.fromString "TG_00000000"; tgid_me.fromString "TG_00000000"; tgid_me.fromString "TG_00000000"; ]; D3 = None; D4 = tgid_me.fromString "TG_00000000"; D5 = tgid_me.fromString "TG_00000000"; D6 = tgid_me.fromString "TG_00000000"; } : ConfRW_002_TargetGroupID.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TargetGroupID_007_data" )>]
    member _.SingleValue_TargetGroupID_007 ( s : ConfRW_002_TargetGroupID.T_Test ) =
        try
            ConfRW_002_TargetGroupID.ConfRW_UT002_TargetGroupID.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3><D3>CSI_00000000-0000-0000-0000-000000000000</D3><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_0X0XX00Y-0000-0000-0000-000000000000</D3></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000-00</D3></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3><D4>CSI_00000000-0000-0000-0000-000000000000</D4></Test>" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3><D5>CSI_00000000-0000-0000-0000-000000000000</D5></Test>" )>]
    [<InlineData( "<Test><D1>00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" )>]
    member _.SingleValue_CtrlSessionID_001 ( s : string ) =
        try
            ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>", "CSI_00000000-0000-0000-0000-000000000000" )>]
    [<InlineData( "<Test><D1>CSI_11111111-1111-1111-1111-111111111111</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>", "CSI_11111111-1111-1111-1111-111111111111" )>]
    [<InlineData( "<Test><D1>CSI_22222222-2222-2222-2222-222222222222</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>", "CSI_22222222-2222-2222-2222-222222222222" )>]
    member _.SingleValue_CtrlSessionID_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.LoadString s
        Assert.True( r.D1 = CtrlSessionID exr )

    static member m_SingleValue_CtrlSessionID_003_data = [|
        [|
            "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_11111111-1111-1111-1111-111111111111</D2><D2>CSI_22222222-2222-2222-2222-222222222222</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" :> obj;
            [ CtrlSessionID( "CSI_11111111-1111-1111-1111-111111111111" ); CtrlSessionID( "CSI_22222222-2222-2222-2222-222222222222" ); ] :> obj
        |];
        [|
            "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_11111111-1111-1111-1111-111111111111</D2><D2>CSI_22222222-2222-2222-2222-222222222222</D2><D2>CSI_33333333-3333-3333-3333-333333333333</D2><D3>CSI_00000000-0000-0000-0000-000000000000</D3></Test>" :> obj;
            [ CtrlSessionID( "CSI_11111111-1111-1111-1111-111111111111" ); CtrlSessionID( "CSI_22222222-2222-2222-2222-222222222222" ); CtrlSessionID( "CSI_33333333-3333-3333-3333-333333333333" ); ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CtrlSessionID_003_data" )>]
    member _.SingleValue_CtrlSessionID_003 ( s : String ) ( exr : CtrlSessionID list ) =
        let r = ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_CtrlSessionID_004_data = [|
        [| "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D3>CSI_22222222-2222-2222-2222-222222222222</D3></Test>" :> obj; Some( CtrlSessionID( "CSI_22222222-2222-2222-2222-222222222222" ) ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CtrlSessionID_004_data" )>]
    member _.SingleValue_CtrlSessionID_004 ( s : String ) ( exr : CtrlSessionID option ) =
        let r = ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.LoadString s
        Assert.True( r.D3 = exr )

    [<Theory>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_11111111-1111-1111-1111-111111111111</D2></Test>", "CSI_98000000-0000-0000-0000-000000000098" )>]
    [<InlineData( "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_00000000-0000-0000-0000-000000000000</D2><D2>CSI_11111111-1111-1111-1111-111111111111</D2><D6>CSI_22222222-2222-2222-2222-222222222222</D6></Test>", "CSI_22222222-2222-2222-2222-222222222222" )>]
    member _.SingleValue_CtrlSessionID_005 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.LoadString s
        Assert.True( r.D4 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000" )
        Assert.True( r.D5 = CtrlSessionID "CSI_99000000-0000-0000-0000-000000000099" )
        Assert.True( r.D6 = CtrlSessionID exr )

    static member m_SingleValue_CtrlSessionID_006_data = [|
        [|
            ( { D1 = CtrlSessionID( "CSI_00000000-0000-0000-0000-000000000000" ); D2 = [ CtrlSessionID "CSI_10000000-0000-0000-0000-000000000000"; CtrlSessionID "CSI_20000000-0000-0000-0000-000000000000"; ]; D3 = None; D4 = CtrlSessionID "CSI_30000000-0000-0000-0000-000000000000"; D5 = CtrlSessionID "CSI_40000000-0000-0000-0000-000000000000"; D6 = CtrlSessionID "CSI_50000000-0000-0000-0000-000000000000"; } : ConfRW_002_CtrlSessionID.T_Test ) :> obj;
            "<Test><D1>CSI_00000000-0000-0000-0000-000000000000</D1><D2>CSI_10000000-0000-0000-0000-000000000000</D2><D2>CSI_20000000-0000-0000-0000-000000000000</D2><D6>CSI_50000000-0000-0000-0000-000000000000</D6></Test>" :> obj
        |];
        [|
            ( { D1 = CtrlSessionID( "CSI_00000000-0000-0000-0000-000000000001" ); D2 = [ CtrlSessionID "CSI_10000000-0000-0000-0000-000000000001"; CtrlSessionID "CSI_20000000-0000-0000-0000-000000000001"; CtrlSessionID "CSI_30000000-0000-0000-0000-000000000001"; ]; D3 = None; D4 = CtrlSessionID "CSI_40000000-0000-0000-0000-000000000001"; D5 = CtrlSessionID "CSI_50000000-0000-0000-0000-000000000001"; D6 = CtrlSessionID "CSI_60000000-0000-0000-0000-000000000001"; } : ConfRW_002_CtrlSessionID.T_Test ) :> obj;
            "<Test><D1>CSI_00000000-0000-0000-0000-000000000001</D1><D2>CSI_10000000-0000-0000-0000-000000000001</D2><D2>CSI_20000000-0000-0000-0000-000000000001</D2><D2>CSI_30000000-0000-0000-0000-000000000001</D2><D6>CSI_60000000-0000-0000-0000-000000000001</D6></Test>" :> obj
        |];
        [|
            ( { D1 = CtrlSessionID( "CSI_00000000-0000-0000-0000-000000000002" ); D2 = [ CtrlSessionID "CSI_10000000-0000-0000-0000-000000000002"; CtrlSessionID "CSI_20000000-0000-0000-0000-000000000002"; ]; D3 = Some( CtrlSessionID "CSI_30000000-0000-0000-0000-000000000002" ); D4 = CtrlSessionID "CSI_40000000-0000-0000-0000-000000000002"; D5 = CtrlSessionID "CSI_50000000-0000-0000-0000-000000000002"; D6 = CtrlSessionID "CSI_60000000-0000-0000-0000-000000000002"; } : ConfRW_002_CtrlSessionID.T_Test ) :> obj;
            "<Test><D1>CSI_00000000-0000-0000-0000-000000000002</D1><D2>CSI_10000000-0000-0000-0000-000000000002</D2><D2>CSI_20000000-0000-0000-0000-000000000002</D2><D3>CSI_30000000-0000-0000-0000-000000000002</D3><D6>CSI_60000000-0000-0000-0000-000000000002</D6></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CtrlSessionID_006_data" )>]
    member _.SingleValue_CtrlSessionID_006 ( s : ConfRW_002_CtrlSessionID.T_Test ) ( exr : string ) =
        let r = ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_CtrlSessionID_007_data = [|
        [| ( { D1 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; D2 = [ CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; ]; D3 = None; D4 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; D5 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; D6 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; } : ConfRW_002_CtrlSessionID.T_Test ) :> obj |];
        [| ( { D1 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; D2 = [ CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; ]; D3 = None; D4 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; D5 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; D6 = CtrlSessionID "CSI_00000000-0000-0000-0000-000000000000"; } : ConfRW_002_CtrlSessionID.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CtrlSessionID_007_data" )>]
    member _.SingleValue_CtrlSessionID_007 ( s : ConfRW_002_CtrlSessionID.T_Test ) =
        try
            ConfRW_002_CtrlSessionID.ConfRW_UT002_CtrlSessionID.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>1</D2><D2>1</D2><D3>1</D3></Test>" )>]
    [<InlineData( "<Test><D1>3155378976000000000</D1><D2>1</D2><D2>1</D2><D3>1</D3></Test>" )>]
    [<InlineData( "<Test><D1>aaa</D1><D2>1</D2><D2>1</D2></Test>" )>]
    [<InlineData( "<Test><D1>1</D1><D2>1</D2><D3>1</D3></Test>" )>]
    [<InlineData( "<Test><D1>1</D1><D2>1</D2><D2>1</D2><D2>1</D2><D2>1</D2><D3>1</D3></Test>" )>]
    [<InlineData( "<Test><D1>1</D1><D2>1</D2><D2>1</D2><D3>1</D3><D3>1</D3></Test>" )>]
    [<InlineData( "<Test><D1>1</D1><D2>1</D2><D2>1</D2><D4>1</D4></Test>" )>]
    [<InlineData( "<Test><D1>1</D1><D2>1</D2><D2>1</D2><D5>1</D5></Test>" )>]
    [<InlineData( "<Test><D1>1</D1><D2>1</D2><D2>1</D2><D6>1</D6></Test>" )>]
    member _.SingleValue_DateTime_001 ( s : string ) =
        try
            ConfRW_002_DateTime.ConfRW_UT002_DateTime.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>3444313530000000</D1><D2>2</D2><D2>3</D2></Test>", "0011/12/01 11:22:33" )>]
    member _.SingleValue_DateTime_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_DateTime.ConfRW_UT002_DateTime.LoadString s
        Assert.True( r.D1 = DateTime.Parse exr )
        Assert.True( r.D1.Kind = DateTimeKind.Utc )

    static member m_SingleValue_DateTime_003_data = [|
        [|
            "<Test><D1>0</D1><D2>637811653200000000</D2><D2>637811653210000000</D2></Test>" :> obj;
             [ DateTime( 2022, 02, 22, 22, 22, 00, DateTimeKind.Utc ); DateTime( 2022, 02, 22, 22, 22, 01, DateTimeKind.Utc ) ] :> obj
        |];
        [|
            "<Test><D1>0</D1><D2>637811653200000000</D2><D2>637811653210000000</D2><D2>637811653220000000</D2></Test>" :> obj;
            [ DateTime( 2022, 02, 22, 22, 22, 00, DateTimeKind.Utc ); DateTime( 2022, 02, 22, 22, 22, 01, DateTimeKind.Utc ); DateTime( 2022, 02, 22, 22, 22, 02, DateTimeKind.Utc ); ] :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_DateTime_003_data" )>]
    member _.SingleValue_DateTime_003 ( s : String ) ( exr : DateTime list ) =
        let r = ConfRW_002_DateTime.ConfRW_UT002_DateTime.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_DateTime_004_data = [|
        [| "<Test><D1>0</D1><D2>1</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>0</D1><D2>1</D2><D2>1</D2><D3>641290304130000000</D3></Test>" :> obj; Some( DateTime( 2033, 03, 03, 03, 33, 33, DateTimeKind.Utc ) ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_DateTime_004_data" )>]
    member _.SingleValue_DateTime_004 ( s : String ) ( exr : DateTime option ) =
        let r = ConfRW_002_DateTime.ConfRW_UT002_DateTime.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_DateTime_005_data = [|
        [|
            "<Test><D1>0</D1><D2>0</D2><D2>0</D2></Test>" :> obj;
            DateTime( 0002, 03, 04, 05, 06, 07, DateTimeKind.Utc ) :> obj;
            Constants.PRODUCT_RELEASE_DATE :> obj;
        |];
        [|
            "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D7>655284640270000000</D7><D8>658783876880000000</D8></Test>" :> obj;
            DateTime( 2077, 07, 07, 07, 07, 07, DateTimeKind.Utc ) :> obj;
            DateTime( 2088, 08, 08, 08, 08, 08, DateTimeKind.Utc ) :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_DateTime_005_data" )>]
    member _.SingleValue_DateTime_005 ( s : string ) ( exr_D7 : DateTime ) ( exr_D8 : DateTime ) =
        let r = ConfRW_002_DateTime.ConfRW_UT002_DateTime.LoadString s
        Assert.True( r.D4 = DateTime( 0L, DateTimeKind.Utc ) )
        Assert.True( r.D5 = DateTime( 0001, 02, 03, 04, 05, 06, DateTimeKind.Utc ) )
        Assert.True( r.D6 = Constants.PRODUCT_RELEASE_DATE )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_DateTime_006_data = [|
        [|
            ( {
                D1 = DateTime( 2000, 1, 1, 1, 1, 1, DateTimeKind.Utc );
                D2 = [ DateTime( 2000, 2, 2, 2, 2, 2, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 3, DateTimeKind.Utc ); ];
                D3 = None;
                D4 = DateTime( 2000, 4, 4, 4, 4, 4, DateTimeKind.Utc );
                D5 = DateTime( 2000, 5, 5, 5, 5, 5, DateTimeKind.Utc );
                D6 = DateTime( 2000, 6, 6, 6, 6, 6, DateTimeKind.Utc );
                D7 = DateTime( 2000, 7, 7, 7, 7, 7, DateTimeKind.Utc );
                D8 = DateTime( 2000, 8, 8, 8, 8, 8, DateTimeKind.Utc );
            } : ConfRW_002_DateTime.T_Test ) :> obj;
            "<Test><D1>630822852610000000</D1><D2>630850537220000000</D2><D2>630850537230000000</D2><D7>630985504270000000</D7><D8>631013188880000000</D8></Test>" :> obj
        |];
        [|
            ( {
                D1 = DateTime( 2000, 1, 1, 1, 1, 1, DateTimeKind.Utc );
                D2 = [ DateTime( 2000, 2, 2, 2, 2, 2, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 3, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 4, DateTimeKind.Utc ); ];
                D3 = None;
                D4 = DateTime( 2000, 4, 4, 4, 4, 4, DateTimeKind.Utc );
                D5 = DateTime( 2000, 5, 5, 5, 5, 5, DateTimeKind.Utc );
                D6 = DateTime( 2000, 6, 6, 6, 6, 6, DateTimeKind.Utc );
                D7 = DateTime( 2000, 7, 7, 7, 7, 7, DateTimeKind.Utc );
                D8 = DateTime( 2000, 8, 8, 8, 8, 8, DateTimeKind.Utc );
            } : ConfRW_002_DateTime.T_Test ) :> obj;
            "<Test><D1>630822852610000000</D1><D2>630850537220000000</D2><D2>630850537230000000</D2><D2>630850537240000000</D2><D7>630985504270000000</D7><D8>631013188880000000</D8></Test>" :> obj
        |];
        [|
            ( {
                D1 = DateTime( 2000, 1, 1, 1, 1, 1, DateTimeKind.Utc );
                D2 = [ DateTime( 2000, 2, 2, 2, 2, 2, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 3, DateTimeKind.Utc ); ];
                D3 = Some( DateTime( 2000, 3, 3, 3, 3, 3, DateTimeKind.Utc ) );
                D4 = DateTime( 2000, 4, 4, 4, 4, 4, DateTimeKind.Utc );
                D5 = DateTime( 2000, 5, 5, 5, 5, 5, DateTimeKind.Utc );
                D6 = DateTime( 2000, 6, 6, 6, 6, 6, DateTimeKind.Utc );
                D7 = DateTime( 2000, 7, 7, 7, 7, 7, DateTimeKind.Utc );
                D8 = DateTime( 2000, 8, 8, 8, 8, 8, DateTimeKind.Utc );
            } : ConfRW_002_DateTime.T_Test ) :> obj;
            "<Test><D1>630822852610000000</D1><D2>630850537220000000</D2><D2>630850537230000000</D2><D3>630876493830000000</D3><D7>630985504270000000</D7><D8>631013188880000000</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_DateTime_006_data" )>]
    member _.SingleValue_DateTime_006 ( s : ConfRW_002_DateTime.T_Test ) ( exr : string ) =
        let r = ConfRW_002_DateTime.ConfRW_UT002_DateTime.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_DateTime_007_data = [|
        [|
            ( {
                D1 = DateTime( 2000, 1, 1, 1, 1, 1, DateTimeKind.Utc );
                D2 = [ DateTime( 2000, 2, 2, 2, 2, 2, DateTimeKind.Utc ); ];
                D3 = Some( DateTime( 2000, 3, 3, 3, 3, 3, DateTimeKind.Utc ); );
                D4 = DateTime( 2000, 4, 4, 4, 4, 4, DateTimeKind.Utc );
                D5 = DateTime( 2000, 5, 5, 5, 5, 5, DateTimeKind.Utc );
                D6 = DateTime( 2000, 6, 6, 6, 6, 6, DateTimeKind.Utc );
                D7 = DateTime( 2000, 7, 7, 7, 7, 7, DateTimeKind.Utc );
                D8 = DateTime( 2000, 8, 8, 8, 8, 8, DateTimeKind.Utc );
            } : ConfRW_002_DateTime.T_Test ) :> obj |];
        [|
            ( {
                D1 = DateTime( 2000, 1, 1, 1, 1, 1, DateTimeKind.Utc );
                D2 = [ DateTime( 2000, 2, 2, 2, 2, 2, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 3, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 4, DateTimeKind.Utc ); DateTime( 2000, 2, 2, 2, 2, 5, DateTimeKind.Utc ); ];
                D3 = Some( DateTime( 2000, 3, 3, 3, 3, 3, DateTimeKind.Utc ); );
                D4 = DateTime( 2000, 4, 4, 4, 4, 4, DateTimeKind.Utc );
                D5 = DateTime( 2000, 5, 5, 5, 5, 5, DateTimeKind.Utc );
                D6 = DateTime( 2000, 6, 6, 6, 6, 6, DateTimeKind.Utc );
                D7 = DateTime( 2000, 7, 7, 7, 7, 7, DateTimeKind.Utc );
                D8 = DateTime( 2000, 8, 8, 8, 8, 8, DateTimeKind.Utc );
            } : ConfRW_002_DateTime.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_DateTime_007_data" )>]
    member _.SingleValue_DateTime_007 ( s : ConfRW_002_DateTime.T_Test ) =
        try
            ConfRW_002_DateTime.ConfRW_UT002_DateTime.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>65536</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_TSIH_001 ( s : string ) =
        try
            ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_TSIH_002 ( s : string ) ( exr : uint16 ) =
        let r = ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.LoadString s
        Assert.True( r.D1 = tsih_me.fromPrim exr )

    static member m_SingleValue_TSIH_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ tsih_me.fromPrim 0us; tsih_me.fromPrim 1us; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ tsih_me.fromPrim 0us; tsih_me.fromPrim 1us; tsih_me.fromPrim 2us; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TSIH_003_data" )>]
    member _.SingleValue_TSIH_003 ( s : String ) ( exr : TSIH_T list ) =
        let r = ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_TSIH_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( tsih_me.fromPrim 1us ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TSIH_004_data" )>]
    member _.SingleValue_TSIH_004 ( s : String ) ( exr : TSIH_T option ) =
        let r = ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_TSIH_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; tsih_me.fromPrim 98us :> obj; tsih_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; tsih_me.fromPrim 4us :> obj; tsih_me.fromPrim 5us :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TSIH_005_data" )>]
    member _.SingleValue_TSIH_005 ( s : string ) ( exr_D7 : TSIH_T ) ( exr_D8 : TSIH_T ) =
        let r = ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.LoadString s
        Assert.True( r.D4 = tsih_me.fromPrim 0us; )
        Assert.True( r.D5 = tsih_me.fromPrim 99us; )
        Assert.True( r.D6 = tsih_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_TSIH_006_data = [|
        [|
            ( { D1 = tsih_me.fromPrim 2us; D2 = [ tsih_me.fromPrim 0us; tsih_me.fromPrim 1us; ]; D3 = None; D4 = tsih_me.fromPrim 0us; D5 = tsih_me.fromPrim 0us; D6 = tsih_me.fromPrim 1us; D7 = tsih_me.fromPrim 2us; D8 = tsih_me.fromPrim 3us; } : ConfRW_002_TSIH_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = tsih_me.fromPrim 3us; D2 = [ tsih_me.fromPrim 0us; tsih_me.fromPrim 1us; tsih_me.fromPrim 2us; ]; D3 = None; D4 = tsih_me.fromPrim 0us; D5 = tsih_me.fromPrim 0us; D6 = tsih_me.fromPrim 2us; D7 = tsih_me.fromPrim 3us; D8 = tsih_me.fromPrim 4us; } : ConfRW_002_TSIH_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = tsih_me.fromPrim 2us; D2 = [ tsih_me.fromPrim 0us; tsih_me.fromPrim 1us; ]; D3 = Some( tsih_me.fromPrim 5us ); D4 = tsih_me.fromPrim 0us; D5 = tsih_me.fromPrim 0us; D6 = tsih_me.fromPrim 3us; D7 = tsih_me.fromPrim 4us; D8 = tsih_me.fromPrim 5us; } : ConfRW_002_TSIH_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TSIH_006_data" )>]
    member _.SingleValue_TSIH_006 ( s : ConfRW_002_TSIH_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_TSIH_007_data = [|
        [| ( { D1 = tsih_me.fromPrim 2us; D2 = [ tsih_me.fromPrim 0us; ]; D3 = None; D4 = tsih_me.fromPrim 0us; D5 = tsih_me.fromPrim 0us; D6 = tsih_me.fromPrim 1us; D7 = tsih_me.fromPrim 2us; D8 = tsih_me.fromPrim 3us; } : ConfRW_002_TSIH_T.T_Test ) :> obj |];
        [| ( { D1 = tsih_me.fromPrim 2us; D2 = [ tsih_me.fromPrim 0us; tsih_me.fromPrim 1us; tsih_me.fromPrim 2us; tsih_me.fromPrim 3us; ]; D3 = None; D4 = tsih_me.fromPrim 0us; D5 = tsih_me.fromPrim 0us; D6 = tsih_me.fromPrim 1us; D7 = tsih_me.fromPrim 2us; D8 = tsih_me.fromPrim 3us; } : ConfRW_002_TSIH_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_TSIH_007_data" )>]
    member _.SingleValue_TSIH_007 ( s : ConfRW_002_TSIH_T.T_Test ) =
        try
            ConfRW_002_TSIH_T.ConfRW_UT002_TSIH_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>1</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" )>]
    [<InlineData( "<Test><D1>AAA</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" )>]
    [<InlineData( "<Test><D1>0xAAAAAAAAAAA</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" )>]
    [<InlineData( "<Test><D1>0xBBBBBBBBBBBBB</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" )>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2></Test>" )>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" )>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D3>0xFFFFFFFFFFFF</D3><D3>0xFFFFFFFFFFFF</D3></Test>" )>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D4>0xFFFFFFFFFFFF</D4></Test>" )>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D5>0xFFFFFFFFFFFF</D5></Test>" )>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D6>0xFFFFFFFFFFFF</D6></Test>" )>]
    member _.SingleValue_ISID_001 ( s : string ) =
        try
            ConfRW_002_ISID.ConfRW_UT002_ISID.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>", "0xFFFFFFFFFFFF" )>]
    [<InlineData( "<Test><D1>0XFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>", "0xFFFFFFFFFFFF" )>]
    [<InlineData( "<Test><D1>0x012345678901</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>", "0x012345678901" )>]
    [<InlineData( "<Test><D1>0xaaaaaaaaaaaa</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>", "0xAAAAAAAAAAAA" )>]
    member _.SingleValue_ISID_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_ISID.ConfRW_UT002_ISID.LoadString s
        Assert.True( isid_me.toString r.D1 = exr )

    static member m_SingleValue_ISID_003_data = [|
        [| "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xAAAAAAAAAAAA</D2><D2>0xBBBBBBBBBBBB</D2></Test>" :> obj; [ isid_me.HexStringToISID "0xAAAAAAAAAAAA"; isid_me.HexStringToISID "0xBBBBBBBBBBBB"; ] :> obj |];
        [| "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xAAAAAAAAAAAA</D2><D2>0xBBBBBBBBBBBB</D2><D2>0xCCCCCCCCCCCC</D2></Test>" :> obj; [ isid_me.HexStringToISID "0xAAAAAAAAAAAA"; isid_me.HexStringToISID "0xBBBBBBBBBBBB"; isid_me.HexStringToISID "0xCCCCCCCCCCCC"; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ISID_003_data" )>]
    member _.SingleValue_ISID_003 ( s : String ) ( exr : ISID_T list ) =
        let r = ConfRW_002_ISID.ConfRW_UT002_ISID.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_ISID_004_data = [|
        [| "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D3>0x111111111111</D3></Test>" :> obj; Some( isid_me.HexStringToISID "0x111111111111" ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ISID_004_data" )>]
    member _.SingleValue_ISID_004 ( s : String ) ( exr : ISID_T option ) =
        let r = ConfRW_002_ISID.ConfRW_UT002_ISID.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_ISID_005_data = [|
        [| "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2></Test>" :> obj; isid_me.HexStringToISID "0x0123456789AB" :> obj; |];
        [| "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0xFFFFFFFFFFFF</D2><D2>0xFFFFFFFFFFFF</D2><D7>0x111111111111</D7></Test>" :> obj; isid_me.HexStringToISID "0x111111111111" :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ISID_005_data" )>]
    member _.SingleValue_ISID_005 ( s : string ) ( exr_D7 : ISID_T ) =
        let r = ConfRW_002_ISID.ConfRW_UT002_ISID.LoadString s
        Assert.True( r.D4 = isid_me.zero )
        Assert.True( r.D5 = isid_me.HexStringToISID "0x1234567890AB" )
        Assert.True( r.D7 = exr_D7 )

    static member m_SingleValue_ISID_006_data = [|
        [|
            ( {
                D1 = isid_me.HexStringToISID "0xFFFFFFFFFFFF";
                D2 = [ isid_me.HexStringToISID "0x111111111111"; isid_me.HexStringToISID "0x222222222222"; ];
                D3 = None;
                D4 = isid_me.HexStringToISID "0x444444444444";
                D5 = isid_me.HexStringToISID "0x555555555555";
                D7 = isid_me.HexStringToISID "0x777777777777";
            } : ConfRW_002_ISID.T_Test ) :> obj;
            "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0x111111111111</D2><D2>0x222222222222</D2><D7>0x777777777777</D7></Test>" :> obj
        |];
        [|
            ( {
                D1 = isid_me.HexStringToISID "0xFFFFFFFFFFFF";
                D2 = [ isid_me.HexStringToISID "0x111111111111"; isid_me.HexStringToISID "0x222222222222"; isid_me.HexStringToISID "0x333333333333"; ];
                D3 = None;
                D4 = isid_me.HexStringToISID "0x444444444444";
                D5 = isid_me.HexStringToISID "0x555555555555";
                D7 = isid_me.HexStringToISID "0x777777777777";
            } : ConfRW_002_ISID.T_Test ) :> obj;
            "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0x111111111111</D2><D2>0x222222222222</D2><D2>0x333333333333</D2><D7>0x777777777777</D7></Test>" :> obj
        |];
        [|
            ( {
                D1 = isid_me.HexStringToISID "0xFFFFFFFFFFFF";
                D2 = [ isid_me.HexStringToISID "0x111111111111"; isid_me.HexStringToISID "0x222222222222"; ];
                D3 = Some( isid_me.HexStringToISID "0x333333333333" );
                D4 = isid_me.HexStringToISID "0x444444444444";
                D5 = isid_me.HexStringToISID "0x555555555555";
                D7 = isid_me.HexStringToISID "0x777777777777";
            } : ConfRW_002_ISID.T_Test ) :> obj;
            "<Test><D1>0xFFFFFFFFFFFF</D1><D2>0x111111111111</D2><D2>0x222222222222</D2><D3>0x333333333333</D3><D7>0x777777777777</D7></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ISID_006_data" )>]
    member _.SingleValue_ISID_006 ( s : ConfRW_002_ISID.T_Test ) ( exr : string ) =
        let r = ConfRW_002_ISID.ConfRW_UT002_ISID.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_ISID_007_data = [|
        [|
            ( {
                D1 = isid_me.HexStringToISID "0xFFFFFFFFFFFF";
                D2 = [ isid_me.HexStringToISID "0x111111111111"; ];
                D3 = Some( isid_me.HexStringToISID "0x333333333333" );
                D4 = isid_me.HexStringToISID "0x444444444444";
                D5 = isid_me.HexStringToISID "0x555555555555";
                D7 = isid_me.HexStringToISID "0x777777777777";
            } : ConfRW_002_ISID.T_Test ) :> obj;
        |];
        [|
            ( {
                D1 = isid_me.HexStringToISID "0xFFFFFFFFFFFF";
                D2 = [ isid_me.HexStringToISID "0x111111111111"; isid_me.HexStringToISID "0x222222222222"; isid_me.HexStringToISID "0x333333333333"; isid_me.HexStringToISID "0x444444444444"; ];
                D3 = Some( isid_me.HexStringToISID "0x333333333333" );
                D4 = isid_me.HexStringToISID "0x444444444444";
                D5 = isid_me.HexStringToISID "0x555555555555";
                D7 = isid_me.HexStringToISID "0x777777777777";
            } : ConfRW_002_ISID.T_Test ) :> obj;
        |]
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ISID_007_data" )>]
    member _.SingleValue_ISID_007 ( s : ConfRW_002_ISID.T_Test ) =
        try
            ConfRW_002_ISID.ConfRW_UT002_ISID.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>65536</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_CID_001 ( s : string ) =
        try
            ConfRW_002_CID_T.ConfRW_UT002_CID_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_CID_002 ( s : string ) ( exr : uint16 ) =
        let r = ConfRW_002_CID_T.ConfRW_UT002_CID_T.LoadString s
        Assert.True( r.D1 = cid_me.fromPrim exr )

    static member m_SingleValue_CID_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ cid_me.fromPrim 0us; cid_me.fromPrim 1us; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ cid_me.fromPrim 0us; cid_me.fromPrim 1us; cid_me.fromPrim 2us; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CID_003_data" )>]
    member _.SingleValue_CID_003 ( s : String ) ( exr : CID_T list ) =
        let r = ConfRW_002_CID_T.ConfRW_UT002_CID_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_CID_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( cid_me.fromPrim 1us ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CID_004_data" )>]
    member _.SingleValue_CID_004 ( s : String ) ( exr : CID_T option ) =
        let r = ConfRW_002_CID_T.ConfRW_UT002_CID_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_CID_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; cid_me.fromPrim 98us :> obj; cid_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; cid_me.fromPrim 4us :> obj; cid_me.fromPrim 5us :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CID_005_data" )>]
    member _.SingleValue_CID_005 ( s : string ) ( exr_D7 : CID_T ) ( exr_D8 : CID_T ) =
        let r = ConfRW_002_CID_T.ConfRW_UT002_CID_T.LoadString s
        Assert.True( r.D4 = cid_me.fromPrim 0us; )
        Assert.True( r.D5 = cid_me.fromPrim 99us; )
        Assert.True( r.D6 = cid_me.fromPrim ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_CID_006_data = [|
        [|
            ( { D1 = cid_me.fromPrim 2us; D2 = [ cid_me.fromPrim 0us; cid_me.fromPrim 1us; ]; D3 = None; D4 = cid_me.fromPrim 0us; D5 = cid_me.fromPrim 0us; D6 = cid_me.fromPrim 1us; D7 = cid_me.fromPrim 2us; D8 = cid_me.fromPrim 3us; } : ConfRW_002_CID_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = cid_me.fromPrim 3us; D2 = [ cid_me.fromPrim 0us; cid_me.fromPrim 1us; cid_me.fromPrim 2us; ]; D3 = None; D4 = cid_me.fromPrim 0us; D5 = cid_me.fromPrim 0us; D6 = cid_me.fromPrim 2us; D7 = cid_me.fromPrim 3us; D8 = cid_me.fromPrim 4us; } : ConfRW_002_CID_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = cid_me.fromPrim 2us; D2 = [ cid_me.fromPrim 0us; cid_me.fromPrim 1us; ]; D3 = Some( cid_me.fromPrim 5us ); D4 = cid_me.fromPrim 0us; D5 = cid_me.fromPrim 0us; D6 = cid_me.fromPrim 3us; D7 = cid_me.fromPrim 4us; D8 = cid_me.fromPrim 5us; } : ConfRW_002_CID_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CID_006_data" )>]
    member _.SingleValue_CID_006 ( s : ConfRW_002_CID_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_CID_T.ConfRW_UT002_CID_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_CID_007_data = [|
        [| ( { D1 = cid_me.fromPrim 2us; D2 = [ cid_me.fromPrim 0us; ]; D3 = None; D4 = cid_me.fromPrim 0us; D5 = cid_me.fromPrim 0us; D6 = cid_me.fromPrim 1us; D7 = cid_me.fromPrim 2us; D8 = cid_me.fromPrim 3us; } : ConfRW_002_CID_T.T_Test ) :> obj |];
        [| ( { D1 = cid_me.fromPrim 2us; D2 = [ cid_me.fromPrim 0us; cid_me.fromPrim 1us; cid_me.fromPrim 2us; cid_me.fromPrim 3us; ]; D3 = None; D4 = cid_me.fromPrim 0us; D5 = cid_me.fromPrim 0us; D6 = cid_me.fromPrim 1us; D7 = cid_me.fromPrim 2us; D8 = cid_me.fromPrim 3us; } : ConfRW_002_CID_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CID_007_data" )>]
    member _.SingleValue_CID_007 ( s : ConfRW_002_CID_T.T_Test ) =
        try
            ConfRW_002_CID_T.ConfRW_UT002_CID_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-2147483649</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2147483648</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_CONCNT_001 ( s : string ) =
        try
            ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2 )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3 )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4 )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5 )>]
    member _.SingleValue_CONCNT_002 ( s : string ) ( exr : int ) =
        let r = ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.LoadString s
        Assert.True( r.D1 = concnt_me.fromPrim exr )

    static member m_SingleValue_CONCNT_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ concnt_me.fromPrim 0; concnt_me.fromPrim 1; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ concnt_me.fromPrim 0; concnt_me.fromPrim 1; concnt_me.fromPrim 2; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CONCNT_003_data" )>]
    member _.SingleValue_CONCNT_003 ( s : String ) ( exr : CONCNT_T list ) =
        let r = ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_CONCNT_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( concnt_me.fromPrim 1 ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CONCNT_004_data" )>]
    member _.SingleValue_CONCNT_004 ( s : String ) ( exr : CONCNT_T option ) =
        let r = ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_CONCNT_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; concnt_me.fromPrim 98 :> obj; concnt_me.fromPrim ( int Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; concnt_me.fromPrim 4 :> obj; concnt_me.fromPrim 5 :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CONCNT_005_data" )>]
    member _.SingleValue_CONCNT_005 ( s : string ) ( exr_D7 : CONCNT_T ) ( exr_D8 : CONCNT_T ) =
        let r = ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.LoadString s
        Assert.True( r.D4 = concnt_me.fromPrim 0; )
        Assert.True( r.D5 = concnt_me.fromPrim 99; )
        Assert.True( r.D6 = concnt_me.fromPrim ( int Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_CONCNT_006_data = [|
        [|
            ( { D1 = concnt_me.fromPrim 2; D2 = [ concnt_me.fromPrim 0; concnt_me.fromPrim 1; ]; D3 = None; D4 = concnt_me.fromPrim 0; D5 = concnt_me.fromPrim 0; D6 = concnt_me.fromPrim 1; D7 = concnt_me.fromPrim 2; D8 = concnt_me.fromPrim 3; } : ConfRW_002_CONCNT_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = concnt_me.fromPrim 3; D2 = [ concnt_me.fromPrim 0; concnt_me.fromPrim 1; concnt_me.fromPrim 2; ]; D3 = None; D4 = concnt_me.fromPrim 0; D5 = concnt_me.fromPrim 0; D6 = concnt_me.fromPrim 2; D7 = concnt_me.fromPrim 3; D8 = concnt_me.fromPrim 4; } : ConfRW_002_CONCNT_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = concnt_me.fromPrim 2; D2 = [ concnt_me.fromPrim 0; concnt_me.fromPrim 1; ]; D3 = Some( concnt_me.fromPrim 5 ); D4 = concnt_me.fromPrim 0; D5 = concnt_me.fromPrim 0; D6 = concnt_me.fromPrim 3; D7 = concnt_me.fromPrim 4; D8 = concnt_me.fromPrim 5; } : ConfRW_002_CONCNT_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CONCNT_006_data" )>]
    member _.SingleValue_CONCNT_006 ( s : ConfRW_002_CONCNT_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_CONCNT_007_data = [|
        [| ( { D1 = concnt_me.fromPrim 2; D2 = [ concnt_me.fromPrim 0; ]; D3 = None; D4 = concnt_me.fromPrim 0; D5 = concnt_me.fromPrim 0; D6 = concnt_me.fromPrim 1; D7 = concnt_me.fromPrim 2; D8 = concnt_me.fromPrim 3; } : ConfRW_002_CONCNT_T.T_Test ) :> obj |];
        [| ( { D1 = concnt_me.fromPrim 2; D2 = [ concnt_me.fromPrim 0; concnt_me.fromPrim 1; concnt_me.fromPrim 2; concnt_me.fromPrim 3; ]; D3 = None; D4 = concnt_me.fromPrim 0; D5 = concnt_me.fromPrim 0; D6 = concnt_me.fromPrim 1; D7 = concnt_me.fromPrim 2; D8 = concnt_me.fromPrim 3; } : ConfRW_002_CONCNT_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_CONCNT_007_data" )>]
    member _.SingleValue_CONCNT_007 ( s : ConfRW_002_CONCNT_T.T_Test ) =
        try
            ConfRW_002_CONCNT_T.ConfRW_UT002_CONCNT_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2></Test>" )>]
    [<InlineData( "<Test><D1></D1><D2>0</D2><D2>0</D2></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D4>0</D4></Test>" )>]
    member _.SingleValue_unit_001 ( s : string ) =
        try
            ConfRW_002_unit.ConfRW_UT002_unit.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Fact>]
    member _.SingleValue_unit_002() =
        let r = ConfRW_002_unit.ConfRW_UT002_unit.LoadString "<Test><D1>0</D1><D2>0</D2><D2>0</D2></Test>"
        Assert.True( r.D1 = () )

    static member m_SingleValue_unit_003_data = [|
        [| "<Test><D1>0</D1><D2>0</D2><D2>0</D2></Test>" :> obj; [ (); () ] :> obj |];
        [| "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D2>0</D2></Test>" :> obj; [ (); (); (); ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_unit_003_data" )>]
    member _.SingleValue_unit_003 ( s : String ) ( exr : unit list ) =
        let r = ConfRW_002_unit.ConfRW_UT002_unit.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_unit_004_data = [|
        [| "<Test><D1>0</D1><D2>0</D2><D2>0</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" :> obj; Some() :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_unit_004_data" )>]
    member _.SingleValue_unit_004 ( s : String ) ( exr : unit option ) =
        let r = ConfRW_002_unit.ConfRW_UT002_unit.LoadString s
        Assert.True( r.D3 = exr )
        Assert.True( r.D4 = () )

    static member m_SingleValue_unit_005_data = [|
        [|
            ( { D1 = (); D2 = [ (); (); ]; D3 = None; D4 = (); } : ConfRW_002_unit.T_Test ) :> obj;
            "<Test><D1>0</D1><D2>0</D2><D2>0</D2></Test>" :> obj
        |];
        [|
            ( { D1 = (); D2 = [ (); (); (); ]; D3 = None; D4 = (); } : ConfRW_002_unit.T_Test ) :> obj;
            "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D2>0</D2></Test>" :> obj
        |];
        [|
            ( { D1 = (); D2 = [ (); (); ]; D3 = Some(); D4 = (); } : ConfRW_002_unit.T_Test ) :> obj;
            "<Test><D1>0</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_unit_005_data" )>]
    member _.SingleValue_unit_005 ( s : ConfRW_002_unit.T_Test ) ( exr : string ) =
        let r = ConfRW_002_unit.ConfRW_UT002_unit.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_unit_006_data = [|
        [|
            ( { D1 = (); D2 = [ (); ]; D3 = None; D4 = (); } : ConfRW_002_unit.T_Test ) :> obj;
        |];
        [|
            ( { D1 = (); D2 = [ (); (); (); (); ]; D3 = None; D4 = (); } : ConfRW_002_unit.T_Test ) :> obj;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_unit_006_data" )>]
    member _.SingleValue_unit_006 ( s : ConfRW_002_unit.T_Test ) =
        try
            ConfRW_002_unit.ConfRW_UT002_unit.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1></D1><D2>Any</D2><D2>Any</D2></Test>" )>]
    [<InlineData( "<Test><D1>aaa</D1><D2>Any</D2><D2>Any</D2></Test>" )>]
    [<InlineData( "<Test><D1>IPFilter( aaa, bbb )</D1><D2>Any</D2><D2>Any</D2></Test>" )>]
    [<InlineData( "<Test><D1>Any</D1><D2>Any</D2></Test>" )>]
    [<InlineData( "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2><D2>Any</D2><D2>Any</D2></Test>" )>]
    [<InlineData( "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2><D3>Any</D3><D3>Any</D3></Test>" )>]
    [<InlineData( "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2><D3>Any</D3><D3>Any</D3><D3>Any</D3></Test>" )>]
    [<InlineData( "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2><D4>Any</D4></Test>" )>]
    member _.SingleValue_IPCondition_001 ( s : string ) =
        try
            ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2></Test>", "Any" )>]
    [<InlineData( "<Test><D1>Loopback</D1><D2>Any</D2><D2>Any</D2></Test>", "Loopback" )>]
    [<InlineData( "<Test><D1>Linklocal</D1><D2>Any</D2><D2>Any</D2></Test>", "Linklocal" )>]
    [<InlineData( "<Test><D1>Private</D1><D2>Any</D2><D2>Any</D2></Test>", "Private" )>]
    [<InlineData( "<Test><D1>Multicast</D1><D2>Any</D2><D2>Any</D2></Test>", "Multicast" )>]
    [<InlineData( "<Test><D1>Global</D1><D2>Any</D2><D2>Any</D2></Test>", "Global" )>]
    [<InlineData( "<Test><D1>IPv4Any</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv4Any" )>]
    [<InlineData( "<Test><D1>IPv4Loopback</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv4Loopback" )>]
    [<InlineData( "<Test><D1>IPv4Linklocal</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv4Linklocal" )>]
    [<InlineData( "<Test><D1>IPv4Private</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv4Private" )>]
    [<InlineData( "<Test><D1>IPv4Multicast</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv4Multicast" )>]
    [<InlineData( "<Test><D1>IPv4Global</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv4Global" )>]
    [<InlineData( "<Test><D1>IPv6Any</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv6Any" )>]
    [<InlineData( "<Test><D1>IPv6Loopback</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv6Loopback" )>]
    [<InlineData( "<Test><D1>IPv6Linklocal</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv6Linklocal" )>]
    [<InlineData( "<Test><D1>IPv6Private</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv6Private" )>]
    [<InlineData( "<Test><D1>IPv6Multicast</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv6Multicast" )>]
    [<InlineData( "<Test><D1>IPv6Global</D1><D2>Any</D2><D2>Any</D2></Test>", "IPv6Global" )>]
    [<InlineData( "<Test><D1>IPFilter( 192.168.1.1, 255.255.0.0 )</D1><D2>Any</D2><D2>Any</D2></Test>", "IPFilter( 192.168.1.1, 255.255.0.0 )" )>]
    member _.SingleValue_IPCondition_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.LoadString s
        let ws = r.D1.ToString()
        Assert.True(( ws = exr ))

    static member m_SingleValue_IPCondition_003_data = [|
        [| "<Test><D1>Any</D1><D2>Loopback</D2><D2>Linklocal</D2></Test>" :> obj; [ IPCondition.Loopback; IPCondition.Linklocal; ] :> obj |];
        [| "<Test><D1>Any</D1><D2>IPv4Any</D2><D2>IPv4Global</D2><D2>IPv6Linklocal</D2></Test>" :> obj; [ IPCondition.IPv4Any; IPCondition.IPv4Global; IPCondition.IPv6Linklocal; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_IPCondition_003_data" )>]
    member _.SingleValue_IPCondition_003 ( s : String ) ( exr : IPCondition list ) =
        let r = ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_IPCondition_004_data = [|
        [| "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2><D3>IPv6Multicast</D3></Test>" :> obj; Some( IPCondition.IPv6Multicast ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_IPCondition_004_data" )>]
    member _.SingleValue_IPCondition_004 ( s : String ) ( exr : IPCondition option ) =
        let r = ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_IPCondition_005_data = [|
        [| "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2></Test>" :> obj; IPCondition.IPv6Linklocal :> obj; |];
        [| "<Test><D1>Any</D1><D2>Any</D2><D2>Any</D2><D7>IPv4Global</D7></Test>" :> obj; IPCondition.IPv4Global :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_IPCondition_005_data" )>]
    member _.SingleValue_IPCondition_005 ( s : string ) ( exr_D7 : IPCondition ) =
        let r = ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.LoadString s
        Assert.True( r.D4 = IPCondition.Loopback )
        Assert.True( r.D5 = IPCondition.Multicast )
        Assert.True( r.D7 = exr_D7 )

    static member m_SingleValue_IPCondition_006_data = [|
        [|
            ( { D1 = IPCondition.IPv4Any; D2 = [ IPCondition.Global; IPCondition.IPv6Any; ]; D3 = None; D4 = IPCondition.Loopback; D5 = IPCondition.Private; D7 = IPCondition.IPv4Private; } : ConfRW_002_IPCondition.T_Test ) :> obj;
            "<Test><D1>IPv4Any</D1><D2>Global</D2><D2>IPv6Any</D2><D7>IPv4Private</D7></Test>" :> obj
        |];
        [|
            ( { D1 = IPCondition.IPv4Any; D2 = [ IPCondition.Global; IPCondition.IPv6Any; IPCondition.IPv4Multicast; ]; D3 = None; D4 = IPCondition.Loopback; D5 = IPCondition.Private; D7 = IPCondition.IPv4Private; } : ConfRW_002_IPCondition.T_Test ) :> obj;
            "<Test><D1>IPv4Any</D1><D2>Global</D2><D2>IPv6Any</D2><D2>IPv4Multicast</D2><D7>IPv4Private</D7></Test>" :> obj
        |];
        [|
            ( { D1 = IPCondition.IPv4Any; D2 = [ IPCondition.Global; IPCondition.IPv6Any; ]; D3 = Some( IPCondition.IPv4Linklocal ); D4 = IPCondition.Loopback; D5 = IPCondition.Private; D7 = IPCondition.IPv4Private; } : ConfRW_002_IPCondition.T_Test ) :> obj;
            "<Test><D1>IPv4Any</D1><D2>Global</D2><D2>IPv6Any</D2><D3>IPv4Linklocal</D3><D7>IPv4Private</D7></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_IPCondition_006_data" )>]
    member _.SingleValue_IPCondition_006 ( s : ConfRW_002_IPCondition.T_Test ) ( exr : string ) =
        let r = ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_IPCondition_007_data = [|
        [| ( { D1 = IPCondition.Any; D2 = [ IPCondition.Any; ]; D3 = None; D4 = IPCondition.Any; D5 = IPCondition.Any; D7 = IPCondition.Any; } : ConfRW_002_IPCondition.T_Test ) :> obj; |];
        [| ( { D1 = IPCondition.Any; D2 = [ IPCondition.Any; IPCondition.Any; IPCondition.Any; IPCondition.Any; ]; D3 = None; D4 = IPCondition.Any; D5 = IPCondition.Any; D7 = IPCondition.Any; } : ConfRW_002_IPCondition.T_Test ) :> obj; |];
        [| ( { D1 = IPCondition.IPFilter( [| 0uy; 1uy; 2uy; |], Array.empty ); D2 = [ IPCondition.Any; IPCondition.Any; ]; D3 = None; D4 = IPCondition.Any; D5 = IPCondition.Any; D7 = IPCondition.Any; } : ConfRW_002_IPCondition.T_Test ) :> obj; |];
        [| ( {
            D1 = IPCondition.IPFilter(
                ( IPAddress.Parse "192.168.1.1" ).GetAddressBytes(),
                ( IPAddress.Parse "1::1" ).GetAddressBytes()
            );
            D2 = [ IPCondition.Any; IPCondition.Any; ];
            D3 = None;
            D4 = IPCondition.Any;
            D5 = IPCondition.Any;
            D7 = IPCondition.Any;
        } : ConfRW_002_IPCondition.T_Test ) :> obj; |]
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_IPCondition_007_data" )>]
    member _.SingleValue_IPCondition_007 ( s : ConfRW_002_IPCondition.T_Test ) =
        try
            ConfRW_002_IPCondition.ConfRW_UT002_IPCondition.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>4294967296</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_ITT_001 ( s : string ) =
        try
            ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_ITT_002 ( s : string ) ( exr : uint32 ) =
        let r = ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.LoadString s
        Assert.True( r.D1 = itt_me.fromPrim exr )

    static member m_SingleValue_ITT_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ itt_me.fromPrim 0u; itt_me.fromPrim 1u; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ itt_me.fromPrim 0u; itt_me.fromPrim 1u; itt_me.fromPrim 2u; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ITT_003_data" )>]
    member _.SingleValue_ITT_003 ( s : String ) ( exr : ITT_T list ) =
        let r = ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_ITT_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( itt_me.fromPrim 1u ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ITT_004_data" )>]
    member _.SingleValue_ITT_004 ( s : String ) ( exr : ITT_T option ) =
        let r = ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_ITT_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; itt_me.fromPrim 98u :> obj; itt_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; itt_me.fromPrim 4u :> obj; itt_me.fromPrim 5u :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ITT_005_data" )>]
    member _.SingleValue_ITT_005 ( s : string ) ( exr_D7 : ITT_T ) ( exr_D8 : ITT_T ) =
        let r = ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.LoadString s
        Assert.True( r.D4 = itt_me.fromPrim 0u; )
        Assert.True( r.D5 = itt_me.fromPrim 99u; )
        Assert.True( r.D6 = itt_me.fromPrim ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_ITT_006_data = [|
        [|
            ( { D1 = itt_me.fromPrim 2u; D2 = [ itt_me.fromPrim 0u; itt_me.fromPrim 1u; ]; D3 = None; D4 = itt_me.fromPrim 0u; D5 = itt_me.fromPrim 0u; D6 = itt_me.fromPrim 1u; D7 = itt_me.fromPrim 2u; D8 = itt_me.fromPrim 3u; } : ConfRW_002_ITT_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = itt_me.fromPrim 3u; D2 = [ itt_me.fromPrim 0u; itt_me.fromPrim 1u; itt_me.fromPrim 2u; ]; D3 = None; D4 = itt_me.fromPrim 0u; D5 = itt_me.fromPrim 0u; D6 = itt_me.fromPrim 2u; D7 = itt_me.fromPrim 3u; D8 = itt_me.fromPrim 4u; } : ConfRW_002_ITT_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = itt_me.fromPrim 2u; D2 = [ itt_me.fromPrim 0u; itt_me.fromPrim 1u; ]; D3 = Some( itt_me.fromPrim 5u ); D4 = itt_me.fromPrim 0u; D5 = itt_me.fromPrim 0u; D6 = itt_me.fromPrim 3u; D7 = itt_me.fromPrim 4u; D8 = itt_me.fromPrim 5u; } : ConfRW_002_ITT_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ITT_006_data" )>]
    member _.SingleValue_ITT_006 ( s : ConfRW_002_ITT_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_ITT_007_data = [|
        [| ( { D1 = itt_me.fromPrim 2u; D2 = [ itt_me.fromPrim 0u; ]; D3 = None; D4 = itt_me.fromPrim 0u; D5 = itt_me.fromPrim 0u; D6 = itt_me.fromPrim 1u; D7 = itt_me.fromPrim 2u; D8 = itt_me.fromPrim 3u; } : ConfRW_002_ITT_T.T_Test ) :> obj |];
        [| ( { D1 = itt_me.fromPrim 2u; D2 = [ itt_me.fromPrim 0u; itt_me.fromPrim 1u; itt_me.fromPrim 2u; itt_me.fromPrim 3u; ]; D3 = None; D4 = itt_me.fromPrim 0u; D5 = itt_me.fromPrim 0u; D6 = itt_me.fromPrim 1u; D7 = itt_me.fromPrim 2u; D8 = itt_me.fromPrim 3u; } : ConfRW_002_ITT_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_ITT_007_data" )>]
    member _.SingleValue_ITT_007 ( s : ConfRW_002_ITT_T.T_Test ) =
        try
            ConfRW_002_ITT_T.ConfRW_UT002_ITT_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>256</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_BLKCNT8_001 ( s : string ) =
        try
            ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2uy )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3uy )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4uy )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5uy )>]
    member _.SingleValue_BLKCNT8_002 ( s : string ) ( exr : uint8 ) =
        let r = ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.LoadString s
        Assert.True( r.D1 = blkcnt_me.ofUInt8 exr )

    static member m_SingleValue_BLKCNT8_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt8 0uy; blkcnt_me.ofUInt8 1uy; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt8 0uy; blkcnt_me.ofUInt8 1uy; blkcnt_me.ofUInt8 2uy; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT8_003_data" )>]
    member _.SingleValue_BLKCNT8_003 ( s : String ) ( exr : BLKCNT8_T list ) =
        let r = ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_BLKCNT8_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( blkcnt_me.ofUInt8 1uy ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT8_004_data" )>]
    member _.SingleValue_BLKCNT8_004 ( s : String ) ( exr : BLKCNT8_T option ) =
        let r = ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_BLKCNT8_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; blkcnt_me.ofUInt8 98uy :> obj; blkcnt_me.ofUInt8 ( uint8 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; blkcnt_me.ofUInt8 4uy :> obj; blkcnt_me.ofUInt8 5uy :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT8_005_data" )>]
    member _.SingleValue_BLKCNT8_005 ( s : string ) ( exr_D7 : BLKCNT8_T ) ( exr_D8 : BLKCNT8_T ) =
        let r = ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.LoadString s
        Assert.True( r.D4 = blkcnt_me.ofUInt8 0uy; )
        Assert.True( r.D5 = blkcnt_me.ofUInt8 99uy; )
        Assert.True( r.D6 = blkcnt_me.ofUInt8 ( uint8 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_BLKCNT8_006_data = [|
        [|
            ( { D1 = blkcnt_me.ofUInt8 2uy; D2 = [ blkcnt_me.ofUInt8 0uy; blkcnt_me.ofUInt8 1uy; ]; D3 = None; D4 = blkcnt_me.ofUInt8 0uy; D5 = blkcnt_me.ofUInt8 0uy; D6 = blkcnt_me.ofUInt8 1uy; D7 = blkcnt_me.ofUInt8 2uy; D8 = blkcnt_me.ofUInt8 3uy; } : ConfRW_002_BLKCNT8_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt8 3uy; D2 = [ blkcnt_me.ofUInt8 0uy; blkcnt_me.ofUInt8 1uy; blkcnt_me.ofUInt8 2uy; ]; D3 = None; D4 = blkcnt_me.ofUInt8 0uy; D5 = blkcnt_me.ofUInt8 0uy; D6 = blkcnt_me.ofUInt8 2uy; D7 = blkcnt_me.ofUInt8 3uy; D8 = blkcnt_me.ofUInt8 4uy; } : ConfRW_002_BLKCNT8_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt8 2uy; D2 = [ blkcnt_me.ofUInt8 0uy; blkcnt_me.ofUInt8 1uy; ]; D3 = Some( blkcnt_me.ofUInt8 5uy ); D4 = blkcnt_me.ofUInt8 0uy; D5 = blkcnt_me.ofUInt8 0uy; D6 = blkcnt_me.ofUInt8 3uy; D7 = blkcnt_me.ofUInt8 4uy; D8 = blkcnt_me.ofUInt8 5uy; } : ConfRW_002_BLKCNT8_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT8_006_data" )>]
    member _.SingleValue_BLKCNT8_006 ( s : ConfRW_002_BLKCNT8_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_BLKCNT8_007_data = [|
        [| ( { D1 = blkcnt_me.ofUInt8 2uy; D2 = [ blkcnt_me.ofUInt8 0uy; ]; D3 = None; D4 = blkcnt_me.ofUInt8 0uy; D5 = blkcnt_me.ofUInt8 0uy; D6 = blkcnt_me.ofUInt8 1uy; D7 = blkcnt_me.ofUInt8 2uy; D8 = blkcnt_me.ofUInt8 3uy; } : ConfRW_002_BLKCNT8_T.T_Test ) :> obj |];
        [| ( { D1 = blkcnt_me.ofUInt8 2uy; D2 = [ blkcnt_me.ofUInt8 0uy; blkcnt_me.ofUInt8 1uy; blkcnt_me.ofUInt8 2uy; blkcnt_me.ofUInt8 3uy; ]; D3 = None; D4 = blkcnt_me.ofUInt8 0uy; D5 = blkcnt_me.ofUInt8 0uy; D6 = blkcnt_me.ofUInt8 1uy; D7 = blkcnt_me.ofUInt8 2uy; D8 = blkcnt_me.ofUInt8 3uy; } : ConfRW_002_BLKCNT8_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT8_007_data" )>]
    member _.SingleValue_BLKCNT8_007 ( s : ConfRW_002_BLKCNT8_T.T_Test ) =
        try
            ConfRW_002_BLKCNT8_T.ConfRW_UT002_BLKCNT8_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>65536</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_BLKCNT16_001 ( s : string ) =
        try
            ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2us )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3us )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4us )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5us )>]
    member _.SingleValue_BLKCNT16_002 ( s : string ) ( exr : uint16 ) =
        let r = ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.LoadString s
        Assert.True( r.D1 = blkcnt_me.ofUInt16 exr )

    static member m_SingleValue_BLKCNT16_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt16 0us; blkcnt_me.ofUInt16 1us; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt16 0us; blkcnt_me.ofUInt16 1us; blkcnt_me.ofUInt16 2us; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT16_003_data" )>]
    member _.SingleValue_BLKCNT16_003 ( s : String ) ( exr : BLKCNT16_T list ) =
        let r = ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_BLKCNT16_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( blkcnt_me.ofUInt16 1us ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT16_004_data" )>]
    member _.SingleValue_BLKCNT16_004 ( s : String ) ( exr : BLKCNT16_T option ) =
        let r = ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_BLKCNT16_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; blkcnt_me.ofUInt16 98us :> obj; blkcnt_me.ofUInt16 ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; blkcnt_me.ofUInt16 4us :> obj; blkcnt_me.ofUInt16 5us :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT16_005_data" )>]
    member _.SingleValue_BLKCNT16_005 ( s : string ) ( exr_D7 : BLKCNT16_T ) ( exr_D8 : BLKCNT16_T ) =
        let r = ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.LoadString s
        Assert.True( r.D4 = blkcnt_me.ofUInt16 0us; )
        Assert.True( r.D5 = blkcnt_me.ofUInt16 99us; )
        Assert.True( r.D6 = blkcnt_me.ofUInt16 ( uint16 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_BLKCNT16_006_data = [|
        [|
            ( { D1 = blkcnt_me.ofUInt16 2us; D2 = [ blkcnt_me.ofUInt16 0us; blkcnt_me.ofUInt16 1us; ]; D3 = None; D4 = blkcnt_me.ofUInt16 0us; D5 = blkcnt_me.ofUInt16 0us; D6 = blkcnt_me.ofUInt16 1us; D7 = blkcnt_me.ofUInt16 2us; D8 = blkcnt_me.ofUInt16 3us; } : ConfRW_002_BLKCNT16_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt16 3us; D2 = [ blkcnt_me.ofUInt16 0us; blkcnt_me.ofUInt16 1us; blkcnt_me.ofUInt16 2us; ]; D3 = None; D4 = blkcnt_me.ofUInt16 0us; D5 = blkcnt_me.ofUInt16 0us; D6 = blkcnt_me.ofUInt16 2us; D7 = blkcnt_me.ofUInt16 3us; D8 = blkcnt_me.ofUInt16 4us; } : ConfRW_002_BLKCNT16_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt16 2us; D2 = [ blkcnt_me.ofUInt16 0us; blkcnt_me.ofUInt16 1us; ]; D3 = Some( blkcnt_me.ofUInt16 5us ); D4 = blkcnt_me.ofUInt16 0us; D5 = blkcnt_me.ofUInt16 0us; D6 = blkcnt_me.ofUInt16 3us; D7 = blkcnt_me.ofUInt16 4us; D8 = blkcnt_me.ofUInt16 5us; } : ConfRW_002_BLKCNT16_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT16_006_data" )>]
    member _.SingleValue_BLKCNT16_006 ( s : ConfRW_002_BLKCNT16_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_BLKCNT16_007_data = [|
        [| ( { D1 = blkcnt_me.ofUInt16 2us; D2 = [ blkcnt_me.ofUInt16 0us; ]; D3 = None; D4 = blkcnt_me.ofUInt16 0us; D5 = blkcnt_me.ofUInt16 0us; D6 = blkcnt_me.ofUInt16 1us; D7 = blkcnt_me.ofUInt16 2us; D8 = blkcnt_me.ofUInt16 3us; } : ConfRW_002_BLKCNT16_T.T_Test ) :> obj |];
        [| ( { D1 = blkcnt_me.ofUInt16 2us; D2 = [ blkcnt_me.ofUInt16 0us; blkcnt_me.ofUInt16 1us; blkcnt_me.ofUInt16 2us; blkcnt_me.ofUInt16 3us; ]; D3 = None; D4 = blkcnt_me.ofUInt16 0us; D5 = blkcnt_me.ofUInt16 0us; D6 = blkcnt_me.ofUInt16 1us; D7 = blkcnt_me.ofUInt16 2us; D8 = blkcnt_me.ofUInt16 3us; } : ConfRW_002_BLKCNT16_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT16_007_data" )>]
    member _.SingleValue_BLKCNT16_007 ( s : ConfRW_002_BLKCNT16_T.T_Test ) =
        try
            ConfRW_002_BLKCNT16_T.ConfRW_UT002_BLKCNT16_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>4294967296</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_BLKCNT32_001 ( s : string ) =
        try
            ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2u )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3u )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4u )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5u )>]
    member _.SingleValue_BLKCNT32_002 ( s : string ) ( exr : uint32 ) =
        let r = ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.LoadString s
        Assert.True( r.D1 = blkcnt_me.ofUInt32 exr )

    static member m_SingleValue_BLKCNT32_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt32 0u; blkcnt_me.ofUInt32 1u; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt32 0u; blkcnt_me.ofUInt32 1u; blkcnt_me.ofUInt32 2u; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT32_003_data" )>]
    member _.SingleValue_BLKCNT32_003 ( s : String ) ( exr : BLKCNT32_T list ) =
        let r = ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_BLKCNT32_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( blkcnt_me.ofUInt32 1u ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT32_004_data" )>]
    member _.SingleValue_BLKCNT32_004 ( s : String ) ( exr : BLKCNT32_T option ) =
        let r = ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_BLKCNT32_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; blkcnt_me.ofUInt32 98u :> obj; blkcnt_me.ofUInt32 ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; blkcnt_me.ofUInt32 4u :> obj; blkcnt_me.ofUInt32 5u :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT32_005_data" )>]
    member _.SingleValue_BLKCNT32_005 ( s : string ) ( exr_D7 : BLKCNT32_T ) ( exr_D8 : BLKCNT32_T ) =
        let r = ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.LoadString s
        Assert.True( r.D4 = blkcnt_me.ofUInt32 0u; )
        Assert.True( r.D5 = blkcnt_me.ofUInt32 99u; )
        Assert.True( r.D6 = blkcnt_me.ofUInt32 ( uint32 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_BLKCNT32_006_data = [|
        [|
            ( { D1 = blkcnt_me.ofUInt32 2u; D2 = [ blkcnt_me.ofUInt32 0u; blkcnt_me.ofUInt32 1u; ]; D3 = None; D4 = blkcnt_me.ofUInt32 0u; D5 = blkcnt_me.ofUInt32 0u; D6 = blkcnt_me.ofUInt32 1u; D7 = blkcnt_me.ofUInt32 2u; D8 = blkcnt_me.ofUInt32 3u; } : ConfRW_002_BLKCNT32_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt32 3u; D2 = [ blkcnt_me.ofUInt32 0u; blkcnt_me.ofUInt32 1u; blkcnt_me.ofUInt32 2u; ]; D3 = None; D4 = blkcnt_me.ofUInt32 0u; D5 = blkcnt_me.ofUInt32 0u; D6 = blkcnt_me.ofUInt32 2u; D7 = blkcnt_me.ofUInt32 3u; D8 = blkcnt_me.ofUInt32 4u; } : ConfRW_002_BLKCNT32_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt32 2u; D2 = [ blkcnt_me.ofUInt32 0u; blkcnt_me.ofUInt32 1u; ]; D3 = Some( blkcnt_me.ofUInt32 5u ); D4 = blkcnt_me.ofUInt32 0u; D5 = blkcnt_me.ofUInt32 0u; D6 = blkcnt_me.ofUInt32 3u; D7 = blkcnt_me.ofUInt32 4u; D8 = blkcnt_me.ofUInt32 5u; } : ConfRW_002_BLKCNT32_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT32_006_data" )>]
    member _.SingleValue_BLKCNT32_006 ( s : ConfRW_002_BLKCNT32_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_BLKCNT32_007_data = [|
        [| ( { D1 = blkcnt_me.ofUInt32 2u; D2 = [ blkcnt_me.ofUInt32 0u; ]; D3 = None; D4 = blkcnt_me.ofUInt32 0u; D5 = blkcnt_me.ofUInt32 0u; D6 = blkcnt_me.ofUInt32 1u; D7 = blkcnt_me.ofUInt32 2u; D8 = blkcnt_me.ofUInt32 3u; } : ConfRW_002_BLKCNT32_T.T_Test ) :> obj |];
        [| ( { D1 = blkcnt_me.ofUInt32 2u; D2 = [ blkcnt_me.ofUInt32 0u; blkcnt_me.ofUInt32 1u; blkcnt_me.ofUInt32 2u; blkcnt_me.ofUInt32 3u; ]; D3 = None; D4 = blkcnt_me.ofUInt32 0u; D5 = blkcnt_me.ofUInt32 0u; D6 = blkcnt_me.ofUInt32 1u; D7 = blkcnt_me.ofUInt32 2u; D8 = blkcnt_me.ofUInt32 3u; } : ConfRW_002_BLKCNT32_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT32_007_data" )>]
    member _.SingleValue_BLKCNT32_007 ( s : ConfRW_002_BLKCNT32_T.T_Test ) =
        try
            ConfRW_002_BLKCNT32_T.ConfRW_UT002_BLKCNT32_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>-1</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>18446744073709551616</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D2>0</D2><D2>0</D2><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D3>0</D3><D3>0</D3></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D4>0</D4></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D5>0</D5></Test>" )>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3><D6>0</D6></Test>" )>]
    member _.SingleValue_BLKCNT64_001 ( s : string ) =
        try
            ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>2</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 2UL )>]
    [<InlineData( "<Test><D1>3</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 3UL )>]
    [<InlineData( "<Test><D1>4</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 4UL )>]
    [<InlineData( "<Test><D1>5</D1><D2>0</D2><D2>0</D2><D3>0</D3></Test>", 5UL )>]
    member _.SingleValue_BLKCNT64_002 ( s : string ) ( exr : uint64 ) =
        let r = ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.LoadString s
        Assert.True( r.D1 = blkcnt_me.ofUInt64 exr )

    static member m_SingleValue_BLKCNT64_003_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt64 0UL; blkcnt_me.ofUInt64 1UL; ] :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D2>2</D2><D3>0</D3></Test>" :> obj; [ blkcnt_me.ofUInt64 0UL; blkcnt_me.ofUInt64 1UL; blkcnt_me.ofUInt64 2UL; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT64_003_data" )>]
    member _.SingleValue_BLKCNT64_003 ( s : String ) ( exr : BLKCNT64_T list ) =
        let r = ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_BLKCNT64_004_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>1</D3></Test>" :> obj; Some( blkcnt_me.ofUInt64 1UL ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT64_004_data" )>]
    member _.SingleValue_BLKCNT64_004 ( s : String ) ( exr : BLKCNT64_T option ) =
        let r = ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_BLKCNT64_005_data = [|
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2></Test>" :> obj; blkcnt_me.ofUInt64 98UL :> obj; blkcnt_me.ofUInt64 ( uint64 Constants.MAX_TARGET_DEVICE_COUNT ) :> obj; |];
        [| "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>4</D7><D8>5</D8></Test>" :> obj; blkcnt_me.ofUInt64 4UL :> obj; blkcnt_me.ofUInt64 5UL :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT64_005_data" )>]
    member _.SingleValue_BLKCNT64_005 ( s : string ) ( exr_D7 : BLKCNT64_T ) ( exr_D8 : BLKCNT64_T ) =
        let r = ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.LoadString s
        Assert.True( r.D4 = blkcnt_me.ofUInt64 0UL; )
        Assert.True( r.D5 = blkcnt_me.ofUInt64 99UL; )
        Assert.True( r.D6 = blkcnt_me.ofUInt64 ( uint64 Constants.MAX_TARGET_DEVICE_COUNT ) )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_BLKCNT64_006_data = [|
        [|
            ( { D1 = blkcnt_me.ofUInt64 2UL; D2 = [ blkcnt_me.ofUInt64 0UL; blkcnt_me.ofUInt64 1UL; ]; D3 = None; D4 = blkcnt_me.ofUInt64 0UL; D5 = blkcnt_me.ofUInt64 0UL; D6 = blkcnt_me.ofUInt64 1UL; D7 = blkcnt_me.ofUInt64 2UL; D8 = blkcnt_me.ofUInt64 3UL; } : ConfRW_002_BLKCNT64_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D7>2</D7><D8>3</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt64 3UL; D2 = [ blkcnt_me.ofUInt64 0UL; blkcnt_me.ofUInt64 1UL; blkcnt_me.ofUInt64 2UL; ]; D3 = None; D4 = blkcnt_me.ofUInt64 0UL; D5 = blkcnt_me.ofUInt64 0UL; D6 = blkcnt_me.ofUInt64 2UL; D7 = blkcnt_me.ofUInt64 3UL; D8 = blkcnt_me.ofUInt64 4UL; } : ConfRW_002_BLKCNT64_T.T_Test ) :> obj;
            "<Test><D1>3</D1><D2>0</D2><D2>1</D2><D2>2</D2><D7>3</D7><D8>4</D8></Test>" :> obj
        |];
        [|
            ( { D1 = blkcnt_me.ofUInt64 2UL; D2 = [ blkcnt_me.ofUInt64 0UL; blkcnt_me.ofUInt64 1UL; ]; D3 = Some( blkcnt_me.ofUInt64 5UL ); D4 = blkcnt_me.ofUInt64 0UL; D5 = blkcnt_me.ofUInt64 0UL; D6 = blkcnt_me.ofUInt64 3UL; D7 = blkcnt_me.ofUInt64 4UL; D8 = blkcnt_me.ofUInt64 5UL; } : ConfRW_002_BLKCNT64_T.T_Test ) :> obj;
            "<Test><D1>2</D1><D2>0</D2><D2>1</D2><D3>5</D3><D7>4</D7><D8>5</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT64_006_data" )>]
    member _.SingleValue_BLKCNT64_006 ( s : ConfRW_002_BLKCNT64_T.T_Test ) ( exr : string ) =
        let r = ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_BLKCNT64_007_data = [|
        [| ( { D1 = blkcnt_me.ofUInt64 2UL; D2 = [ blkcnt_me.ofUInt64 0UL; ]; D3 = None; D4 = blkcnt_me.ofUInt64 0UL; D5 = blkcnt_me.ofUInt64 0UL; D6 = blkcnt_me.ofUInt64 1UL; D7 = blkcnt_me.ofUInt64 2UL; D8 = blkcnt_me.ofUInt64 3UL; } : ConfRW_002_BLKCNT64_T.T_Test ) :> obj |];
        [| ( { D1 = blkcnt_me.ofUInt64 2UL; D2 = [ blkcnt_me.ofUInt64 0UL; blkcnt_me.ofUInt64 1UL; blkcnt_me.ofUInt64 2UL; blkcnt_me.ofUInt64 3UL; ]; D3 = None; D4 = blkcnt_me.ofUInt64 0UL; D5 = blkcnt_me.ofUInt64 0UL; D6 = blkcnt_me.ofUInt64 1UL; D7 = blkcnt_me.ofUInt64 2UL; D8 = blkcnt_me.ofUInt64 3UL; } : ConfRW_002_BLKCNT64_T.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_BLKCNT64_007_data" )>]
    member _.SingleValue_BLKCNT64_007 ( s : ConfRW_002_BLKCNT64_T.T_Test ) =
        try
            ConfRW_002_BLKCNT64_T.ConfRW_UT002_BLKCNT64_T.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>aaa</D1><D2>512</D2><D2>512</D2><D3>512</D3></Test>" )>]
    [<InlineData( "<Test><D1>-1</D1><D2>512</D2><D2>512</D2><D3>512</D3></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D3>512</D3></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D2>512</D2><D2>512</D2><D3>512</D3></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D3>512</D3><D3>512</D3></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D3>512</D3><D3>512</D3><D3>512</D3></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D3>512</D3><D4>512</D4></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D3>512</D3><D5>512</D5></Test>" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D3>512</D3><D6>512</D6></Test>" )>]
    member _.SingleValue_Blocksize_001 ( s : string ) =
        try
            ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.LoadString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

    [<Theory>]
    [<InlineData( "<Test><D1>512</D1><D2>512</D2><D2>512</D2><D3>512</D3></Test>", "512" )>]
    [<InlineData( "<Test><D1>4096</D1><D2>512</D2><D2>512</D2><D3>512</D3></Test>", "4096" )>]
    member _.SingleValue_Blocksize_002 ( s : string ) ( exr : string ) =
        let r = ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.LoadString s
        Assert.True( r.D1 = Blocksize.fromStringValue exr )

    static member m_SingleValue_Blocksize_003_data = [|
        [| "<Test><D1>512</D1><D2>512</D2><D2>4096</D2><D3>512</D3></Test>" :> obj; [ Blocksize.BS_512; Blocksize.BS_4096; ] :> obj |];
        [| "<Test><D1>512</D1><D2>512</D2><D2>4096</D2><D2>512</D2><D3>512</D3></Test>" :> obj; [ Blocksize.BS_512; Blocksize.BS_4096; Blocksize.BS_512; ] :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Blocksize_003_data" )>]
    member _.SingleValue_Blocksize_003 ( s : String ) ( exr : Blocksize list ) =
        let r = ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.LoadString s
        Assert.True( r.D2 = exr )

    static member m_SingleValue_Blocksize_004_data = [|
        [| "<Test><D1>512</D1><D2>512</D2><D2>512</D2></Test>" :> obj; None :> obj |];
        [| "<Test><D1>512</D1><D2>512</D2><D2>512</D2><D3>4096</D3></Test>" :> obj; Some( Blocksize.BS_4096 ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Blocksize_004_data" )>]
    member _.SingleValue_Blocksize_004 ( s : String ) ( exr : Blocksize option ) =
        let r = ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.LoadString s
        Assert.True( r.D3 = exr )

    static member m_SingleValue_Blocksize_005_data = [|
        [| "<Test><D1>512</D1><D2>512</D2><D2>512</D2></Test>" :> obj; Blocksize.BS_4096 :> obj; Blocksize.BS_4096 :> obj; |];
        [| "<Test><D1>512</D1><D2>512</D2><D2>512</D2><D7>512</D7><D8>512</D8></Test>" :> obj; Blocksize.BS_512 :> obj; Blocksize.BS_512 :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Blocksize_005_data" )>]
    member _.SingleValue_Blocksize_005 ( s : string ) ( exr_D7 : Blocksize ) ( exr_D8 : Blocksize ) =
        let r = ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.LoadString s
        Assert.True( r.D4 = Blocksize.BS_512 )
        Assert.True( r.D5 = Blocksize.BS_512 )
        Assert.True( r.D6 = Blocksize.BS_512 )
        Assert.True( r.D7 = exr_D7 )
        Assert.True( r.D8 = exr_D8 )

    static member m_SingleValue_Blocksize_006_data = [|
        [|
            ( { D1 = Blocksize.BS_512; D2 = [ Blocksize.BS_512; Blocksize.BS_4096; ]; D3 = None; D4 = Blocksize.BS_512; D5 = Blocksize.BS_512; D6 = Blocksize.BS_4096; D7 = Blocksize.BS_512; D8 = Blocksize.BS_4096; } : ConfRW_002_Blocksize.T_Test ) :> obj;
            "<Test><D1>512</D1><D2>512</D2><D2>4096</D2><D7>512</D7><D8>4096</D8></Test>" :> obj
        |];
        [|
            ( { D1 = Blocksize.BS_4096; D2 = [ Blocksize.BS_512; Blocksize.BS_4096; Blocksize.BS_512; ]; D3 = None; D4 = Blocksize.BS_512; D5 = Blocksize.BS_512; D6 = Blocksize.BS_512; D7 = Blocksize.BS_4096; D8 = Blocksize.BS_512; } : ConfRW_002_Blocksize.T_Test ) :> obj;
            "<Test><D1>4096</D1><D2>512</D2><D2>4096</D2><D2>512</D2><D7>4096</D7><D8>512</D8></Test>" :> obj
        |];
        [|
            ( { D1 = Blocksize.BS_512; D2 = [ Blocksize.BS_512; Blocksize.BS_4096; ]; D3 = Some( Blocksize.BS_4096 ); D4 = Blocksize.BS_512; D5 = Blocksize.BS_512; D6 = Blocksize.BS_4096; D7 = Blocksize.BS_512; D8 = Blocksize.BS_4096; } : ConfRW_002_Blocksize.T_Test ) :> obj;
            "<Test><D1>512</D1><D2>512</D2><D2>4096</D2><D3>4096</D3><D7>512</D7><D8>4096</D8></Test>" :> obj
        |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Blocksize_006_data" )>]
    member _.SingleValue_Blocksize_006 ( s : ConfRW_002_Blocksize.T_Test ) ( exr : string ) =
        let r = ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.ToString s
        Assert.True(( r = exr ))

    static member m_SingleValue_Blocksize_007_data = [|
        [| ( { D1 = Blocksize.BS_512; D2 = [ Blocksize.BS_512; ]; D3 = None; D4 = Blocksize.BS_512; D5 = Blocksize.BS_512; D6 = Blocksize.BS_4096; D7 = Blocksize.BS_512; D8 = Blocksize.BS_4096; } : ConfRW_002_Blocksize.T_Test ) :> obj |];
        [| ( { D1 = Blocksize.BS_512; D2 = [ Blocksize.BS_512; Blocksize.BS_4096; Blocksize.BS_512; Blocksize.BS_4096; ]; D3 = None; D4 = Blocksize.BS_512; D5 = Blocksize.BS_512; D6 = Blocksize.BS_4096; D7 = Blocksize.BS_512; D8 = Blocksize.BS_4096; } : ConfRW_002_Blocksize.T_Test ) :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_SingleValue_Blocksize_007_data" )>]
    member _.SingleValue_Blocksize_007 ( s : ConfRW_002_Blocksize.T_Test ) =
        try
            ConfRW_002_Blocksize.ConfRW_UT002_Blocksize.ToString s |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _->
            ()

