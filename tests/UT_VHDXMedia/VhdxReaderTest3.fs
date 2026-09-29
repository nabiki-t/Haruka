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

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test


//=============================================================================
// Type definitions

//=============================================================================
// Class implementation

type VhdxReaderTest3_Test () =

    let genBATEntries ( ent : ( byte * uint64 )[] ) : byte[] =
        let v = Array.zeroCreate<byte> ( ent.Length * 8 )
        ent
        |> Array.iteri ( fun idx ( s, f ) ->
            let pos = idx * 8
            ByteFunc.WriteU64LE v ( uint32 pos ) f
            v.[pos] <- ( s &&& 0x07uy )
        )
        v

    let defBatEntries ( pbsize : uint32 ) ( chunkRatio : uint32 ) ( batEntryCount : uint32 ) ( hasParent : bool ) : byte[] =
        let v = [|
            for i in 0u .. batEntryCount - 1u do
                if i % ( chunkRatio + 1u ) = chunkRatio then
                    // sector bitmap
                    if hasParent then
                        ( 6uy, uint64 ( pbsize * i ) + 1048576UL )
                    else
                        ( 0uy, 0UL )
                else
                    // payload block
                    ( 6uy,  uint64 ( pbsize * i ) + 1048576UL )
        |]
        genBATEntries v


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    static member m_GetPayloadBlockEntry_001_data : obj[][] = [|
        [|
            0UL; 0UL; BatEntryStatePB.PayloadNotPresent; 0x100000UL;
        |];
        [|
            1UL; 1UL; BatEntryStatePB.PayloadUndefined; 0x200000UL;
        |];
        [|
            2UL; 2UL; BatEntryStatePB.PayloadZero; 0x300000UL;
        |];
        [|
            3UL; 3UL; BatEntryStatePB.PayloadUnapped; 0x400000UL;
        |];
        [|
            4UL; 5UL; BatEntryStatePB.PayloadFullyPresent; 0x600000UL;
        |];
        [|
            5UL; 6UL; BatEntryStatePB.PayloadPartiallyPresent; 0x700000UL;
        |];
        [|
            6UL; 7UL; BatEntryStatePB.PayloadNotPresent; 0x800000UL;
        |];
        [|
            7UL; 8UL; BatEntryStatePB.PayloadUndefined; 0x000000UL;
        |];
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
            |> genBATEntries
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
            |> genBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetPayloadBlockEntry v 4UL idx |> ignore
            )
        Assert.StartsWith( "A reserved payload BAT entry state value was specified", r.Message )

    [<Fact>]
    member _.GetPayloadBlockEntry_Fail_002 () =
        let v =
            [|
                ( 0uy, 0x1FFFFFUL );  // payload 0
            |]
            |> genBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetPayloadBlockEntry v 4UL 0UL |> ignore
            )
        Assert.StartsWith( "The FileOffset value of the payload BAT entry must be a multiple of 1 MB", r.Message )

    static member m_GetSectorBitmapBlockEntry_001_data : obj[][] = [|
        [|
            0UL; 4UL; BatEntryStateSB.SectorBitmapNotPresent; 0x500000UL;
        |];
        [|
            1UL; 9UL; BatEntryStateSB.SectorBitmapPresent; 0x000000UL;
        |];
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
                ( 6uy, 0x000000UL );  // sector bitmap 1
            |]
            |> genBATEntries
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
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 0 - 3
                ( 1uy, 0x100000UL );        // sector bitmap 0
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 4 - 7
                ( 2uy, 0x100000UL );        // sector bitmap 1
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 8 - 11
                ( 3uy, 0x100000UL );        // sector bitmap 2
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 12 - 15
                ( 4uy, 0x100000UL );        // sector bitmap 3
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 16 - 19
                ( 5uy, 0x100000UL );        // sector bitmap 4
                for i = 0 to 3 do
                    ( 0uy, 0x100000UL );    // payload 20 - 23
                ( 7uy, 0x100000UL );        // sector bitmap 5
            |]
            |> genBATEntries
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
                ( 0uy, 0x1FFFFFUL );        // sector bitmap 0
            |]
            |> genBATEntries
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.GetSectorBitmapBlockEntry v 4UL 0UL |> ignore
            )
        Assert.StartsWith( "The FileOffset value of the sector bitmap BAT entry must be a multiple of 1 MB", r.Message )

    [<Fact>]
    member _.ReadBat_Fail_001 () =
        task {
            // chunkSize : 4GB
            // chunkRatio : 16
            // payloadBlockCount : 256
            // sectorBitmapBlockCount : 16
            // batEntryCount : 271
            let vdi : VirtualDiskInfo = {
                PayloadBlockSize = 268435456u;      // 256MB
                LeaveBlockAllocated = false;
                HasParent = false;
                VirtualDiskSize = 68719476736UL;    // 64GB
                VirtualDiskId = Guid();
                LogicalSectorSize = Blocksize.BS_512;
                PhysicalSectorSize = Blocksize.BS_512;
                ParentLocator = Map.empty;
            }

            let batRegion : RegionEntry = {
                Guid = VhdxCommons.REGENT_TYPE_BAT;
                FileOffset = 0UL;
                Length = 0u;
                Required = true;
            }

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadBat [] 0UL fa batRegion vdi
                    ()
                } )
            Assert.StartsWith( "The BAT entry has insufficient data length", r.Message )
        }

    [<Fact>]
    member _.ReadBat_Fail_002 () =
        task {
            // chunkSize : 4GB
            // chunkRatio : 16
            // payloadBlockCount : 256
            // sectorBitmapBlockCount : 16
            // batEntryCount : 271
            let vdi : VirtualDiskInfo = {
                PayloadBlockSize = 268435456u;      // 256MB
                LeaveBlockAllocated = false;
                HasParent = false;
                VirtualDiskSize = 68719476736UL;    // 64GB
                VirtualDiskId = Guid();
                LogicalSectorSize = Blocksize.BS_512;
                PhysicalSectorSize = Blocksize.BS_512;
                ParentLocator = Map.empty;
            }

            let batRegion : RegionEntry = {
                Guid = VhdxCommons.REGENT_TYPE_BAT;
                FileOffset = 0UL;
                Length = 1048576ul;
                Required = true;
            }

            let batData = defBatEntries vdi.PayloadBlockSize 16u 271u false
            ByteFunc.WriteU64LE batData 8u 1048576UL

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 1048576UL
            do! fa.Write 0UL ( ArraySegment batData )

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadBat [] 1048576UL fa batRegion vdi
                    ()
                } )
            Assert.StartsWith( "The regions indicated by BAT entries must not overlap", r.Message )
        }
