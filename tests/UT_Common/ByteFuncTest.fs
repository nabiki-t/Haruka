//=============================================================================
// Haruka Software Storage.
// ByteFuncTest.fs : Test cases for global functions defined at ByteFunc.fs.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

type ByteFunc_Test () =

    [<Theory>]
    [<InlineData( 0, 0u )>]
    [<InlineData( 15, 0u )>]
    [<InlineData( 16, 1u )>]
    member _.ReadGuid_001 ( vlen : int32 ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadGuid v pos
            |> ignore
        )
        |> ignore

    [<Theory>]
    [<InlineData( 16, 0u, "03020100-0504-0706-0809-0a0b0c0d0e0f" )>]
    [<InlineData( 17, 1u, "04030201-0605-0807-090a-0b0c0d0e0f10" )>]
    member _.ReadGuid_002 ( vlen : int32 ) ( pos : uint32 ) ( exstr : string ) =
        let v = Array.zeroCreate<byte> vlen
        for i = 0 to vlen - 1 do
            v.[i] <- byte i
        let g = ByteFunc.ReadGuid v pos
        Assert.StrictEqual( Guid( exstr ), g )

    [<Theory>]
    [<InlineData( 0, 0u )>]
    [<InlineData( 15, 0u )>]
    [<InlineData( 16, 1u )>]
    member _.WriteGuid_001 ( vlen : int32 ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteGuid v pos ( Guid() )
            |> ignore
        )
        |> ignore

    [<Theory>]
    [<InlineData( 16, 0u, "03020100-0504-0706-0809-0a0b0c0d0e0f" )>]
    [<InlineData( 17, 1u, "04030201-0605-0807-090a-0b0c0d0e0f10" )>]
    member _.WriteGuid_002 ( vlen : int32 ) ( pos : uint32 ) ( gstr : string ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteGuid v pos ( Guid( gstr ) )

        for i = int32 pos to int32 pos + 15 do
            Assert.StrictEqual( byte i, v.[i] )

    [<Fact>]
    member _.ReadS16BE_001() =
        Assert.True( ByteFunc.ReadS16BE [| 0uy .. 16uy |] 1u = 0x0102s )

    [<Fact>]
    member _.ReadS16BEPB_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( ByteFunc.ReadS16BEPB p 1u = 0x0102s )

    [<Fact>]
    member _.ReadU16BE_001() =
        Assert.True( ByteFunc.ReadU16BE [| 0uy .. 16uy |] 2u = 0x0203us )

    [<Fact>]
    member _.ReadU16BEPB_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( ByteFunc.ReadU16BEPB p 2u = 0x0203us )

    [<Fact>]
    member _.ReadS32BE_001() =
        Assert.True( ByteFunc.ReadS32BE [| 0uy .. 16uy |] 3u = 0x03040506 )

    [<Fact>]
    member _.ReadS32BEPB_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( ByteFunc.ReadS32BEPB p 3u = 0x03040506 )

    [<Fact>]
    member _.ReadU32BE_001() =
        Assert.True( ByteFunc.ReadU32BE [| 0uy .. 16uy |] 4u = 0x04050607u )

    [<Fact>]
    member _.ReadU32BEPB_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 8 )
        Assert.True( ByteFunc.ReadU32BEPB p 4u = 0x04050607u )

    [<Fact>]
    member _.ReadS64BE_001() =
        Assert.True( ByteFunc.ReadS64BE [| 0uy .. 16uy |] 5u = 0x05060708090A0B0CL )

    [<Fact>]
    member _.ReadS64BEPB_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 15 )
        Assert.True( ByteFunc.ReadS64BEPB p 5u = 0x05060708090A0B0CL )

    [<Fact>]
    member _.ReadU64BE_001() =
        Assert.True( ByteFunc.ReadU64BE [| 0uy .. 16uy |] 6u = 0x060708090A0B0C0DUL )

    [<Fact>]
    member _.ReadU64BEPB_001() =
        let p = PooledBuffer.Rent( [| 0uy .. 16uy |], 15 )
        Assert.True( ByteFunc.ReadU64BEPB p 6u = 0x060708090A0B0C0DUL )

    [<Fact>]
    member _.WriteS16BE_001() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        ByteFunc.WriteS16BE wbuf 0u 0xF1F2s
        Assert.True( ( wbuf = [| 0xF1uy; 0xF2uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.WriteU16BE_002() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        ByteFunc.WriteU16BE wbuf 0u 0xF2F3us
        Assert.True( ( wbuf = [| 0xF2uy; 0xF3uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.WriteS32BE_003() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        ByteFunc.WriteS32BE wbuf 0u 0xF3F4F5F6
        Assert.True( ( wbuf = [| 0xF3uy; 0xF4uy; 0xF5uy; 0xF6uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.WriteU32BE_004() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        ByteFunc.WriteU32BE wbuf 0u 0xF4F5F6F7u
        Assert.True( ( wbuf = [| 0xF4uy; 0xF5uy; 0xF6uy; 0xF7uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ) )

    [<Fact>]
    member _.WriteS64BE_005() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        ByteFunc.WriteS64BE wbuf 0u 0xF5F6F7F8F9FAFBFCL
        Assert.True( ( wbuf = [| 0xF5uy; 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; |] ) )

    [<Fact>]
    member _.WriteU64BE_006() =
        let wbuf : byte[] = Array.zeroCreate( 8 )
        ByteFunc.WriteU64BE wbuf 0u 0xF6F7F8F9FAFBFCFDUL
        Assert.True( ( wbuf = [| 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; 0xFDuy; |] ) )

    [<Fact>]
    member _.S16ToNVBE_007() =
        Assert.True( ( ByteFunc.S16ToNVBE 0xF1F2s = [| 0xF1uy; 0xF2uy; |] ) )

    [<Fact>]
    member _.U16ToNVBE_008() =
        Assert.True( ( ByteFunc.U16ToNVBE 0xF2F3us = [| 0xF2uy; 0xF3uy; |] ) )

    [<Fact>]
    member _.S32ToNVBE_009() =
        Assert.True( ( ByteFunc.S32ToNVBE 0xF3F4F5F6 = [| 0xF3uy; 0xF4uy; 0xF5uy; 0xF6uy; |] ) )

    [<Fact>]
    member _.U32ToNVBE_010() =
        Assert.True( ( ByteFunc.U32ToNVBE 0xF4F5F6F7u = [| 0xF4uy; 0xF5uy; 0xF6uy; 0xF7uy; |] ) )

    [<Fact>]
    member _.S64ToNVBE_011() =
        Assert.True( ( ByteFunc.S64ToNVBE 0xF5F6F7F8F9FAFBFCL = [| 0xF5uy; 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; |] ) )

    [<Fact>]
    member _.U64ToNVBE_012() =
        Assert.True( ( ByteFunc.U64ToNVBE 0xF6F7F8F9FAFBFCFDUL = [| 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; 0xFDuy; |] ) )
