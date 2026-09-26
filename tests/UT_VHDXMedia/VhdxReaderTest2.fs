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

    let updateMTEOffset ( mdi : MetadataTableEntry[] ) : MetadataTableEntry[] =
        mdi
        |> Array.mapFold
            ( fun pos itr ->
                let ne = if itr.Length > 0u then { itr with Offset = pos } else itr
                let npos = pos + itr.Length
                ( ne, npos )
            ) 65536u
        |> fst

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
        [|
            defFileParameterMTE( genFileParameter vdi.PayloadBlockSize vdi.LeaveBlockAllocated vdi.HasParent );
            defVirtualDiskSizeMTE( genVirtualDiskSize vdi.VirtualDiskSize );
            defVirtualDiskIdMTE( genVirtualDiskId vdi.VirtualDiskId );
            defLogicalSectorSizeMTE( genLogicalSectorSize ( Blocksize.toUInt32 vdi.LogicalSectorSize ) );
            defPhysicalSectorSizeMTE( genPhysicalSectorSize ( Blocksize.toUInt32 vdi.PhysicalSectorSize ) );
            defParentLocatorMTE( genParentLocator ( vdi.ParentLocator |> Seq.map ( fun kv -> kv.Key, kv.Value ) |> Seq.toArray ) );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576

    let checkReadMetadataFailResult ( expmsg : string ) ( v : byte[] ) =
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadMetadata v |> ignore
            )
        Assert.StartsWith( expmsg, r.Message )


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
        checkReadMetadataFailResult expmsg ( Array.zeroCreate<byte> len )

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
        checkReadMetadataFailResult expmsg v

    [<Fact>]
    member _.ReadMetadata_Header_Fail_002 () =
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
        |> checkReadMetadataFailResult "The number of user entries is incorrect"

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
        [|
            {
                ItemId = Guid.NewGuid();
                Offset = a;
                Length = b;
                IsUser = false;
                IsVirtualDisk = true;
                IsRequired = false;
                Data = Array.zeroCreate<byte>( int32 b );
            };
            {
                ItemId = Guid.NewGuid();
                Offset = c;
                Length = d;
                IsUser = false;
                IsVirtualDisk = true;
                IsRequired = false;
                Data = Array.zeroCreate<byte>( int32 d );
            };
            {
                ItemId = Guid.NewGuid();
                Offset = e;
                Length = f;
                IsUser = false;
                IsVirtualDisk = true;
                IsRequired = false;
                Data = Array.zeroCreate<byte>( int32 f );
            };
        |]
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "There are metadata items with overlapping ranges"

    [<Fact>]
    member _.ReadMetadata_Header_Fail_004 () =
        [|
            defFileParameterMTE( genFileParameter 1048576u true false );
            {
                ItemId = Guid.NewGuid();
                Offset = 0u;
                Length = 16u;
                IsUser = false;
                IsVirtualDisk = true;
                IsRequired = true;
                Data = Array.zeroCreate<byte>( 16 );
            };
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid() ) );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "There are unknown item for which IsRequired is true"

    static member m_ReadMetadata_Header_Fail_005_data : obj[][] = [|
        [|
            [| ( false, 0 ); ( false, 1 ); ( false, 1 ); |]; "There are items with duplicate ItemIDs among the items where IsUser is false";
        |];
        [|
            [| ( false, 0 ); ( false, 0 ); ( true, 1 ); |]; "There are items with duplicate ItemIDs among the items where IsUser is false";
        |];
        [|
            [| ( true, 0 ); ( true, 0 ); ( true, 1 ); |]; "There are items with duplicate ItemIDs among the items where IsUser is true";
        |];
        [|
            [| ( false, 0 ); ( true, 1 ); ( true, 1 ); |]; "There are items with duplicate ItemIDs among the items where IsUser is true";
        |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadMetadata_Header_Fail_005_data" )>]
    member _.ReadMetadata_Header_Fail_005 ( sguid : ( bool * int )[] ) ( expmsg : string ) =
        [|
            for ( f, g ) in sguid do
                {
                    ItemId = Guid( sprintf "%032X" g );
                    Offset = 0u;
                    Length = 0u;
                    IsUser = f;
                    IsVirtualDisk = true;
                    IsRequired = false;
                    Data = [||];
                };
        |]
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult expmsg

    [<Fact>]
    member _.ReadMetadata_Header_001 () =
        let diskid = Guid.NewGuid()
        let v =
            [|
                defFileParameterMTE( genFileParameter 1048576u true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
                defVirtualDiskIdMTE( genVirtualDiskId diskid );
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );
                for i = 1 to 1024 do
                    {
                        ItemId = Guid.NewGuid();
                        Offset = 0u;
                        Length = 0u;
                        IsUser = true;
                        IsVirtualDisk = true;
                        IsRequired = false;
                        Data = [||];
                    };
                for i = 1 to 1018 do
                    {
                        ItemId = Guid.NewGuid();
                        Offset = 0u;
                        Length = 0u;
                        IsUser = false;
                        IsVirtualDisk = true;
                        IsRequired = false;
                        Data = [||];
                    };
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        Assert.StrictEqual( 1048576u, r.PayloadBlockSize )
        Assert.True( r.LeaveBlockAllocated )
        Assert.False( r.HasParent )
        Assert.StrictEqual( 67108864UL, r.VirtualDiskSize )
        Assert.StrictEqual( diskid, r.VirtualDiskId )
        Assert.StrictEqual( Blocksize.BS_512, r.LogicalSectorSize )
        Assert.StrictEqual( Blocksize.BS_512, r.PhysicalSectorSize )

    [<Fact>]
    member _.ReadMetadata_Header_002 () =
        let diskid = Guid.NewGuid()
        let v =
            [|
                defFileParameterMTE( genFileParameter 1048576u true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
                defVirtualDiskIdMTE( genVirtualDiskId diskid );
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );
                for i = 1 to 2042 do
                    {
                        ItemId = Guid.NewGuid();
                        Offset = 0u;
                        Length = 0u;
                        IsUser = false;
                        IsVirtualDisk = true;
                        IsRequired = false;
                        Data = [||];
                    };
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        Assert.StrictEqual( 1048576u, r.PayloadBlockSize )
        Assert.True( r.LeaveBlockAllocated )
        Assert.False( r.HasParent )
        Assert.StrictEqual( 67108864UL, r.VirtualDiskSize )
        Assert.StrictEqual( diskid, r.VirtualDiskId )
        Assert.StrictEqual( Blocksize.BS_512, r.LogicalSectorSize )
        Assert.StrictEqual( Blocksize.BS_512, r.PhysicalSectorSize )

    [<Fact>]
    member _.ReadMetadata_Header_003 () =
        let diskid = Guid.NewGuid()
        let v =
            [|
                defFileParameterMTE( genFileParameter 1048576u true false );    // 8 bytes
                defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );         // 8 bytes
                defVirtualDiskIdMTE( genVirtualDiskId diskid );                 // 16 bytes
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );           // 4 bytes
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );         // 4 bytes ( total 40 bytes )
                {
                    ItemId = Guid.NewGuid();
                    Offset = 0u;
                    Length = 1048576u;                                          // 1MB
                    IsUser = false;
                    IsVirtualDisk = true;
                    IsRequired = false;
                    Data = Array.zeroCreate<byte> 1048576;
                };
                {
                    ItemId = Guid.NewGuid();
                    Offset = 0u;
                    Length = 983000u;                                           // 1MB - 64KB - 40 bytes
                    IsUser = false;
                    IsVirtualDisk = true;
                    IsRequired = false;
                    Data = Array.zeroCreate<byte> 983000;
                };
            |]
            |> updateMTEOffset
            |> genMetadataTable 2097152
        let r = VhdxReader.ReadMetadata v
        Assert.StrictEqual( 1048576u, r.PayloadBlockSize )
        Assert.True( r.LeaveBlockAllocated )
        Assert.False( r.HasParent )
        Assert.StrictEqual( 67108864UL, r.VirtualDiskSize )
        Assert.StrictEqual( diskid, r.VirtualDiskId )
        Assert.StrictEqual( Blocksize.BS_512, r.LogicalSectorSize )
        Assert.StrictEqual( Blocksize.BS_512, r.PhysicalSectorSize )

    [<Fact>]
    member _.ReadMetadata_FileParameter_Fail_001 () =
        [|
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Metadata item(file parameter) missing"

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 7 )>]
    [<InlineData( 9 )>]
    member _.ReadMetadata_FileParameter_Fail_002 ( len : int32 ) =
        [|
            {
                ItemId = Guid( "CAA16737-FA36-4D43-B3B6-33F0AA44E76B" );
                Offset = if len = 0 then 0u else 65536u;
                Length = uint32 len;
                IsUser = false;
                IsVirtualDisk = false;
                IsRequired = true;
                Data = Array.zeroCreate<byte>( len );
            };
        |]
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Invalid Length of metadata item(file parameter)"

    [<Theory>]
    [<InlineData( 0u )>]
    [<InlineData( 1048575u )>]      // 1MB - 1
    [<InlineData( 268435457u )>]    // 256MB + 1
    [<InlineData( 3145728u )>]      // 3MB
    [<InlineData( 0xFFFFFFFFu )>]
    member _.ReadMetadata_FileParameter_Fail_003 ( len : uint32 ) =
        [|
            { defFileParameterMTE( genFileParameter len true true ) with Offset = 65536u }
        |]
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Incorrect payload block size"

    [<Theory>]
    [<InlineData( 0x00100000u )>]   // 1MB
    [<InlineData( 0x00200000u )>]   // 2MB
    [<InlineData( 0x00400000u )>]   // 4MB
    [<InlineData( 0x00800000u )>]   // 8MB
    [<InlineData( 0x01000000u )>]   // 16MB
    [<InlineData( 0x02000000u )>]   // 32MB
    [<InlineData( 0x04000000u )>]   // 64MB
    [<InlineData( 0x08000000u )>]   // 128MB
    [<InlineData( 0x10000000u )>]   // 256MB
    member _.ReadMetadata_FileParameter_001 ( len : uint32 ) =
        let v =
            [|
                defFileParameterMTE( genFileParameter len true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
                defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        Assert.StrictEqual( len, r.PayloadBlockSize )

    [<Fact>]
    member _.ReadMetadata_VirtualDiskSize_Fail_001 () =
        [|
            { defFileParameterMTE( genFileParameter 0x00100000u true true ) with Offset = 65536u }
        |]
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Metadata item(virtual disk size) missing"

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 7 )>]
    [<InlineData( 9 )>]
    member _.ReadMetadata_VirtualDiskSize_Fail_002 ( len : int32 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( Array.zeroCreate<byte> len );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Length of metadata item(virtual disk size) is invalid"

    [<Theory>]
    [<InlineData( 0x0UL )>]
    [<InlineData( 0x400000000001UL )>]
    [<InlineData( 0xFFFFFFFFFFFFFFFFUL )>]
    member _.ReadMetadata_VirtualDiskSize_Fail_003 ( dsize : uint64 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize dsize );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "The virtual disk size"

    [<Theory>]
    [<InlineData( 0x200UL )>]               // 512 bytes
    [<InlineData( 0x400000000000UL )>]      // 64TB
    [<InlineData( 0x3FFFFFFFFE00UL )>]      // 64TB - 512
    member _.ReadMetadata_VirtualDiskSize_001 ( dsize : uint64 ) =
        let v =
            [|
                defFileParameterMTE( genFileParameter 0x00100000u true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize dsize );
                defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        Assert.StrictEqual( dsize, r.VirtualDiskSize )

    [<Fact>]
    member _.ReadMetadata_VirtualDiskID_Fail_001 () =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Metadata item(virtual disk ID) missing"

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 15 )>]
    [<InlineData( 17 )>]
    member _.ReadMetadata_VirtualDiskID_Fail_002 ( len : int32 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( Array.zeroCreate<byte> len );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Length of metadata item(virtual disk ID) is invalid"

    [<Fact>]
    member _.ReadMetadata_VirtualDiskID_001 () =
        let diskid = Guid.NewGuid()
        let v =
            [|
                defFileParameterMTE( genFileParameter 0x00100000u true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
                defVirtualDiskIdMTE( genVirtualDiskId diskid );
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        Assert.StrictEqual( diskid, r.VirtualDiskId )

    [<Fact>]
    member _.ReadMetadata_LogicalSectorSize_Fail_001 () =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Metadata item(logical sector size) missing"

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 3 )>]
    [<InlineData( 5 )>]
    member _.ReadMetadata_LogicalSectorSize_Fail_002 ( len : int32 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
            defLogicalSectorSizeMTE( Array.zeroCreate<byte> len );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Length of metadata item(logical sector size) is invalid"

    [<Theory>]
    [<InlineData( 0u )>]
    [<InlineData( 511u )>]
    [<InlineData( 513u )>]
    [<InlineData( 4095u )>]
    [<InlineData( 4097u )>]
    [<InlineData( 0xFFFFFFFFu )>]
    member _.ReadMetadata_LogicalSectorSize_Fail_003 ( lss : uint32 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
            defLogicalSectorSizeMTE( genLogicalSectorSize lss );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Incorrect logical sector size"

    [<Theory>]
    [<InlineData( 512u,  1UL )>]
    [<InlineData( 512u,  511UL )>]
    [<InlineData( 512u,  513UL )>]
    [<InlineData( 512u,  4095UL )>]
    [<InlineData( 512u,  4097UL )>]
    [<InlineData( 512u,  0x3FFFFFFFFAEDUL )>]      // 64TB - 513
    [<InlineData( 512u,  0x3FFFFFFFFAEFUL )>]      // 64TB - 511
    [<InlineData( 4096u, 1UL )>]
    [<InlineData( 4096u, 512UL )>]
    [<InlineData( 4096u, 4095UL )>]
    [<InlineData( 4096u, 4097UL )>]
    [<InlineData( 4096u, 0x3FFFFFFFEFFFUL )>]      // 64TB - 4097
    [<InlineData( 4096u, 0x3FFFFFFFF001UL )>]      // 64TB - 4095
    [<InlineData( 4096u, 0x3FFFFFFFFE00UL )>]      // 64TB - 512
    member _.ReadMetadata_LogicalSectorSize_Fail_004 ( lss : uint32 ) ( dsize : uint64 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize dsize );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
            defLogicalSectorSizeMTE( genLogicalSectorSize lss );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "The virtual disk size is not a multiple of the logical sector size"

    [<Theory>]
    [<InlineData( 512u,  512UL )>]
    [<InlineData( 512u,  4096UL )>]
    [<InlineData( 512u,  0x3FFFFFFFF000UL )>]      // 64TB - 4096
    [<InlineData( 512u,  0x3FFFFFFFFE00UL )>]      // 64TB - 512
    [<InlineData( 512u,  0x400000000000UL )>]      // 64TB
    [<InlineData( 4096u, 4096UL )>]
    [<InlineData( 4096u, 0x3FFFFFFFF000UL )>]      // 64TB - 4096
    [<InlineData( 4096u, 0x400000000000UL )>]      // 64TB
    member _.ReadMetadata_LogicalSectorSize_001 ( lss : uint32 ) ( dsize : uint64 ) =
        let v =
            [|
                defFileParameterMTE( genFileParameter 0x00100000u true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize dsize );
                defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
                defLogicalSectorSizeMTE( genLogicalSectorSize lss );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize 512u );
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        if lss = 512u then
            Assert.StrictEqual( Blocksize.BS_512, r.LogicalSectorSize )
        else
            Assert.StrictEqual( Blocksize.BS_4096, r.LogicalSectorSize )

    [<Fact>]
    member _.ReadMetadata_PhysicalSectorSize_Fail_001 () =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
            defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Metadata item(physical sector size) missing"

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 3 )>]
    [<InlineData( 5 )>]
    member _.ReadMetadata_PhysicalSectorSize_Fail_002 ( len : int32 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
            defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
            defPhysicalSectorSizeMTE( Array.zeroCreate<byte> len );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Length of metadata item(physical sector size) is invalid"

    [<Theory>]
    [<InlineData( 0u )>]
    [<InlineData( 511u )>]
    [<InlineData( 513u )>]
    [<InlineData( 4095u )>]
    [<InlineData( 4097u )>]
    [<InlineData( 0xFFFFFFFFu )>]
    member _.ReadMetadata_PhysicalSectorSize_Fail_003 ( pss : uint32 ) =
        [|
            defFileParameterMTE( genFileParameter 0x00100000u true false );
            defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
            defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
            defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
            defPhysicalSectorSizeMTE( genPhysicalSectorSize pss );
        |]
        |> updateMTEOffset
        |> genMetadataTable 1048576
        |> checkReadMetadataFailResult "Incorrect physical sector size"

    [<Theory>]
    [<InlineData( 512u )>]
    [<InlineData( 4096u )>]
    member _.ReadMetadata_PhysicalSectorSize_001 ( pss : uint32 ) =
        let v =
            [|
                defFileParameterMTE( genFileParameter 0x00100000u true false );
                defVirtualDiskSizeMTE( genVirtualDiskSize 67108864UL );
                defVirtualDiskIdMTE( genVirtualDiskId ( Guid.NewGuid() ) );
                defLogicalSectorSizeMTE( genLogicalSectorSize 512u );
                defPhysicalSectorSizeMTE( genPhysicalSectorSize pss );
            |]
            |> updateMTEOffset
            |> genMetadataTable 1048576
        let r = VhdxReader.ReadMetadata v
        if pss = 512u then
            Assert.StrictEqual( Blocksize.BS_512, r.PhysicalSectorSize )
        else
            Assert.StrictEqual( Blocksize.BS_4096, r.PhysicalSectorSize )
