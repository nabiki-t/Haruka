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

    let genMetadataTable ( len : int ) ( mdi : MetadataTableEntry[] ) : byte[] =
        let v = Array.zeroCreate<byte> len
        ByteFunc.WriteU64BE v 0u 0x6D65746164617461UL
        ByteFunc.WriteU16LE v 10u ( uint16 mdi.Length )
        mdi
        |> Array.iteri( fun idx itr ->
            let pos = 32 + idx * 32
            ByteFunc.WriteGuid v ( uint32 pos ) itr.ItemId
            ByteFunc.WriteU32LE v ( uint32 pos + 16u ) itr.Offset
            ByteFunc.WriteU32LE v ( uint32 pos + 20u ) itr.Length
            let f =  ( if itr.IsUser then 1uy else 0uy ) ||| ( if itr.IsVirtualDisk then 2uy else 0uy ) ||| ( if itr.IsRequired then 4uy else 0uy )
            v.[ pos + 24 ] <- f
            Array.blit itr.Data 0 v ( int32 itr.Offset ) itr.Data.Length
        )
        v

    let genFileParameter ( blockSize : uint32 ) ( leaveBlockAllocated : bool ) ( hasParent : bool ) : byte[] =
        let v = Array.zeroCreate<byte> 8
        ByteFunc.WriteU32LE v 0u blockSize
        v.[4] <- ( if leaveBlockAllocated then 1uy else 0uy ) ||| ( if hasParent then 2uy else 0uy )
        v

    let genVirtualDiskSize ( virtualDiskSize : uint64 ) : byte[] =
        let v = Array.zeroCreate<byte> 8
        ByteFunc.WriteU64LE v 0u virtualDiskSize
        v

    let genVirtualDiskId ( virtualDiskId : Guid ) : byte[] =
        let v = Array.zeroCreate<byte> 16
        ByteFunc.WriteGuid v 0u virtualDiskId
        v

    let genLogicalSectorSize ( logicalSectorSize : uint32 ) : byte[] =
        let v = Array.zeroCreate<byte> 4
        ByteFunc.WriteU32LE v 0u logicalSectorSize
        v

    let genPhysicalSectorSize ( physicalSectorSize : uint32 ) : byte[] =
        let v = Array.zeroCreate<byte> 4
        ByteFunc.WriteU32LE v 0u physicalSectorSize
        v

    let genParentLocator ( parentLocator : ( string * string ) [] ) : byte[] =
        let parentLocator_u16 =
            parentLocator
            |> Array.map ( fun ( e, v ) -> ( Encoding.Unicode.GetBytes e, Encoding.Unicode.GetBytes v ) )
        let offset, valueLen =
            parentLocator_u16
            |> Array.mapFold ( fun pos ( e, v ) -> ( uint32 pos, uint32( pos + e.Length ) ), pos + e.Length + v.Length ) 0
        let buflen = 32 + 12 * parentLocator.Length + valueLen
        let v = Array.zeroCreate<byte> buflen

        // parent locator header
        ByteFunc.WriteGuid v 0u ( Guid "B04AEFB7-D19E-4A81-B789-25B8E9445913" )
        ByteFunc.WriteU16LE v 18u ( uint16 parentLocator.Length )

        // parent locator entry, entry and value
        offset
        |> Array.iteri ( fun idx ( eo, vo ) ->
            // parent locator entry
            let pos = 32 + 12 * idx |> uint32
            ByteFunc.WriteU32LE v ( pos + 0u ) eo
            ByteFunc.WriteU32LE v ( pos + 4u ) vo
            let e16, v16 = parentLocator_u16.[idx]
            ByteFunc.WriteU16LE v ( pos + 8u ) ( uint16 e16.Length )
            ByteFunc.WriteU16LE v ( pos + 8u ) ( uint16 v16.Length )
            // entry
            Array.blit e16 0 v ( int eo ) e16.Length
            // value
            Array.blit v16 0 v ( int vo ) v16.Length
        )
        v

    let defFileParameterMTE ( data : byte[] ) =
        {
            ItemId = Guid( "CAA16737-FA36-4D43-B3B6-33F0AA44E76B" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = false;
            IsRequired = true;
            Data = data;
        };

    let defVirtualDiskSizeMTE ( data : byte[] ) =
        {
            ItemId = Guid( "2FA54224-CD1B-4876-B211-5DBED83BF4B8" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    let defVirtualDiskIdMTE ( data : byte[] ) =
        {
            ItemId = Guid( "BECA12AB-B2E6-4523-93EF-C309E000C746" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    let defLogicalSectorSizeMTE ( data : byte[] ) =
        {
            ItemId = Guid( "8141BF1D-A96F-4709-BA47-F233A8FAAB5F" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    let defPhysicalSectorSizeMTE ( data : byte[] ) =
        {
            ItemId = Guid( "CDA348C7-445D-4471-9CC9-E9885251C556" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    let defParentLocatorMTE ( data : byte[] ) =
        {
            ItemId = Guid( "A8D35F2D-B30B-454D-ABF7-D3D84834AB0C" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = false;
            IsRequired = true;
            Data = data;
        };

    let defMetadataTable ( vdi : VirtualDiskInfo ) : byte[] =
        let mte =
            [|
                defFileParameterMTE( genFileParameter vdi.PayloadBlockSize vdi.LeaveBlockAllocated vdi.HasParent );
                defVirtualDiskSizeMTE( genVirtualDiskSize vdi.VirtualDiskSize );
                defVirtualDiskIdMTE( genVirtualDiskId vdi.VirtualDiskId );
                defLogicalSectorSizeMTE( genLogicalSectorSize ( Blocksize.toUInt32 vdi.LogicalSectorSize ) );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize ( Blocksize.toUInt32 vdi.PhysicalSectorSize ) );
                defParentLocatorMTE( genParentLocator ( vdi.ParentLocator |> Seq.map ( fun kv -> kv.Key, kv.Value ) |> Seq.toArray ) );
            |]
            |> Array.mapFold( fun pos itr -> ( { itr with Offset = pos }, pos + itr.Length ) ) 65536u
            |> fst
        genMetadataTable 1048576 mte

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

    [<Theory>]
    [<InlineData( 0, "The metadata region must be at least 1 MB" )>]
    [<InlineData( 1048575, "The metadata region must be at least 1 MB" )>]
    [<InlineData( 1048577, "The metadata region must be a multiple of 1 MB" )>]
    member _.ReadMetadata_Fail_001( len : int ) ( expmsg : string ) =
        let v = Array.zeroCreate<byte> len
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadMetadata v |> ignore
            )
        Assert.StartsWith( expmsg, r.Message )

    static member m_ReadMetadata_Header_Fail_001_data : obj[][] = [|
        [|  // Signature
            0; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |];
            "The signatures in the metadata table do not match";
        |];
        [|  // EntryCount equals zero.
            10; [| 0x00uy; 0x00uy; |];
            "The number of metadata entries is invalid";
        |];
        [|  // EntryCount equals 2048.
            10; [| 0x00uy; 0x08uy; |];
            "The number of metadata entries is invalid";
        |];
        [|  // EntryCount equals -1.
            10; [| 0xFFuy; 0xFFuy; |];
            "The number of metadata entries is invalid";
        |];
        [|  // Offset equals 65535.
            48; [| 0xFFuy; 0xFFuy; 0x00uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
        [|  // Offset equals 0 and length is greator than zero.
            48; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
        [|  // Offset equals 0 and length is greator than zero.
            48; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
        [|  // Length equals 0 and offset is greator than zero.
            52; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
        [|  // offset(FFFC0000) + length(80000) overflow. 
            48; [| 0x00uy; 0x00uy; 0xFCuy; 0xFFuy; 0x00uy; 0x00uy; 0x08uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
        [|  // offset(C0000) + length(40001) exceeds data.Length.
            48; [| 0x00uy; 0x00uy; 0x0Cuy; 0x00uy; 0x01uy; 0x00uy; 0x04uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
        [|  // The length exceeds 1M.
            52; [| 0x01uy; 0x00uy; 0x10uy; 0x00uy; |];
            "There are invalid metadata entry";
        |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadMetadata_Header_Fail_001_data" )>]
    member _.ReadMetadata_Header_Fail_001 ( pos1 : int ) ( patch1 : byte[] ) ( expmsg : string ) =
        let vdi = {
            PayloadBlockSize = 1048576u;
            LeaveBlockAllocated = false;
            HasParent = false;
            VirtualDiskSize = 1073741824UL;
            VirtualDiskId = Guid();
            LogicalSectorSize = Blocksize.BS_512;
            PhysicalSectorSize = Blocksize.BS_512;
            ParentLocator = [||] |> Map<string,string>;
        }
        let v = defMetadataTable vdi
        Array.blit patch1 0 v pos1 patch1.Length
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadMetadata v |> ignore
            )
        Assert.StartsWith( expmsg, r.Message )

    [<Fact>]
    member _.ReadMetadata_Header_Fail_002 () =
        let v =
            [|
                for i = 0 to 1024 do
                    {
                        ItemId = Guid.NewGuid();
                        Offset = 0u;
                        Length = 0u;
                        IsUser = true;
                        IsVirtualDisk = true;
                        IsRequired = false;
                        Data = [||];
                    };
            |]
            |> genMetadataTable 1048576
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadMetadata v |> ignore
            )
        Assert.StartsWith( "The number of user entries is incorrect", r.Message )

    [<Theory>]
    [<InlineData( 0x10000u, 0x10u, 0x10000u, 0x10u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10000u, 0x30u, 0x10000u, 0x10u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10000u, 0x30u, 0x10010u, 0x10u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10000u, 0x30u, 0x10020u, 0x10u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10000u, 0x10u, 0x10000u, 0x30u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10010u, 0x10u, 0x10000u, 0x30u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10020u, 0x10u, 0x10000u, 0x30u, 0x10030u, 0x10u )>]
    [<InlineData( 0x10030u, 0x10u, 0x10010u, 0x20u, 0x10000u, 0x20u )>]
    member _.ReadMetadata_Header_Fail_003 ( a : uint32 ) ( b : uint32 ) ( c : uint32 ) ( d : uint32 ) ( e : uint32 ) ( f : uint32 ) =
        let v =
            [|
                {
                    ItemId = Guid.NewGuid();
                    Offset = a;
                    Length = b;
                    IsUser = false;
                    IsVirtualDisk = true;
                    IsRequired = true;
                    Data = Array.zeroCreate<byte>( int32 b );
                };
                {
                    ItemId = Guid.NewGuid();
                    Offset = c;
                    Length = d;
                    IsUser = false;
                    IsVirtualDisk = true;
                    IsRequired = true;
                    Data = Array.zeroCreate<byte>( int32 d );
                };
                {
                    ItemId = Guid.NewGuid();
                    Offset = e;
                    Length = f;
                    IsUser = false;
                    IsVirtualDisk = true;
                    IsRequired = true;
                    Data = Array.zeroCreate<byte>( int32 f );
                };
            |]
            |> genMetadataTable 1048576
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadMetadata v |> ignore
            )
        Assert.StartsWith( "There are metadata items with overlapping ranges", r.Message )
