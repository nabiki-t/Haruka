//=============================================================================
// Haruka Software Storage.
// Crc32C_Test.fs : Test cases for Crc32C class.
//


//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.Intrinsics.X86
open System.Runtime.CompilerServices

open Xunit

open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation


type Crc32CTest() =

    [<Fact>]
    member _.CRC32_soft_001() =
        let v = [| for _ in 1 .. 32 -> 0uy |]
        let r = PrivateCaller.Invoke< Crc32C >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x8A9136AAu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_002() =
        let v = [| for _ in 1 .. 32 -> 0xFFuy |]
        let r = PrivateCaller.Invoke< Crc32C >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x62A8Ab43u = ~~~r )

    [<Fact>]
    member _.CRC32_soft_003() =
        let v = [| for i in 0uy .. 0x1Fuy -> i |]
        let r = PrivateCaller.Invoke< Crc32C >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_004() =
        let v = [| for i = 0x1F downto 0 do yield byte i |]
        let r = PrivateCaller.Invoke< Crc32C >( "CRC32_soft", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
        Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_soft_005() =
        let v = [|
            yield 0xDDuy;
            for i in 0uy .. 0x1Fuy -> i
            yield 0xCCuy;
        |]
        let r = PrivateCaller.Invoke< Crc32C >( "CRC32_soft", 0xFFFFFFFFu, v, 1, v.Length - 2 ) :?> uint32
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
        let r = PrivateCaller.Invoke< Crc32C >( "CRC32_soft", 0xFFFFFFFFu, v, 2, v.Length - 4 ) :?> uint32
        Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_001() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0uy |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x8A9136AAu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_002() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0xFFuy |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x62A8Ab43u = ~~~r )

    [<Fact>]
    member _.CRC32_x64_003() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for i in 0uy .. 0x1Fuy -> i |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_004() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [| for i = 0x1F downto 0 do yield byte i |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x64", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x64_005() =
        if Sse42.IsSupported && Sse42.X64.IsSupported then
            let v = [|
                yield 0xAAuy;
                for i in 0uy .. 0x1Fuy -> i
                yield 0xBBuy;
            |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x64", 0xFFFFFFFFu, v, 1, v.Length - 2 ) :?> uint32
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
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x64", 0xFFFFFFFFu, v, 2, v.Length - 4 ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_001() =
        if Sse42.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0uy |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x8A9136AAu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_002() =
        if Sse42.IsSupported then
            let v = [| for _ in 1 .. 32 -> 0xFFuy |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x62A8Ab43u = ~~~r )

    [<Fact>]
    member _.CRC32_x86_003() =
        if Sse42.IsSupported then
            let v = [| for i in 0uy .. 0x1Fuy -> i |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x46DD794Eu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_004() =
        if Sse42.IsSupported then
            let v = [| for i = 0x1F downto 0 do yield byte i |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x86", 0xFFFFFFFFu, v, 0, v.Length ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_x86_005() =
        if Sse42.IsSupported then
            let v = [|
                yield 0xCCuy;
                for i in 0uy .. 0x1Fuy -> i
                yield 0xDDuy;
            |]
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x86", 0xFFFFFFFFu, v, 1, v.Length - 2 ) :?> uint32
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
            let r = PrivateCaller.Invoke< Crc32C >( "CRC32_x86", 0xFFFFFFFFu, v, 2, v.Length - 4 ) :?> uint32
            Assert.True( 0x113FDB5Cu = ~~~r )

    [<Fact>]
    member _.CRC32_013() =
        let wbuf = [|
            0x01uy; 0xC0uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x14uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x14uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy;
            0x28uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        Assert.True( 0xD9963A56u = Crc32C.Compute [| wbuf.[0..10];  wbuf.[11..23]; wbuf.[24..27]; wbuf.[28..37]; wbuf.[38..47]; |]  )

    [<Fact>]
    member _.CRC32_014() =
        let wv1 = [|
            yield 0uy;
            yield 1uy;
            for _ in 1 .. 32 -> 0uy;
            yield 2uy;
            yield 3uy;
        |]
        Assert.True( 0x8A9136AAu = Crc32C.Compute ( ArraySegment( wv1, 2, 32 ) ) )

    [<Fact>]
    member _.CRC32_015() =
        let wv2 = [|
            yield 0uy;
            yield 1uy;
            yield 2uy;
            yield 3uy;
            for _ in 1 .. 32 -> 0xFFuy;
        |]
        Assert.True( 0x62A8Ab43u = Crc32C.Compute ( ArraySegment( wv2, 4, 32 ) ) )

    [<Fact>]
    member _.CRC32_016() =
        let wv3 = [|
            for i in 0uy .. 0x1Fuy -> i;
            yield 0uy;
            yield 1uy;
            yield 2uy;
            yield 3uy;
        |]
        Assert.True( 0x46DD794Eu = Crc32C.Compute ( ArraySegment( wv3, 0, 32 ) ) )

    [<Fact>]
    member _.CRC32_017() =
        let wv4 = [|
            for i = 0x1F downto 0 do yield byte i;
        |]
        Assert.True( 0x113FDB5Cu = Crc32C.Compute ( ArraySegment( wv4, 0, 32 ) ) )

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
        Assert.True( 0xD9963A56u = Crc32C.Compute wv5 )
