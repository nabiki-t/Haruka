//=============================================================================
// Haruka Software Storage.
// VhdxReaderTest3.fs : Test cases for VhdxReader class.
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
open System.Runtime.InteropServices

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test

//=============================================================================
// Type definitions

//=============================================================================
// Class implementation

type VhdxReader_Test3 () =

    // chunkSize : 4GB
    // chunkRatio : 16
    // payloadBlockCount : 256
    // sectorBitmapBlockCount : 16
    // batEntryCount : 271
    let defaultVDI : VirtualDiskInfo = {
        PayloadBlockSize = 268435456u;      // 256MB
        LeaveBlockAllocated = false;
        HasParent = false;
        VirtualDiskSize = 0x1000000000UL;    // 64GB
        VirtualDiskId = Guid();
        LogicalSectorSize = Blocksize.BS_512;
        PhysicalSectorSize = Blocksize.BS_512;
        ParentLocator = Map.empty;
    }

    let defaultBatRegion : RegionEntry = {
        Guid = VhdxCommons.REGENT_TYPE_BAT;
        FileOffset = 0UL;
        Length = 1048576ul;
        Required = true;
    }



    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    static member m_GetPayloadBlockEntry_001_data : obj[][] = [|
        [| 0UL; 0UL; BatEntryStatePB.PayloadNotPresent;       0x000000UL; |];
        [| 1UL; 1UL; BatEntryStatePB.PayloadUndefined;        0x000000UL; |];
        [| 2UL; 2UL; BatEntryStatePB.PayloadZero;             0x000000UL; |];
        [| 3UL; 3UL; BatEntryStatePB.PayloadUnapped;          0x000000UL; |];
        [| 4UL; 5UL; BatEntryStatePB.PayloadFullyPresent;     0x600000UL; |];
        [| 5UL; 6UL; BatEntryStatePB.PayloadPartiallyPresent; 0x700000UL; |];
        [| 6UL; 7UL; BatEntryStatePB.PayloadNotPresent;       0x000000UL; |];
        [| 7UL; 8UL; BatEntryStatePB.PayloadUndefined;        0x000000UL; |];
    |]

    [<Theory>]
    [<MemberData( "m_GetPayloadBlockEntry_001_data" )>]
    member _.GetPayloadBlockEntry_001 ( idx : uint64 ) ( expBatEntryIndex : uint64 ) ( expState : BatEntryStatePB ) ( expFileOffset : uint64 ) =
        let v =
            [|
                ( 0uy, 0x100000UL );  // payload 0
                ( 1uy, 0x200000UL );  // payload 1
                ( 2uy, 0x300000UL );  // payload 2
                ( 3uy, 0x400000UL );  // payload 3
                ( 0uy, 0x500000UL );  // sector bitmap 0
                ( 6uy, 0x600000UL );  // payload 4
                ( 7uy, 0x700000UL );  // payload 5
                ( 0uy, 0x800000UL );  // payload 6
                ( 1uy, 0x000000UL );  // payload 7
                ( 6uy, 0xA00000UL );  // sector bitmap 1
            |]
            |> VhdxTestFunc.GenBATEntries
        let r = VhdxReader.GetPayloadBlockEntry v 4UL idx
        Assert.StrictEqual( expBatEntryIndex, r.BatEntryIndex )
        Assert.StrictEqual( expState, r.State )
        Assert.StrictEqual( expFileOffset, r.FileOffset )

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.GetPayloadBlockEntry_Fail_001 ( idx : uint64 ) =
        let v =
            [|
                ( 4uy, 0x100000UL );  // payload 0
                ( 5uy, 0x200000UL );  // payload 1
            |]
            |> VhdxTestFunc.GenBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetPayloadBlockEntry v 4UL idx |> ignore
            )
        Assert.StartsWith( "A reserved payload BAT entry state value was specified", r.Message )

    [<Theory>]
    [<InlineData( 6uy )>]
    [<InlineData( 7uy )>]
    member _.GetPayloadBlockEntry_Fail_002 ( s : byte ) =
        let v =
            [|
                ( s, 0x1FFFFFUL );  // payload 0
            |]
            |> VhdxTestFunc.GenBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetPayloadBlockEntry v 4UL 0UL |> ignore
            )
        Assert.StartsWith( "The FileOffset value of the payload BAT entry must be a multiple of 1 MB", r.Message )

    [<Theory>]
    [<InlineData( 6uy )>]
    [<InlineData( 7uy )>]
    member _.GetPayloadBlockEntry_Fail_003 ( s : byte ) =
        let v =
            [|
                ( s, 0UL );  // payload 0
            |]
            |> VhdxTestFunc.GenBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetPayloadBlockEntry v 4UL 0UL |> ignore
            )
        Assert.StartsWith( "The FileOffset value of the payload BAT entry must be at least 1 MB", r.Message )


    static member m_GetSectorBitmapBlockEntry_001_data : obj[][] = [|
        [| 0UL; 4UL; BatEntryStateSB.SectorBitmapNotPresent; 0x000000UL; |];
        [| 1UL; 9UL; BatEntryStateSB.SectorBitmapPresent;    0xA00000UL; |];
    |]

    [<Theory>]
    [<MemberData( "m_GetSectorBitmapBlockEntry_001_data" )>]
    member _.GetSectorBitmapBlockEntry_001 ( idx : uint64 ) ( expBatEntryIndex : uint64 ) ( expState : BatEntryStateSB ) ( expFileOffset : uint64 ) =
        let v =
            [|
                ( 0uy, 0x100000UL );  // payload 0
                ( 1uy, 0x200000UL );  // payload 1
                ( 2uy, 0x300000UL );  // payload 2
                ( 3uy, 0x400000UL );  // payload 3
                ( 0uy, 0x500000UL );  // sector bitmap 0
                ( 6uy, 0x600000UL );  // payload 4
                ( 7uy, 0x700000UL );  // payload 5
                ( 0uy, 0x800000UL );  // payload 6
                ( 1uy, 0x900000UL );  // payload 7
                ( 6uy, 0xA00000UL );  // sector bitmap 1
            |]
            |> VhdxTestFunc.GenBATEntries
        let struct ( entryIndex, state, fileOffset ) = VhdxReader.GetSectorBitmapBlockEntry v 4UL idx
        Assert.StrictEqual( expBatEntryIndex, entryIndex )
        Assert.StrictEqual( expState, state )
        Assert.StrictEqual( expFileOffset, fileOffset )

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    [<InlineData( 2UL )>]
    [<InlineData( 3UL )>]
    [<InlineData( 4UL )>]
    [<InlineData( 5UL )>]
    member _.GetSectorBitmapBlockEntry_Fail_001 ( idx : uint64 ) =
        let v =
            [|
                for i = 0 to 3 do ( 0uy, 0x100000UL );  // payload 0 - 3
                ( 1uy, 0x100000UL );                    // sector bitmap 0
                for i = 0 to 3 do ( 0uy, 0x100000UL );  // payload 4 - 7
                ( 2uy, 0x100000UL );                    // sector bitmap 1
                for i = 0 to 3 do ( 0uy, 0x100000UL );  // payload 8 - 11
                ( 3uy, 0x100000UL );                    // sector bitmap 2
                for i = 0 to 3 do ( 0uy, 0x100000UL );  // payload 12 - 15
                ( 4uy, 0x100000UL );                    // sector bitmap 3
                for i = 0 to 3 do ( 0uy, 0x100000UL );  // payload 16 - 19
                ( 5uy, 0x100000UL );                    // sector bitmap 4
                for i = 0 to 3 do ( 0uy, 0x100000UL );  // payload 20 - 23
                ( 7uy, 0x100000UL );                    // sector bitmap 5
            |]
            |> VhdxTestFunc.GenBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetSectorBitmapBlockEntry v 4UL idx |> ignore
            )
        Assert.StartsWith( "A reserved sector bitmap BAT entry state value was specified", r.Message )

    [<Fact>]
    member _.GetSectorBitmapBlockEntry_Fail_002 () =
        let v =
            [|
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 0 - 3
                ( 6uy, 0x1FFFFFUL );        // sector bitmap 0
            |]
            |> VhdxTestFunc.GenBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetSectorBitmapBlockEntry v 4UL 0UL |> ignore
            )
        Assert.StartsWith( "The FileOffset value of the sector bitmap BAT entry must be a multiple of 1 MB", r.Message )

    [<Fact>]
    member _.GetSectorBitmapBlockEntry_Fail_003 () =
        let v =
            [|
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 0 - 3
                ( 6uy, 0UL );               // sector bitmap 0
            |]
            |> VhdxTestFunc.GenBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetSectorBitmapBlockEntry v 4UL 0UL |> ignore
            )
        Assert.StartsWith( "The FileOffset value of the sector bitmap BAT entry must be at least 1 MB", r.Message )

    [<Fact>]
    member _.ReadBat_Fail_001 () =
        task {
            let batRegion = {
                defaultBatRegion with
                    Length = 0u;
            }

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let! _ = VhdxReader.ReadBat [] 0UL fa batRegion defaultVDI
                        ()
                    } )
                Assert.StartsWith( "The BAT entry has insufficient data length", r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 8u,   0x100006UL,           0x2000000000UL,       "The regions indicated by BAT entries must not overlap" )>]
    [<InlineData( 128u, 0x1100006UL,          0x2000000000UL,       "The regions indicated by BAT entries must not overlap" )>]
    [<InlineData( 8u,   0x1000000006UL,       0x1000000000UL,       "Invalid file offset in the payload BAT entry" )>]
    [<InlineData( 8u,   0xFF0100006UL,        0x1000000000UL,       "Invalid file offset in the payload BAT entry" )>]
    [<InlineData( 8u,   0xFFFFFFFFFFF00006UL, 0x7FFFFFFFFFFFFFFFUL, "Invalid file offset in the payload BAT entry" )>]
    [<InlineData( 128u, 0x2000000006UL,       0x2000000000UL,       "Invalid file offset in the sector bitmap BAT entry" )>]
    [<InlineData( 128u, 0xFFFFFFFFFFF00006UL, 0x7FFFFFFFFFFFFFFFUL, "Invalid file offset in the sector bitmap BAT entry" )>]
    member _.ReadBat_Fail_002 ( patchpos : uint32 ) ( patch : uint64 ) ( lastFileSize : uint64 ) ( expmsg : string ) =
        task {
            // batEntryCount : 272
            let vdi = {
                defaultVDI with
                    HasParent = true;
            }

            let batData = VhdxTestFunc.DefaultBatEntries vdi.PayloadBlockSize 16u 272u true
            ByteFunc.WriteU64LE batData patchpos patch

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 1048576UL
                do! fa.Write 0UL ( ArraySegment batData )

                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let! _ = VhdxReader.ReadBat [] lastFileSize fa defaultBatRegion vdi
                        ()
                    } )
                Assert.StartsWith( expmsg, r.Message )

            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.ReadBat_001 () =
        task {
            // batEntryCount : 272
            let vdi = {
                defaultVDI with
                    HasParent = true;
            }

            let sbdata = [|
                for i = 0 to 15 do
                    let v = Array.zeroCreate<byte> 1048576
                    Random.Shared.NextBytes v
                    yield v
            |]

            let batData = VhdxTestFunc.DefaultBatEntries vdi.PayloadBlockSize 16u 272u true
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 17825792UL   // 17MB
                do! fa.Write 0UL ( ArraySegment batData )

                for idx = 0 to 15 do
                    do! fa.Write ( ( uint64 idx + 1UL ) * 1048576UL ) ( ArraySegment sbdata.[idx] )

                let! r = VhdxReader.ReadBat [] 0x2000000000UL fa defaultBatRegion vdi
                Assert.StrictEqual( 0UL, r.BATRegionOffset )
                Assert.StrictEqual( 1048576ul, r.BATRegionLength )
                Assert.StrictEqual( 0x100000000UL, r.ChunkSize )    // 4GB
                Assert.StrictEqual( 16UL, r.ChunkRatio )
                Assert.StrictEqual( 256UL, r.PayloadBlockCount )
                Assert.StrictEqual( 16UL, r.SectorBitmapBlockCount )
                Assert.StrictEqual( 272UL, r.BatEntryCount )

                Assert.StrictEqual( 0UL, r.Payloads.[0].BatEntryIndex )
                Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, r.Payloads.[0].State )
                Assert.StrictEqual( 17825792UL, r.Payloads.[0].FileOffset )

                Assert.StrictEqual( 270UL, r.Payloads.[255].BatEntryIndex )
                Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, r.Payloads.[255].State )
                Assert.StrictEqual( 17UL * 1048576UL + 255UL * 1048576UL * 256UL, r.Payloads.[255].FileOffset )

                for i = 0 to 15 do
                    Assert.StrictEqual( ( uint64 i + 1UL ) * 17UL - 1UL, r.SectorBitmap.[i].BatEntryIndex )
                    Assert.StrictEqual( BatEntryStateSB.SectorBitmapPresent, r.SectorBitmap.[i].SBState )
                    Assert.StrictEqual( ( uint64 i + 1UL ) * 1048576UL, r.SectorBitmap.[i].FileOffset )
                    Assert.True( sbdata.[i] = r.SectorBitmap.[i].Bitmap )

            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.ReadBat_002 () =
        task {
            // batEntryCount : 272
            let vdi = {
                defaultVDI with
                    HasParent = true;
            }
            let batData = Array.replicate 272 ( 0uy, 0UL ) |> VhdxTestFunc.GenBATEntries
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 17825792UL   // 17MB
                do! fa.Write 0UL ( ArraySegment batData )

                let! r = VhdxReader.ReadBat [] 0x2000000000UL fa defaultBatRegion vdi
                Assert.StrictEqual( 0UL, r.BATRegionOffset )
                Assert.StrictEqual( 1048576ul, r.BATRegionLength )
                Assert.StrictEqual( 256UL, r.PayloadBlockCount )
                Assert.StrictEqual( 16UL, r.SectorBitmapBlockCount )
                Assert.StrictEqual( 272UL, r.BatEntryCount )

                for i = 0 to 15 do
                    Assert.StrictEqual( ( uint64 i + 1UL ) * 17UL - 1UL, r.SectorBitmap.[i].BatEntryIndex )
                    Assert.StrictEqual( BatEntryStateSB.SectorBitmapNotPresent, r.SectorBitmap.[i].SBState )
                    Assert.StrictEqual( 0UL, r.SectorBitmap.[i].FileOffset )
                    Assert.Empty r.SectorBitmap.[i].Bitmap

            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.ReadBat_003 () =
        task {
            // Set values ​​in the sector bitmap entries.
            let batData = VhdxTestFunc.DefaultBatEntries defaultVDI.PayloadBlockSize 16u 272u true
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 1048576UL   // 1MB
                do! fa.Write 0UL ( ArraySegment batData )

                let! r = VhdxReader.ReadBat [] 0x2000000000UL fa defaultBatRegion defaultVDI
                Assert.StrictEqual( 0UL, r.BATRegionOffset )
                Assert.StrictEqual( 1048576ul, r.BATRegionLength )
                Assert.StrictEqual( 0x100000000UL, r.ChunkSize )    // 4GB
                Assert.StrictEqual( 16UL, r.ChunkRatio )
                Assert.StrictEqual( 256UL, r.PayloadBlockCount )
                Assert.StrictEqual( 16UL, r.SectorBitmapBlockCount )
                Assert.StrictEqual( 271UL, r.BatEntryCount )

                Assert.StrictEqual( 0UL, r.Payloads.[0].BatEntryIndex )
                Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, r.Payloads.[0].State )
                Assert.StrictEqual( 17825792UL, r.Payloads.[0].FileOffset )

                Assert.StrictEqual( 270UL, r.Payloads.[255].BatEntryIndex )
                Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, r.Payloads.[255].State )
                Assert.StrictEqual( 17UL * 1048576UL + 255UL * 1048576UL * 256UL, r.Payloads.[255].FileOffset )

                for i = 0 to 15 do
                    Assert.StrictEqual( ( uint64 i + 1UL ) * 17UL - 1UL, r.SectorBitmap.[i].BatEntryIndex )
                    Assert.StrictEqual( BatEntryStateSB.SectorBitmapNotPresent, r.SectorBitmap.[i].SBState )
                    Assert.StrictEqual( 0UL, r.SectorBitmap.[i].FileOffset )
                    Assert.Empty( r.SectorBitmap.[i].Bitmap )

            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 0x2FFFFUL )>]
    member _.ReadVhdx_Fail_001 ( fsize : uint64 ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize fsize
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx fa
                    ()
                })
                Assert.StartsWith( "The VHDX file is too small", r.Message )

            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    static member m_ReadVhdx_Fail_002_data : obj[][] = [|
        [|
            [|
                ( 0UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
            |];
            "File type identifier signature mismatch"
        |];
        [|
            [|
                ( 0x10000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
                ( 0x20000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
            |];
            "No valid header exists"
        |];
        [|
            [|
                ( 0x30000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
                ( 0x40000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
            |];
            "No valid region table exists"
        |];
        [|
            [|
                let v = VhdxTestFunc.GetDefaultRegionTable 32 [| 0x00uy; 0x00uy; 0x10uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy; |]
                ( 0x30000UL, v );
                ( 0x40000UL, v );
            |];
            "No valid region table exists"
        |];
        [|
            [|
                // missing metadata resion entry in region table 1
                let v =
                    VhdxTestFunc.GenRegionTable {
                        Signature = 0x72656769u
                        Checksum = 0u;
                        EntryCount = 0u;
                        Entries = [{
                            Guid = VhdxCommons.REGENT_TYPE_BAT;
                            FileOffset = 2097152UL;
                            Length = 1048576u;
                            Required = true;
                        }];
                    } 0 [||]
                ( 0x30000UL, v );
                ( 0x40000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );   // The signature of region table 2 is corrupted
            |];
            "Metadata region not found"
        |];
        [|
            [|
                // missing BAT resion entry in region table 1
                let v =
                    VhdxTestFunc.GenRegionTable {
                        Signature = 0x72656769u
                        Checksum = 0u;
                        EntryCount = 0u;
                        Entries = [{
                            Guid = VhdxCommons.REGENT_TYPE_METADATA;
                            FileOffset = 2097152UL;
                            Length = 1048576u;
                            Required = true;
                        }];
                    } 0 [||]
                ( 0x30000UL, v );
                ( 0x40000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );   // The signature of region table 2 is corrupted
            |];
            "BAT region not found"
        |];
        [|
            [|
                ( 0x200000UL, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
            |];
            "The signatures in the metadata table do not match"
        |];
        [|
            [|
                ( 0x300000UL, [| 0x04uy; |] );
            |];
            "A reserved payload BAT entry state value was specified"
        |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadVhdx_Fail_002_data" )>]
    member _.ReadVhdx_Fail_002 ( patch : ( uint64 * byte[] )[] ) ( expmsg : string ) =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            for ( patchpos, patchdata ) in patch do
                do! fa.Write patchpos ( ArraySegment patchdata )
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx fa
                    ()
                })
            Assert.StartsWith( expmsg, r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_Fail_003 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! cstr = VhdxReader.ReadVhdx fa

            // The payload overlaps with the log area.
            let pos1 = cstr.BAT.Payloads.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            let wv = Array.zeroCreate<byte> 8
            ByteFunc.WriteU64LE wv 0u cstr.ImmHeader.LogOffset
            wv.[0] <- 7uy
            do! fa.Write pos1 ( ArraySegment wv )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx fa
                    ()
                })
            Assert.StartsWith( "The payload must not overlap with the log area", r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_Fail_004 () =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun _ cfa -> task {
            let! cstr = VhdxReader.ReadVhdx cfa

            // The sector bitmap overlaps with the log area.
            let pos1 = cstr.BAT.SectorBitmap.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            let wv = Array.zeroCreate<byte> 8
            ByteFunc.WriteU64LE wv 0u cstr.ImmHeader.LogOffset
            wv.[0] <- 6uy
            do! cfa.Write pos1 ( ArraySegment wv )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx cfa
                    ()
                })
            Assert.StartsWith( "The sector bitmap must not overlap with the log area", r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_Fail_005 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! cstr = VhdxReader.ReadVhdx fa

            // The payload overlaps with the BAT or metadata area.
            let pos1 = cstr.BAT.Payloads.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            let wv = Array.zeroCreate<byte> 8
            ByteFunc.WriteU64LE wv 0u cstr.Region.Entries.[0].FileOffset
            wv.[0] <- 7uy
            do! fa.Write pos1 ( ArraySegment wv )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx fa
                    ()
                })
            Assert.StartsWith( "The payload must not overlap with other areas", r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_Fail_006 () =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun _ cfa -> task {
            let! cstr = VhdxReader.ReadVhdx cfa

            // The sector bitmap overlaps with the BAT or metadata area.
            let pos1 = cstr.BAT.SectorBitmap.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            let wv = Array.zeroCreate<byte> 8
            ByteFunc.WriteU64LE wv 0u cstr.Region.Entries.[0].FileOffset
            wv.[0] <- 6uy
            do! cfa.Write pos1 ( ArraySegment wv )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx cfa
                    ()
                })
            Assert.StartsWith( "The sector bitmap must not overlap with other areas", r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_Fail_007 () =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun _ cfa -> task {
            do! cfa.SetFileSize 0xF00000UL
            let! cstr = VhdxReader.ReadVhdx cfa

            // update first payload block to PartiallyPresent
            let pos1 = cstr.BAT.Payloads.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            do! cfa.Write pos1 ( ArraySegment [| 0x07uy; 0x00uy; 0xA0uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] )

            // update first sector bitmap to NotPresent
            let pos2 = cstr.BAT.SectorBitmap.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            do! cfa.Write pos2 ( ArraySegment [| 0x00uy |] )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx cfa
                    ()
                })
            Assert.StartsWith( "There is no sector bitmap BAT entry corresponding to the payload BAT entry for PartiallyPresent", r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_Fail_008 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            do! fa.SetFileSize 0xF00000UL
            let! cstr = VhdxReader.ReadVhdx fa

            // update first payload block to PartiallyPresent
            let pos1 = cstr.BAT.Payloads.[0].BatEntryIndex * 8UL + cstr.BAT.BATRegionOffset
            do! fa.Write pos1 ( ArraySegment [| 0x07uy; 0x00uy; 0xA0uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadVhdx fa
                    ()
                })
            Assert.StartsWith( "A fixed or dynamic VHDX file exists with a payload BAT entry for PartiallyPresent", r.Message )
        } )

    [<Fact>]
    member _.ReadVhdx_001 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! cstr = VhdxReader.ReadVhdx fa
            Assert.StrictEqual( 0x100000u, cstr.ImmHeader.LogLength )
            Assert.False( cstr.VDI.HasParent )
            Assert.StrictEqual( 0x100000u, cstr.VDI.PayloadBlockSize )
            Assert.StrictEqual( 0x4000000UL, cstr.VDI.VirtualDiskSize )
            Assert.StrictEqual( Blocksize.BS_512, cstr.VDI.LogicalSectorSize )
            Assert.StrictEqual( 64UL, cstr.BAT.PayloadBlockCount )
            Assert.StrictEqual( 1UL, cstr.BAT.SectorBitmapBlockCount )
        } )

    [<Fact>]
    member _.ReadVhdx_002 () =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun _ fa -> task {
            let! cstr = VhdxReader.ReadVhdx fa
            Assert.StrictEqual( 0x100000u, cstr.ImmHeader.LogLength )
            Assert.True( cstr.VDI.HasParent )
            Assert.StrictEqual( 0x100000u, cstr.VDI.PayloadBlockSize )
            Assert.StrictEqual( 0x4000000UL, cstr.VDI.VirtualDiskSize )
            Assert.StrictEqual( Blocksize.BS_512, cstr.VDI.LogicalSectorSize )
            Assert.StrictEqual( 64UL, cstr.BAT.PayloadBlockCount )
            Assert.StrictEqual( 1UL, cstr.BAT.SectorBitmapBlockCount )
        } )

    [<Fact>]
    member _.ReadVhdx_003 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let initialFileSize = fa.FileSize
            let! cstr = VhdxReader.ReadVhdx fa
            let idx = [
                sec4k_me.ofUInt64 48UL;    // Region Table 1
                sec4k_me.ofUInt64 64UL;    // Region Table 2
                for itr in cstr.Region.Entries do
                    sec4k_me.ofUInt64( itr.FileOffset / 4096UL )
            ]
            do! VhdxCorrupter.Inject fa idx
            let! cstr2 = VhdxReader.ReadVhdx fa

            Assert.StrictEqual( cstr.Creator, cstr2.Creator )
            Assert.StrictEqual( cstr.ImmHeader.Signature, cstr2.ImmHeader.Signature )
            Assert.StrictEqual( cstr.ImmHeader.LogVersion, cstr2.ImmHeader.LogVersion )
            Assert.StrictEqual( cstr.ImmHeader.Version, cstr2.ImmHeader.Version )
            Assert.StrictEqual( cstr.ImmHeader.LogLength, cstr2.ImmHeader.LogLength )
            Assert.StrictEqual( cstr.ImmHeader.LogOffset, cstr2.ImmHeader.LogOffset )
            Assert.StrictEqual( cstr.ImmHeader.Offset, cstr2.ImmHeader.Offset )
            Assert.StrictEqual( initialFileSize, cstr2.LastFileSize )
            Assert.StrictEqual( initialFileSize, cstr.LastFileSize )
            Assert.StrictEqual( cstr.Region, cstr2.Region )
            Assert.StrictEqual( cstr.VDI, cstr2.VDI )
            Assert.StrictEqual( cstr.BAT, cstr2.BAT )
            Assert.Empty( cstr.Log )
            Assert.NotEmpty( cstr2.Log )
        } )

    [<Fact>]
    member _.ReadAllStructures_Fail_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let! _ = VhdxReader.ReadAllStructures fa
                        ()
                    } )
                Assert.StartsWith( "The VHDX file is too small", r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }


    [<Fact>]
    member _.ReadAllStructures_Fail_002 () =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun pfa cfa -> task {
            let! pstr = VhdxReader.ReadVhdx pfa
            // corrupt DataWriteGuid on header 1
            let v = VhdxTestFunc.GenHeaderBytes pstr.ImmHeader { pstr.LoadedVarHeader with DataWriteGuid = Guid.NewGuid() }
            do! pfa.Write 65536UL ( ArraySegment v )
            // corrupt Signature on header 2
            do! pfa.Write 131072UL ( [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] |> ArraySegment )
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadAllStructures cfa
                    ()
                } )
            Assert.StartsWith( "Data Write Guid does not match", r.Message )
        } )

    [<Fact>]
    member _.ReadAllStructures_Fail_003 () =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun pfa cfa -> task {
            let! pstr = VhdxReader.ReadVhdx pfa
            let! cstr = VhdxReader.ReadVhdx cfa
            // corrupt FileWriteGuid on header 1
            let v = VhdxTestFunc.GenHeaderBytes pstr.ImmHeader { pstr.LoadedVarHeader with FileWriteGuid = cstr.LoadedVarHeader.FileWriteGuid }
            do! pfa.Write 65536UL ( ArraySegment v )
            // corrupt Signature on header 2
            do! pfa.Write 131072UL ( [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] |> ArraySegment )
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadAllStructures cfa
                    ()
                } )
            Assert.StartsWith( "The same file is specified as the parent VHDX file", r.Message )
        } )

    static member m_ReadAllStructures_Fail_004_data : obj[][] = [|
        [| ( fun vdi -> { vdi with VirtualDiskSize = 0x8000000UL } ); "The virtual disk size of the parent" |];
        [| ( fun vdi -> { vdi with VirtualDiskId = Guid.NewGuid() } ); "The virtual disk ID of the parent" |];
        [| ( fun vdi -> { vdi with LogicalSectorSize = Blocksize.BS_4096 } ); "The logical sector size of the parent" |];
        [| ( fun vdi -> { vdi with PhysicalSectorSize = Blocksize.BS_4096 } ); "The physical sector size of the parent" |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadAllStructures_Fail_004_data" )>]
    member _.ReadAllStructures_Fail_004 ( vdiud : VirtualDiskInfo -> VirtualDiskInfo ) ( expmsg : string ) =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun pfa cfa -> task {
            let! pstr = VhdxReader.ReadVhdx pfa
            let! cstr = VhdxReader.ReadVhdx cfa

            // Update metadata of child disk.
            let metadataBytes = VhdxTestFunc.VDItoMetadataTable ( vdiud cstr.VDI )
            let metadataRegion =
                cstr.Region.Entries
                |> List.find ( fun e -> e.Guid = VhdxCommons.REGENT_TYPE_METADATA )
            do! cfa.Write metadataRegion.FileOffset ( ArraySegment metadataBytes )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadAllStructures cfa
                    ()
                } )
            Assert.StartsWith( expmsg, r.Message )
        } )

    static member m_ReadAllStructures_Fail_005_data : obj[][] = [|
        [| "relative_path"; ( fun ( fa : FileAccessor ) -> Path.GetFileName fa.FileName )  |];
        [| "absolute_win32_path"; ( fun ( fa : FileAccessor ) -> fa.FileName )  |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadAllStructures_Fail_005_data" )>]
    member _.ReadAllStructures_001 ( keyname : string ) ( genpname : FileAccessor -> string ) =
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun pfa cfa -> task {
            let! pstr = VhdxReader.ReadVhdx pfa
            let! cstr = VhdxReader.ReadVhdx cfa

            // Update metadata of child disk.
            let wparentLocator =
                [|
                    ( "parent_linkage", cstr.VDI.ParentLocator.[ "parent_linkage" ] );
                    ( keyname, genpname pfa; );
                |]
                |> Map
            let metadataBytes = VhdxTestFunc.VDItoMetadataTable { cstr.VDI with ParentLocator = wparentLocator }
            let metadataRegion =
                cstr.Region.Entries
                |> List.find ( fun e -> e.Guid = VhdxCommons.REGENT_TYPE_METADATA )
            do! cfa.Write metadataRegion.FileOffset ( ArraySegment metadataBytes )

            let! r = VhdxReader.ReadAllStructures cfa
            Assert.StrictEqual( 2, r.Length )
            Assert.StrictEqual( cfa.FileName, ( fst r.[1] ).FileName )
            Assert.StrictEqual( pfa.FileName, ( fst r.[0] ).FileName )
        } )

    [<Fact>]
    member _.ReadAllStructures_002 () =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            VhdxTestFunc.CreateDifferencingVHDXFileAndTest ( fun pfa cfa -> task {
                let! pstr = VhdxReader.ReadVhdx pfa
                let! cstr = VhdxReader.ReadVhdx cfa

                // Get relative path name of parent disk
                let wpfname = VhdxTestFunc.GetVolumePath pfa.FileName

                // Update metadata of child disk.
                let wparentLocator =
                    [|
                        ( "parent_linkage", cstr.VDI.ParentLocator.[ "parent_linkage" ] );
                        ( "volume_path", wpfname; );
                    |]
                    |> Map
                let metadataBytes = VhdxTestFunc.VDItoMetadataTable { cstr.VDI with ParentLocator = wparentLocator }
                let metadataRegion =
                    cstr.Region.Entries
                    |> List.find ( fun e -> e.Guid = VhdxCommons.REGENT_TYPE_METADATA )
                do! cfa.Write metadataRegion.FileOffset ( ArraySegment metadataBytes )

                let! r = VhdxReader.ReadAllStructures cfa
                Assert.StrictEqual( 2, r.Length )
                Assert.StrictEqual( cfa.FileName, ( fst r.[1] ).FileName )
                Assert.StrictEqual( wpfname, ( fst r.[0] ).FileName )

            } )
        else
            Task.FromResult()

    [<Fact>]
    member _.ReadAllStructures_003 () =
        task {
            let fnames = Array.init 3 ( fun _ -> Path.GetTempFileName() )
            let fas = fnames |> Array.map ( fun n -> FileAccessor( n, 1u, false ) )
            try
                for i = 0 to fas.Length - 1 do
                    let pfa = if i = 0 then None else ( Some fas.[ i - 1 ] )
                    do! VhdxCreator.Create pfa fas.[i] 0x100000u 0x100000u false 0x4000000UL Blocksize.BS_512

                let! r = VhdxReader.ReadAllStructures fas.[ fas.Length - 1 ]
                
                ( fas, r )
                ||> Array.iter2( fun expfa ( fa, _ ) ->
                    Assert.StrictEqual( Path.GetFileName expfa.FileName, Path.GetFileName fa.FileName )
                )

            finally
                for itr in fas do
                    itr.Close()
                for itr in fnames do
                    GlbFunc.DeleteFile itr
        }

