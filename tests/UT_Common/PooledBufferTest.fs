namespace Haruka.Test.UT.Commons

open System
open System.Threading
open System.Threading.Tasks
open System.IO
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

type PooledBuffer_Test() =

    [<Fact>]
    member _.IEquatable_001() =
        let b1 = Array.zeroCreate<byte> 1
        let b2 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b2, 1 )
        Assert.False(( ( p1 :> IEquatable<PooledBuffer> ).Equals p2 ))

    [<Fact>]
    member _.IEquatable_002() =
        let b1 = Array.zeroCreate<byte> 2
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 2 )
        Assert.False(( ( p1 :> IEquatable<PooledBuffer> ).Equals p2 ))

    [<Fact>]
    member _.IEquatable_003() =
        let b1 = Array.zeroCreate<byte> 2
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 1 )
        Assert.True(( ( p1 :> IEquatable<PooledBuffer> ).Equals p2 ))

    [<Fact>]
    member _.IEqualityComparer_Equals_001() =
        let b1 = Array.zeroCreate<byte> 1
        let b2 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b2, 1 )
        Assert.False(( ( p1 :> IEqualityComparer<PooledBuffer> ).Equals p2 ))

    [<Fact>]
    member _.IEqualityComparer_Equals_002() =
        let b1 = Array.zeroCreate<byte> 2
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 2 )
        Assert.False(( ( p1 :> IEqualityComparer<PooledBuffer> ).Equals p2 ))

    [<Fact>]
    member _.IEqualityComparer_Equals_003() =
        let b1 = Array.zeroCreate<byte> 2
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 1 )
        Assert.True(( ( p1 :> IEqualityComparer<PooledBuffer> ).Equals p2 ))

    [<Fact>]
    member _.IEqualityComparer_GetHashCode_001() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        Assert.True(( ( p1 :> IEqualityComparer<PooledBuffer> ).GetHashCode() = b1.GetHashCode() ))

    [<Fact>]
    member _.Equals_obj_001() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        Assert.False(( p1.Equals( box 1 ) ))

    [<Fact>]
    member _.Equals_obj_002() =
        let b1 = Array.zeroCreate<byte> 1
        let b2 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b2, 1 )
        Assert.False(( p1.Equals( p2 :> obj ) ))

    [<Fact>]
    member _.Equals_obj_003() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 2 )
        Assert.False(( p1.Equals( p2 :> obj ) ))

    [<Fact>]
    member _.Equals_obj_004() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 1 )
        Assert.True(( p1.Equals( p2 :> obj ) ))

    [<Fact>]
    member _.GetHashCode_001() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        Assert.True(( p1.GetHashCode() = b1.GetHashCode() ))

    [<Fact>]
    member _.ToString_001() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        Assert.True(( p1.ToString() = "PooledBuffer" ))

    [<Fact>]
    member _.Equals_001() =
        let b1 = Array.zeroCreate<byte> 1
        let b2 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b2, 1 )
        Assert.False(( p1.Equals p2 ))

    [<Fact>]
    member _.Equals_002() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 2 )
        Assert.False(( p1.Equals p2 ))

    [<Fact>]
    member _.Equals_003() =
        let b1 = Array.zeroCreate<byte> 1
        let p1 = PooledBuffer( b1, 1 )
        let p2 = PooledBuffer( b1, 1 )
        Assert.True(( p1.Equals p2 ))

    [<Fact>]
    member _.Rent_Return_001() =
        let p1 = PooledBuffer.Rent 16
        p1.Return()

    [<Fact>]
    member _.GetPartialBytes_001() =
        let p1 = PooledBuffer( [| 0uy .. 255uy |], 256 )
        Assert.True(( ( p1.GetPartialBytes 1 5 ) = [| 1uy; 2uy; 3uy; 4uy; 5uy |] ))

    [<Fact>]
    member _.GetArraySegment_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 256 )
        let as1 = p1.GetArraySegment 1 5
        Assert.True(( Object.ReferenceEquals( as1.Array, b ) ))
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
        let p1 = PooledBuffer( b, 256 )
        Assert.True(( Object.ReferenceEquals( p1.Array, b ) ))

    [<Fact>]
    member _.Length_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 99 )
        Assert.True(( p1.Length = 99 ))

    [<Fact>]
    member _.Count_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 88 )
        Assert.True(( p1.Count = 88 ))

    [<Fact>]
    member _.ArraySegment_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 256 )
        let as1 = p1.ArraySegment
        Assert.True(( Object.ReferenceEquals( as1.Array, b ) ))
        Assert.True(( as1.Count = 256 ))
        Assert.True(( as1.Offset = 0 ))

    [<Fact>]
    member _.Item_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 256 )
        Assert.True(( p1.[0] = 0uy ))
        Assert.True(( p1.[255] = 255uy ))

    [<Fact>]
    member _.static_length_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 256 )
        Assert.True(( PooledBuffer.length p1 = 256 ))

    [<Fact>]
    member _.static_length_002() =
        let b = [||]
        let p1 = PooledBuffer( b, 0 )
        Assert.True(( PooledBuffer.length p1 = 0 ))

    [<Fact>]
    member _.static_ulength_001() =
        let b = [| 0uy .. 255uy |]
        let p1 = PooledBuffer( b, 256 )
        Assert.True(( PooledBuffer.ulength p1 = 256u ))

    [<Fact>]
    member _.static_ulength_002() =
        let b = [||]
        let p1 = PooledBuffer( b, 0 )
        Assert.True(( PooledBuffer.ulength p1 = 0u ))

    [<Fact>]
    member _.static_ValueEquals_001() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer( b1, 4 )
        let p2 = PooledBuffer( b2, 3 )
        Assert.False(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_002() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer( b1, 3 )
        let p2 = PooledBuffer( b2, 3 )
        Assert.True(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_003() =
        let b1 = [| 10uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer( b1, 3 )
        let p2 = PooledBuffer( b2, 3 )
        Assert.False(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_004() =
        let b1 = [| 1uy; 2uy; 3uy; 40uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer( b1, 3 )
        let p2 = PooledBuffer( b2, 3 )
        Assert.True(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_005() =
        let b1 = [| 1uy; 2uy; 3uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer( b1, 5 )
        let p2 = PooledBuffer( b2, 5 )
        Assert.False(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEquals_006() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer( b1, 5 )
        let p2 = PooledBuffer( b2, 5 )
        Assert.True(( PooledBuffer.ValueEquals p1 p2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_001() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer( b1, 4 )
        Assert.False(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_002() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer( b1, 3 )
        Assert.True(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_003() =
        let b1 = [| 1uy; 2uy; 3uy; 4uy; |]
        let b2 = [| 1uy; 2uy; 3uy; |]
        let p1 = PooledBuffer( b1, 3 )
        Assert.True(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_ValueEqualsWithArray_004() =
        let b1 = [| 1uy; 2uy; 3uy; |]
        let b2 = [| 1uy; 2uy; 3uy; 4uy; |]
        let p1 = PooledBuffer( b1, 4 )
        Assert.False(( PooledBuffer.ValueEqualsWithArray p1 b2 ))

    [<Fact>]
    member _.static_Rent_001() =
        let p1 = PooledBuffer.Rent 10
        Assert.True(( p1.Array.Length >= 10 ))
        Assert.True(( p1.Length = 10 ))

    [<Fact>]
    member _.static_Rent_002() =
        let p1 = PooledBuffer.Rent 0
        Assert.True(( p1.Array.Length = 0 ))
        Assert.True(( p1.Length = 0 ))

    [<Fact>]
    member _.static_Rent_003() =
        let b1 = [| 1uy; 2uy; 3uy |]
        let p1 = PooledBuffer.Rent b1
        Assert.True(( p1.Length = 3 ))
        for i = 0 to 2 do
            Assert.True(( p1.Array.[i] = b1.[i] ))
        Assert.False(( Object.ReferenceEquals( p1.Array, b1 ) ))

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
    member _.static_Truncate_001() =
        let p1 = PooledBuffer.Rent 15
        let p2 = PooledBuffer.Truncate 8 p1
        Assert.True(( p1.Length = 15 ))
        Assert.True(( p2.Length = 8 ))
        Assert.True(( Object.ReferenceEquals( p1.Array, p2.Array ) ))

    [<Fact>]
    member _.static_Truncate_002() =
        let p1 = PooledBuffer.Rent 15
        let p2 = PooledBuffer.Truncate 17 p1
        Assert.True(( p1.Length = 15 ))
        Assert.True(( p2.Length = 15 ))
        Assert.True(( Object.ReferenceEquals( p1.Array, p2.Array ) ))

    [<Fact>]
    member _.static_Empty_001() =
        let p1 = PooledBuffer.Empty
        Assert.True(( p1.Length = 0 ))
        Assert.True(( p1.Array.Length = 0 ))
