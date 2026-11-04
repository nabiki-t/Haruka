//=============================================================================
// Haruka Software Storage.
// VhdxWriterTest.fs : Test cases for VhdxWriter class.
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
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test

//=============================================================================
// Type definitions

//=============================================================================
// Class implementation

type VhdxWriter_Test () =

    static member GenStructures ( logLength : uint32 ) ( payloadBlockSize : uint32 ) ( virtualDiskSize : uint64 ) ( sectorSize : Blocksize ) : VhdxStructures =
        let chunkSize = Blocksize.toUInt64 sectorSize * 8388608UL
        let chunkRate = chunkSize / uint64 payloadBlockSize
        let payloadBlockCount = Functions.CeilDiv virtualDiskSize ( uint64 payloadBlockSize )
        let sectorBitmapCount = Functions.CeilDiv payloadBlockCount chunkRate
        let batEntryCount = payloadBlockCount + ( payloadBlockCount - 1UL ) / chunkRate
        let batRegionSize = Functions.CeilUnit ( batEntryCount * 8UL ) 0x100000UL
        let metadataStartPos = 0x100000UL + uint64 logLength
        let batRegionStartPos = metadataStartPos + 0x100000UL
        {
            Creator = "";
            ImmHeader = {
                Signature = 0u;
                Checksum = 0u;
                LogVersion = 0us;
                Version = 1us;
                LogLength = logLength;
                LogOffset = 0x100000UL;
                Offset = 0x10000UL;
                Index = 0;
            };
            LoadedVarHeader = {
                SequenceNumber = 1UL;
                FileWriteGuid = Guid.NewGuid();
                DataWriteGuid = Guid.NewGuid();
                LogGuid = Guid();
            };
            Log = [];
            LastFileSize = virtualDiskSize;
            Region = {
                Signature = 0u;
                Checksum = 0u;
                EntryCount = 0u;
                Entries = [
                    {
                        Guid = VhdxCommons.REGENT_TYPE_METADATA;
                        FileOffset = metadataStartPos;
                        Length = 0x100000u;
                        Required = true;
                    };
                    {
                        Guid = VhdxCommons.REGENT_TYPE_BAT;
                        FileOffset = batRegionStartPos;
                        Length = uint32 batRegionSize;
                        Required = true;
                    };
                ];
            };
            VDI = {
                PayloadBlockSize = payloadBlockSize;
                LeaveBlockAllocated = false;
                HasParent = false;
                VirtualDiskSize = virtualDiskSize;
                VirtualDiskId = Guid();
                LogicalSectorSize = sectorSize;
                PhysicalSectorSize = sectorSize;
                ParentLocator = [] |> Map<string,string>;
            };
            BAT = {
                BATRegionOffset = batRegionStartPos;
                BATRegionLength = uint32 batRegionSize;
                ChunkSize = chunkSize;
                ChunkRatio = chunkRate;
                PayloadBlockCount = payloadBlockCount;
                SectorBitmapBlockCount = sectorBitmapCount;
                BatEntryCount = batEntryCount;
                Payloads = [|
                    for i in 0UL .. payloadBlockCount - 1UL do
                        yield {
                            BatEntryIndex = i + ( i / chunkRate );
                            State = BatEntryStatePB.PayloadFullyPresent;
                            FileOffset = batRegionStartPos + batRegionSize + ( uint64 payloadBlockSize ) * i;
                        }
                |];
                SectorBitmap = [|
                    for i in 0UL .. sectorBitmapCount - 1UL do
                        yield {
                            BatEntryIndex = ( i + 1UL ) * chunkRate + i;
                            SBState = BatEntryStateSB.SectorBitmapNotPresent;
                            FileOffset = 0UL;
                            Bitmap = Array.empty;
                        }
                |];
            }
        }

    [<Fact>]
    member _.BuildFreeList_Fail_001() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                VDI.PayloadBlockSize = 0u;
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.BuildFreeList str 1UL |> ignore
        )
        Assert.StartsWith( "Unexpected the payload block size value", r.Message )

    [<Fact>]
    member _.BuildFreeList_Fail_002() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                LastFileSize = 18014398501093377UL;
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.BuildFreeList str 1UL |> ignore
        )
        Assert.StartsWith( "The file is too large to build the free-space bitmap", r.Message )

    [<Theory>]
    [<InlineData( 0xFFFFFFFFFFF00000UL, 0x100000u )>]
    member _.BuildFreeList_Fail_003 ( offset : uint64 ) ( length : uint32 ) =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                ImmHeader.LogOffset = offset;
                ImmHeader.LogLength = length;
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.BuildFreeList str 1UL |> ignore
        )
        Assert.StartsWith( "Unexpected the log offset or the log length value", r.Message )

    [<Fact>]
    member _.BuildFreeList_Fail_004() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                Region.Entries =
                    wstr.Region.Entries
                    |> List.map ( fun e -> { e with FileOffset = UInt64.MaxValue - 0x0FFFFUL } )
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.BuildFreeList str 1UL |> ignore
        )
        Assert.StartsWith( "Unexpected the region offset or the region length value", r.Message )

    [<Fact>]
    member _.BuildFreeList_Fail_005() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                BAT.SectorBitmap = [|
                    for i in 0UL .. wstr.BAT.SectorBitmapBlockCount - 1UL do
                        yield {
                            wstr.BAT.SectorBitmap.[ int i ] with
                                SBState = BatEntryStateSB.SectorBitmapPresent;
                                FileOffset = UInt64.MaxValue - 0x0FFFFUL;
                        }
                |];
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.BuildFreeList str 1UL |> ignore
        )
        Assert.StartsWith( "Unexpected the sector bitmap offset value", r.Message )

    [<Fact>]
    member _.BuildFreeList_Fail_006() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                BAT.Payloads = [|
                    for i in 0UL .. wstr.BAT.PayloadBlockCount - 1UL do
                        yield {
                            wstr.BAT.Payloads.[ int i ] with
                                FileOffset = UInt64.MaxValue - 0x0FFFFFUL;
                        }
                |];
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.BuildFreeList str 1UL |> ignore
        )
        Assert.StartsWith( "Unexpected the payload block offset value", r.Message )

    [<Fact>]
    member _.BuildFreeList_PayloadNotPresent_001() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xF00000UL Blocksize.BS_512
        let str = {
            wstr with
                BAT.Payloads = [|
                    { BatEntryIndex = 0UL; State = BatEntryStatePB.PayloadNotPresent; FileOffset = 0x400000UL };
                    { BatEntryIndex = 0UL; State = BatEntryStatePB.PayloadUndefined; FileOffset = 0x600000UL };
                    { BatEntryIndex = 0UL; State = BatEntryStatePB.PayloadZero; FileOffset = 0x800000UL };
                    { BatEntryIndex = 0UL; State = BatEntryStatePB.PayloadUnapped; FileOffset = 0xA00000UL };
                    { BatEntryIndex = 0UL; State = BatEntryStatePB.PayloadFullyPresent; FileOffset = 0xC00000UL };
                    { BatEntryIndex = 1UL; State = BatEntryStatePB.PayloadPartiallyPresent; FileOffset = 0xE00000UL };
                |];
                LastFileSize = 0xF00000UL;
            }
        let r = VhdxWriter.BuildFreeList str 10UL
        let exp = [|
            0x400000UL;
            0x500000UL;
            0x600000UL;
            0x700000UL;
            0x800000UL;
            0x900000UL;
            0xA00000UL;
            0xB00000UL;
            0xD00000UL;
        |]
        Assert.True(( exp = r ))

    [<Fact>]
    member _.BuildFreeList_SectorBitmapNotPresent_001() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x800000UL Blocksize.BS_512
        let str = {
            wstr with
                BAT.SectorBitmap = [|
                    { BatEntryIndex = 0UL; SBState = BatEntryStateSB.SectorBitmapNotPresent;  FileOffset = 0x400000UL;  Bitmap = [||] };
                    { BatEntryIndex = 0UL; SBState = BatEntryStateSB.SectorBitmapPresent;  FileOffset = 0x600000UL;  Bitmap = [||] };
                |]
                BAT.Payloads = [||]
                LastFileSize = 0x800000UL;
            }
        let r = VhdxWriter.BuildFreeList str 10UL
        let exp = [|
            0x400000UL;
            0x500000UL;
            0x700000UL;
        |]
        Assert.True(( exp = r ))


    static member m_BuildFreeList_001_data : obj[][] = [|
        [|
            [| 0x500000UL; 0x700000UL; 0x900000UL; |];
            0xA00000UL;
            10UL;
            [| 0x400000UL; 0x600000UL; 0x800000UL; |];
        |];
        [|
            [| 0x600000UL; 0x700000UL; |];
            0xA00000UL;
            10UL;
            [| 0x400000UL; 0x500000UL; 0x800000UL; 0x900000UL; |];
        |];
        [|
            [| 0xB00000UL; 0xC00000UL; |];
            0xB00000UL;
            3UL;
            [| 0x400000UL; 0x500000UL; 0x600000UL; |];
        |];
        [|
            [| 0x400000UL; 0x500000UL; 0x600000UL; |];
            0x700000UL;
            3UL;
            [||];
        |];
        [|
            [||];
            0xA00000UL;
            0UL;
            [||];
        |];
        [|
            [||];
            0xA00000UL;
            6UL;
            [| 0x400000UL; 0x500000UL; 0x600000UL; 0x700000UL; 0x800000UL; 0x900000UL; |];
        |];
        [|
            [||];
            0xA00000UL;
            7UL;
            [| 0x400000UL; 0x500000UL; 0x600000UL; 0x700000UL; 0x800000UL; 0x900000UL; |];
        |];
        [|
            [| 0x580000UL; 0x880000UL; |];
            0xA00000UL;
            10UL;
            [| 0x400000UL; 0x700000UL; |];
        |];
        [|
            [| 0x400000UL; 0x500000UL; 0x600000UL; 0x700000UL; 0x800000UL; |];
            0xA80000UL;
            10UL;
            [| 0x900000UL; |];
        |];
    |]

    [<Theory>]
    [<MemberData( "m_BuildFreeList_001_data" )>]
    member _.BuildFreeList_PayloadBlock_001 ( interval : uint64[] ) ( fsize : uint64 ) ( maxcnt : uint64 ) ( rxpr : uint64 [] ) =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xA00000UL Blocksize.BS_512
        let str2 = {
            str with
                BAT.Payloads = [|
                    for i in interval do
                        yield {
                            BatEntryIndex = 0UL;
                            State = BatEntryStatePB.PayloadFullyPresent;
                            FileOffset = i;
                        }
                |];
                LastFileSize = fsize;
        }
        let r = VhdxWriter.BuildFreeList str2 maxcnt
        Assert.True(( rxpr = r ))

    [<Theory>]
    [<MemberData( "m_BuildFreeList_001_data" )>]
    member _.BuildFreeList_SectorBitmap_001 ( interval : uint64[] ) ( fsize : uint64 ) ( maxcnt : uint64 ) ( rxpr : uint64 [] ) =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xA00000UL Blocksize.BS_512
        let str2 = {
            str with
                BAT.Payloads = [|
                |];
                BAT.SectorBitmap = [|
                    for i in interval do
                        yield {
                            BatEntryIndex = 0UL;
                            SBState = BatEntryStateSB.SectorBitmapPresent;
                            Bitmap = [||];
                            FileOffset = i;
                        }
                |];
                LastFileSize = fsize;
        }
        let r = VhdxWriter.BuildFreeList str2 maxcnt
        Assert.True(( rxpr = r ))

    static member m_BuildFreeList_DifferentBlockSize_001_data : obj[][] = [|
        [|
            0x200000u;
            0xA00000UL;
            [| 0x400000UL; 0x800000UL |];
            0xA00000UL;
            10UL;
            [| 0x600000UL |];
        |];
        [|
            0x200000u;
            0xA00000UL;
            [||];
            0xA00000UL;
            10UL;
            [| 0x400000UL; 0x600000UL; 0x800000UL |];
        |];
        [|
            0x400000u;
            0x1000000UL;
            [| 0x400000UL; 0xC00000UL |];
            0x1000000UL;
            10UL;
            [| 0x800000UL |];
        |];
    |]

    [<Theory>]
    [<MemberData( "m_BuildFreeList_DifferentBlockSize_001_data" )>]
    member _.BuildFreeList_DifferentBlockSize_001 ( payloadBlockSize : uint32 ) ( virtualDiskSize : uint64 ) ( interval : uint64[] ) ( fsize : uint64 ) ( maxcnt : uint64 ) ( rxpr : uint64 [] ) =
        let str = VhdxWriter_Test.GenStructures 0x100000u payloadBlockSize virtualDiskSize Blocksize.BS_512
        let str2 = {
            str with
                BAT.Payloads = [|
                    for i in interval do
                        yield {
                            BatEntryIndex = 0UL;
                            State = BatEntryStatePB.PayloadFullyPresent;
                            FileOffset = i;
                        }
                |];
                LastFileSize = fsize;
        }
        let r = VhdxWriter.BuildFreeList str2 maxcnt
        Assert.True(( rxpr = r ))

    static member m_UpdatePBForAllocate_001_data : obj[][] = [|
        [| BatEntryStatePB.PayloadFullyPresent; |];
        [| BatEntryStatePB.PayloadPartiallyPresent; |];
    |]

    [<Theory>]
    [<MemberData( "m_UpdatePBForAllocate_001_data" )>]
    member _.UpdatePBForAllocate_001 ( stat : BatEntryStatePB ) =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xA00000UL Blocksize.BS_512
        str.BAT.Payloads.[0] <- {
            str.BAT.Payloads.[0] with
                State = stat;
        }
        use e = [||] |> Array.toSeq |> _.GetEnumerator()
        let updated4KSecs = HashSet<SEC4K_T>()
        let gfs = VhdxWriter.UpdatePBForAllocate str blkcnt_me.zero64 e 0xA00000UL updated4KSecs
        Assert.StrictEqual( 0xA00000UL, gfs )
        Assert.Empty updated4KSecs

    static member m_UpdatePBForAllocate_002_data : obj[][] = [|
        [| BatEntryStatePB.PayloadNotPresent; |];
        [| BatEntryStatePB.PayloadUndefined; |];
        [| BatEntryStatePB.PayloadZero; |];
        [| BatEntryStatePB.PayloadUnapped; |];
    |]

    [<Theory>]
    [<MemberData( "m_UpdatePBForAllocate_002_data" )>]
    member _.UpdatePBForAllocate_002 ( stat : BatEntryStatePB ) =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xA00000UL Blocksize.BS_512
        str.BAT.Payloads.[0] <- {
            str.BAT.Payloads.[0] with
                State = stat;
                FileOffset = 0UL;
        }
        use e = [| 0xB00000UL; 0xC00000UL; |] |> Array.toSeq |> _.GetEnumerator()
        let updated4KSecs = HashSet<SEC4K_T>()
        let gfs = VhdxWriter.UpdatePBForAllocate str blkcnt_me.zero64 e 0xA00000UL updated4KSecs
        Assert.StrictEqual( 0xA00000UL, gfs )

        Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, str.BAT.Payloads.[0].State )
        Assert.StrictEqual( 0xB00000UL, str.BAT.Payloads.[0].FileOffset )
        Assert.True( e.MoveNext() )
        Assert.StrictEqual( 0xC00000UL, e.Current )
        Assert.StrictEqual( 1, updated4KSecs.Count )
        Assert.True( updated4KSecs.Contains( sec4k_me.ofUInt64 768UL ) )

    [<Theory>]
    [<MemberData( "m_UpdatePBForAllocate_002_data" )>]
    member _.UpdatePBForAllocate_003 ( stat : BatEntryStatePB ) =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xA00000UL Blocksize.BS_512
        str.BAT.Payloads.[0] <- {
            str.BAT.Payloads.[0] with
                State = stat;
                FileOffset = 0UL;
        }
        use e = [||] |> Array.toSeq |> _.GetEnumerator()
        let updated4KSecs = HashSet<SEC4K_T>()
        let gfs = VhdxWriter.UpdatePBForAllocate str blkcnt_me.zero64 e 0xA00000UL updated4KSecs
        Assert.StrictEqual( 0xB00000UL, gfs )

        Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, str.BAT.Payloads.[0].State )
        Assert.StrictEqual( 0xA00000UL, str.BAT.Payloads.[0].FileOffset )
        Assert.False( e.MoveNext() )
        Assert.StrictEqual( 1, updated4KSecs.Count )
        Assert.True( updated4KSecs.Contains( sec4k_me.ofUInt64 768UL ) )

    [<Fact>]
    member _.UpdatePBForAllocate_004 () =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0xA00000UL Blocksize.BS_512
        let str2 = {
            str with
                VDI.HasParent = true;
        }
        str2.BAT.Payloads.[0] <- {
            str2.BAT.Payloads.[0] with
                State = BatEntryStatePB.PayloadNotPresent;
                FileOffset = 0UL;
        }
        use e = [||] |> Array.toSeq |> _.GetEnumerator()
        let updated4KSecs = HashSet<SEC4K_T>()
        let gfs = VhdxWriter.UpdatePBForAllocate str2 blkcnt_me.zero64 e 0xA00000UL updated4KSecs
        Assert.StrictEqual( 0xB00000UL, gfs )

        Assert.StrictEqual( BatEntryStatePB.PayloadPartiallyPresent, str2.BAT.Payloads.[0].State )
        Assert.StrictEqual( 0xA00000UL, str2.BAT.Payloads.[0].FileOffset )
        Assert.False( e.MoveNext() )
        Assert.StrictEqual( 1, updated4KSecs.Count )
        Assert.True( updated4KSecs.Contains( sec4k_me.ofUInt64 768UL ) )

    [<Fact>]
    member _.UpdatePBForAllocate_005 () =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x4000000u 0x400000000000UL Blocksize.BS_512
        str.BAT.Payloads.[1048575] <- {
            str.BAT.Payloads.[1048575] with
                State = BatEntryStatePB.PayloadNotPresent;
                FileOffset = 0UL;
                BatEntryIndex = 999UL
        }
        use e = [| 0xA00000UL |] |> Array.toSeq |> _.GetEnumerator()
        let updated4KSecs = HashSet<SEC4K_T>()
        let gfs = VhdxWriter.UpdatePBForAllocate str ( blkcnt_me.ofUInt64 0x1FFFFFFFFFUL ) e 0x400000000000UL updated4KSecs
        Assert.StrictEqual( 0x400000000000UL, gfs )

        Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, str.BAT.Payloads.[1048575].State )
        Assert.StrictEqual( 0xA00000UL, str.BAT.Payloads.[1048575].FileOffset )
        Assert.StrictEqual( 1, updated4KSecs.Count )
        Assert.True( updated4KSecs.Contains( sec4k_me.ofUInt64 769UL ) )

    [<Theory>]
    [<InlineData( 0x2000000000UL,       0x400000000000UL, 512 )>]
    [<InlineData( 0xFFFFFFFFFFFFFFFFUL, 0x400000000000UL, 512 )>]
    [<InlineData( 0x8000UL,             0x1000000UL,      512 )>]
    [<InlineData( 0x400000000UL,        0x400000000000UL, 4096 )>]
    [<InlineData( 0x1000UL,             0x1000000UL,      4096 )>]
    member _.UpdatePBForAllocate_Fail_001 ( lba : uint64 ) ( vds : uint64 ) ( argbs : int ) =
        let bs = if argbs = 512 then Blocksize.BS_512 else Blocksize.BS_4096
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x4000000u vds bs
        use e = [||] |> Array.toSeq |> _.GetEnumerator()
        let updated4KSecs = HashSet<SEC4K_T>()
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxWriter.UpdatePBForAllocate str ( blkcnt_me.ofUInt64 lba ) e vds updated4KSecs |> ignore
            )
        Assert.StartsWith( "Unexpected addcess range", r.Message )
