//=============================================================================
// Haruka Software Storage.
// FunctionsTest.fs : Test cases for global functions defined at Functions.fs.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Runtime.Intrinsics.X86
open System.Collections.Generic
open System.Net

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

type Functions_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases
   
    [<Theory>]
    [<InlineData( 0, 4, 0 )>]
    [<InlineData( 1, 4, 4 )>]
    [<InlineData( 4, 4, 4 )>]
    [<InlineData( 5, 4, 8 )>]
    member _.AddPaddingLength_001 ( v1 : int ) ( v2 : int ) ( v3 : int ) =
        Assert.True( Functions.AddPaddingLengthInt32 v1 v2 = v3 )

    [<Theory>]
    [<InlineData( 0u, 5u, 0u )>]
    [<InlineData( 1u, 5u, 5u )>]
    [<InlineData( 5u, 5u, 5u )>]
    [<InlineData( 6u, 5u, 10u )>]
    member _.AddPaddingLength_002 ( v1 : uint32 ) ( v2 : uint32 ) ( v3 : uint32 ) =
        Assert.True( Functions.AddPaddingLengthUInt32 v1 v2 = v3 )

    [<Theory>]
    [<InlineData( 0s, 2s, 0s )>]
    [<InlineData( 1s, 2s, 2s )>]
    [<InlineData( 3s, 2s, 4s )>]
    [<InlineData( 4s, 2s, 4s )>]
    member _.AddPaddingLength_003 ( v1 : int16 ) ( v2 : int16 ) ( v3 : int16 ) =
        Assert.True( Functions.AddPaddingLengthInt16 v1 v2 = v3 )

    [<Theory>]
    [<InlineData( 0us, 4us, 0us )>]
    [<InlineData( 1us, 4us, 4us )>]
    [<InlineData( 4us, 4us, 4us )>]
    [<InlineData( 5us, 4us, 8us )>]
    member _.AddPaddingLength_004 ( v1 : uint16 ) ( v2 : uint16 ) ( v3 : uint16 ) =
        Assert.True( Functions.AddPaddingLengthUInt16 v1 v2 = v3 )

    [<Theory>]
    [<InlineData( 0y, 4y, 0y )>]
    [<InlineData( 1y, 4y, 4y )>]
    [<InlineData( 4y, 4y, 4y )>]
    [<InlineData( 5y, 4y, 8y )>]
    member _.AddPaddingLength_005 ( v1 : sbyte ) ( v2 : sbyte ) ( v3 : sbyte ) =
        Assert.True( Functions.AddPaddingLengthInt8 v1 v2 = v3 )
    
    [<Theory>]
    [<InlineData( 0uy, 4uy, 0uy )>]
    [<InlineData( 1uy, 4uy, 4uy )>]
    [<InlineData( 4uy, 4uy, 4uy )>]
    [<InlineData( 5uy, 4uy, 8uy )>]
    member _.AddPaddingLength_006 ( v1 : byte ) ( v2 : byte ) ( v3 : byte ) =
        Assert.True( Functions.AddPaddingLengthUInt8 v1 v2 = v3 )

    [<Fact>]
    member _.CRC32_soft_001() =
        let v = [| for _ in 1 .. 32 -> 0uy |]
        let r = PrivateCaller.Invoke< Functions >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x8A9136AAu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_002() =
        let v = [| for _ in 1 .. 32 -> 0xFFuy |]
        let r = PrivateCaller.Invoke< Functions >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x62A8Ab43u = ~~~r )

    [<Fact>]
    member _.CRC32_soft_003() =
        let v = [| for i in 0uy .. 0x1Fuy -> i |]
        let r = PrivateCaller.Invoke< Functions >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_004() =
        let v = [| for i = 0x1F downto 0 do yield byte i |]
        let r = PrivateCaller.Invoke< Functions >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_005() =
        let v = [|
            yield 0xDDuy;
            for i in 0uy .. 0x1Fuy -> i
            yield 0xCCuy;
        |]
        let r = PrivateCaller.Invoke< Functions >( "CRC32_soft", 0xFFFFFFFFu, v, 1, v.Length - 2 ) :?> uint32
        Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_006() =
        let v = [|
            yield 0xDDuy;
            yield 0xDDuy;
            for i = 0x1F downto 0 do yield byte i
            yield 0xCCuy;
            yield 0xCCuy;
        |]
        let r = PrivateCaller.Invoke< Functions >( "CRC32_soft", 0xFFFFFFFFu, v, 2, v.Length - 4 ) :?> uint32
        Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_001() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0uy |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x8A9136AAu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_002() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0xFFuy |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x62A8Ab43u = ~~~r )

    [<Fact>]
    member _.CRC32_x64_003() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for i in 0uy .. 0x1Fuy -> i |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_004() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for i = 0x1F downto 0 do yield byte i |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_005() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [|
                yield 0xAAuy;
                for i in 0uy .. 0x1Fuy -> i
                yield 0xBBuy;
            |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x64", 0xFFFFFFFFu, v, 1, v.Length - 2 ) :?> uint32
            Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_006() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [|
                yield 0xAAuy;
                yield 0xABuy;
                for i = 0x1F downto 0 do yield byte i
                yield 0xBBuy;
                yield 0xBCuy;
            |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x64", 0xFFFFFFFFu, v, 2, v.Length - 4 ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_001() =
        if Sse42.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0uy |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x8A9136AAu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_002() =
        if Sse42.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0xFFuy |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x62A8Ab43u = ~~~r )

    [<Fact>]
    member _.CRC32_x86_003() =
        if Sse42.IsSupported then
            let v = [| for i in 0uy .. 0x1Fuy -> i |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_004() =
        if Sse42.IsSupported then
            let v = [| for i = 0x1F downto 0 do yield byte i |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_005() =
        if Sse42.IsSupported then
            let v = [|
                yield 0xCCuy;
                for i in 0uy .. 0x1Fuy -> i
                yield 0xDDuy;
            |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x86", 0xFFFFFFFFu, v, 1, v.Length - 2 ) :?> uint32
            Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_006() =
        if Sse42.IsSupported then
            let v = [|
                yield 0xCDuy;
                yield 0xCDuy;
                for i = 0x1F downto 0 do yield byte i
                yield 0xCDuy;
                yield 0xCDuy;
            |]
            let r = PrivateCaller.Invoke< Functions >( "CRC32_x86", 0xFFFFFFFFu, v, 2, v.Length - 4 ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_013() =
        let wbuf = [|
            0x01uy; 0xC0uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x14uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x14uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy;
            0x28uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        Assert.True( 0xD9963A56u = Functions.CRC32v [| wbuf.[0..10];  wbuf.[11..23]; wbuf.[24..27]; wbuf.[28..37]; wbuf.[38..47]; |]  )

    [<Fact>]
    member _.CRC32_014() =
        let wv1 = [|
            yield 0uy;
            yield 1uy;
            for _ in 1 .. 32 -> 0uy;
            yield 2uy;
            yield 3uy;
        |]
        Assert.True( 0x8A9136AAu = Functions.CRC32_A ( ArraySegment( wv1, 2, 32 ) ) )

    [<Fact>]
    member _.CRC32_015() =
        let wv2 = [|
            yield 0uy;
            yield 1uy;
            yield 2uy;
            yield 3uy;
            for _ in 1 .. 32 -> 0xFFuy;
        |]
        Assert.True( 0x62A8Ab43u = Functions.CRC32_A ( ArraySegment( wv2, 4, 32 ) ) )

    [<Fact>]
    member _.CRC32_016() =
        let wv3 = [|
            for i in 0uy .. 0x1Fuy -> i;
            yield 0uy;
            yield 1uy;
            yield 2uy;
            yield 3uy;
        |]
        Assert.True( 0x46DD794Eu = Functions.CRC32_A ( ArraySegment( wv3, 0, 32 ) ) )

    [<Fact>]
    member _.CRC32_017() =
        let wv4 = [|
            for i = 0x1F downto 0 do yield byte i;
        |]
        Assert.True( 0x113FDB5Cu = Functions.CRC32_A ( ArraySegment( wv4, 0, 32 ) ) )

    [<Fact>]
    member _.CRC32_018() =
        let wbuf = [|
            0x01uy; 0xC0uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x14uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x14uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy;
            0x28uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let wv5 = [|
            ArraySegment( wbuf, 0, 11 );
            ArraySegment( wbuf, 11, 13 );
            ArraySegment( wbuf, 24, 4 );
            ArraySegment( wbuf, 28, 10 );
            ArraySegment( wbuf, 38, 10 );
        |]
        Assert.True( 0xD9963A56u = Functions.CRC32_AS wv5 )

    [<Fact>]
    member _.NetworkBytesToInt16_001() =
        Assert.True( Functions.NetworkBytesToInt16 [| 0uy .. 16uy |] 1 = 0x0102s )

    [<Fact>]
    member _.NetworkBytesToInt16_InPooledBuffer_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( Functions.NetworkBytesToInt16_InPooledBuffer p 1 = 0x0102s )

    [<Fact>]
    member _.NetworkBytesToUInt16_001() =
        Assert.True( Functions.NetworkBytesToUInt16 [| 0uy .. 16uy |] 2 = 0x0203us )

    [<Fact>]
    member _.NetworkBytesToUInt16_InPooledBuffer_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( Functions.NetworkBytesToUInt16_InPooledBuffer p 2 = 0x0203us )

    [<Fact>]
    member _.NetworkBytesToInt32_001() =
        Assert.True( Functions.NetworkBytesToInt32 [| 0uy .. 16uy |] 3 = 0x03040506 )

    [<Fact>]
    member _.NetworkBytesToInt32_InPooledBuffer_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( Functions.NetworkBytesToInt32_InPooledBuffer p 3 = 0x03040506 )

    [<Fact>]
    member _.NetworkBytesToUInt32_001() =
        Assert.True( Functions.NetworkBytesToUInt32 [| 0uy .. 16uy |] 4 = 0x04050607u )

    [<Fact>]
    member _.NetworkBytesToUInt32_InPooledBuffer_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( Functions.NetworkBytesToUInt32_InPooledBuffer p 4 = 0x04050607u )

    [<Fact>]
    member _.NetworkBytesToInt64_001() =
        Assert.True( Functions.NetworkBytesToInt64 [| 0uy .. 16uy |] 5 = 0x05060708090A0B0CL )

    [<Fact>]
    member _.NetworkBytesToInt64_InPooledBuffer_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 15 )
        Assert.True( Functions.NetworkBytesToInt64_InPooledBuffer p 5 = 0x05060708090A0B0CL )

    [<Fact>]
    member _.NetworkBytesToUInt64_001() =
        Assert.True( Functions.NetworkBytesToUInt64 [| 0uy .. 16uy |] 6 = 0x060708090A0B0C0DUL )

    [<Fact>]
    member _.NetworkBytesToUInt64_InPooledBuffer_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 15 )
        Assert.True( Functions.NetworkBytesToUInt64_InPooledBuffer p 6 = 0x060708090A0B0C0DUL )

    [<Fact>]
    member _.IntToNetworkBytes_001() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        Functions.Int16ToNetworkBytes wbuf 0 0xF1F2s
        Assert.True( ( wbuf = [| 0xF1uy; 0xF2uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_002() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        Functions.UInt16ToNetworkBytes wbuf 0 0xF2F3us
        Assert.True( ( wbuf = [| 0xF2uy; 0xF3uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_003() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        Functions.Int32ToNetworkBytes wbuf 0 0xF3F4F5F6
        Assert.True( ( wbuf = [| 0xF3uy; 0xF4uy; 0xF5uy; 0xF6uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_004() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        Functions.UInt32ToNetworkBytes wbuf 0 0xF4F5F6F7u
        Assert.True( ( wbuf = [| 0xF4uy; 0xF5uy; 0xF6uy; 0xF7uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_005() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        Functions.Int64ToNetworkBytes wbuf 0 0xF5F6F7F8F9FAFBFCL
        Assert.True( ( wbuf = [| 0xF5uy; 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_006() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        Functions.UInt64ToNetworkBytes wbuf 0 0xF6F7F8F9FAFBFCFDUL
        Assert.True( ( wbuf = [| 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; 0xFDuy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_007() =
        Assert.True( ( Functions.Int16ToNetworkBytes_NewVec 0xF1F2s = [| 0xF1uy; 0xF2uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_008() =
        Assert.True( ( Functions.UInt16ToNetworkBytes_NewVec 0xF2F3us = [| 0xF2uy; 0xF3uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_009() =
        Assert.True( ( Functions.Int32ToNetworkBytes_NewVec 0xF3F4F5F6 = [| 0xF3uy; 0xF4uy; 0xF5uy; 0xF6uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_010() =
        Assert.True( ( Functions.UInt32ToNetworkBytes_NewVec 0xF4F5F6F7u = [| 0xF4uy; 0xF5uy; 0xF6uy; 0xF7uy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_011() =
        Assert.True( ( Functions.Int64ToNetworkBytes_NewVec 0xF5F6F7F8F9FAFBFCL = [| 0xF5uy; 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; |] ) )

    [<Fact>]
    member _.IntToNetworkBytes_012() =
        Assert.True( ( Functions.UInt64ToNetworkBytes_NewVec 0xF6F7F8F9FAFBFCFDUL = [| 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; 0xFDuy; |] ) )

    [<Fact>]
    member _.CheckBitflag_001() =
        Assert.False( Functions.CheckBitflag 0x00uy 0x01uy )

    [<Fact>]
    member _.CheckBitflag_002() =
        Assert.True( Functions.CheckBitflag 0x22uy 0x02uy )

    [<Fact>]
    member _.SetBitflag_001() =
        Assert.True( ( Functions.SetBitflag true 0xF1uy ) = 0xF1uy )

    [<Fact>]
    member _.SetBitflag_002() =
        Assert.True( ( Functions.SetBitflag false 0x1Fuy ) = 0x00uy )

    [<Fact>]
    member _.CompareStringHeader_001() =
        Assert.True( Functions.CompareStringHeader "aaa" "aaabbb" = 0 )

    [<Fact>]
    member _.CompareStringHeader_002() =
        Assert.True( Functions.CompareStringHeader "aaabbb" "aaa" = 0 )

    [<Fact>]
    member _.CompareStringHeader_003() =
        Assert.True( Functions.CompareStringHeader "BBBbbb" "aaa" < 0 )

    [<Fact>]
    member _.CompareStringHeader_004() =
        Assert.True( Functions.CompareStringHeader "AAAbbb" "XXX" < 0 )

    [<Fact>]
    member _.SplitByteArray_001() =
        Assert.True( ( Functions.SplitByteArray 0uy [| 1uy; 2uy; 0uy; 3uy; 4uy; 0uy; 9uy |] ) = [[|1uy; 2uy|]; [|3uy; 4uy|]; [|9uy|]] );

    [<Fact>]
    member _.SplitByteArray_002() =
        Assert.True( ( Functions.SplitByteArray 0uy [| 0uy; 2uy; 0uy; 0uy; 4uy; 0uy; 9uy; 0uy |] ) = [Array.empty; [|2uy|]; Array.empty; [|4uy|]; [|9uy|]; Array.empty] );

    [<Fact>]
    member _.SplitByteArray_003() =
        Assert.True( ( Functions.SplitByteArray 0uy Array.empty ) = [Array.empty] );

//    [<Fact>]
//    member _.CreateI_TnexusIdentifier_001() =
//        Assert.True(
//            ( Functions.CreateI_TnexusIdentifier "INITIATOR" { T = 0xA0uy; A = 0x0Buy; B = 0xCCCCus; C = 0xDDuy; D = 0xEEEEus } "TARGET" 0xFFFFus ) = "( INITIATOR,i,0xABCCCCDDEEEE, TARGET,t,0xFFFF )"
//        )

    [<Fact>]
    member _.CompareOptValueWithDefault_001() =
        Assert.True ( ( Functions.CompareOptValueWithDefault true 1 ( Some 1 ) ) )

    [<Fact>]
    member _.CompareOptValueWithDefault_002() =
        Assert.False( ( Functions.CompareOptValueWithDefault true 1 ( Some 2 ) ) )

    [<Fact>]
    member _.CompareOptValueWithDefault_003() =
        Assert.False( ( Functions.CompareOptValueWithDefault false 1 None ) )

    [<Fact>]
    member _.CompareOptValueWithDefault_004() =
        Assert.True( ( Functions.CompareOptValueWithDefault true 1 None ) )

    [<Fact>]
    member _.CompareOptValue_001() =
        Assert.True( ( Functions.CompareOptValue 1 ( Some 1 ) ) )

    [<Fact>]
    member _.CompareOptValue_002() =
        Assert.False( ( Functions.CompareOptValue 1 ( Some 2 ) ) )

    [<Fact>]
    member _.CompareOptValue_003() =
        Assert.False( ( Functions.CompareOptValue 1 None ) )

    [<Fact>]
    member _.TruncateSeq_uint32_001() =
        let s = seq{ 1; 2; 3; 4; 5; 6; 7; 8 }
        let ex = seq{ 1; 2; 3; 4; 5; }
        let r = Functions.TruncateSeq_uint32 5u s
        Assert.True( Seq.toArray ex = Seq.toArray r )

    [<Fact>]
    member _.TruncateSeq_uint32_002() =
        let s = seq{ 1; 2; 3; 4; 5; 6; 7; 8 }
        let ex : int seq = Seq.empty
        let r = Functions.TruncateSeq_uint32 0u s
        Assert.True( Seq.toArray ex = Seq.toArray r )

    [<Fact>]
    member _.TruncateSeq_uint32_003() =
        let s = seq{ 1; 2; 3; 4; 5; 6; 7; 8 }
        let ex = seq{ 1; 2; 3; 4; 5; 6; 7; 8 }
        let r = Functions.TruncateSeq_uint32 10u s
        Assert.True( Seq.toArray ex = Seq.toArray r )

    [<Fact>]
    member _.TruncateSeq_uint32_004() =
        let s = seq{ 1; 2; 3; 4; 5; 6; 7; 8 }
        let ex = seq{ 1; 2; 3; 4; 5; 6; 7; 8 }
        let r = Functions.TruncateSeq_uint32 8u s
        Assert.True( Seq.toArray ex = Seq.toArray r )

    [<Fact>]
    member _.TruncateSeq_uint32_005() =
        let s : int seq = Seq.empty
        let ex : int seq = Seq.empty
        let r = Functions.TruncateSeq_uint32 5u s
        Assert.True( Seq.toArray ex = Seq.toArray r )

    [<Fact>]
    member _.TruncateSeq_uint32_006() =
        let s : int seq = Seq.empty
        let ex : int seq = Seq.empty
        let r = Functions.TruncateSeq_uint32 0u s
        Assert.True( Seq.toArray ex = Seq.toArray r )

    [<Fact>]
    member _.AppendPathName_001() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "c:%ca%cb" c c
        let i2 = "abc"
        let o = sprintf "c:%ca%cb%cabc" c c c
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_002() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = "c:/a/b"
        let i2 = "abc"
        let o = sprintf "c:%ca%cb%cabc" c c c
        let r = Functions.AppendPathName i1 i2
        Assert.True(( r = o ))

    [<Fact>]
    member _.AppendPathName_003() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "c:%ca%cb%c" c c c
        let i2 = "abc"
        let o = sprintf "c:%ca%cb%cabc" c c c
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_004() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = "c:/a/b/"
        let i2 = "abc"
        let o = sprintf "c:%ca%cb%cabc" c c c
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_005() =
        let i1 = ""
        let i2 = "abc"
        let o = "abc"
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_006() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = ""
        let i2 = ""
        let o = ""
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_007() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "%caaa%cbbb" c c
        let i2 = sprintf "ccc%cddd" c
        let o = sprintf "%caaa%cbbb%cccc%cddd" c c c c
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_008() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "aaa%cbbb" c
        let i2 = sprintf "ccc%cddd" c
        let o = sprintf "aaa%cbbb%cccc%cddd" c c c
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.AppendPathName_009() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "aaa%cbbb%c..%cccc" c c c
        let i2 = sprintf "ccc%cddd%c.%c%c" c c c c
        let o = sprintf "aaa%cccc%cccc%cddd%c" c c c c
        Assert.True( Functions.AppendPathName i1 i2 = o )

    [<Fact>]
    member _.GetParentName_001() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "c:%caaa%cbbb" c c
        let o = sprintf "c:%caaa" c
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_002() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "c:%caaa%cbbb%c" c c c
        let o = sprintf "c:%caaa" c
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_003() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "c:%caaa%cbbb%c." c c c
        let o = sprintf "c:%caaa" c
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_004() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "%caaa%cbbb%ceee" c c c
        let o = sprintf "%caaa%cbbb" c c
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_005() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "%caaa%cbbb%ceee%c..%c..%c..%c" c c c c c c c
        let o = sprintf "%c" c
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_006() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "D:%caaa%cbbb%ceee%c..%c..%c..%c" c c c c c c c
        let o = sprintf "D:%c" c
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_007() =
        let i1 = "aaa"
        let o = ""
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_008() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "aaa%cbbb" c
        let o = "aaa"
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.GetParentName_009() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = ""
        let o = ""
        Assert.True( Functions.GetParentName i1 = o )

    [<Fact>]
    member _.OptimizePathName_001() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "C:%caaa%c%cbbb%c.%cccc%c.." c c c c c c
        let o = sprintf "C:%caaa%cbbb" c c
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_002() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "C:%caaa%c..%cbbb%cccc%c..%c..%c..%c.." c c c c c c c c
        let o = sprintf "C:%c" c
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_003() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "%caaa%c..%cbbb%cccc%c..%c..%c..%c.." c c c c c c c c
        let o = sprintf "%c" c
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_004() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "aaa%c%cbbb%c.%cccc%c.." c c c c c
        let o = sprintf "aaa%cbbb" c
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_005() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "aaa%c..%cbbb%cccc%c..%c..%c..%c.." c c c c c c c
        let o = ""
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_006() =
        let i1 = "aaa"
        let o = "aaa"
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_007() =
        let i1 = ""
        let o = ""
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.OptimizePathName_008() =
        let c = System.IO.Path.DirectorySeparatorChar
        let i1 = sprintf "aaa%c..%c..%cbbb%ceee" c c c c
        let o = sprintf "bbb%ceee" c
        Assert.True( Functions.OptimizePathName i1 = o )

    [<Fact>]
    member _.RetryAsync1_001() =
        let mutable cnt = 0
        let f() = Task.FromResult( Ok( 0 ) )
        let e ( _ : Exception ) : bool =
            cnt <- cnt + 1
            true

        let r = Functions.RetryAsync1 f e
        Assert.True(( r.Result = Ok( 0 ) ))
        Assert.True(( cnt = 0 ))

    [<Fact>]
    member _.RetryAsync1_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 1 then
                return Error( "" )
            else
                return Ok( 1 )
        }
        let e ( _ : Exception ) : bool =
            cnt2 <- cnt2 + 1
            true

        let r = Functions.RetryAsync1 f e
        Assert.True(( r.Result = Ok( 1 ) ))
        Assert.True(( cnt1 = 2 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.RetryAsync1_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 9 then
                return Error( "" )
            else
                return Ok( "abc" )
        }
        let e ( _ : Exception ) : bool =
            cnt2 <- cnt2 + 1
            true

        let r = Functions.RetryAsync1 f e
        Assert.True(( r.Result = Ok( "abc" ) ))
        Assert.True(( cnt1 = 10 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.RetryAsync1_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 10 then
                return Error( "" )
            else
                return Ok()
        }
        let e ( _ : Exception ) : bool =
            cnt2 <- cnt2 + 1
            true

        let r = Functions.RetryAsync1 f e
        Assert.True(( r.Result = Error("") ))
        Assert.True(( cnt1 = 10 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.RetryAsync1_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 1 then
                raise <| IOException("aaa")
            return Ok()
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                true
            | _ ->
                Assert.Fail __LINE__
                true

        let r = Functions.RetryAsync1 f e
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( x ) -> Assert.True(( x = "aaa" ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.RetryAsync1_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 1 then
                raise <| IOException("aaa")
            return Ok()
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                false
            | _ ->
                Assert.Fail __LINE__
                false

        let r = Functions.RetryAsync1 f e
        match r.Result with
        | Ok( _ ) -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        Assert.True(( cnt1 = 2 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.RetryAsync1_007() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 9 then
                raise <| IOException("aaa")
            return Ok()
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                false
            | _ ->
                Assert.Fail __LINE__
                false

        let r = Functions.RetryAsync1 f e
        match r.Result with
        | Ok( _ ) -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        Assert.True(( cnt1 = 10 ))
        Assert.True(( cnt2 = 9 ))

    [<Fact>]
    member _.RetryAsync1_008() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 10 then
                raise <| IOException("aaa")
            return Ok()
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                false
            | _ ->
                Assert.Fail __LINE__
                false

        let r = Functions.RetryAsync1 f e
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( x ) -> Assert.True(( x = "aaa" ))
        Assert.True(( cnt1 = 10 ))
        Assert.True(( cnt2 = 9 ))

    [<Fact>]
    member _.RetryAsync2_001() =
        let mutable cnt = 0
        let f() = Task.FromResult( 0 )
        let e ( _ : Exception ) : bool =
            cnt <- cnt + 1
            true

        let r = Functions.RetryAsync2 f e
        Assert.True(( r.Result = Ok( 0 ) ))
        Assert.True(( cnt = 0 ))

    [<Fact>]
    member _.RetryAsync2_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 1 then
                raise <| IOException("aaa")
            return "abc"
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                true
            | _ ->
                Assert.Fail __LINE__
                true

        let r = Functions.RetryAsync2 f e
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( x ) -> Assert.True(( x = "aaa" ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.RetryAsync3_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 1 then
                raise <| IOException("aaa")
            return "abc"
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                false
            | _ ->
                Assert.Fail __LINE__
                false

        let r = Functions.RetryAsync2 f e
        match r.Result with
        | Ok( x ) -> Assert.True(( x = "abc" ))
        | Error( _ ) -> Assert.Fail __LINE__
        Assert.True(( cnt1 = 2 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.RetryAsync2_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 9 then
                raise <| IOException("aaa")
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                false
            | _ ->
                Assert.Fail __LINE__
                false

        let r = Functions.RetryAsync2 f e
        match r.Result with
        | Ok( x ) -> Assert.True(( x = () ))
        | Error( _ ) -> Assert.Fail __LINE__
        Assert.True(( cnt1 = 10 ))
        Assert.True(( cnt2 = 9 ))

    [<Fact>]
    member _.RetryAsync2_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let f() = task {
            cnt1 <- cnt1 + 1
            if cnt1 <= 10 then
                raise <| IOException("aaa")
        }
        let e ( ar : Exception ) : bool =
            cnt2 <- cnt2 + 1
            match ar with
            | :? IOException ->
                false
            | _ ->
                Assert.Fail __LINE__
                false

        let r = Functions.RetryAsync2 f e
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( x ) -> Assert.True(( x = "aaa" ))
        Assert.True(( cnt1 = 10 ))
        Assert.True(( cnt2 = 9 ))

    [<Fact>]
    member _.GetOkValue_001() =
        Assert.True(( Functions.GetOkValue 1 ( Ok 0 ) = 0 ))

    [<Fact>]
    member _.GetOkValue_002() =
        Assert.True(( Functions.GetOkValue 1 ( Error 0 ) = 1 ))

    [<Fact>]
    member _.GetErrorValue_001() =
        Assert.True(( Functions.GetErrorValue 1 ( Ok 0 ) = 1 ))

    [<Fact>]
    member _.GetErrorValue_002() =
        Assert.True(( Functions.GetErrorValue 1 ( Error 0 ) = 0 ))

    [<Fact>]
    member _.CreateDirectoryAsync_001() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_CreateDirectoryAsync_001"
        GlbFunc.DeleteDir dname
        let r = Functions.CreateDirectoryAsync dname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        Directory.Delete dname

    [<Fact>]
    member _.CreateDirectoryAsync_002() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_CreateDirectoryAsync_002"
        File.WriteAllText( dname, "" )
        let r = Functions.CreateDirectoryAsync dname
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        File.Delete dname

    [<Fact>]
    member _.CreateDirectoryAsync_003() =
        let dname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let st = Environment.TickCount
        let r = Functions.CreateDirectoryAsync dname
        let et = Environment.TickCount
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        Assert.True(( et - st < 90 ))

    [<Fact>]
    member _.DeleteDirectoryAsync_001() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteDirectoryAsync_001"
        GlbFunc.DeleteDir dname
        let r = Functions.DeleteDirectoryAsync dname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__

    [<Fact>]
    member _.DeleteDirectoryAsync_002() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteDirectoryAsync_002"
        Directory.CreateDirectory dname |> ignore
        let r = Functions.DeleteDirectoryAsync dname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__

    [<Fact>]
    member _.DeleteDirectoryAsync_003() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteDirectoryAsync_003"
        File.WriteAllText( dname, "" )
        let r = Functions.DeleteDirectoryAsync dname
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        File.Delete dname

    [<Fact>]
    member _.DeleteDirectoryAsync_004() =
        let dname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let r = Functions.DeleteDirectoryAsync dname
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()

    [<Fact>]
    member _.DeleteDirectoryAsync_005() =
        let r = Functions.DeleteDirectoryAsync ""
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()

    [<Fact>]
    member _.DeleteDirectoryAsync_006() =
        let r = Functions.DeleteDirectoryAsync ( String.replicate 32769 "a" )
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()


    [<Fact>]
    member _.DeleteDirectoryRecursvelyAsync_001() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "DeleteDirectoryRecursvelyAsync_001"
        GlbFunc.DeleteDir dname
        let r = Functions.DeleteDirectoryRecursvelyAsync dname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__

    [<Fact>]
    member _.DeleteDirectoryRecursvelyAsync_002() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "DeleteDirectoryRecursvelyAsync_002"
        Directory.CreateDirectory dname |> ignore
        let r = Functions.DeleteDirectoryRecursvelyAsync dname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteDirectoryRecursvelyAsync_003() =
        let r = Functions.DeleteDirectoryRecursvelyAsync ""
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__

    [<Fact>]
    member _.DeleteDirectoryRecursvelyAsync_004() =
        let r = Functions.DeleteDirectoryRecursvelyAsync ( String.replicate 32769 "a" )
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__

    [<Fact>]
    member _.DeleteDirectoryRecursvelyAsync_005() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "DeleteDirectoryRecursvelyAsync_003"
        File.WriteAllText( dname, "a" )
        let r = Functions.DeleteDirectoryRecursvelyAsync dname
        match r.Result with
        | Ok() -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        Assert.True( File.Exists dname )
        File.Delete dname

    [<Fact>]
    member _.DeleteDirectoryRecursvelyAsync_006() =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "DeleteDirectoryRecursvelyAsync_005"
        let d1 = Functions.AppendPathName dname "a"
        let d1_1 = Functions.AppendPathName d1 "a01"
        let d1_2 = Functions.AppendPathName d1 "a02"
        let d1_3 = Functions.AppendPathName d1 "a03"
        let f1_1_1 = Functions.AppendPathName d1_1 "a01_01"
        let f1_1_2 = Functions.AppendPathName d1_1 "a01_02"
        let f1_1_3 = Functions.AppendPathName d1_1 "a01_03"
        let f1_2_1 = Functions.AppendPathName d1_2 "a02_01"
        let f1_2_2 = Functions.AppendPathName d1_2 "a02_02"
        let f1_2_3 = Functions.AppendPathName d1_2 "a02_03"
        let f1_3_1 = Functions.AppendPathName d1_3 "a03_01"
        let f1_3_2 = Functions.AppendPathName d1_3 "a03_02"
        let f1_3_3 = Functions.AppendPathName d1_3 "a03_03"
        Directory.CreateDirectory dname |> ignore
        Directory.CreateDirectory d1 |> ignore
        Directory.CreateDirectory d1 |> ignore
        Directory.CreateDirectory d1_1 |> ignore
        Directory.CreateDirectory d1_2 |> ignore
        Directory.CreateDirectory d1_3 |> ignore
        File.WriteAllText( f1_1_1, "a" )
        File.WriteAllText( f1_1_2, "a" )
        File.WriteAllText( f1_1_3, "a" )
        File.WriteAllText( f1_2_1, "a" )
        File.WriteAllText( f1_2_2, "a" )
        File.WriteAllText( f1_2_3, "a" )
        File.WriteAllText( f1_3_1, "a" )
        File.WriteAllText( f1_3_2, "a" )
        File.WriteAllText( f1_3_3, "a" )

        let r = Functions.DeleteDirectoryRecursvelyAsync dname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        Assert.False( Directory.Exists dname )


    [<Fact>]
    member _.ReadAllBytesAsync_001() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_ReadAllBytesAsync_001"
        let tdata = [| 0uy; 1uy; 2uy; 3uy; |]
        File.WriteAllBytes( fname, tdata )
        let r = Functions.ReadAllBytesAsync fname
        match r.Result with
        | Ok( v ) -> Assert.True(( v = tdata ))
        | Error( _ ) -> Assert.Fail __LINE__
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.ReadAllBytesAsync_002() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_ReadAllBytesAsync_002"
        let r = Functions.ReadAllBytesAsync fname
        match r.Result with
        | Ok( v ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()

    [<Fact>]
    member _.ReadAllBytesAsync_003() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_ReadAllBytesAsync_003"
        let s = File.OpenWrite fname
        let r = Functions.ReadAllBytesAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        s.Close()
        s.Dispose()
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.ReadAllBytesAsync_004() =
        let fname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let r = Functions.ReadAllBytesAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        File.OpenWrite

    [<Fact>]
    member _.ReadAllTextAsync_001() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_ReadAllTextAsync_001"
        let tdata = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        File.WriteAllText( fname, tdata )
        let r = Functions.ReadAllTextAsync fname
        match r.Result with
        | Ok( v ) -> Assert.True(( v = tdata ))
        | Error( _ ) -> Assert.Fail __LINE__
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.ReadAllTextAsync_002() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_ReadAllTextAsync_002"
        let r = Functions.ReadAllTextAsync fname
        match r.Result with
        | Ok( v ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()

    [<Fact>]
    member _.ReadAllTextAsync_003() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_ReadAllTextAsync_003"
        let s = File.OpenWrite fname
        let r = Functions.ReadAllTextAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        s.Close()
        s.Dispose()
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.ReadAllTextAsync_004() =
        let fname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let r = Functions.ReadAllTextAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        File.OpenWrite

    [<Fact>]
    member _.WriteAllBytesAsync_001() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_WriteAllBytesAsync_001"
        let tdata = [| 0uy; 1uy; 2uy; 3uy; |]
        let r = Functions.WriteAllBytesAsync fname tdata
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        let wb = File.ReadAllBytes( fname )
        Assert.True(( wb = tdata ))
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.WriteAllBytesAsync_002() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_WriteAllBytesAsync_002"
        let tdata = [| 99uy; 99uy; 99uy; 99uy; |]
        File.WriteAllBytes( fname, [| 0uy .. 255uy |] )
        let r = Functions.WriteAllBytesAsync fname tdata
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        let wb = File.ReadAllBytes( fname )
        Assert.True(( wb = tdata ))
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.WriteAllBytesAsync_003() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_WriteAllBytesAsync_003"
        let tdata = [| 99uy; 99uy; 99uy; 99uy; |]
        let s = File.OpenWrite fname
        let r = Functions.WriteAllBytesAsync fname tdata
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        s.Close()
        s.Dispose()
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.WriteAllBytesAsync_004() =
        let fname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let tdata = [| 99uy; 99uy; 99uy; 99uy; |]
        let r = Functions.WriteAllBytesAsync fname tdata
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        File.OpenWrite

    [<Fact>]
    member _.WriteAllTextAsync_001() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_WriteAllTextAsync_001"
        let tdata = "aaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
        let r = Functions.WriteAllTextAsync fname tdata
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        let wb = File.ReadAllText( fname )
        Assert.True(( wb = tdata ))
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.WriteAllTextAsync_002() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_WriteAllTextAsync_002"
        let tdata = "aaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
        File.WriteAllBytes( fname, [| 0uy .. 255uy |] )
        let r = Functions.WriteAllTextAsync fname tdata
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        let wb = File.ReadAllText( fname )
        Assert.True(( wb = tdata ))
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.WriteAllTextAsync_003() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_WriteAllTextAsync_003"
        let tdata = "aaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
        let s = File.OpenWrite fname
        let r = Functions.WriteAllTextAsync fname tdata
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        s.Close()
        s.Dispose()
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.WriteAllTextAsync_004() =
        let fname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let tdata = "aaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
        let r = Functions.WriteAllTextAsync fname tdata
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        File.OpenWrite

    [<Fact>]
    member _.DeleteFileAsync_001() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteFileAsync_001"
        File.WriteAllText( fname, "aaa" )
        let r = Functions.DeleteFileAsync fname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.DeleteFileAsync_002() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteFileAsync_002"
        GlbFunc.DeleteFile fname
        let r = Functions.DeleteFileAsync fname
        match r.Result with
        | Ok() -> ()
        | Error( _ ) -> Assert.Fail __LINE__

    [<Fact>]
    member _.DeleteFileAsync_003() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteFileAsync_003"
        File.WriteAllText( fname, "aaa" )
        let s = File.OpenWrite fname
        let r = Functions.DeleteFileAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        s.Close()
        s.Dispose()
        GlbFunc.DeleteFile fname

    [<Fact>]
    member _.DeleteFileAsync_004() =
        let fname = Functions.AppendPathName ( Path.GetTempPath() ) "Functions_DeleteFileAsync_004"
        Directory.CreateDirectory fname |> ignore
        let r = Functions.DeleteFileAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()
        GlbFunc.DeleteDir fname

    [<Fact>]
    member _.DeleteFileAsync_005() =
        let fname = ( Path.GetTempPath() ) + string( Path.PathSeparator ) + ( String.replicate 65535 "a" )
        let r = Functions.DeleteFileAsync fname
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()

    [<Fact>]
    member _.DeleteFileAsync_006() =
        let r = Functions.DeleteFileAsync ""
        match r.Result with
        | Ok( _ ) -> Assert.Fail __LINE__
        | Error( _ ) -> ()

    [<Fact>]
    member _.loopAsyncWithState_001() =
        let f2 ( a : int ) = Task.FromResult struct( false, 0 )
        let r = ( Functions.loopAsyncWithState f2 0 ).Result
        Assert.True(( r = 0 ))

    [<Fact>]
    member _.loopAsyncWithState_002() =
        let f2 ( a : int ) =
            Task.FromResult( if a = 3 then struct( false, a ) else struct( true, a + 1 ) )
        let r = ( Functions.loopAsyncWithState f2 0 ).Result
        Assert.True(( r = 3 ))

    [<Fact>]
    member _.loopAsyncWithState_003() =
        let f2 ( a : struct {| a1 : int; a2 : int list |} ) =
            Task.FromResult( struct( ( a.a1 < 10 ), struct {| a1 = a.a1 + 1; a2 = a.a1 :: a.a2 |} ) )
        let r = ( Functions.loopAsyncWithState f2 ( struct {| a1 = 0; a2 = [] |} ) ).Result
        Assert.True(( r.a1 = 11 ))
        Assert.True(( r.a2 = [ 10; 9; 8; 7; 6; 5; 4; 3; 2; 1; 0 ] ))

    [<Fact>]
    member _.loopAsyncWithState_004() =
        let f2 ( a : int, b : int ) =
            Task.FromResult struct( ( a < 5 ), ( a + 1, b + 2 ) )
        let r1, r2 = ( Functions.loopAsyncWithState f2 ( 0, 1 ) ).Result
        Assert.True(( r1 = 6 ))
        Assert.True(( r2 = 13 ))

    [<Fact>]
    member _.loopAsyncWithState_005() =
        let f2 ( a : int ) =
            task {
                raise <| Exception "abcd"
                return struct( false, 0 )
            }
        try
            let _ = ( Functions.loopAsyncWithState f2 0 ).Result
            Assert.Fail("")
        with
        | _ as x ->
            Assert.True(( x.InnerException.Message = "abcd" ))

    [<Fact>]
    member _.loopAsyncWithState_006() =
        let mutable s = 0
        let f2 ( a : int ) =
            task {
                s <- s + 1
                if a = 3 then
                    raise <| Exception "xxxx"
                return struct( true, ( a + 1 ) )
            }
        try
            let _ = ( Functions.loopAsyncWithState f2 0 ).Result
            Assert.Fail("")
        with
        | _ as x ->
            Assert.True(( x.InnerException.Message = "xxxx" ))
        Assert.True(( s = 4 ))

    [<Fact>]
    member _.loopAsync_001() =
        let mutable s : int = 0
        let f2 () =
            task {
                s <- s + 1
                return false
            }
        ( Functions.loopAsync f2 ).Wait()
        Assert.True(( s = 1 ))

    [<Fact>]
    member _.loopAsync_002() =
        let mutable s : int = 0
        let f2 () =
            task {
                s <- s + 1
                return ( s < 3 )
            }
        ( Functions.loopAsync f2 ).Wait()
        Assert.True(( s = 3 ))

    [<Fact>]
    member _.loopAsync_003() =
        let f2 () =
            task {
                raise <| Exception "abcd"
                return false
            }
        try
            ( Functions.loopAsync f2 ).Wait()
            Assert.Fail __LINE__
        with
        | _ as x ->
            Assert.True(( x.InnerException.Message = "abcd" ))

    [<Fact>]
    member _.loopAsync_004() =
        let mutable s : int = 0
        let f2 () =
            task {
                s <- s + 1
                if s = 3 then
                    raise <| Exception "xyz"
                return ( s < 3 )
            }
        try
            ( Functions.loopAsync f2 ).Wait()
            Assert.Fail __LINE__
        with
        | _ as x ->
            Assert.True(( x.InnerException.Message = "xyz" ))
        Assert.True(( s = 3 ))

    [<Fact>]
    member _.RunTaskSynchronously_001() =
        let t : Task<int> = Task.FromResult 99
        let a = Functions.RunTaskSynchronously t
        Assert.True(( a = 99 ))

    [<Fact>]
    member _.RunTaskSynchronously_002() =
        let t : Task<int> =
            task {
                raise <| IOException "abc"
                return 99
            }
        try
            Functions.RunTaskSynchronously t
            |> ignore
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            Assert.True(( x.Message = "abc" ))

    [<Fact>]
    member _.RunTaskSynchronously_003() =
        let mutable s2 = 0
        let t : Task =
            task {
                do! Task.Delay 10
                s2 <- 1
            }
        Functions.RunTaskSynchronously t
        Assert.True(( s2 = 1 ))

    [<Fact>]
    member _.RunTaskSynchronously_004() =
        let t : Task =
            task {
                raise <| IOException "xyz"
            }
        try
            Functions.RunTaskSynchronously t
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            Assert.True(( x.Message = "xyz" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.StartTask_001() =
        let w1 = new SemaphoreSlim( 1 )
        let w2 = new SemaphoreSlim( 1 )
        w1.Wait()
        w2.Wait()
        fun () -> task {
            do! w1.WaitAsync()
            do! Task.Delay 10
            w2.Release() |> ignore
        }
        |> Functions.StartTask
        w1.Release() |> ignore
        w2.Wait()

    [<Fact>]
    member _.StartTask_002() =
        let w1 = new SemaphoreSlim( 1 )
        let w2 = new SemaphoreSlim( 1 )
        w1.Wait()
        w2.Wait()
        fun () -> task {
            do! w1.WaitAsync()
            do! Task.Delay 10
            w2.Release() |> ignore
            return ()
        }
        |> Functions.StartTask
        w1.Release() |> ignore
        w2.Wait()


    [<Fact>]
    member _.ReceiveBytesFromNetwork_001() =
        let s, c = GlbFunc.GetNetConn()
        task {
            let! v = Functions.ReceiveBytesFromNetwork s 0
            Assert.True(( v.Length = 0 ))
        }
        |> Functions.RunTaskSynchronously
        GlbFunc.ClosePorts [| c; s; |]

    [<Fact>]
    member _.ReceiveBytesFromNetwork_002() =
        let s, c = GlbFunc.GetNetConn()
        let ra = new Random()
        let rv = [| 0uy .. 255uy |]
        [|
            fun () -> task {
                let! v = Functions.ReceiveBytesFromNetwork s rv.Length
                Assert.True(( v = rv ))
            };
            fun () -> task {
                c.Write( rv, 0, rv.Length )
                c.Flush()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| c; s; |]

    [<Fact>]
    member _.ReceiveBytesFromNetwork_003() =
        let s, c = GlbFunc.GetNetConn()
        let ra = new Random()
        let rv = Array.zeroCreate<byte>( 16384 )
        ra.NextBytes rv
        [|
            fun () -> task {
                let! v = Functions.ReceiveBytesFromNetwork s rv.Length
                Assert.True(( v = rv ))
            };
            fun () -> task {
                c.Write( rv, 0, 4096 )
                c.Flush()
                do! Task.Delay 5
                c.Write( rv, 4096, 4096 )
                c.Flush()
                do! Task.Delay 5
                c.Write( rv, 8192, 4096 )
                c.Flush()
                do! Task.Delay 5
                c.Write( rv, 12288, 4096 )
                c.Flush()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| c; s; |]

    [<Fact>]
    member _.ReceiveBytesFromNetwork_004() =
        let s, c = GlbFunc.GetNetConn()
        let ra = new Random()
        let rv = [| 0uy .. 255uy |]
        [|
            fun () -> task {
                try
                    let! v = Functions.ReceiveBytesFromNetwork s ( rv.Length * 2 )
                    Assert.Fail __LINE__
                with
                | :? IOException as x ->
                    Assert.True(( x.Message.StartsWith "Connection closed." ))
            };
            fun () -> task {
                c.Write( rv, 0, rv.Length - 1 )
                c.Flush()
                c.Close()
                c.Dispose()
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| c; s; |]

    [<Fact>]
    member _.RunTaskInSequencial_001() =
        let mutable s = 0
        let v =
            [|
                for i = 0 to 10 do
                    yield fun () -> task {
                        do! Task.Delay 1
                        Assert.True(( s = i ))
                        s <- s + 1
                        return s
                    };
            |]
            |> Functions.RunTaskInSequencial
            |> Functions.RunTaskSynchronously
        Assert.True(( s = 11 ))
        Assert.True(( v = [| 1 .. 11 |] ))

    [<Fact>]
    member _.RunTaskInSequencial_002() =
        let v =
            Array.empty
            |> Functions.RunTaskInSequencial
            |> Functions.RunTaskSynchronously
        Assert.True(( v = Array.empty ))

    [<Fact>]
    member _.RunTaskInSequencial_003() =
        let mutable s = 0
        try
            [|
                fun () -> task {
                    do! Task.Delay 1
                    s <- 1
                    return 1;
                };
                fun () -> task {
                    do! Task.Delay 1
                    s <- 2
                    return 2;
                };
                fun () -> task {
                    do! Task.Delay 1
                    s <- 3
                    raise <| IOException( "aaa" )
                    return 3;
                };
                fun () -> task {
                    do! Task.Delay 1
                    s <- 4
                    return 4;
                };
            |]
            |> Functions.RunTaskInSequencial
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            Assert.True(( x.Message = "aaa" ))
        Assert.True(( s = 3 ))

    [<Fact>]
    member _.RunTaskInPallalel_001() =
        let mutable s = 0
        let v =
            [|
                for i = 0 to 10 do
                    yield fun () -> task {
                        do! Task.Delay 1
                        let w = Interlocked.Increment( &s )
                        return w
                    };
            |]
            |> Functions.RunTaskInPallalel
            |> Functions.RunTaskSynchronously
        Assert.True(( s = 11 ))
        Assert.True(( ( Array.sort v ) = [| 1 .. 11 |] ))

    [<Fact>]
    member _.RunTaskInPallalel_002() =
        let v =
            Array.empty
            |> Functions.RunTaskInPallalel
            |> Functions.RunTaskSynchronously
        Assert.True(( v = Array.empty ))

    [<Fact>]
    member _.RunTaskInPallalel_003() =
        let w1 = new SemaphoreSlim( 1 )
        let w2 = new SemaphoreSlim( 1 )
        w1.Wait()
        w2.Wait()
        let mutable s1 = 0
        let mutable s2 = 0
        [|
            fun () -> task {
                do! w1.WaitAsync()
                Assert.True(( s1 = 1 ))
                s2 <- 2
                w2.Release() |> ignore
            };
            fun () -> task {
                do! Task.Delay 1
                s1 <- 1
                w1.Release() |> ignore
                do! w2.WaitAsync()
                Assert.True(( s2 = 2 ))
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

    [<Fact>]
    member _.RunTaskInPallalel_004() =
        let mutable s = 0
        try
            [|
                fun () -> task {
                    do! Task.Delay 1
                    s <- 3
                    raise <| IOException( "aaa" )
                    return 3;
                };
            |]
            |> Functions.RunTaskInPallalel
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            Assert.True(( x.Message = "aaa" ))
        Assert.True(( s = 3 ))

    [<Fact>]
    member _.PairByIndex_001() =
        let v : ( int * int ) seq [] = Array.empty
        let r = Functions.PairByIndex v fst snd
        Assert.True(( Seq.length r = 0 ))

    [<Fact>]
    member _.PairByIndex_002() =
        let s1 : ( int * int ) seq = Array.empty
        let v = [| s1 |]
        let r = Functions.PairByIndex v fst snd
        Assert.True(( Seq.length r = 0 ))

    [<Fact>]
    member _.PairByIndex_003() =
        let s1 = seq{ ( 1, 10 ) }
        let v = [| s1 |]
        let r = Functions.PairByIndex v fst snd |> Seq.toArray
        Assert.True(( Seq.length r = 1 ))
        Assert.True(( r.[0] = ( 1, [| Some 10 |] ) ))

    [<Fact>]
    member _.PairByIndex_004() =
        let s1 = seq{ ( 2, 20 ); ( 1, 10 ) }
        let v = [| s1 |]
        let r = Functions.PairByIndex v fst snd |> Seq.toArray
        Assert.True(( Seq.length r = 2 ))
        Assert.True(( r.[0] = ( 1, [| Some 10 |] ) ))
        Assert.True(( r.[1] = ( 2, [| Some 20 |] ) ))

    [<Fact>]
    member _.PairByIndex_005() =
        let s1 = seq{ ( 2, 20 ); ( 1, 10 ) }
        let s2 = seq{ ( 3, -30 ); ( 4, -40 ) }
        let v = [| s1; s2 |]
        let r = Functions.PairByIndex v fst snd |> Seq.toArray
        Assert.True(( Seq.length r = 4 ))
        Assert.True(( r.[0] = ( 1, [| Some 10; None |] ) ))
        Assert.True(( r.[1] = ( 2, [| Some 20; None |] ) ))
        Assert.True(( r.[2] = ( 3, [| None; Some -30  |] ) ))
        Assert.True(( r.[3] = ( 4, [| None; Some -40 |] ) ))

    [<Fact>]
    member _.PairByIndex_006() =
        let s1 = seq{ ( 2, 20 ); ( 1, 10 ); ( 3, 30 ) }
        let s2 = seq{ ( 3, -30 ); ( 4, -40 ); ( 5, -50 ) }
        let s3 = seq{ ( 8, 800 ); ( 2, 200 ); ( 0, 0 ); ( 3, 300 ) }
        let v = [| s1; s2; s3 |]
        let r = Functions.PairByIndex v fst snd |> Seq.toArray
        Assert.True(( Seq.length r = 7 ))
        Assert.True(( r.[0] = ( 0, [| None;    None;     Some 0   |] ) ))
        Assert.True(( r.[1] = ( 1, [| Some 10; None;     None     |] ) ))
        Assert.True(( r.[2] = ( 2, [| Some 20; None;     Some 200 |] ) ))
        Assert.True(( r.[3] = ( 3, [| Some 30; Some -30; Some 300 |] ) ))
        Assert.True(( r.[4] = ( 4, [| None;    Some -40; None     |] ) ))
        Assert.True(( r.[5] = ( 5, [| None;    Some -50; None     |] ) ))
        Assert.True(( r.[6] = ( 8, [| None;    None;     Some 800 |] ) ))

    [<Fact>]
    member _.GenUniqueNumber_001() =
        let v = Array.empty
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 0uy ))

    [<Fact>]
    member _.GenUniqueNumber_002() =
        let v = [| 0uy; |]
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 1uy ))

    [<Fact>]
    member _.GenUniqueNumber_003() =
        let v = [| 255uy; |]
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 0uy ))

    [<Fact>]
    member _.GenUniqueNumber_004() =
        let v = [| 0uy; 254uy; |]
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 255uy ))

    [<Fact>]
    member _.GenUniqueNumber_005() =
        let v = [| 0uy; 255uy; |]
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 1uy ))

    [<Fact>]
    member _.GenUniqueNumber_006() =
        let v = [|
            for i = 0uy to 253uy do
                yield i;
            yield 255uy;
        |]
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 254uy ))

    [<Fact>]
    member _.GenUniqueNumber_007() =
        let v = [| 0uy .. 255uy |];
        let r = Functions.GenUniqueNumber ( (+) 1uy ) 0uy v
        Assert.True(( r = 0uy ))

    [<Fact>]
    member _.CheckAccessRange_001() =
        Assert.True(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 0UL ) 0UL 0UL 1UL ))

    [<Fact>]
    member _.CheckAccessRange_002() =
        Assert.True(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 0UL ) 0UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_003() =
        Assert.True(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 0UL ) 100UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_004() =
        Assert.False(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 0UL ) 101UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_005() =
        Assert.True(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 9UL ) 10UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_006() =
        Assert.False(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 9UL ) 11UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_007() =
        Assert.True(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 10UL ) 0UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_008() =
        Assert.False(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 10UL ) 1UL 10UL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_009() =
        Assert.True(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 0xFFFFFFFFFFFFFFFEUL ) 10UL 0xFFFFFFFFFFFFFFFFUL 10UL ))

    [<Fact>]
    member _.CheckAccessRange_010() =
        Assert.False(( Functions.CheckAccessRange ( blkcnt_me.ofUInt64 0xFFFFFFFFFFFFFFFEUL ) 11UL 0xFFFFFFFFFFFFFFFFUL 10UL ))

    [<Fact>]
    member _.CompareMultiLevelKey_001() =
        let r = Functions.CompareMultiLevelKey Array.empty Array.empty
        Assert.True(( r = 0 ))

    [<Fact>]
    member _.CompareMultiLevelKey_002() =
        let r = Functions.CompareMultiLevelKey [| "a" |] Array.empty
        Assert.True(( r = 1 ))

    [<Fact>]
    member _.CompareMultiLevelKey_003() =
        let r = Functions.CompareMultiLevelKey Array.empty [| "a" |]
        Assert.True(( r = -1 ))

    [<Fact>]
    member _.CompareMultiLevelKey_004() =
        let r = Functions.CompareMultiLevelKey [| "b" |] [| "a" |]
        Assert.True(( r = 1 ))

    [<Fact>]
    member _.CompareMultiLevelKey_005() =
        let r = Functions.CompareMultiLevelKey [| "a" |] [| "b" |]
        Assert.True(( r = -1 ))

    [<Fact>]
    member _.CompareMultiLevelKey_006() =
        let r = Functions.CompareMultiLevelKey [| "a" |] [| "a" |]
        Assert.True(( r = 0 ))

    [<Fact>]
    member _.CompareMultiLevelKey_007() =
        let r = Functions.CompareMultiLevelKey [| "x"; "y"; "z"; "a"; "b" |] [| "x"; "y"; "z"; ""; "b" |]
        Assert.True(( r = 1 ))

    [<Fact>]
    member _.SearchAndConvert_001() =
        let d = new Dictionary< int, int >()
        let r = Functions.SearchAndConvert d 0 id -1
        Assert.True(( r = -1 ))

    [<Fact>]
    member _.SearchAndConvert_002() =
        let d = new Dictionary< int, int >()
        d.Add( 1, 1 )
        let r = Functions.SearchAndConvert d 0 id -1
        Assert.True(( r = -1 ))

    [<Fact>]
    member _.SearchAndConvert_003() =
        let d = new Dictionary< int, int >()
        d.Add( 1, 1 )
        let r = Functions.SearchAndConvert d 1 ( fun i -> i / 0 ) -1
        Assert.True(( r = -1 ))

    [<Fact>]
    member _.SearchAndConvert_004() =
        let d = new Dictionary< int, int >()
        d.Add( 1, 1 )
        let r = Functions.SearchAndConvert d 1 ( fun i -> i + 1 ) 2
        Assert.True(( r = 2 ))

    [<Fact>]
    member _.DivideRespDataSegment_001() =
        let r = Functions.DivideRespDataSegment 0u 0u 0u 0u
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.DivideRespDataSegment_002() =
        let r = Functions.DivideRespDataSegment 0u 50u 100u 100u
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = struct( 0u, 50u, true ) ))

    [<Fact>]
    member _.DivideRespDataSegment_003() =
        let r = Functions.DivideRespDataSegment 0u 100u 100u 100u
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = struct( 0u, 100u, true ) ))

    [<Fact>]
    member _.DivideRespDataSegment_004() =
        let r = Functions.DivideRespDataSegment 0u 200u 100u 100u
        Assert.True(( r.Length = 2 ))
        Assert.True(( r.[0] = struct( 0u, 100u, true ) ))
        Assert.True(( r.[1] = struct( 100u, 100u, true ) ))

    [<Fact>]
    member _.DivideRespDataSegment_005() =
        let r = Functions.DivideRespDataSegment 0u 300u 200u 100u
        Assert.True(( r.Length = 3 ))
        Assert.True(( r.[0] = struct( 0u, 100u, false ) ))
        Assert.True(( r.[1] = struct( 100u, 100u, true ) ))
        Assert.True(( r.[2] = struct( 200u, 100u, true ) ))

    [<Fact>]
    member _.DivideRespDataSegment_006() =
        let r = Functions.DivideRespDataSegment 0u 300u 100u 200u
        Assert.True(( r.Length = 3 ))
        Assert.True(( r.[0] = struct( 0u, 100u, true ) ))
        Assert.True(( r.[1] = struct( 100u, 100u, true ) ))
        Assert.True(( r.[2] = struct( 200u, 100u, true ) ))

    [<Fact>]
    member _.DivideRespDataSegment_007() =
        let r = Functions.DivideRespDataSegment 0u 300u 120u 80u
        Assert.True(( r.Length = 5 ))
        Assert.True(( r.[0] = struct( 0u, 80u, false ) ))
        Assert.True(( r.[1] = struct( 80u, 40u, true ) ))
        Assert.True(( r.[2] = struct( 120u, 80u, false ) ))
        Assert.True(( r.[3] = struct( 200u, 40u, true ) ))
        Assert.True(( r.[4] = struct( 240u, 60u, true ) ))

    [<Fact>]
    member _.DivideRespDataSegment_008() =
        let r = Functions.DivideRespDataSegment 1000u 300u 120u 80u
        Assert.True(( r.Length = 5 ))
        Assert.True(( r.[0] = struct( 1000u, 80u, false ) ))
        Assert.True(( r.[1] = struct( 1080u, 40u, true ) ))
        Assert.True(( r.[2] = struct( 1120u, 80u, false ) ))
        Assert.True(( r.[3] = struct( 1200u, 40u, true ) ))
        Assert.True(( r.[4] = struct( 1240u, 60u, true ) ))

    [<Fact>]
    member _.IPAddressToString_001() =
        let ip = IPAddress.Parse "192.168.1.1"
        let r = Functions.IPAddressToString ip "abc"
        Assert.True(( r = "192.168.1.1" ))

    [<Fact>]
    member _.IPAddressToString_002() =
        let ip = IPAddress.Parse "::FFFF:192.168.111.222"
        let r = Functions.IPAddressToString ip "abc"
        Assert.True(( r = "192.168.111.222" ))

    [<Fact>]
    member _.IPAddressToString_003() =
        let ip = IPAddress.Parse "1111:2222:3333:4444:5555:6666:7777:8888"
        let r = Functions.IPAddressToString ip "abc"
        Assert.True(( r = "[1111:2222:3333:4444:5555:6666:7777:8888]" ))

    [<Fact>]
    member _.IPAddressToString_004() =
        let ip = IPAddress.Parse "::1"
        let r = Functions.IPAddressToString ip "abc"
        Assert.True(( r = "[::1]" ))

    [<Fact>]
    member _.ConfiguredNetPortAddressToTargetAddressStr_001() =
        let r = Functions.ConfiguredNetPortAddressToTargetAddressStr "abc" ValueNone
        Assert.True(( r = "abc" ))

    [<Fact>]
    member _.ConfiguredNetPortAddressToTargetAddressStr_002() =
        let r = Functions.ConfiguredNetPortAddressToTargetAddressStr "192.168.1.1" ValueNone
        Assert.True(( r = "192.168.1.1" ))

    [<Fact>]
    member _.ConfiguredNetPortAddressToTargetAddressStr_003() =
        let ip = IPAddress.Parse "192.168.1.1"
        let r = Functions.ConfiguredNetPortAddressToTargetAddressStr "" ( ValueSome ip )
        Assert.True(( r = "192.168.1.1" ))

    [<Fact>]
    member _.ConfiguredNetPortAddressToTargetAddressStr_004() =
        let r = Functions.ConfiguredNetPortAddressToTargetAddressStr "" ValueNone
        Assert.True(( r = "" ))
