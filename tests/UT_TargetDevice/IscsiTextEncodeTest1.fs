namespace Haruka.Test.UT.TargetDevice

open System.Text

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test


type IscsiTextEncode1_Test () =

    static member genNegStatVal v =
        {
            NegoStat_AuthMethod = v;
            NegoStat_CHAP_A = v;
            NegoStat_CHAP_I = v;
            NegoStat_CHAP_C = v;
            NegoStat_CHAP_N = v;
            NegoStat_CHAP_R = v;
            NegoStat_HeaderDigest = v;
            NegoStat_DataDigest = v;
            NegoStat_MaxConnections = v;
            NegoStat_SendTargets = v;
            NegoStat_TargetName = v;
            NegoStat_InitiatorName = v;
            NegoStat_TargetAlias = v;
            NegoStat_InitiatorAlias = v;
            NegoStat_TargetAddress = v;
            NegoStat_TargetPortalGroupTag = v;
            NegoStat_InitialR2T = v;
            NegoStat_ImmediateData = v;
            NegoStat_MaxRecvDataSegmentLength_I = v;
            NegoStat_MaxRecvDataSegmentLength_T = v;
            NegoStat_MaxBurstLength = v;
            NegoStat_FirstBurstLength = v;
            NegoStat_DefaultTime2Wait = v;
            NegoStat_DefaultTime2Retain = v;
            NegoStat_MaxOutstandingR2T = v;
            NegoStat_DataPDUInOrder = v;
            NegoStat_DataSequenceInOrder = v;
            NegoStat_ErrorRecoveryLevel = v;
            NegoStat_SessionType = v;
            NegoStat_UnknownKeys = v;
        }

    [<Fact>]
    member _.StandardLabelBytes2String_001() =
        Assert.True( IscsiTextEncode.StandardLabelBytes2String Array.empty = ValueNone )
        Assert.True( IscsiTextEncode.StandardLabelBytes2String [| 1uy .. 64uy |] = ValueNone )

    [<Theory>]
    [<InlineData( "0abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghij", false )>]
    [<InlineData( "Aabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghij", true )>]
    [<InlineData( "A0b1d2f3h4j5l6n7p8r9t0_.+-@ABCDEFGHIJKLMNOPQRSTUVWXYZa@@d..ghi.", true )>]
    [<InlineData( "A", true )>]
    [<InlineData( "X!", false )>]
    [<InlineData( "+abc", false )>]
    member _.StandardLabelBytes2String_002 ( s : string ) ( r : bool ) =
        let w = IscsiTextEncode.StandardLabelBytes2String( Encoding.UTF8.GetBytes s )
        if r then
            Assert.True(( w = ValueSome( s ) ))
        else
            Assert.True(( w = ValueNone ))

    [<Theory>]
    [<InlineData( "0123456789ABCDEFGHIJKLMNOPRSTUVWXYZabcdefghijklmnopqrstuvwxyz.-+@/_][:", true )>]
    [<InlineData( "", true )>]
    [<InlineData( "!#$%&'()", false )>]
    [<InlineData( "\"", false )>]
    [<InlineData( "aabbcddeee\`uuuuu", false )>]
    member _.TextValueBytes2String_001 ( s : string ) ( r : bool ) =
        let w = IscsiTextEncode.TextValueBytes2String( Encoding.UTF8.GetBytes s )
        if r then
            Assert.True(( w = ValueSome( s ) ))
        else
            Assert.True(( w = ValueNone ))

    [<Theory>]
    [<InlineData( "0:123456789-.:abcdefghijklmnopqrstuvwxyz--..::--..::", true )>]
    [<InlineData( ":", true )>]
    [<InlineData( "", false )>]
    [<InlineData( "\"", false )>]
    [<InlineData( "+[]\`uu=~\nuuu", false )>]
    member _.ISCSINameValueBytes2String_001 ( s : string ) ( r : bool ) =
        let w = IscsiTextEncode.ISCSINameValueBytes2String( Encoding.UTF8.GetBytes s )
        if r then
            Assert.True(( w = ValueSome( s ) ))
        else
            Assert.True(( w = ValueNone ))

    [<Theory>]
    [<InlineData( "Yes", true, true )>]
    [<InlineData( "No", true, false )>]
    [<InlineData( "Noo", false, true )>]
    [<InlineData( "YES", false, true )>]
    [<InlineData( "no", false, true )>]
    [<InlineData( "", false, true )>]
    member _.BooleanValueBytes2Bool_001 ( s : string ) ( r1 : bool ) ( r2 : bool )  =
        let exr = if r1 then ValueSome( r2 ) else ValueNone
        Assert.True( IscsiTextEncode.BooleanValueBytes2Bool <| Encoding.UTF8.GetBytes s = exr )

    [<Theory>]
    [<InlineData( true, "Yes" )>]
    [<InlineData( false, "No" )>]
    member _.Bool2BooleanValueBytes_001 ( inv : bool ) ( r : string ) =
        Assert.True( IscsiTextEncode.Bool2BooleanValueBytes inv |> Encoding.UTF8.GetString = r )

    [<Theory>]
    [<InlineData( "0x0000", 0us, 65535us, true, 0us )>]
    [<InlineData( "0x0001", 0us, 65535us, true, 1us )>]
    [<InlineData( "0xFFFF", 0us, 65535us, true, 65535us )>]
    [<InlineData( "0X1234", 0us, 65535us, true, 0x1234us )>]
    [<InlineData( "0", 0us, 65535us, true, 0us )>]
    [<InlineData( "1", 0us, 65535us, true, 1us )>]
    [<InlineData( "12345", 0us, 65535us, true, 12345us )>]
    [<InlineData( "65535", 0us, 65535us, true, 65535us )>]
    [<InlineData( "-1234", 0us, 65535us, false, 0us )>]
    [<InlineData( "65536", 0us, 65535us, false, 0us )>]
    [<InlineData( "", 0us, 65535us, false, 0us )>]
    [<InlineData( "6s536", 0us, 65535us, false, 0us )>]
    [<InlineData( "abcde", 0us, 65535us, false, 0us )>]
    [<InlineData( "0x", 0us, 65535us, false, 0us )>]
    [<InlineData( "0X", 0us, 65535us, false, 0us )>]
    [<InlineData( "1X", 0us, 65535us, false, 0us )>]
    [<InlineData( "9", 10us, 100us, false, 0us )>]
    [<InlineData( "10", 10us, 100us, true, 10us )>]
    [<InlineData( "50", 10us, 100us, true, 50us )>]
    [<InlineData( "100", 10us, 100us, true, 100us )>]
    [<InlineData( "101", 10us, 100us, false, 0us )>]
    [<InlineData( "0x0F", 0x10us, 0x100us, false, 0us )>]
    [<InlineData( "0x10", 0x10us, 0x100us, true, 0x10us )>]
    [<InlineData( "0x50", 0x10us, 0x100us, true, 0x50us )>]
    [<InlineData( "0x100", 0x10us, 0x100us, true, 0x100us )>]
    [<InlineData( "0x101", 0x10us, 0x100us, false, 0us )>]
    member _.NumericalValueBytes2uint16_001 ( s : string ) ( m1 : uint16 ) ( m2 : uint16 ) ( r1 : bool ) ( v : uint16 ) =
        let exv = if r1 then ValueSome( v ) else ValueNone
        Assert.True( IscsiTextEncode.NumericalValueBytes2uint16 ( Encoding.UTF8.GetBytes s ) m1 m2 = exv )

    [<Theory>]
    [<InlineData( "0x00000000", 0u, 0xFFFFFFFFu, true, 0u )>]
    [<InlineData( "0x00000001", 0u, 0xFFFFFFFFu, true, 1u )>]
    [<InlineData( "0xFFFFFFFF", 0u, 0xFFFFFFFFu, true, 0xFFFFFFFFu )>]
    [<InlineData( "0X1234", 0u, 0xFFFFFFFFu, true, 0x1234u )>]
    [<InlineData( "0", 0u, 0xFFFFFFFFu, true, 0u )>]
    [<InlineData( "1", 0u, 0xFFFFFFFFu, true, 1u  )>]
    [<InlineData( "12345", 0u, 0xFFFFFFFFu, true, 12345u )>]
    [<InlineData( "4294967295", 0u, 0xFFFFFFFFu, true, 4294967295u )>]
    [<InlineData( "-1234", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "4294967296", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "6s536", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "abcde", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "0x", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "0X", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "1X", 0u, 0xFFFFFFFFu, false, 0u )>]
    [<InlineData( "9", 10u, 100u, false, 0u )>]
    [<InlineData( "10", 10u, 100u, true, 10u )>]
    [<InlineData( "50", 10u, 100u, true, 50u )>]
    [<InlineData( "100", 10u, 100u, true, 100u )>]
    [<InlineData( "101", 10u, 100u, false, 0u )>]
    [<InlineData( "0x0F", 0x10u, 0x100u, false, 0u )>]
    [<InlineData( "0x10", 0x10u, 0x100u, true, 0x10u )>]
    [<InlineData( "0x50", 0x10u, 0x100u, true, 0x50u )>]
    [<InlineData( "0x100", 0x10u, 0x100u, true, 0x100u )>]
    [<InlineData( "0x101", 0x10u, 0x100u, false, 0u )>]
    member _.NumericalValueBytes2uint32_001 ( s : string ) ( m1 : uint32 ) ( m2 : uint32 ) ( r1 : bool ) ( v : uint32 ) =
        let exv = if r1 then ValueSome( v ) else ValueNone
        Assert.True( IscsiTextEncode.NumericalValueBytes2uint32 ( Encoding.UTF8.GetBytes s ) m1 m2 = exv )

    [<Theory>]
    [<InlineData( "0x0000000000000000", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 0UL )>]
    [<InlineData( "0x0000000000000001", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 1UL )>]
    [<InlineData( "0xFFFFFFFFFFFFFFFF", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 0xFFFFFFFFFFFFFFFFUL )>]
    [<InlineData( "0X1234", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 0x1234UL )>]
    [<InlineData( "0", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 0UL )>]
    [<InlineData( "1", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 1UL )>]
    [<InlineData( "12345", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 12345UL )>]
    [<InlineData( "18446744073709551615", 0UL, 0xFFFFFFFFFFFFFFFFUL, true, 18446744073709551615UL )>]
    [<InlineData( "-1234", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "18446744073709551616", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "6s536", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "abcde", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "0x", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "0X", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "1X", 0UL, 0xFFFFFFFFFFFFFFFFUL, false, 0 )>]
    [<InlineData( "9", 10UL, 100UL, false, 0 )>]
    [<InlineData( "10", 10UL, 100UL, true, 10UL )>]
    [<InlineData( "50", 10UL, 100UL, true, 50UL )>]
    [<InlineData( "100", 10UL, 100UL, true, 100UL )>]
    [<InlineData( "101", 10UL, 100UL, false, 0 )>]
    [<InlineData( "0x0F", 0x10UL, 0x100UL, false, 0 )>]
    [<InlineData( "0x10", 0x10UL, 0x100UL, true, 0x10UL )>]
    [<InlineData( "0x50", 0x10UL, 0x100UL, true, 0x50UL )>]
    [<InlineData( "0x100", 0x10UL, 0x100UL, true, 0x100UL )>]
    [<InlineData( "0x101", 0x10UL, 0x100UL, false, 0 )>]
    member _.NumericalValueBytes2uint64_001( s : string ) ( m1 : uint64 ) ( m2 : uint64 ) ( r1 : bool ) ( v : uint64 ) =
        let exv = if r1 then ValueSome( v ) else ValueNone
        Assert.True( IscsiTextEncode.NumericalValueBytes2uint64 ( Encoding.UTF8.GetBytes s ) m1 m2 = exv )

    [<Theory>]
    [<InlineData( "0bQUJDREVGRw==", true, "ABCDEFG" )>]
    [<InlineData( "0a", false, "" )>]
    [<InlineData( "", false, "" )>]
    [<InlineData( "0", false, "" )>]
    [<InlineData( "1B", false, "" )>]
    [<InlineData( "0B", false, "" )>]
    [<InlineData( "0BYQ==", true, "a" )>]
    [<InlineData( "0bYWI=", true, "ab"  )>]
    [<InlineData( "0bYWJj", true, "abc" )>]
    [<InlineData( "0BYWJjZA==", true, "abcd" )>]
    [<InlineData( "0bYWJjZGU=", true, "abcde" )>]
    [<InlineData( "0BYWJjZGVm", true, "abcdef" )>]
    [<InlineData( "0BYWJjZGV", false, "" )>]
    [<InlineData( "0BYWJjZG", false, "" )>]
    [<InlineData( "0BYWJjZ", false, "" )>]
    member _.Base64ConstantBytes2Binary_001( s : string ) ( r1 : bool ) ( v : string ) =
        let exv = if r1 then ValueSome( Encoding.UTF8.GetBytes v ) else ValueNone
        Assert.True( IscsiTextEncode.Base64ConstantBytes2Binary ( Encoding.UTF8.GetBytes s ) = exv )

    [<Theory>]
    [<InlineData( "ABCDEFG", "0bQUJDREVGRw==" )>]
    [<InlineData( "", "" )>]
    [<InlineData( "a", "0bYQ==" )>]
    [<InlineData( "ab", "0bYWI=" )>]
    [<InlineData( "abc", "0bYWJj" )>]
    [<InlineData( "abcd", "0bYWJjZA==" )>]
    [<InlineData( "abcde", "0bYWJjZGU=" )>]
    [<InlineData( "abcdef", "0bYWJjZGVm" )>]
    member _.Binary2Base64ConstantBytes_001( s : string ) ( v : string ) =
        Assert.True( ( IscsiTextEncode.Binary2Base64ConstantBytes <| Encoding.UTF8.GetBytes s ) = ( Encoding.UTF8.GetBytes v ) )

    static member HexConstantBytes2Binary_001_data = [|
        [| "0x00010203040506" :> obj; ValueSome( [| 0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy |] ) :> obj; |];
        [| "0XFFFEFDFCFBFAF9" :> obj; ValueSome( [| 0xFFuy; 0xFEuy; 0xFDuy; 0xFCuy; 0xFBuy; 0xFAuy; 0xF9uy |] ) :> obj; |];
        [| "" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "0" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "1x" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "0X" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "0xabcdefABCDEF" :> obj; ValueSome( [| 0xabuy; 0xcduy; 0xefuy; 0xABuy; 0xCDuy; 0xEFuy; |] ) :> obj; |];
        [| "0XabcdefABCDE" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "0x0123456789" :> obj; ValueSome( [| 0x01uy; 0x23uy; 0x45uy; 0x67uy; 0x89uy; |] ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "HexConstantBytes2Binary_001_data" )>]
    member _.HexConstantBytes2Binary_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let r1 = o2 :?> byte [] voption
        Assert.True( IscsiTextEncode.HexConstantBytes2Binary <| Encoding.UTF8.GetBytes v1 = r1 )

    static member Binary2HexConstantBytes_001_data = [|
        [| [| 0x00uy; 0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy; 0x06uy |] :> obj; "0x00010203040506" :> obj |];
        [| [| 0xFEuy; 0xEDuy; 0xDCuy; 0xCBuy; 0xBAuy; 0xA9uy; 0x98uy |] :> obj; "0xFEEDDCCBBAA998" :> obj |];
        [| ( Array.empty : byte[] ) :> obj; "" :> obj |];
    |]

    [<Theory>]
    [<MemberData( "Binary2HexConstantBytes_001_data" )>]
    member _.Binary2HexConstantBytes_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> byte[]
        let r1 = o2 :?> string
        Assert.True( IscsiTextEncode.Binary2HexConstantBytes <| v1 = Encoding.UTF8.GetBytes r1 )

    static member DecimalConstantBytes2Binary_001_data = [|
        [| "0" :> obj; ValueSome( [| 0x00uy; |] ) :> obj; |];
        [| "1" :> obj; ValueSome( [| 0x01uy; |] ) :> obj; |];
        [| "255" :> obj; ValueSome( [| 0xFFuy; |] ) :> obj; |];
        [| "65535" :> obj; ValueSome( [| 0xFFuy; 0xFFuy |] ) :> obj; |];
        [| "244837814107886" :> obj; ValueSome( [| 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0xFEuy;  0xEEuy |] ) :> obj; |];
        [| "" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "a" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "244837814ggg107886" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "18446744073709551616" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "DecimalConstantBytes2Binary_001_data" )>]
    member _.DecimalConstantBytes2Binary_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let r1 = o2 :?> byte [] voption
        Assert.True( ( IscsiTextEncode.DecimalConstantBytes2Binary <| Encoding.UTF8.GetBytes v1 = r1 ) )

    static member BinaryValueBytes2Binary_001_data = [|
        [| "0bQUJDREVGRw==" :> obj; ValueSome( Encoding.UTF8.GetBytes "ABCDEFG" ) :> obj; |];
        [| "0BYWJjZA==" :> obj; ValueSome( Encoding.UTF8.GetBytes "abcd" ) :> obj; |];
        [| "0x00010203040506" :> obj; ValueSome( [| 0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy |] ) :> obj; |];
        [| "0XFFFEFDFCFBFAF9" :> obj; ValueSome( [| 0xFFuy; 0xFEuy; 0xFDuy; 0xFCuy; 0xFBuy; 0xFAuy; 0xF9uy |] ) :> obj; |];
        [| "244837814107886" :> obj; ValueSome( [| 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0xFEuy;  0xEEuy |] ) :> obj; |];
        [| "" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "a" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "1x" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "BinaryValueBytes2Binary_001_data" )>]
    member _.BinaryValueBytes2Binary_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let r1 = o2 :?> byte [] voption
        Assert.True( IscsiTextEncode.BinaryValueBytes2Binary <| Encoding.UTF8.GetBytes v1 = r1 )

    static member LargeBinaryValueBytes2Binary_001_data = [|
        [| "0bQUJDREVGRw==" :> obj; ValueSome( Encoding.UTF8.GetBytes "ABCDEFG" ) :> obj; |];
        [| "0BYWJjZA==" :> obj; ValueSome( Encoding.UTF8.GetBytes "abcd" ) :> obj; |];
        [| "0x00010203040506" :> obj; ValueSome( [| 0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy |] ) :> obj; |];
        [| "0XFFFEFDFCFBFAF9" :> obj; ValueSome( [| 0xFFuy; 0xFEuy; 0xFDuy; 0xFCuy; 0xFBuy; 0xFAuy; 0xF9uy |] ) :> obj; |];
        [| "244837814107886" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "0" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "a" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
        [| "1x" :> obj; ( ValueNone : byte [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "LargeBinaryValueBytes2Binary_001_data" )>]
    member _.LargeBinaryValueBytes2Binary_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let r1 = o2 :?> byte [] voption
        Assert.True( IscsiTextEncode.LargeBinaryValueBytes2Binary <| Encoding.UTF8.GetBytes v1 = r1 )

    [<Theory>]
    [<InlineData( "0~0", 0us, 65535us, true, 0us, 0us )>]
    [<InlineData( "0~65535", 0us, 65535us, true, 0us, 65535us )>]
    [<InlineData( "128~256", 0us, 65535us, true, 128us, 256us )>]
    [<InlineData( "65535~65535", 0us, 65535us, true, 65535us, 65535us )>]
    [<InlineData( "65535~65533", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "65535~65536", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "abc~65535", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "-5~65535", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "~65535", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "128~65g35", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "128~-8", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "-10~-8", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "128-129", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "~129", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "1234~", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "1234", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "0", 0us, 65535us, false, 0us, 0us )>]
    [<InlineData( "128~256", 128us, 256us, true, 128us, 256us )>]
    [<InlineData( "127~256", 128us, 256us, false, 0us, 0us )>]
    [<InlineData( "128~257", 128us, 256us, false, 0us, 0us )>]
    member _.NumericRangeBytes2uint16Pair_001 ( s : string ) ( m1 : uint16 ) ( m2 : uint16 ) ( r : bool ) ( v1 : uint16 ) ( v2 : uint16 ) =
        let exr = if r then ValueSome( struct( v1, v2 ) ) else ValueNone
        Assert.True( IscsiTextEncode.NumericRangeBytes2uint16Pair ( Encoding.UTF8.GetBytes s ) m1 m2 = exr )

    [<Theory>]
    [<InlineData( "0~0", 0u, 4294967295u, true, 0u, 0u )>]
    [<InlineData( "0~4294967295", 0u, 4294967295u, true, 0u, 4294967295u )>]
    [<InlineData( "128~256", 0u, 4294967295u, true, 128u, 256u )>]
    [<InlineData( "4294967295~4294967295", 0u, 4294967295u, true, 4294967295u, 4294967295u )>]
    [<InlineData( "4294967295~4294967294", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "4294967295~4294967296", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "abc~4294967295", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "-5~4294967295", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "~4294967295", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "128~4294g67295", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "128~-8", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "-10~-8", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "128-129", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "~129", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "1234~", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "1234", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "0", 0u, 4294967295u, false, 0us, 0us )>]
    [<InlineData( "128~256", 128u, 256u, true, 128u, 256u )>]
    [<InlineData( "127~256", 128u, 256u, false, 0us, 0us )>]
    [<InlineData( "128~257", 128u, 256u, false, 0us, 0us )>]
    member _.NumericRangeBytes2uint32Pair_001( s : string ) ( m1 : uint32 ) ( m2 : uint32 ) ( r : bool ) ( v1 : uint32 ) ( v2 : uint32 ) =
        let exr = if r then ValueSome( struct( v1, v2 ) ) else ValueNone
        Assert.True( IscsiTextEncode.NumericRangeBytes2uint32Pair ( Encoding.UTF8.GetBytes s ) m1 m2 = exr )

    [<Theory>]
    [<InlineData( "0~0", 0UL, 18446744073709551615UL, true, 0UL, 0UL )>]
    [<InlineData( "0~18446744073709551615", 0UL, 18446744073709551615UL, true, 0UL, 18446744073709551615UL )>]
    [<InlineData( "128~256", 0UL, 18446744073709551615UL, true, 128UL, 256UL )>]
    [<InlineData( "18446744073709551615~18446744073709551615", 0UL, 18446744073709551615UL, true, 18446744073709551615UL, 18446744073709551615UL )>]
    [<InlineData( "18446744073709551615~18446744073709551614", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "18446744073709551615~18446744073709551616", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "abc~18446744073709551615", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "-5~18446744073709551615", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "~18446744073709551615", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "128~18446744o7370955i615", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "128~-8", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "-10~-8", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "128-129", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "~129", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "1234~", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "1234", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "0", 0UL, 18446744073709551615UL, false, 0UL, 0UL )>]
    [<InlineData( "128~256", 128UL, 256UL, true, 128UL, 256UL )>]
    [<InlineData( "127~256", 128UL, 256UL, false, 0UL, 0UL )>]
    [<InlineData( "128~257", 128UL, 256UL, false, 0UL, 0UL )>]
    member _.NumericRangeBytes2uint64Pair_001( s : string ) ( m1 : uint64 ) ( m2 : uint64 ) ( r : bool ) ( v1 : uint64 ) ( v2 : uint64 ) =
        let exr = if r then ValueSome( struct( v1, v2 ) ) else ValueNone
        Assert.True( IscsiTextEncode.NumericRangeBytes2uint64Pair ( Encoding.UTF8.GetBytes s ) m1 m2 = exr )

    static member ListOfValuesBytes2Strings_001_data = [|
        [| "abcdefghijklmnopqrstuvwxyz,ABCDEFGHIJKLMNOPQRSTUVWXYZ,0123456789,[-:+]@_/" :> obj; ValueSome( [| "abcdefghijklmnopqrstuvwxyz"; "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; "0123456789"; "[-:+]@_/"; |] ) :> obj; |];
        [| "abcdefghijklmnopqrstuvwxyz,,AAAA" :> obj; ValueSome( [| "abcdefghijklmnopqrstuvwxyz"; ""; "AAAA"; |] ) :> obj; |];
        [| ",BBBB" :> obj; ValueSome( [| ""; "BBBB"; |] ) :> obj; |];
        [| "," :> obj; ValueSome( [| ""; ""; |] ) :> obj; |];
        [| "" :> obj; ( ValueNone : string [] voption ) :> obj; |];
        [| "-+[],gggg" :> obj; ValueSome( [| "-+[]"; "gggg"; |] ) :> obj; |];
        [| "()=,AAAAAA" :> obj; ( ValueNone : string [] voption ) :> obj; |];
        [| "BBBB,AAA&&''AA," :> obj; ( ValueNone : string [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "ListOfValuesBytes2Strings_001_data" )>]
    member _.ListOfValuesBytes2Strings_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let v2 = o2 :?> string [] voption
        Assert.True( IscsiTextEncode.ListOfValuesBytes2Strings <| Encoding.UTF8.GetBytes v1 = v2 )

    static member Strings2ListOfValuesBytes_001_data = [|
        [| [| "abcdefghijklmnopqrstuvwxyz"; "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; "0123456789"; "[-:+]@_/"; |] :> obj; "abcdefghijklmnopqrstuvwxyz,ABCDEFGHIJKLMNOPQRSTUVWXYZ,0123456789,[-:+]@_/" :> obj; |];
        [| [| ""; "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; ""; |] :> obj; ",ABCDEFGHIJKLMNOPQRSTUVWXYZ," :> obj; |];
        [| [| "a"; ""; "B"; "C"; "D"; "E"; |] :> obj; "a,,B,C,D,E" :> obj; |];
        [| [| ""; |] :> obj; "" :> obj; |];
        [| ( Array.empty : string[] ) :> obj; "" :> obj; |];
        [| [| ""; ""; |] :> obj; "," :> obj; |];
        [| [| "AAAA"; |] :> obj; "AAAA" :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "Strings2ListOfValuesBytes_001_data" )>]
    member _.Strings2ListOfValuesBytes_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string []
        let v2 = o2 :?> string
        Assert.True( ( IscsiTextEncode.Strings2ListOfValuesBytes <| v1 ) = Encoding.UTF8.GetBytes v2 )

    static member ListOfValuesBytes2uint16_001_data = [|
        [| "0,1,2,3,4" :> obj; ValueSome( [| 0us; 1us; 2us; 3us; 4us; |] ) :> obj; |];
        [| "0,1,2,3," :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "0,1,2,3" :> obj; ValueSome( [| 0us; 1us; 2us; 3us; |] ) :> obj; |];
        [| "0,1,2," :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "0,1,2" :> obj; ValueSome( [| 0us; 1us; 2us; |] ) :> obj; |];
        [| "0,1," :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "0,1" :> obj; ValueSome( [| 0us; 1us; |] ) :> obj; |];
        [| "0," :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "0" :> obj; ValueSome( [| 0us; |] ) :> obj; |];
        [| ",10" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "abc,10" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "65535,65534,65533" :> obj; ValueSome( [| 65535us; 65534us; 65533us; |] ) :> obj; |];
        [| "65536,65534,65533" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "-1,10" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "7,-8,9" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
        [| "0xFF,8,9" :> obj; ( ValueNone : uint16 [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "ListOfValuesBytes2uint16_001_data" )>]
    member _.ListOfValuesBytes2uint16_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let v2 = o2 :?> uint16 [] voption
        Assert.True( IscsiTextEncode.ListOfValuesBytes2uint16 <| Encoding.UTF8.GetBytes v1 = v2 )

    static member uint16ToListOfValuesBytes_001_data = [|
        [| [| 0us; 1us; 2us; 3us; 4us; |] :> obj; "0,1,2,3,4" :> obj; |];
        [| [| 0us; 1us; 2us; 3us; |] :> obj; "0,1,2,3" :> obj; |];
        [| [| 0us; 1us; 2us; |] :> obj; "0,1,2" :> obj; |];
        [| [| 0us; 1us; |] :> obj; "0,1" :> obj; |];
        [| [| 0us; |] :> obj; "0" :> obj; |];
        [| ( [| |] : uint16[] ) :> obj; "" :> obj; |];
        [| [| 65535us; 65534us; 65533us; |] :> obj; "65535,65534,65533" :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "uint16ToListOfValuesBytes_001_data" )>]
    member _.uint16ToListOfValuesBytes_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> uint16 []
        let v2 = o2 :?> string
        Assert.True( ( IscsiTextEncode.uint16ToListOfValuesBytes <| v1 ) = Encoding.UTF8.GetBytes v2 )

    static member ListOfValuesBytes2uint32_001_data = [|
        [|  "0,1,2,3,4" :> obj; ValueSome( [| 0u; 1u; 2u; 3u; 4u; |] ) :> obj; |];
        [|  "0,1,2,3," :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [|  "0,1,2,3" :> obj; ValueSome( [| 0u; 1u; 2u; 3u; |] ) :> obj; |];
        [|  "0,1,2," :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [|  "0,1,2" :> obj; ValueSome( [| 0u; 1u; 2u; |] ) :> obj; |];
        [|  "0,1," :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "0,1" :> obj; ValueSome( [| 0u; 1u; |] ) :> obj; |];
        [| "0," :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "0" :> obj; ValueSome( [| 0u; |] ) :> obj; |];
        [| ",10" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "abc,10" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "4294967295,4294967294,4294967293" :> obj; ValueSome( [| 4294967295u; 4294967294u; 4294967293u; |] ) :> obj; |];
        [| "4294967296,4294967295,4294967294" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "-1,10" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "7,-8,9" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
        [| "0xFF,8,9" :> obj; ( ValueNone : uint [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "ListOfValuesBytes2uint32_001_data" )>]
    member _.ListOfValuesBytes2uint32_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let v2 = o2 :?> uint [] voption
        Assert.True( IscsiTextEncode.ListOfValuesBytes2uint32 <| Encoding.UTF8.GetBytes v1 = v2 )

    static member uint32ToListOfValuesBytes_001_data = [|
        [| [| 0u; 1u; 2u; 3u; 4u; |] :> obj; "0,1,2,3,4" :> obj; |];
        [| [| 0u; 1u; 2u; 3u; |] :> obj; "0,1,2,3" :> obj; |];
        [| [| 0u; 1u; 2u; |] :> obj; "0,1,2" :> obj; |];
        [| [| 0u; 1u; |] :> obj; "0,1" :> obj; |];
        [| [| 0u; |] :> obj; "0" :> obj; |];
        [| ( Array.empty : uint[] ) :> obj; "" :> obj; |];
        [| [| 4294967295u; 4294967294u; 4294967293u; |] :> obj; "4294967295,4294967294,4294967293" :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "uint32ToListOfValuesBytes_001_data" )>]
    member _.uint32ToListOfValuesBytes_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> uint[]
        let v2 = o2 :?> string
        Assert.True( ( IscsiTextEncode.uint32ToListOfValuesBytes <| v1 ) = Encoding.UTF8.GetBytes v2 )

    static member ListOfValuesBytes2uint64_001_data = [|
        [| "0,1,2,3,4" :> obj; ValueSome( [| 0UL; 1UL; 2UL; 3UL; 4UL; |] ) :> obj; |];
        [| "0,1,2,3," :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "0,1,2,3" :> obj; ValueSome( [| 0UL; 1UL; 2UL; 3UL; |] ) :> obj; |];
        [| "0,1,2," :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "0,1,2" :> obj; ValueSome( [| 0UL; 1UL; 2UL; |] ) :> obj; |];
        [| "0,1," :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "0,1" :> obj; ValueSome( [| 0UL; 1UL; |] ) :> obj; |];
        [| "0," :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "0" :> obj; ValueSome( [| 0UL; |] ) :> obj; |];
        [| ",10" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "abc,10" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "18446744073709551615,18446744073709551614,18446744073709551613" :> obj; ValueSome( [| 18446744073709551615UL; 18446744073709551614UL; 18446744073709551613UL; |] ) :> obj; |];
        [| "18446744073709551616,18446744073709551615,18446744073709551614" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "-1,10" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "7,-8,9" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
        [| "0xFF,8,9" :> obj; ( ValueNone : uint64 [] voption ) :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "ListOfValuesBytes2uint64_001_data" )>]
    member _.ListOfValuesBytes2uint64_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> string
        let v2 = o2 :?> uint64 [] voption
        Assert.True( IscsiTextEncode.ListOfValuesBytes2uint64 <| Encoding.UTF8.GetBytes v1 = v2 )

    static member uint64ToListOfValuesBytes_001_data = [|
        [| [| 0UL; 1UL; 2UL; 3UL; 4UL; |] :> obj; "0,1,2,3,4" :> obj; |];
        [| [| 0UL; 1UL; 2UL; 3UL; |] :> obj; "0,1,2,3" :> obj; |];
        [| [| 0UL; 1UL; 2UL; |] :> obj; "0,1,2" :> obj; |];
        [| [| 0UL; 1UL; |] :> obj; "0,1" :> obj; |];
        [| [| 0UL; |] :> obj; "0" :> obj; |];
        [| ( Array.empty : uint64[] ) :> obj; "" :> obj; |];
        [| [| 18446744073709551615UL; 18446744073709551614UL; 18446744073709551613UL; |] :> obj; "18446744073709551615,18446744073709551614,18446744073709551613" :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "uint64ToListOfValuesBytes_001_data" )>]
    member _.uint64ToListOfValuesBytes_001 ( o1 : obj ) ( o2 : obj ) =
        let v1 = o1 :?> uint64 [] 
        let v2 = o2 :?> string
        Assert.True( ( IscsiTextEncode.uint64ToListOfValuesBytes <| v1 ) = Encoding.UTF8.GetBytes v2 )

    [<Fact>]
    member _.TextKeyData2KeyValues_001() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=val1"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key2=val2"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key3=val3"
                        yield 0uy;
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "Key4=val4"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key5=val5"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key6=val6"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1" ) );
                        ( "Key2", TextValueType.Value( Encoding.UTF8.GetBytes "val2" ) );
                        ( "Key3", TextValueType.Value( Encoding.UTF8.GetBytes "val3" ) );
                        ( "Key4", TextValueType.Value( Encoding.UTF8.GetBytes "val4" ) );
                        ( "Key5", TextValueType.Value( Encoding.UTF8.GetBytes "val5" ) );
                        ( "Key6", TextValueType.Value( Encoding.UTF8.GetBytes "val6" ) );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_002() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=val1"
                        yield 0uy
                        yield! Encoding.UTF8.GetBytes "Key2=val2"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key3=val3"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key"
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "4=val4"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key5=val5"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key6=val6"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1" ) );
                        ( "Key2", TextValueType.Value( Encoding.UTF8.GetBytes "val2" ) );
                        ( "Key3", TextValueType.Value( Encoding.UTF8.GetBytes "val3" ) );
                        ( "Key4", TextValueType.Value( Encoding.UTF8.GetBytes "val4" ) );
                        ( "Key5", TextValueType.Value( Encoding.UTF8.GetBytes "val5" ) );
                        ( "Key6", TextValueType.Value( Encoding.UTF8.GetBytes "val6" ) );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_003() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=val1"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key2=val2"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key3=val3"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key4=va"
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "l4"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key5=val5"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key6=val6"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1" ) );
                        ( "Key2", TextValueType.Value( Encoding.UTF8.GetBytes "val2" ) );
                        ( "Key3", TextValueType.Value( Encoding.UTF8.GetBytes "val3" ) );
                        ( "Key4", TextValueType.Value( Encoding.UTF8.GetBytes "val4" ) );
                        ( "Key5", TextValueType.Value( Encoding.UTF8.GetBytes "val5" ) );
                        ( "Key6", TextValueType.Value( Encoding.UTF8.GetBytes "val6" ) );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_004() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1="
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "val1"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key2"
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "=val2"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key3=val3"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key4=val4"
                    |];
                    [|
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key5=val5"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key6=val6"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1" ) );
                        ( "Key2", TextValueType.Value( Encoding.UTF8.GetBytes "val2" ) );
                        ( "Key3", TextValueType.Value( Encoding.UTF8.GetBytes "val3" ) );
                        ( "Key4", TextValueType.Value( Encoding.UTF8.GetBytes "val4" ) );
                        ( "Key5", TextValueType.Value( Encoding.UTF8.GetBytes "val5" ) );
                        ( "Key6", TextValueType.Value( Encoding.UTF8.GetBytes "val6" ) );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_005() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=val1val1val1"
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "val1val1val1"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1val1val1val1val1val1" ) );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_006() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=val1val1val1val1val1val1"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1val1val1val1val1val1" ) );
                    |]
                )
        )
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=val1val1val1val1val1val1"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType.Value( Encoding.UTF8.GetBytes "val1val1val1val1val1val1" ) );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_007() =
        Assert.True( IscsiTextEncode.TextKeyData2KeyValues [| Array.empty; |] = ValueSome( Array.empty ) )

    [<Fact>]
    member _.TextKeyData2KeyValues_008() =
        Assert.True( IscsiTextEncode.TextKeyData2KeyValues [| [| 0uy; 0uy |]; |] = ValueNone )

    [<Fact>]
    member _.TextKeyData2KeyValues_009() =
        Assert.True( IscsiTextEncode.TextKeyData2KeyValues [| [| 0uy; |]; [| 0uy |]; Array.empty |] = ValueNone )

    [<Fact>]
    member _.TextKeyData2KeyValues_010() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Key1=NotUnderstood"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key2=Irrelevant"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key3=Reject"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key4=Re"
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "ject"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key5=Irrelevant"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Key6=NotUnderstood"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Key1", TextValueType<byte[]>.ISV_NotUnderstood );
                        ( "Key2", TextValueType<byte[]>.ISV_Irrelevant );
                        ( "Key3", TextValueType<byte[]>.ISV_Reject );
                        ( "Key4", TextValueType<byte[]>.ISV_Reject );
                        ( "Key5", TextValueType<byte[]>.ISV_Irrelevant );
                        ( "Key6", TextValueType<byte[]>.ISV_NotUnderstood );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_011() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "&&&&&&(())=NotUnderstood"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "%%'''&&&00am$#!\"=Irrelevant"
                        yield 0uy;
                    |];
                |] =
                ValueNone
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_012() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "bbbbbbbbbbcccccddddd"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "hhhhhhhhh=Irrelevant"
                        yield 0uy;
                    |];
                |] =
                ValueNone
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_013() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Bbbbbbbbbb==cccccddddd"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Hhhhhhhhh=Irrelevant"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Bbbbbbbbbb", TextValueType<byte[]>.Value( Encoding.UTF8.GetBytes "=cccccddddd" ) );
                        ( "Hhhhhhhhh", TextValueType<byte[]>.ISV_Irrelevant );
                    |]
                )

        )

    [<Fact>]
    member _.TextKeyData2KeyValues_014() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "Aaaaa="
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "Hhhhhhhhh=Irrelevant"
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    [|
                        ( "Aaaaa", TextValueType.Value( Array.empty ) );
                        ( "Hhhhhhhhh", TextValueType<byte[]>.ISV_Irrelevant );
                    |]
                )
        )

    [<Fact>]
    member _.TextKeyData2KeyValues_015() =
        Assert.True (
            IscsiTextEncode.TextKeyData2KeyValues
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "=bbbbbb"
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "HHhhHhhhh=Irrelevant"
                        yield 0uy;
                    |];
                |] =
                ValueNone
        )

    [<Fact>]
    member _.SearchTextKeyValue_001() =
        Assert.True(
                IscsiTextEncode.SearchTextKeyValue 
                    "key1"
                    [|
                        ( "key1", TextValueType<byte[]>.ISV_NotUnderstood );
                        ( "key2", TextValueType<byte[]>.ISV_Irrelevant );
                        ( "key3", TextValueType<byte[]>.ISV_Reject );
                        ( "key4", TextValueType<byte[]>.ISV_Reject );
                        ( "key5", TextValueType<byte[]>.ISV_Irrelevant );
                        ( "key6", TextValueType<byte[]>.ISV_NotUnderstood );
                    |] =
                    TextValueType<byte[]>.ISV_NotUnderstood
        )

    [<Fact>]
    member _.SearchTextKeyValue_002() =
        Assert.True(
            IscsiTextEncode.SearchTextKeyValue 
                "key6"
                [|
                    ( "key1", TextValueType<byte[]>.ISV_NotUnderstood );
                    ( "key2", TextValueType<byte[]>.ISV_Irrelevant );
                    ( "key3", TextValueType<byte[]>.ISV_Reject );
                    ( "key4", TextValueType<byte[]>.ISV_Reject );
                    ( "key5", TextValueType<byte[]>.ISV_Irrelevant );
                    ( "key6", TextValueType<byte[]>.Value( [| 0uy .. 9uy |] ) );
                |] =
                TextValueType<byte[]>.Value( [| 0uy .. 9uy |] )
        )

    [<Fact>]
    member _.SearchTextKeyValue_003() =
        Assert.True(
            IscsiTextEncode.SearchTextKeyValue 
                "key2"
                [|
                    ( "key1", TextValueType<byte[]>.ISV_NotUnderstood );
                    ( "key2", TextValueType<byte[]>.ISV_Irrelevant );
                    ( "key3", TextValueType<byte[]>.ISV_Reject );
                |] =
                TextValueType<byte[]>.ISV_Irrelevant
        )

    [<Fact>]
    member _.SearchTextKeyValue_004() =
        Assert.True(
            IscsiTextEncode.SearchTextKeyValue 
                "aaaaa"
                [|
                    ( "key1", TextValueType<byte[]>.ISV_NotUnderstood );
                    ( "key2", TextValueType<byte[]>.ISV_Irrelevant );
                    ( "key3", TextValueType<byte[]>.ISV_Reject );
                |] =
                TextValueType<byte[]>.ISV_Missing
        )

    [<Fact>]
    member _.SearchTextKeyValue_005() =
        Assert.True(
            IscsiTextEncode.SearchTextKeyValue 
                "aaaaa"
                [|
                |] =
                TextValueType<byte[]>.ISV_Missing
        )

    [<Fact>]
    member _.SearchTextKeyValue_006() =
        Assert.True(
            IscsiTextEncode.SearchTextKeyValue 
                "key2"
                [|
                    ( "key2", TextValueType<byte[]>.ISV_Irrelevant );
                |] =
                TextValueType<byte[]>.ISV_Irrelevant
        )

    [<Fact>]
    member _.SearchTextKeyValue_007() =
        Assert.True(
            IscsiTextEncode.SearchTextKeyValue 
                ""
                [|
                    ( "key2", TextValueType<byte[]>.ISV_Irrelevant );
                |] =
                TextValueType<byte[]>.ISV_Missing
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.Value( Encoding.UTF8.GetBytes "CHAP,KRB5,SPKM1,SPKM2,SRP,aaaaaa,None" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod =
                            TextValueType.Value(
                                [|
                                    AuthMethodCandidateValue.AMC_CHAP;
                                    AuthMethodCandidateValue.AMC_KRB5;
                                    AuthMethodCandidateValue.AMC_SPKM1;
                                    AuthMethodCandidateValue.AMC_SPKM2;
                                    AuthMethodCandidateValue.AMC_SRP;
                                    AuthMethodCandidateValue.AMC_NotUnderstood;
                                    AuthMethodCandidateValue.AMC_None
                                |] );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_001_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "AuthMethod", TextValueType.Value( Encoding.UTF8.GetBytes "CHAP,KRB5,SPKM1,SPKM2,SRP,()==,None" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.Value( Encoding.UTF8.GetBytes "12345,0,65535" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 12345us; 0us; 65535us |] );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_002_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_A", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.Value( Encoding.UTF8.GetBytes "128" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.Value( 128us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_003_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_I", TextValueType.Value( Encoding.UTF8.GetBytes "256" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.Value( Encoding.UTF8.GetBytes "0bQUJDREVGRw==" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.Value( Encoding.UTF8.GetBytes "ABCDEFG" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_004_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_C", TextValueType.Value( Encoding.UTF8.GetBytes "ABCCC%%&$$##" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.Value( Encoding.UTF8.GetBytes "aaaabbbbcccc" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.Value( "aaaabbbbcccc" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_005_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_N", TextValueType.Value( Encoding.UTF8.GetBytes "ABCCC%%&$$##" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException ->
            ()
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.Value( Encoding.UTF8.GetBytes "0x000102030405060708090A0B0C0D0E0F" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.Value( [| 0x00uy .. 0x0Fuy |] );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_006_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "CHAP_R", TextValueType.Value( Encoding.UTF8.GetBytes "ABCCC%%&$$##" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.Value( Encoding.UTF8.GetBytes "CRC32C,None,aaaaaaaaaaaa" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; DigestType.DST_NotUnderstood |] );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_007_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "HeaderDigest", TextValueType.Value( Encoding.UTF8.GetBytes "CRC32C,None,((())))001236%%&$$##" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.Value( Encoding.UTF8.GetBytes "CRC32C,None,aaaaaaaaaaaa" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; DigestType.DST_NotUnderstood |] );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_008_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "DataDigest", TextValueType.Value( Encoding.UTF8.GetBytes "CRC32C,None,((())))001236%%&$$##" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.ISV_NotUnderstood
                }
        )
    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.Value( Encoding.UTF8.GetBytes "12345" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.Value( 12345us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_009_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxConnections", TextValueType.Value( Encoding.UTF8.GetBytes "0" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.Value( Encoding.UTF8.GetBytes "abcdefgABCDEFG" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.Value( "abcdefgABCDEFG" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_010_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "SendTargets", TextValueType.Value( Encoding.UTF8.GetBytes "aa()))&&%%$$aaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.Value( Encoding.UTF8.GetBytes "abcdefg0123456789" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.Value( "abcdefg0123456789" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_011_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetName", TextValueType.Value( Encoding.UTF8.GetBytes "aa()))&&%%$$aaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.Value( Encoding.UTF8.GetBytes "abcdefg0123456789" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.Value( "abcdefg0123456789" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_012_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorName", TextValueType.Value( Encoding.UTF8.GetBytes "aa()))&&%%$$aaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_013_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAlias", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_013_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAlias", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_013_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAlias", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_013_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAlias", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_013_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAlias", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_013_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAlias", TextValueType.Value( Encoding.UTF8.GetBytes "abcdefgABCDEFG" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.Value( "abcdefgABCDEFG" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_014_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorAlias", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_014_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorAlias", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_014_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorAlias", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_014_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorAlias", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_014_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorAlias", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_014_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitiatorAlias", TextValueType.Value( Encoding.UTF8.GetBytes "abcdefgABCDEFG" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.Value( "abcdefgABCDEFG" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_015_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAddress", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_015_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAddress", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_015_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAddress", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_015_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAddress", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_015_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAddress", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_015_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetAddress", TextValueType.Value( Encoding.UTF8.GetBytes "abcdefgABCDEFG" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.Value( "abcdefgABCDEFG" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.Value( Encoding.UTF8.GetBytes "65432" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.Value( 65432us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_016_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "TargetPortalGroupTag", TextValueType.Value( Encoding.UTF8.GetBytes "65536" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.Value( Encoding.UTF8.GetBytes "No" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_017_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "InitialR2T", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.Value( Encoding.UTF8.GetBytes "Yes" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_018_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "ImmediateData", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "512" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 512u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777215" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 16777215u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "511" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_019_010() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                true
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777216" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "512" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 512u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777215" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 16777215u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "511" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_020_010() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxRecvDataSegmentLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777216" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "512" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.Value( 512u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777215" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.Value( 16777215u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "511" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_021_010() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777216" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "512" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.Value( 512u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777215" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.Value( 16777215u );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "511" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_022_010() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "FirstBurstLength", TextValueType.Value( Encoding.UTF8.GetBytes "16777216" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.Value( Encoding.UTF8.GetBytes "3600" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.Value( 3600us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_023_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Wait", TextValueType.Value( Encoding.UTF8.GetBytes "3601" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.Value( Encoding.UTF8.GetBytes "0" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.Value( 0us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.Value( Encoding.UTF8.GetBytes "3600" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.Value( 3600us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_024_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DefaultTime2Retain", TextValueType.Value( Encoding.UTF8.GetBytes "3601" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.Value( Encoding.UTF8.GetBytes "1" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.Value( 1us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.Value( Encoding.UTF8.GetBytes "65535" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.Value( 65535us );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_025_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "MaxOutstandingR2T", TextValueType.Value( Encoding.UTF8.GetBytes "0" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.Value( Encoding.UTF8.GetBytes "Yes" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.Value( true );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_026_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataPDUInOrder", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.Value( Encoding.UTF8.GetBytes "No" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.Value( false );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_027_007() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "DataSequenceInOrder", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.Value( Encoding.UTF8.GetBytes "0" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 0uy );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.Value( Encoding.UTF8.GetBytes "1" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 1uy );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_008() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.Value( Encoding.UTF8.GetBytes "2" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 2uy );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.Value( Encoding.UTF8.GetBytes "aaaxxxxxxaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_028_010() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "ErrorRecoveryLevel", TextValueType.Value( Encoding.UTF8.GetBytes "3" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_001() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.ISV_Missing ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.ISV_Missing
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_002() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.ISV_NotUnderstood ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.ISV_NotUnderstood
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_003() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.ISV_Irrelevant ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.ISV_Irrelevant
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_004() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_005() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.ISV_Reject ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.ISV_Reject
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_006() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.Value( Encoding.UTF8.GetBytes "Discovery" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.Value( "Discovery" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_007() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.Value( Encoding.UTF8.GetBytes "Normal" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.Value( "Normal" );
                }
        )

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_008() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.Value( Encoding.UTF8.GetBytes "aa(((6&&&&$$$##!!!aaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_029_009() =
        try
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "SessionType", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaaaaa" ) )
            |> ignore
            Assert.Fail __LINE__
        with
        | :? System.ArgumentException
        | :? System.NullReferenceException as x ->
            ()
        | _ ->       
            Assert.Fail __LINE__

    [<Fact>]
    member _.UpdateTextKeyValuesRecord_030() =
        Assert.True(
            IscsiTextEncode.UpdateTextKeyValuesRecord
                false
                TextKeyValues.defaultTextKeyValues
                ( "aaaaaaaaa", TextValueType.Value( Encoding.UTF8.GetBytes "aaaaaaaaa" ) ) =
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "aaaaaaaaa" |];
                }
        )

    [<Fact>]
    member _.RecognizeTextKeyData_001() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "AuthMethod=CHAP,None";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "HeaderDigest=CRC32C";
                        yield 0uy;
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxConnections=1024";
                        yield 0uy;
                    |];
                |] =
                ValueSome(
                    {
                        TextKeyValues.defaultTextKeyValues with
                            AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None; |] );
                            HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] );
                            DataDigest = TextValueType.Value( [| DigestType.DST_None; |] );
                            MaxConnections = TextValueType.Value( 1024us );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_002() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=1024";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxBurstLength=2048";
                    |];
                    [|
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxRecvDataSegmentLength_I = TextValueType.Value( 1024u );
                            MaxBurstLength = TextValueType.Value( 2048u );
                            DataDigest = TextValueType.Value( [| DigestType.DST_None; |] );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_003() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=1024";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxBurst";
                    |];
                    [|
                        yield! Encoding.UTF8.GetBytes "Length=2048";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                    |];
                |] =
                ValueSome(
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxRecvDataSegmentLength_I = TextValueType.Value( 1024u );
                            MaxBurstLength = TextValueType.Value( 2048u );
                            DataDigest = TextValueType.Value( [| DigestType.DST_None; |] );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_004() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=1024";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxBurstLength=2048";
                    |];
                    [|
                        yield 0uy;
                    |]
                    [|
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                    |];
                |] =
                ValueSome (
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxRecvDataSegmentLength_I = TextValueType.Value( 1024u );
                            MaxBurstLength = TextValueType.Value( 2048u );
                            DataDigest = TextValueType.Value( [| DigestType.DST_None; |] );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_005() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=1024";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxBurstLength=2048";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                    |];
                |] =
                ValueSome(
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxRecvDataSegmentLength_I = TextValueType.Value( 1024u );
                            MaxBurstLength = TextValueType.Value( 2048u );
                            DataDigest = TextValueType.Value( [| DigestType.DST_None; |] );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_006() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=1024";
                        yield 0uy;
                    |];
                |] =
                ValueSome(
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxRecvDataSegmentLength_I = TextValueType.Value( 1024u );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_007() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=1024";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxBurstLength=2048";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                    |];
                    [|
                    |]
                |] =
                ValueSome (
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxRecvDataSegmentLength_I = TextValueType.Value( 1024u );
                            MaxBurstLength = TextValueType.Value( 2048u );
                            DataDigest = TextValueType.Value( [| DigestType.DST_None; |] );
                    }
                )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_008() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                    |];
                    [|
                    |]
                |] =
                ValueSome( TextKeyValues.defaultTextKeyValues )
        )

    [<Fact>]
    member _.RecognizeTextKeyData_009() =
        Assert.True(
            IscsiTextEncode.RecognizeTextKeyData
                false
                [|
                    [|
                        yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength====1024";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "MaxBurstLength=2048";
                        yield 0uy;
                        yield! Encoding.UTF8.GetBytes "DataDigest=None";
                        yield 0uy;
                    |];
                    [|
                    |]
                |] =
                ValueNone
        )

    [<Fact>]
    member _.CreateTextKeyValueString_001() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    AuthMethod =
                        TextValueType.Value(
                            [|
                                AuthMethodCandidateValue.AMC_CHAP;
                                AuthMethodCandidateValue.AMC_SRP;
                                AuthMethodCandidateValue.AMC_KRB5;
                                AuthMethodCandidateValue.AMC_SPKM1;
                                AuthMethodCandidateValue.AMC_SPKM2;
                                AuthMethodCandidateValue.AMC_None;
                            |]
                        );
                    CHAP_A = TextValueType.Value( [| 1us; 2us; 5us; |] );
                    CHAP_I = TextValueType.Value( 128us );
                    CHAP_C = TextValueType.Value( Encoding.UTF8.GetBytes "ABCDEFG" );
                    CHAP_N = TextValueType.Value( "abcdefg" );
                    CHAP_R = TextValueType.Value( Encoding.UTF8.GetBytes "abcdef" );
                    HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; |] ); 
                    DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; |] ); 
                    MaxConnections = TextValueType.Value( 512us );
                    SendTargets = TextValueType.Value( "1111111111" );
                    TargetName = TextValueType.Value( "2222222222" );
                    InitiatorName = TextValueType.Value( "3333333333" );
                    TargetAlias = TextValueType.Value( "4444444444" );
                    InitiatorAlias = TextValueType.Value( "5555555555" );
                    TargetAddress = TextValueType.Value( "6666666666" );
                    TargetPortalGroupTag = TextValueType.Value( 1024us );
                    InitialR2T = TextValueType.Value( true );
                    ImmediateData = TextValueType.Value( false );
                    MaxRecvDataSegmentLength_I = TextValueType.Value( 4096u );
                    MaxRecvDataSegmentLength_T = TextValueType.Value( 8192u );
                    MaxBurstLength = TextValueType.Value( 10000u );
                    FirstBurstLength = TextValueType.Value( 10001u );
                    DefaultTime2Wait = TextValueType.Value( 100us );
                    DefaultTime2Retain = TextValueType.Value( 101us );
                    MaxOutstandingR2T = TextValueType.Value( 102us );
                    DataPDUInOrder = TextValueType.Value( true );
                    DataSequenceInOrder = TextValueType.Value( false );
                    ErrorRecoveryLevel = TextValueType.Value( 1uy );
                    SessionType = TextValueType.Value( "Discovery" );
                    UnknownKeys = [| "XXX"; "YYY"; "ZZZ" |];
                }
                {
                    IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend ) with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated;
                }

        let t1 = [|
                    yield! Encoding.UTF8.GetBytes "AuthMethod=CHAP,SRP,KRB5,SPKM1,SPKM2,None";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_A=1,2,5";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_I=128";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_C=0bQUJDREVGRw==";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_N=abcdefg";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_R=0bYWJjZGVm";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "HeaderDigest=CRC32C,None";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataDigest=CRC32C,None";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxConnections=512";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SendTargets=1111111111";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetName=2222222222";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorName=3333333333";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAlias=4444444444";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorAlias=5555555555";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAddress=6666666666";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetPortalGroupTag=1024";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitialR2T=Yes";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ImmediateData=No";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxBurstLength=10000";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "FirstBurstLength=10001";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Wait=100";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Retain=101";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxOutstandingR2T=102";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataPDUInOrder=Yes";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataSequenceInOrder=No";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ErrorRecoveryLevel=1";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SessionType=Discovery";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "XXX=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "YYY=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ZZZ=NotUnderstood";
                    yield 0uy;
                |]
        Assert.True <| ( r1 = t1 )


    [<Fact>]
    member _.CreateTextKeyValueString_002() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod =
                            TextValueType.Value(
                                [|
                                    AuthMethodCandidateValue.AMC_CHAP;
                                    AuthMethodCandidateValue.AMC_SRP;
                                    AuthMethodCandidateValue.AMC_KRB5;
                                    AuthMethodCandidateValue.AMC_SPKM1;
                                    AuthMethodCandidateValue.AMC_SPKM2;
                                    AuthMethodCandidateValue.AMC_None;
                                |]
                            );
                        CHAP_I = TextValueType.Value( 128us );
                        CHAP_N = TextValueType.Value( "abcdefg" );
                        HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; |] ); 
                        MaxConnections = TextValueType.Value( 512us );
                        TargetName = TextValueType.Value( "2222222222" );
                        TargetAlias = TextValueType.Value( "4444444444" );
                        TargetAddress = TextValueType.Value( "6666666666" );
                        InitialR2T = TextValueType.Value( true );
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 4096u );
                        MaxBurstLength = TextValueType.Value( 10000u );
                        DefaultTime2Wait = TextValueType.Value( 100us );
                        MaxOutstandingR2T = TextValueType.Value( 102us );
                        DataSequenceInOrder = TextValueType.Value( false );
                        SessionType = TextValueType.Value( "Discovery" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                        NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                        NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend;
                        NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                        NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend;
                        NegoStat_TargetAddress = NegoStatusValue.NSG_WaitSend;
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend;
                        NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend;
                        NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend;
                        NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                }
        let t1 = [|
                    yield! Encoding.UTF8.GetBytes "AuthMethod=CHAP,SRP,KRB5,SPKM1,SPKM2,None";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_I=128";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_N=abcdefg";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "HeaderDigest=CRC32C,None";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxConnections=512";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetName=2222222222";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAlias=4444444444";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAddress=6666666666";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitialR2T=Yes";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=4096";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxBurstLength=10000";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Wait=100";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxOutstandingR2T=102";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataSequenceInOrder=No";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SessionType=Discovery";
                    yield 0uy;
                |]
        Assert.True <| ( r1 = t1 )

    [<Fact>]
    member _.CreateTextKeyValueString_003() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 1us; 2us; 5us; |] );
                        CHAP_C = TextValueType.Value( Encoding.UTF8.GetBytes "ABCDEFG" );
                        CHAP_R = TextValueType.Value( Encoding.UTF8.GetBytes "abcdef" );
                        DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; |] ); 
                        SendTargets = TextValueType.Value( "1111111111" );
                        InitiatorName = TextValueType.Value( "3333333333" );
                        InitiatorAlias = TextValueType.Value( "5555555555" );
                        TargetPortalGroupTag = TextValueType.Value( 1024us );
                        ImmediateData = TextValueType.Value( false );
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 8192u );
                        FirstBurstLength = TextValueType.Value( 10001u );
                        DefaultTime2Retain = TextValueType.Value( 101us );
                        DataPDUInOrder = TextValueType.Value( true );
                        ErrorRecoveryLevel = TextValueType.Value( 1uy );
                        UnknownKeys = [| "XXX"; "YYY"; "ZZZ" |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                        NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                        NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend;
                        NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend;
                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                        NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                        NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitSend;
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitSend;
                        NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend;
                        NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend;
                        NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend;
                        NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend;
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitSend;
                }
        let t1 = [|
                    yield! Encoding.UTF8.GetBytes "CHAP_A=1,2,5";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_C=0bQUJDREVGRw==";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_R=0bYWJjZGVm";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataDigest=CRC32C,None";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SendTargets=1111111111";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorName=3333333333";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorAlias=5555555555";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetPortalGroupTag=1024";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ImmediateData=No";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=8192";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "FirstBurstLength=10001";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Retain=101";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataPDUInOrder=Yes";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ErrorRecoveryLevel=1";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "XXX=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "YYY=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ZZZ=NotUnderstood";
                    yield 0uy;
                |]
        Assert.True <| ( r1 = t1 )

    [<Fact>]
    member _.CreateTextKeyValueString_004() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    AuthMethod = TextValueType.ISV_Missing;
                    CHAP_A = TextValueType.ISV_Missing;
                    CHAP_I = TextValueType.ISV_Missing;
                    CHAP_C = TextValueType.ISV_Missing;
                    CHAP_N = TextValueType.ISV_Missing;
                    CHAP_R = TextValueType.ISV_Missing;
                    HeaderDigest = TextValueType.ISV_Missing;
                    DataDigest = TextValueType.ISV_Missing ;
                    MaxConnections = TextValueType.ISV_Missing;
                    SendTargets = TextValueType.ISV_Missing;
                    TargetName = TextValueType.ISV_Missing;
                    InitiatorName = TextValueType.ISV_Missing;
                    TargetAlias = TextValueType.ISV_Missing;
                    InitiatorAlias = TextValueType.ISV_Missing;
                    TargetAddress = TextValueType.ISV_Missing;
                    TargetPortalGroupTag = TextValueType.ISV_Missing;
                    InitialR2T = TextValueType.ISV_Missing;
                    ImmediateData = TextValueType.ISV_Missing;
                    MaxRecvDataSegmentLength_I = TextValueType.ISV_Missing;
                    MaxRecvDataSegmentLength_T = TextValueType.ISV_Missing;
                    MaxBurstLength = TextValueType.ISV_Missing;
                    FirstBurstLength = TextValueType.ISV_Missing;
                    DefaultTime2Wait = TextValueType.ISV_Missing;
                    DefaultTime2Retain = TextValueType.ISV_Missing;
                    MaxOutstandingR2T = TextValueType.ISV_Missing;
                    DataPDUInOrder = TextValueType.ISV_Missing;
                    DataSequenceInOrder = TextValueType.ISV_Missing;
                    ErrorRecoveryLevel = TextValueType.ISV_Missing;
                    SessionType = TextValueType.ISV_Missing;
                    UnknownKeys = [| |];
                }
                {
                    IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend ) with
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated;
                }

        let t1 = Array.empty
        Assert.True <| ( r1 = t1 )

    [<Fact>]
    member _.CreateTextKeyValueString_005() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    AuthMethod = TextValueType.ISV_NotUnderstood;
                    CHAP_A = TextValueType.ISV_NotUnderstood;
                    CHAP_I = TextValueType.ISV_NotUnderstood;
                    CHAP_C = TextValueType.ISV_NotUnderstood;
                    CHAP_N = TextValueType.ISV_NotUnderstood;
                    CHAP_R = TextValueType.ISV_NotUnderstood;
                    HeaderDigest = TextValueType.ISV_NotUnderstood;
                    DataDigest = TextValueType.ISV_NotUnderstood ;
                    MaxConnections = TextValueType.ISV_NotUnderstood;
                    SendTargets = TextValueType.ISV_NotUnderstood;
                    TargetName = TextValueType.ISV_NotUnderstood;
                    InitiatorName = TextValueType.ISV_NotUnderstood;
                    TargetAlias = TextValueType.ISV_NotUnderstood;
                    InitiatorAlias = TextValueType.ISV_NotUnderstood;
                    TargetAddress = TextValueType.ISV_NotUnderstood;
                    TargetPortalGroupTag = TextValueType.ISV_NotUnderstood;
                    InitialR2T = TextValueType.ISV_NotUnderstood;
                    ImmediateData = TextValueType.ISV_NotUnderstood;
                    MaxRecvDataSegmentLength_I = TextValueType.ISV_NotUnderstood;
                    MaxRecvDataSegmentLength_T = TextValueType.ISV_NotUnderstood;
                    MaxBurstLength = TextValueType.ISV_NotUnderstood;
                    FirstBurstLength = TextValueType.ISV_NotUnderstood;
                    DefaultTime2Wait = TextValueType.ISV_NotUnderstood;
                    DefaultTime2Retain = TextValueType.ISV_NotUnderstood;
                    MaxOutstandingR2T = TextValueType.ISV_NotUnderstood;
                    DataPDUInOrder = TextValueType.ISV_NotUnderstood;
                    DataSequenceInOrder = TextValueType.ISV_NotUnderstood;
                    ErrorRecoveryLevel = TextValueType.ISV_NotUnderstood;
                    SessionType = TextValueType.ISV_NotUnderstood;
                    UnknownKeys = [| |];
                }
                {
                    IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend ) with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated;
                }

        let t1 = [|
                    yield! Encoding.UTF8.GetBytes "AuthMethod=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_A=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_I=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_C=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_N=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_R=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "HeaderDigest=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataDigest=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxConnections=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SendTargets=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetName=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorName=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAlias=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorAlias=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAddress=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetPortalGroupTag=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitialR2T=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ImmediateData=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxBurstLength=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "FirstBurstLength=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Wait=NotUnderstood"
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Retain=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxOutstandingR2T=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataPDUInOrder=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataSequenceInOrder=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ErrorRecoveryLevel=NotUnderstood";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SessionType=NotUnderstood";
                    yield 0uy;
                |]
        Assert.True <| ( r1 = t1 )

    [<Fact>]
    member _.CreateTextKeyValueString_006() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    AuthMethod = TextValueType.ISV_Irrelevant;
                    CHAP_A = TextValueType.ISV_Irrelevant;
                    CHAP_I = TextValueType.ISV_Irrelevant;
                    CHAP_C = TextValueType.ISV_Irrelevant;
                    CHAP_N = TextValueType.ISV_Irrelevant;
                    CHAP_R = TextValueType.ISV_Irrelevant;
                    HeaderDigest = TextValueType.ISV_Irrelevant;
                    DataDigest = TextValueType.ISV_Irrelevant ;
                    MaxConnections = TextValueType.ISV_Irrelevant;
                    SendTargets = TextValueType.ISV_Irrelevant;
                    TargetName = TextValueType.ISV_Irrelevant;
                    InitiatorName = TextValueType.ISV_Irrelevant;
                    TargetAlias = TextValueType.ISV_Irrelevant;
                    InitiatorAlias = TextValueType.ISV_Irrelevant;
                    TargetAddress = TextValueType.ISV_Irrelevant;
                    TargetPortalGroupTag = TextValueType.ISV_Irrelevant;
                    InitialR2T = TextValueType.ISV_Irrelevant;
                    ImmediateData = TextValueType.ISV_Irrelevant;
                    MaxRecvDataSegmentLength_I = TextValueType.ISV_Irrelevant;
                    MaxRecvDataSegmentLength_T = TextValueType.ISV_Irrelevant;
                    MaxBurstLength = TextValueType.ISV_Irrelevant;
                    FirstBurstLength = TextValueType.ISV_Irrelevant;
                    DefaultTime2Wait = TextValueType.ISV_Irrelevant;
                    DefaultTime2Retain = TextValueType.ISV_Irrelevant;
                    MaxOutstandingR2T = TextValueType.ISV_Irrelevant;
                    DataPDUInOrder = TextValueType.ISV_Irrelevant;
                    DataSequenceInOrder = TextValueType.ISV_Irrelevant;
                    ErrorRecoveryLevel = TextValueType.ISV_Irrelevant;
                    SessionType = TextValueType.ISV_Irrelevant;
                    UnknownKeys = [| |];
                }
                {
                    IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive ) with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated ||| NegoStatusValue.NSG_WaitReceive;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated ||| NegoStatusValue.NSG_WaitReceive;
                }

        let t1 = [|
                    yield! Encoding.UTF8.GetBytes "AuthMethod=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_A=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_I=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_C=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_N=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_R=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "HeaderDigest=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataDigest=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxConnections=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SendTargets=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetName=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorName=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAlias=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorAlias=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAddress=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetPortalGroupTag=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitialR2T=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ImmediateData=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxBurstLength=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "FirstBurstLength=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Wait=Irrelevant"
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Retain=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxOutstandingR2T=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataPDUInOrder=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataSequenceInOrder=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ErrorRecoveryLevel=Irrelevant";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SessionType=Irrelevant";
                    yield 0uy;
                |]
        Assert.True <| ( r1 = t1 )

    [<Fact>]
    member _.CreateTextKeyValueString_007() =
        let r1 =
            IscsiTextEncode.CreateTextKeyValueString
                {
                    AuthMethod = TextValueType.ISV_Reject;
                    CHAP_A = TextValueType.ISV_Reject;
                    CHAP_I = TextValueType.ISV_Reject;
                    CHAP_C = TextValueType.ISV_Reject;
                    CHAP_N = TextValueType.ISV_Reject;
                    CHAP_R = TextValueType.ISV_Reject;
                    HeaderDigest = TextValueType.ISV_Reject;
                    DataDigest = TextValueType.ISV_Reject ;
                    MaxConnections = TextValueType.ISV_Reject;
                    SendTargets = TextValueType.ISV_Reject;
                    TargetName = TextValueType.ISV_Reject;
                    InitiatorName = TextValueType.ISV_Reject;
                    TargetAlias = TextValueType.ISV_Reject;
                    InitiatorAlias = TextValueType.ISV_Reject;
                    TargetAddress = TextValueType.ISV_Reject;
                    TargetPortalGroupTag = TextValueType.ISV_Reject;
                    InitialR2T = TextValueType.ISV_Reject;
                    ImmediateData = TextValueType.ISV_Reject;
                    MaxRecvDataSegmentLength_I = TextValueType.ISV_Reject;
                    MaxRecvDataSegmentLength_T = TextValueType.ISV_Reject;
                    MaxBurstLength = TextValueType.ISV_Reject;
                    FirstBurstLength = TextValueType.ISV_Reject;
                    DefaultTime2Wait = TextValueType.ISV_Reject;
                    DefaultTime2Retain = TextValueType.ISV_Reject;
                    MaxOutstandingR2T = TextValueType.ISV_Reject;
                    DataPDUInOrder = TextValueType.ISV_Reject;
                    DataSequenceInOrder = TextValueType.ISV_Reject;
                    ErrorRecoveryLevel = TextValueType.ISV_Reject;
                    SessionType = TextValueType.ISV_Reject;
                    UnknownKeys = [| |];
                }
                {
                    IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive ) with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated ||| NegoStatusValue.NSG_WaitReceive;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated ||| NegoStatusValue.NSG_WaitReceive;
                }
        let t1 = [|
                    yield! Encoding.UTF8.GetBytes "AuthMethod=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_A=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_I=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_C=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_N=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "CHAP_R=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "HeaderDigest=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataDigest=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxConnections=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SendTargets=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetName=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorName=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAlias=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitiatorAlias=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetAddress=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "TargetPortalGroupTag=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "InitialR2T=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ImmediateData=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxBurstLength=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "FirstBurstLength=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Wait=Reject"
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DefaultTime2Retain=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "MaxOutstandingR2T=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataPDUInOrder=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "DataSequenceInOrder=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "ErrorRecoveryLevel=Reject";
                    yield 0uy;
                    yield! Encoding.UTF8.GetBytes "SessionType=Reject";
                    yield 0uy;
                |]
        Assert.True <| ( r1 = t1 )

    [<Fact>]
    member _.IsAllKeyNegotiated_001() =
        let v1 = IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive )
        Assert.False( IscsiTextEncode.IsAllKeyNegotiated v1 )

    [<Fact>]
    member _.IsAllKeyNegotiated_002() =
        let v2 = IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitSend )
        Assert.False( IscsiTextEncode.IsAllKeyNegotiated v2 )

    [<Fact>]
    member _.IsAllKeyNegotiated_003() =
        let v3 = IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitReceive )
        Assert.False( IscsiTextEncode.IsAllKeyNegotiated v3 )

    [<Fact>]
    member _.IsAllKeyNegotiated_004() =
        let v4 = IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSV_Negotiated )
        Assert.True( IscsiTextEncode.IsAllKeyNegotiated v4 )

    [<Fact>]
    member _.IsAllKeyNegotiated_005() =
        let v5 = {
            IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSV_Negotiated ) with
                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive;
        }
        Assert.False( IscsiTextEncode.IsAllKeyNegotiated v5 )

    [<Fact>]
    member _.ClearSendWaitStatus_001() =
        let v1 = IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend )
        let v2 = IscsiTextEncode1_Test.genNegStatVal ( NegoStatusValue.NSG_WaitReceive )
        Assert.True( IscsiTextEncode.ClearSendWaitStatus v1 = v2 )

