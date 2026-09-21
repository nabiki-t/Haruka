//=============================================================================
// Haruka Software Storage.
// VhdxReaderTest2.fs : Test cases for VhdxReader class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.VHDXMedia

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Text
open System.Net

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test


//=============================================================================
// Type definitions

//=============================================================================
// Class implementation

type VhdxReaderTest2_Test () =

    let genRegionTable ( t : RegionTable ) ( pos : int ) ( patch : byte[] ) : byte[] =
        let v = Array.zeroCreate<byte> 65536
        ByteFunc.WriteU32BE v 0u t.Signature
        ByteFunc.WriteS32LE v 8u t.Entries.Length
        t.Entries
        |> List.iteri ( fun idx itr ->
            let pos = uint32 ( 16 + idx * 32 )
            if pos <= 65504u then
                ByteFunc.WriteGuid v ( pos + 0u ) itr.Guid
                ByteFunc.WriteU64LE v ( pos + 16u ) itr.FileOffset
                ByteFunc.WriteU32LE v ( pos + 24u ) itr.Length
                ByteFunc.WriteU32LE v ( pos + 28u ) ( if itr.Required then 1u else 0u )
        )
        Array.blit patch 0 v pos patch.Length
        let crc = Crc32C.Compute v
        ByteFunc.WriteU32LE v 4u crc
        v

    let defReginTable ( pos : int ) ( patch : byte[] ) : byte[] =
        genRegionTable {
            Signature = 0x72656769u
            Checksum = 0u;
            EntryCount = 0u;
            Entries = [
                {
                    Guid = VhdxCommons.REGENT_TYPE_BAT;
                    FileOffset = 1048576UL;
                    Length = 1048576u;
                    Required = true;
                };
                {
                    Guid = VhdxCommons.REGENT_TYPE_METADATA;
                    FileOffset = 2097152UL;
                    Length = 1048576u;
                    Required = true;
                };
            ];
        } pos patch


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.ReadRegionTable_Fail_001() =
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadRegionTable [||] 0UL |> ignore
            )
        Assert.StartsWith( "The region table must be 64KB length", r.Message )

    static member m_ReadRegionTable_Fail_002 : obj[][] = [|
        [|  // Signature
            0; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |];
            0; [||];
            3145728UL;
        |];
        [|  // Checksum
            0; [||];
            4; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |];
            3145728UL;
        |];
        [|  // EntryCount equals 2048.
            8; [| 0x00uy; 0x08uy; 0x00uy; 0x00uy; |];
            0; [||];
            3145728UL;
        |];
        [|  // EntryCount equals 0.
            8; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            0; [||];
            3145728UL;
        |]
        [|  // EntryCount equals -1.
            8; [| 0x00uy; 0x00uy; 0x00uy; 0x80uy; |];
            0; [||];
            3145728UL;
        |]
        [|  // FileOffset equals 0
            32; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            0; [||];
            UInt64.MaxValue;
        |];
        [|  // FileOffset is not a multiple of 1 MB.
            32; [| 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            0; [||];
            UInt64.MaxValue;
        |];
        [|  // FileOffset equals 64TB.
            32; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x40uy; 0x00uy; 0x00uy; |];
            0; [||];
            UInt64.MaxValue;
        |];
        [|  // FileOffset equals -1.
            32; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x80uy; |];
            0; [||];
            UInt64.MaxValue;
        |];
        [|  // Length equals 0.
            40; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            0; [||];
            3145728UL;
        |];
        [|  // Length is not a multiple of 1 MB.
            40; [| 0x00uy; 0x00uy; 0x18uy; 0x00uy; |];
            0; [||];
            3145728UL;
        |];
        [|  // Length equals -1.
            40; [| 0x00uy; 0x00uy; 0x00uy; 0x80uy; |];
            0; [||];
            UInt64.MaxValue;
        |];
        [|  // FileOffset + Length exceeds 64 TB.
            32; [| 0x00uy; 0x00uy; 0xF0uy; 0xFFuy; 0xFFuy; 0x3Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x20uy; 0x00uy; |];
            0; [||];
            UInt64.MaxValue;
        |];
        [|  // FileOffset + Length exceeds file size.
            72; [| 0x00uy; 0x00uy; 0x20uy; 0x00uy; |];
            0; [||];
            3145728UL;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadRegionTable_Fail_002" )>]
    member _.ReadRegionTable_Fail_002 ( pos1 : int ) ( patch1 : byte[] ) ( pos2 : int ) ( patch2 : byte[] ) ( filelen : uint64 ) =
        let d = defReginTable pos1 patch1
        Array.blit patch2 0 d pos2 patch2.Length
        let r = VhdxReader.ReadRegionTable d filelen
        Assert.StrictEqual( None, r )

    [<Theory>]
    [<InlineData( 0x100000UL, 0x100000u, 0x100000UL, 0x100000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x100000UL, 0x300000u, 0x100000UL, 0x100000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x100000UL, 0x300000u, 0x200000UL, 0x100000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x100000UL, 0x300000u, 0x300000UL, 0x100000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x100000UL, 0x100000u, 0x100000UL, 0x300000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x200000UL, 0x100000u, 0x100000UL, 0x300000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x300000UL, 0x100000u, 0x100000UL, 0x300000u, 0x400000UL, 0x100000u, false )>]
    [<InlineData( 0x400000UL, 0x100000u, 0x200000UL, 0x200000u, 0x100000UL, 0x200000u, false )>]
    [<InlineData( 0x100000UL, 0x100000u, 0x200000UL, 0x100000u, 0x300000UL, 0x100000u, true )>]
    [<InlineData( 0x300000UL, 0x100000u, 0x100000UL, 0x100000u, 0x200000UL, 0x100000u, true )>]
    [<InlineData( 0x300000UL, 0x100000u, 0x400000UL, 0x100000u, 0x100000UL, 0x100000u, true )>]
    member _.ReadRegionTable_001 ( a : uint64 ) ( b : uint32 ) ( c : uint64 ) ( d : uint32 ) ( e : uint64 ) ( f : uint32 ) ( exp : bool ) =
        let v =
            genRegionTable {
                Signature = 0x72656769u
                Checksum = 0u;
                EntryCount = 0u;
                Entries = [
                    {
                        Guid = VhdxCommons.REGENT_TYPE_BAT;
                        FileOffset = a;
                        Length = b;
                        Required = true;
                    };
                    {
                        Guid = VhdxCommons.REGENT_TYPE_METADATA;
                        FileOffset = c;
                        Length = d;
                        Required = true;
                    };
                    {
                        Guid = Guid();
                        FileOffset = e;
                        Length = f;
                        Required = true;
                    };
                ];
            } 0 [||]
        let r = VhdxReader.ReadRegionTable v 5242880UL
        if exp then
            Assert.True( r.IsSome )
        else
            Assert.True( r.IsNone )

    [<Theory>]
    [<InlineData( "00000000000000000000000000000000", "00000000000000000000000000000000", "00000000000000000000000000000000", false )>]
    [<InlineData( "00000000000000000000000000000000", "00000000000000000000000000000001", "00000000000000000000000000000000", false )>]
    [<InlineData( "00000000000000000000000000000000", "00000000000000000000000000000001", "00000000000000000000000000000002", true )>]
    member _.ReadRegionTable_002 ( g1 : string ) ( g2 : string ) ( g3 : string ) ( exp : bool ) =
        let v =
            genRegionTable {
                Signature = 0x72656769u
                Checksum = 0u;
                EntryCount = 0u;
                Entries = [
                    {
                        Guid = Guid( g1 );
                        FileOffset = 0x100000UL;
                        Length = 0x100000u;
                        Required = true;
                    };
                    {
                        Guid = Guid( g2 );
                        FileOffset = 0x200000UL;
                        Length = 0x100000u;
                        Required = true;
                    };
                    {
                        Guid = Guid( g3 );
                        FileOffset = 0x300000UL;
                        Length = 0x100000u;
                        Required = true;
                    };
                ];
            } 0 [||]
        let r = VhdxReader.ReadRegionTable v 5242880UL
        if exp then
            Assert.True( r.IsSome )
        else
            Assert.True( r.IsNone )

    [<Theory>]
    [<InlineData( 0, false )>]
    [<InlineData( 1, true )>]
    [<InlineData( 2047, true )>]
    [<InlineData( 2048, false )>]
    member _.ReadRegionTable_003 ( cnt : int ) ( exp : bool ) =
        let guiddata = [|
            for i = 0 to cnt - 1 do
                yield Guid.NewGuid()
        |]
        let rangedata =
            [|
                for i = 1 to cnt do
                    ( ( uint64 i * 0x100000UL ), 0x100000u )
            |]
            |> Array.randomShuffle
        let v =
            genRegionTable {
                Signature = 0x72656769u
                Checksum = 0u;
                EntryCount = 0u;
                Entries = [
                    for i = 0 to cnt - 1 do
                        {
                            Guid = guiddata.[i];
                            FileOffset = rangedata.[i] |> fst;
                            Length = rangedata.[i] |> snd;
                            Required = ( i % 3 = 1 );
                        };
                ];
            } 0 [||]
        let r = VhdxReader.ReadRegionTable v UInt64.MaxValue
        if exp then
            Assert.StrictEqual( 0x72656769u, r.Value.Signature )
            Assert.StrictEqual( uint32 cnt, r.Value.EntryCount )
            for i = 0 to cnt - 1 do
                let ent = r.Value.Entries.[i]
                Assert.StrictEqual( guiddata.[i], ent.Guid )
                Assert.StrictEqual( rangedata.[i] |> fst, ent.FileOffset )
                Assert.StrictEqual( rangedata.[i] |> snd, ent.Length )
                Assert.StrictEqual( i % 3 = 1, ent.Required )
            Assert.True( r.IsSome )
        else
            Assert.True( r.IsNone )
