//=============================================================================
// Haruka Software Storage.
// PooledBufferTest.fs : Test cases for PooledBuffer class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.Collections.Generic

open Xunit

open Haruka.Commons
open Haruka.Constants

//=============================================================================
// Class implementation

type PooledBuffer_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Rent_Return_001() =
        let p1 = PooledBuffer.Rent 16
        Assert.True(( p1.Array.Length >= 16 ))
        p1.Return()
        Assert.True(( p1.Array.Length = 0 ))
        p1.Return()
        Assert.True(( p1.Array.Length = 0 ))

    [<Fact>]
    member _.Rent_Return_002() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.Array.Length = 0 ))
        p1.Return()
        Assert.True(( p1.Array.Length = 0 ))

    [<Fact>]
    member _.GetPartialBytes_001() =
        let p1 = PooledBuffer.Rent [| 0uy .. 255uy |]
        Assert.True(( ( p1.GetPartialBytes 1 5 ) = [| 1uy; 2uy; 3uy; 4uy; 5uy |] ))

    [<Fact>]
    member _.GetArraySegment_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent b
        let as1 = p1.GetArraySegment 1 5
        Assert.True(( Functions.IsSame as1.Array p1.Array ))
        Assert.True(( as1.Count = 5 ))
        Assert.True(( as1.Offset = 1 ))
        Assert.True(( as1.[0] = 1uy ))
        Assert.True(( as1.[1] = 2uy ))
        Assert.True(( as1.[2] = 3uy ))
        Assert.True(( as1.[3] = 4uy ))
        Assert.True(( as1.[4] = 5uy ))

    [<Fact>]
    member _.Array_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent b
        Assert.False(( Functions.IsSame p1.Array b ))

    [<Fact>]
    member _.Array_002() =
        let p1 = PooledBuffer.Rent 10
        Assert.True(( p1.Array.Length >= 10 ))
        p1.Return()
        Assert.True(( p1.Array.Length = 0 ))

    [<Fact>]
    member _.Array_003() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.Array.Length = 0 ))

    [<Fact>]
    member _.Length_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent( b, 99 )
        Assert.True(( p1.Length = 99 ))

    [<Fact>]
    member _.Length_002() =
        let p1 = PooledBuffer.Rent 10
        Assert.True(( p1.Length = 10 ))
        p1.Return()
        Assert.True(( p1.Length = 0 ))

    [<Fact>]
    member _.Length_003() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.Length = 0 ))

    [<Fact>]
    member _.uLength_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent( b, 99 )
        Assert.True(( p1.uLength = 99u ))

    [<Fact>]
    member _.uLength_002() =
        let p1 = PooledBuffer.Rent 10
        Assert.True(( p1.uLength = 10u ))
        p1.Return()
        Assert.True(( p1.uLength = 0u ))

    [<Fact>]
    member _.uLength_003() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.uLength = 0u ))

    [<Fact>]
    member _.Count_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent( b, 88 )
        Assert.True(( p1.Count = 88 ))

    [<Fact>]
    member _.Count_002() =
        let p1 = PooledBuffer.Rent 20
        Assert.True(( p1.Count = 20 ))
        p1.Return()
        Assert.True(( p1.Count = 0 ))

    [<Fact>]
    member _.Count_003() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.Count = 0 ))

    [<Fact>]
    member _.uCount_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent( b, 88 )
        Assert.True(( p1.uCount = 88u ))

    [<Fact>]
    member _.uCount_002() =
        let p1 = PooledBuffer.Rent 20
        Assert.True(( p1.uCount = 20u ))
        p1.Return()
        Assert.True(( p1.uCount = 0u ))

    [<Fact>]
    member _.uCount_003() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.uCount = 0u ))

    [<Fact>]
    member _.ArraySegment_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent b
        let as1 = p1.ArraySegment
        Assert.True(( Functions.IsSame as1.Array p1.Array ))
        Assert.True(( as1.Count = 256 ))
        Assert.True(( as1.Offset = 0 ))

    [<Fact>]
    member _.Item_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent b
        Assert.True(( p1.[0] = 0uy ))
        Assert.True(( p1.[255] = 255uy ))

    [<Fact>]
    member _.IsEmpty_001() =
        let p1 = PooledBuffer.Rent 10
        Assert.False(( p1.IsEmpty() ))
        p1.Return()
        Assert.True(( p1.IsEmpty() ))

    [<Fact>]
    member _.IsEmpty_002() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.IsEmpty() ))

    [<Theory>]
    [<InlineData( 256, 10, 10 )>]
    [<InlineData( 256, 0, 0 )>]
    [<InlineData( 256, -1, 0 )>]
    [<InlineData( 256, 256, 256 )>]
    [<InlineData( 256, 257, 256 )>]
    [<InlineData( 0, 0, 0)>]
    [<InlineData( 0, 1, 0)>]
    member _.Truncate_001 ( blen : int, req : int, res : int ) =
        let p1 = PooledBuffer.Rent blen
        Assert.True(( p1.Length = blen ))
        p1.Truncate req
        Assert.True(( p1.Length = res ))

    [<Fact>]
    member _.static_length_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent b
        Assert.True(( PooledBuffer.length p1 = 256 ))

    [<Fact>]
    member _.static_length_002() =
        let b = [||]
        let p1 = PooledBuffer.Rent b
        Assert.True(( PooledBuffer.length p1 = 0 ))

    [<Fact>]
    member _.static_ulength_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer.Rent b
        Assert.True(( PooledBuffer.ulength p1 = 256u ))

    [<Fact>]
    member _.static_ulength_002() =
        let b = [||]
        let p1 = PooledBuffer.Rent b
        Assert.True(( PooledBuffer.ulength p1 = 0u ))

    [<Fact>]
    member _.static_ValueEquals_001() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer.Rent b1
        let p2 = PooledBuffer.Rent b2
        Assert.False(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_002() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer.Rent b1
        let p2 = PooledBuffer.Rent b2
        Assert.True(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_003() =
        let b1 = [| 10uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer.Rent b1
        let p2 = PooledBuffer.Rent b2
        Assert.False(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_001() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer.Rent b1
        Assert.False(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_002() =
        let b1 = [| 1uy; 2uy; 3uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer.Rent b1
        Assert.True(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_003() =
        let b1 = [| 10uy; 2uy; 3uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer.Rent b1
        Assert.False(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_Rent_001_001() =
        let p1 = PooledBuffer.Rent 10
        Assert.True(( p1.Array.Length >= 10 ))
        Assert.True(( p1.Length = 10 ))

    [<Fact>]
    member _.static_Rent_001_002() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.Array.Length = 0 ))
        Assert.True(( p1.Length = 0 ))

    [<Fact>]
    member _.static_Rent_001_003() =
        let p1 = PooledBuffer.Rent -1
        Assert.True(( p1.Array.Length = 0 ))
        Assert.True(( p1.Length = 0 ))

    [<Fact>]
    member _.static_Rent_002_001() =
        let b1 = [| 1uy; 2uy; 3uy |]
        let p1 = PooledBuffer.Rent b1
        Assert.True(( p1.Length = 3 ))
        for i = 0 to 2 do
            Assert.True(( p1.Array.[i] = b1.[i] ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_002_002() =
        let b1 = [||]
        let p1 = PooledBuffer.Rent b1
        Assert.True(( p1.Array.Length = 0 ))
        Assert.True(( p1.Length = 0 ))

    [<Fact>]
    member _.static_Rent_003_001() =
        let b1 = 
            let v = Array.zeroCreate 128
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0 )
        Assert.True(( p1.Length = 0 ))
        Assert.True(( p1.Array.Length = 0 ))

    [<Fact>]
    member _.static_Rent_003_002() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 97 )
        Assert.True(( p1.Length = 97 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_003_003() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 98 )
        Assert.True(( p1.Length = 98 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1 ))
        Assert.True(( p1.Array.Length >= 98 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_001() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0, 97 )
        Assert.True(( p1.Length = 97 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1 ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_002() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0, 98 )
        Assert.True(( p1.Length = 98 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1 ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_003() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0, 96 )
        Assert.True(( p1.Length = 96 ))
        Assert.True(( p1.Array.[ 0 .. 95 ] = b1.[ 0 .. 95 ] ))
        Assert.True(( p1.Array.Length >= 96 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_004() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0, 0 )
        Assert.True(( p1.Length = 0 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_005() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0, -1 )
        Assert.True(( p1.Length = 0 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_006() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 1, 96 )
        Assert.True(( p1.Length = 96 ))
        Assert.True(( p1.Array.[ 0 .. 95 ] = b1.[ 1 .. 96 ] ))
        Assert.True(( p1.Array.Length >= 96 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_007() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 1, 95 )
        Assert.True(( p1.Length = 95 ))
        Assert.True(( p1.Array.[ 0 .. 94 ] = b1.[ 1 .. 95 ] ))
        Assert.True(( p1.Array.Length >= 95 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_008() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 80, 97 )
        Assert.True(( p1.Length = 97 ))
        Assert.True(( p1.Array.[ 0 .. 16 ] = b1.[ 80 .. 96 ] ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_009() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, -1, 10 )
        Assert.True(( p1.Length = 10 ))
        Assert.True(( p1.Array.[ 0 .. 9 ] = b1.[ 0 .. 9 ] ))
        Assert.True(( p1.Array.Length >= 10 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_004_010() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 97, 10 )
        Assert.True(( p1.Length = 10 ))
        Assert.True(( p1.Array.Length >= 10 ))
        Assert.False(( Functions.IsSame p1.Array b1 ))

    [<Fact>]
    member _.static_Rent_005_001() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            v
        let p1 = PooledBuffer.Rent( b1, 0 )
        Assert.True(( p1.Length = 0 ))
        Assert.True(( p1.Array.Length = 0 ))

    [<Fact>]
    member _.static_Rent_005_002() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 97 )
        Assert.True(( p1.Length = 97 ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1.Array.[ 0 .. 96 ] ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_005_003() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 98 )
        Assert.True(( p1.Length = 98 ))
        Assert.True(( p1.Array.Length >= 98 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1.Array.[ 0 .. 96 ] ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_001() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 0, 97 )
        Assert.True(( p1.Length = 97 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1.Array.[ 0 .. 96 ] ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_002() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 0, 98 )
        Assert.True(( p1.Length = 98 ))
        Assert.True(( p1.Array.[ 0 .. 96 ] = b1.Array.[ 0 .. 96 ] ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_003() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 0, 96 )
        Assert.True(( p1.Length = 96 ))
        Assert.True(( p1.Array.[ 0 .. 95 ] = b1.Array.[ 0 .. 95 ] ))
        Assert.True(( p1.Array.Length >= 96 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_004() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 0, 0 )
        Assert.True(( p1.Length = 0 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_005() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 0, -1 )
        Assert.True(( p1.Length = 0 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_006() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 1, 96 )
        Assert.True(( p1.Length = 96 ))
        Assert.True(( p1.Array.[ 0 .. 95 ] = b1.Array.[ 1 .. 96 ] ))
        Assert.True(( p1.Array.Length >= 96 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_007() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 1, 95 )
        Assert.True(( p1.Length = 95 ))
        Assert.True(( p1.Array.[ 0 .. 94 ] = b1.Array.[ 1 .. 95 ] ))
        Assert.True(( p1.Array.Length >= 95 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_008() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 80, 97 )
        Assert.True(( p1.Length = 97 ))
        Assert.True(( p1.Array.[ 0 .. 16 ] = b1.Array.[ 80 .. 96 ] ))
        Assert.True(( p1.Array.Length >= 97 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_009() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, -1, 10 )
        Assert.True(( p1.Length = 10 ))
        Assert.True(( p1.Array.[ 0 .. 9 ] = b1.Array.[ 0 .. 9 ] ))
        Assert.True(( p1.Array.Length >= 10 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_Rent_006_010() =
        let b1 = 
            let v = Array.zeroCreate 97
            Random.Shared.NextBytes v
            PooledBuffer.Rent v
        let p1 = PooledBuffer.Rent( b1, 97, 10 )
        Assert.True(( p1.Length = 10 ))
        Assert.True(( p1.Array.Length >= 10 ))
        Assert.False(( Functions.IsSame p1.Array b1.Array ))

    [<Fact>]
    member _.static_RentAndInit_001() =
        let p1 = PooledBuffer.Rent 16
        for i = 0 to p1.Array.Length - 1 do
            p1.Array.[i] <- byte i
        PooledBuffer.Return p1
        let p2 = PooledBuffer.RentAndInit 16
        for i = 0 to 15 do
            Assert.True(( p2.[i] = 0uy ))

    [<Fact>]
    member _.static_RentAndInit_002() =
        let p2 = PooledBuffer.RentAndInit 0
        Assert.True(( p2.Array.Length = 0 ))
        Assert.True(( p2.Length = 0 ))

    [<Fact>]
    member _.static_Return_001() =
        let p2 = PooledBuffer.Rent 10
        Assert.True(( p2.Array.Length >= 10 ))
        Assert.True(( p2.Length = 10 ))
        PooledBuffer.Return p2
        Assert.True(( p2.Array.Length = 0 ))
        Assert.True(( p2.Length = 0 ))

    [<Fact>]
    member _.static_Return_002() =
        let v = [|
            for i = 0 to 9 do
                yield PooledBuffer.Rent i
        |]
        PooledBuffer.Return v

        for i = 0 to 9 do
            Assert.True(( v.[i].Array.Length = 0 ))
            Assert.True(( v.[i].Length = 0 ))

    [<Fact>]
    member _.static_Return_003() =
        PooledBuffer.Return [||]

    [<Fact>]
    member _.static_Empty_001() =
        let p1 = PooledBuffer.Empty
        Assert.True(( p1.Length = 0 ))
        Assert.True(( p1.Array.Length = 0 ))
