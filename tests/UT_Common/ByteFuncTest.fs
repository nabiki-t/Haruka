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
    [<InlineData( 16, 0xFFFFFFEFu )>]
    [<InlineData( 16, 0xFFFFFFF0u )>]
    [<InlineData( 16, 0xFFFFFFF1u )>]
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
    [<InlineData( 16, 0xFFFFFFEFu )>]
    [<InlineData( 16, 0xFFFFFFF0u )>]
    [<InlineData( 16, 0xFFFFFFF1u )>]
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

    [<Theory>]
    [<InlineData( 2, 0, 0x0001s )>]
    [<InlineData( 16, 14, 0x0E0Fs )>]
    member _.ReadS16BE_001 ( vlen : int ) ( pos : uint32 ) ( exval : int16 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadS16BE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadS16BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS16BE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0001s )>]
    [<InlineData( 16, 14, 0x0E0Fs )>]
    member _.ReadS16BEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : int16 ) =
        let v = PooledBuffer.Rent( [| 0uy .. byte vlen - 1uy |] )
        Assert.StrictEqual( exval, ByteFunc.ReadS16BEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadS16BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS16BEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0001us )>]
    [<InlineData( 16, 14, 0x0E0Fus )>]
    member _.ReadU16BE_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint16 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadU16BE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadU16BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU16BE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0001us )>]
    [<InlineData( 16, 14, 0x0E0Fus )>]
    member _.ReadU16BEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint16 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadU16BEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadU16BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU16BEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0100s )>]
    [<InlineData( 16, 14, 0x0F0Es )>]
    member _.ReadS16LE_001 ( vlen : int ) ( pos : uint32 ) ( exval : int16 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadS16LE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadS16LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS16LE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0100s )>]
    [<InlineData( 16, 14, 0x0F0Es )>]
    member _.ReadS16LEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : int16 ) =
        let v = PooledBuffer.Rent( [| 0uy .. byte vlen - 1uy |] )
        Assert.StrictEqual( exval, ByteFunc.ReadS16LEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadS16LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS16LEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0100us )>]
    [<InlineData( 16, 14, 0x0F0Eus )>]
    member _.ReadU16LE_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint16 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadU16LE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadU16LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU16LE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 2, 0, 0x0100us )>]
    [<InlineData( 16, 14, 0x0F0Eus )>]
    member _.ReadU16LEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint16 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadU16LEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.ReadU16LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU16LEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x00010203 )>]
    [<InlineData( 16, 12, 0x0C0D0E0F )>]
    member _.ReadS32BE_001 ( vlen : int ) ( pos : uint32 ) ( exval : int32 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadS32BE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadS32BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS32BE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x00010203 )>]
    [<InlineData( 16, 12, 0x0C0D0E0F )>]
    member _.ReadS32BEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : int32 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadS32BEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadS32BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS32BEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x00010203u )>]
    [<InlineData( 16, 12, 0x0C0D0E0Fu )>]
    member _.ReadU32BE_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint32 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadU32BE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadU32BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU32BE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x00010203u )>]
    [<InlineData( 16, 12, 0x0C0D0E0Fu )>]
    member _.ReadU32BEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint32 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadU32BEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadU32BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU32BEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x03020100 )>]
    [<InlineData( 16, 12, 0x0F0E0D0C )>]
    member _.ReadS32LE_001 ( vlen : int ) ( pos : uint32 ) ( exval : int32 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadS32LE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadS32LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS32LE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x03020100 )>]
    [<InlineData( 16, 12, 0x0F0E0D0C )>]
    member _.ReadS32LEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : int32 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadS32LEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadS32LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS32LEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x03020100u )>]
    [<InlineData( 16, 12, 0x0F0E0D0Cu )>]
    member _.ReadU32LE_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint32 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadU32LE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadU32LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU32LE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 4, 0, 0x03020100u )>]
    [<InlineData( 16, 12, 0x0F0E0D0Cu )>]
    member _.ReadU32LEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint32 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadU32LEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 3, 0 )>]
    [<InlineData( 4, 1 )>]
    [<InlineData( 4, 0xFFFFFFFBu )>]
    [<InlineData( 4, 0xFFFFFFFCu )>]
    [<InlineData( 4, 0xFFFFFFFDu )>]
    member _.ReadU32LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU32LEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0001020304050607L )>]
    [<InlineData( 16, 8, 0x08090A0B0C0D0E0FL )>]
    member _.ReadS64BE_001 ( vlen : int ) ( pos : uint32 ) ( exval : int64 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadS64BE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadS64BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS64BE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0001020304050607L )>]
    [<InlineData( 16, 8, 0x08090A0B0C0D0E0FL )>]
    member _.ReadS64BEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : int64 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadS64BEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadS64BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS64BEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0001020304050607UL )>]
    [<InlineData( 16, 8, 0x08090A0B0C0D0E0FUL )>]
    member _.ReadU64BE_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint64 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadU64BE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadU64BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU64BE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0001020304050607UL )>]
    [<InlineData( 16, 8, 0x08090A0B0C0D0E0FUL )>]
    member _.ReadU64BEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint64 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadU64BEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadU64BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU64BEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0706050403020100L )>]
    [<InlineData( 16, 8, 0x0F0E0D0C0B0A0908L )>]
    member _.ReadS64LE_001 ( vlen : int ) ( pos : uint32 ) ( exval : int64 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadS64LE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadS64LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS64LE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0706050403020100L )>]
    [<InlineData( 16, 8, 0x0F0E0D0C0B0A0908L )>]
    member _.ReadS64LEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : int64 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadS64LEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadS64LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadS64LEPB v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0706050403020100UL )>]
    [<InlineData( 16, 8, 0x0F0E0D0C0B0A0908UL )>]
    member _.ReadU64LE_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint64 ) =
        let v = [| 0uy .. byte vlen - 1uy |]
        Assert.StrictEqual( exval, ByteFunc.ReadU64LE v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadU64LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU64LE v pos |> ignore
        ) |> ignore

    [<Theory>]
    [<InlineData( 8, 0, 0x0706050403020100UL )>]
    [<InlineData( 16, 8, 0x0F0E0D0C0B0A0908UL )>]
    member _.ReadU64LEPB_001 ( vlen : int ) ( pos : uint32 ) ( exval : uint64 ) =
        let v = [| 0uy .. byte vlen - 1uy |] |> PooledBuffer.Rent
        Assert.StrictEqual( exval, ByteFunc.ReadU64LEPB v pos )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 7, 0 )>]
    [<InlineData( 8, 1 )>]
    [<InlineData( 8, 0xFFFFFFF7u )>]
    [<InlineData( 8, 0xFFFFFFF8u )>]
    [<InlineData( 8, 0xFFFFFFF9u )>]
    member _.ReadU64LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.ReadU64LEPB v pos |> ignore
        ) |> ignore

    static member m_WriteS16BE_001_data : obj[][] = [|
        [| 2; 0; 0xFEDCs; [| 0xFEuy; 0xDCuy; |] |]
        [| 8; 6; 0xFEDCs; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xFEuy; 0xDCuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteS16BE_001_data" )>]
    member _.WriteS16BE_001 ( vlen : int ) ( pos : uint32 ) ( testval : int16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteS16BE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS16BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS16BE v pos 0s |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteS16BE_001_data" )>]
    member _.WriteS16BEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : int16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteS16BEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS16BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS16BEPB v pos 0s |> ignore
        ) |> ignore

    static member m_WriteU16BE_001_data : obj[][] = [|
        [| 2; 0; 0xFEDCus; [| 0xFEuy; 0xDCuy; |] |]
        [| 8; 6; 0xFEDCus; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xFEuy; 0xDCuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteU16BE_001_data" )>]
    member _.WriteU16BE_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteU16BE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU16BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU16BE v pos 0us |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteU16BE_001_data" )>]
    member _.WriteU16BEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteU16BEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU16BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU16BEPB v pos 0us |> ignore
        ) |> ignore

    static member m_WriteS16LE_001_data : obj[][] = [|
        [| 2; 0; 0xFEDCs; [| 0xDCuy; 0xFEuy; |] |]
        [| 8; 6; 0xFEDCs; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xDCuy; 0xFEuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteS16LE_001_data" )>]
    member _.WriteS16LE_001 ( vlen : int ) ( pos : uint32 ) ( testval : int16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteS16LE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS16LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS16LE v pos 0s |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteS16LE_001_data" )>]
    member _.WriteS16LEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : int16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteS16LEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS16LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS16LEPB v pos 0s |> ignore
        ) |> ignore

    static member m_WriteU16LE_001_data : obj[][] = [|
        [| 2; 0; 0xFEDCus; [| 0xDCuy; 0xFEuy; |] |]
        [| 8; 6; 0xFEDCus; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xDCuy; 0xFEuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteU16LE_001_data" )>]
    member _.WriteU16LE_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteU16LE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU16LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU16LE v pos 0us |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteU16LE_001_data" )>]
    member _.WriteU16LEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint16 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteU16LEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU16LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU16LEPB v pos 0us |> ignore
        ) |> ignore

    static member m_WriteS32BE_001_data : obj[][] = [|
        [| 4; 0; 0xFEDCBA98; [| 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy |] |]
        [| 8; 4; 0xFEDCBA98; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteS32BE_001_data" )>]
    member _.WriteS32BE_001 ( vlen : int ) ( pos : uint32 ) ( testval : int32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteS32BE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS32BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS32BE v pos 0 |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteS32BE_001_data" )>]
    member _.WriteS32BEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : int32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteS32BEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS32BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS32BEPB v pos 0 |> ignore
        ) |> ignore

    static member m_WriteU32BE_001_data : obj[][] = [|
        [| 4; 0; 0xFEDCBA98u; [| 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy |] |]
        [| 8; 4; 0xFEDCBA98u; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteU32BE_001_data" )>]
    member _.WriteU32BE_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteU32BE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU32BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU32BE v pos 0u |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteU32BE_001_data" )>]
    member _.WriteU32BEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteU32BEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU32BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU32BEPB v pos 0u |> ignore
        ) |> ignore

    static member m_WriteS32LE_001_data : obj[][] = [|
        [| 4; 0; 0xFEDCBA98; [| 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
        [| 8; 4; 0xFEDCBA98; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteS32LE_001_data" )>]
    member _.WriteS32LE_001 ( vlen : int ) ( pos : uint32 ) ( testval : int32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteS32LE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS32LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS32LE v pos 0 |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteS32LE_001_data" )>]
    member _.WriteS32LEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : int32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteS32LEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS32LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS32LEPB v pos 0 |> ignore
        ) |> ignore

    static member m_WriteU32LE_001_data : obj[][] = [|
        [| 4; 0; 0xFEDCBA98u; [| 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
        [| 8; 4; 0xFEDCBA98u; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteU32LE_001_data" )>]
    member _.WriteU32LE_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteU32LE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU32LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU32LE v pos 0u |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteU32LE_001_data" )>]
    member _.WriteU32LEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint32 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteU32LEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU32LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU32LEPB v pos 0u |> ignore
        ) |> ignore

    static member m_WriteS64BE_001_data : obj[][] = [|
        [| 8;  0; 0xFEDCBA9876543210L; [| 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy; 0x76uy; 0x54uy; 0x32uy; 0x10uy |] |]
        [| 16; 8; 0xFEDCBA9876543210L; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy; 0x76uy; 0x54uy; 0x32uy; 0x10uy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteS64BE_001_data" )>]
    member _.WriteS64BE_001 ( vlen : int ) ( pos : uint32 ) ( testval : int64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteS64BE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS64BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS64BE v pos 0L |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteS64BE_001_data" )>]
    member _.WriteS64BEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : int64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteS64BEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS64BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS64BEPB v pos 0L |> ignore
        ) |> ignore

    static member m_WriteU64BE_001_data : obj[][] = [|
        [| 8;  0; 0xFEDCBA9876543210UL; [| 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy; 0x76uy; 0x54uy; 0x32uy; 0x10uy |] |]
        [| 16; 8; 0xFEDCBA9876543210UL; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xFEuy; 0xDCuy; 0xBAuy; 0x98uy; 0x76uy; 0x54uy; 0x32uy; 0x10uy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteU64BE_001_data" )>]
    member _.WriteU64BE_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteU64BE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU64BE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU64BE v pos 0UL |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteU64BE_001_data" )>]
    member _.WriteU64BEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteU64BEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU64BEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU64BEPB v pos 0UL |> ignore
        ) |> ignore

    static member m_WriteS64LE_001_data : obj[][] = [|
        [| 8;  0; 0xFEDCBA9876543210L; [| 0x10uy; 0x32uy; 0x54uy; 0x76uy; 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
        [| 16; 8; 0xFEDCBA9876543210L; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x32uy; 0x54uy; 0x76uy; 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteS64LE_001_data" )>]
    member _.WriteS64LE_001 ( vlen : int ) ( pos : uint32 ) ( testval : int64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteS64LE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS64LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS64LE v pos 0 |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteS64LE_001_data" )>]
    member _.WriteS64LEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : int64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteS64LEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteS64LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteS64LEPB v pos 0L |> ignore
        ) |> ignore

    static member m_WriteU64LE_001_data : obj[][] = [|
        [| 8;  0; 0xFEDCBA9876543210UL; [| 0x10uy; 0x32uy; 0x54uy; 0x76uy; 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
        [| 16; 8; 0xFEDCBA9876543210UL; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x32uy; 0x54uy; 0x76uy; 0x98uy; 0xBAuy; 0xDCuy; 0xFEuy; |] |]
    |]

    [<Theory>]
    [<MemberData( "m_WriteU64LE_001_data" )>]
    member _.WriteU64LE_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen
        ByteFunc.WriteU64LE v pos testval
        Assert.True(( exr = v ))

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU64LE_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU64LE v pos 0UL |> ignore
        ) |> ignore

    [<Theory>]
    [<MemberData( "m_WriteU64LE_001_data" )>]
    member _.WriteU64LEPB_001 ( vlen : int ) ( pos : uint32 ) ( testval : uint64 ) ( exr : byte[] ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        ByteFunc.WriteU64LEPB v pos testval
        Assert.True( PooledBuffer.ValueEqualsWithArray v exr )

    [<Theory>]
    [<InlineData( 0, 0 )>]
    [<InlineData( 1, 0 )>]
    [<InlineData( 2, 1 )>]
    [<InlineData( 2, 0xFFFFFFFDu )>]
    [<InlineData( 2, 0xFFFFFFFEu )>]
    [<InlineData( 2, 0xFFFFFFFFu )>]
    member _.WriteU64LEPB_002 ( vlen : int ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> vlen |> PooledBuffer.Rent
        Assert.ThrowsAny<Exception> ( fun () ->
            ByteFunc.WriteU64LEPB v pos 0UL |> ignore
        ) |> ignore

    [<Fact>]
    member _.S16ToNVBE_001() =
        Assert.True( ( ByteFunc.S16ToNVBE 0xF1F2s = [| 0xF1uy; 0xF2uy; |] ) )

    [<Fact>]
    member _.S16ToNVLE_001() =
        Assert.True( ( ByteFunc.S16ToNVLE 0xF1F2s = [| 0xF2uy; 0xF1uy; |] ) )

    [<Fact>]
    member _.U16ToNVBE_001() =
        Assert.True( ( ByteFunc.U16ToNVBE 0xF2F3us = [| 0xF2uy; 0xF3uy; |] ) )

    [<Fact>]
    member _.U16ToNVLE_001() =
        Assert.True( ( ByteFunc.U16ToNVLE 0xF2F3us = [| 0xF3uy; 0xF2uy; |] ) )

    [<Fact>]
    member _.S32ToNVBE_001() =
        Assert.True( ( ByteFunc.S32ToNVBE 0xF3F4F5F6 = [| 0xF3uy; 0xF4uy; 0xF5uy; 0xF6uy; |] ) )

    [<Fact>]
    member _.S32ToNVLE_001() =
        Assert.True( ( ByteFunc.S32ToNVLE 0xF3F4F5F6 = [| 0xF6uy; 0xF5uy; 0xF4uy; 0xF3uy; |] ) )

    [<Fact>]
    member _.U32ToNVBE_001() =
        Assert.True( ( ByteFunc.U32ToNVBE 0xF4F5F6F7u = [| 0xF4uy; 0xF5uy; 0xF6uy; 0xF7uy; |] ) )

    [<Fact>]
    member _.U32ToNVLE_001() =
        Assert.True( ( ByteFunc.U32ToNVLE 0xF4F5F6F7u = [| 0xF7uy; 0xF6uy; 0xF5uy; 0xF4uy; |] ) )

    [<Fact>]
    member _.S64ToNVBE_001() =
        Assert.True( ( ByteFunc.S64ToNVBE 0xF5F6F7F8F9FAFBFCL = [| 0xF5uy; 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; |] ) )

    [<Fact>]
    member _.S64ToNVLE_001() =
        Assert.True( ( ByteFunc.S64ToNVLE 0xF5F6F7F8F9FAFBFCL = [| 0xFCuy; 0xFBuy; 0xFAuy; 0xF9uy; 0xF8uy; 0xF7uy; 0xF6uy; 0xF5uy; |] ) )

    [<Fact>]
    member _.U64ToNVBE_001() =
        Assert.True( ( ByteFunc.U64ToNVBE 0xF6F7F8F9FAFBFCFDUL = [| 0xF6uy; 0xF7uy; 0xF8uy; 0xF9uy; 0xFAuy; 0xFBuy; 0xFCuy; 0xFDuy; |] ) )

    [<Fact>]
    member _.U64ToNVLE_001() =
        Assert.True( ( ByteFunc.U64ToNVLE 0xF6F7F8F9FAFBFCFDUL = [| 0xFDuy; 0xFCuy; 0xFBuy; 0xFAuy; 0xF9uy; 0xF8uy; 0xF7uy; 0xF6uy; |] ) )
