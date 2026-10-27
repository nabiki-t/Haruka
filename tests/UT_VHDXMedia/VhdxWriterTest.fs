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
                            BatEntryIndex = 0UL;
                            State = BatEntryStatePB.PayloadFullyPresent;
                            FileOffset = batRegionStartPos + batRegionSize + ( uint64 payloadBlockSize ) * i;
                        }
                |];
                SectorBitmap = [|
                    for i in 0UL .. sectorBitmapCount - 1UL do
                        yield {
                            BatEntryIndex = 0UL;
                            SBState = BatEntryStateSB.SectorBitmapNotPresent;
                            FileOffset = 0UL;
                            Bitmap = Array.empty;
                        }
                |];
            }
        }

    [<Fact>]
    member _.GetUsedRegions_001() =
        Assert.Empty ( VhdxWriter.GetUsedRegions [||] )

    [<Fact>]
    member _.GetUsedRegions_002() =
        let v : PayloadBATEntry[] = [|
            { BatEntryIndex = 0UL; State = BatEntryStatePB.PayloadNotPresent;       FileOffset = 1UL; };
            { BatEntryIndex = 1UL; State = BatEntryStatePB.PayloadUndefined;        FileOffset = 2UL; };
            { BatEntryIndex = 2UL; State = BatEntryStatePB.PayloadZero;             FileOffset = 3UL; };
            { BatEntryIndex = 3UL; State = BatEntryStatePB.PayloadUnapped;          FileOffset = 4UL; };
            { BatEntryIndex = 4UL; State = BatEntryStatePB.PayloadFullyPresent;     FileOffset = 5UL; };
            { BatEntryIndex = 5UL; State = BatEntryStatePB.PayloadPartiallyPresent; FileOffset = 6UL; };
        |]
        let r = VhdxWriter.GetUsedRegions v
        Assert.True( [| 5UL; 6UL; |] = r )

    [<Fact>]
    member _.GetUsedRegions_003() =
        let v : PayloadBATEntry[] = [|
            { BatEntryIndex = 4UL; State = BatEntryStatePB.PayloadFullyPresent;     FileOffset = 9UL; };
            { BatEntryIndex = 5UL; State = BatEntryStatePB.PayloadPartiallyPresent; FileOffset = 6UL; };
            { BatEntryIndex = 4UL; State = BatEntryStatePB.PayloadFullyPresent;     FileOffset = 8UL; };
            { BatEntryIndex = 5UL; State = BatEntryStatePB.PayloadPartiallyPresent; FileOffset = 3UL; };
        |]
        let r = VhdxWriter.GetUsedRegions v
        Assert.True( [| 3UL; 6UL; 8UL; 9UL; |] = r )

    [<Fact>]
    member _.MergeIntervals_Fail_001() =
        let str = {
            VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 with
                ImmHeader.LogOffset = UInt64.MaxValue - 0x0FFFFFUL;
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.MergeIntervals str [||] |> ignore
        )
        Assert.StartsWith( "Unexpected the log offset or the log length value", r.Message )

    [<Fact>]
    member _.MergeIntervals_Fail_002() =
        let str = {
            VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 with
                Region.Entries = [
                    {
                        Guid = VhdxCommons.REGENT_TYPE_METADATA;
                        FileOffset = UInt64.MaxValue - 0x0FFFFFUL;
                        Length = 0x100000u;
                        Required = true;
                    };
                ]
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.MergeIntervals str [||] |> ignore
        )
        Assert.StartsWith( "Unexpected the region offset or the region length value", r.Message )

    [<Fact>]
    member _.MergeIntervals_Fail_003() =
        let wstr = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let str = {
            wstr with
                BAT.SectorBitmap = [|
                    for i in 0UL .. wstr.BAT.SectorBitmapBlockCount - 1UL do
                        yield {
                            BatEntryIndex = 0UL;
                            SBState = BatEntryStateSB.SectorBitmapPresent;
                            FileOffset = UInt64.MaxValue - 0x0FFFFFUL;
                            Bitmap = Array.empty;
                        }
                |];
        }
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.MergeIntervals str [||] |> ignore
        )
        Assert.StartsWith( "Unexpected the sector bitmap offset value", r.Message )

    [<Fact>]
    member _.MergeIntervals_Fail_004() =
        let str = VhdxWriter_Test.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxWriter.MergeIntervals str [| UInt64.MaxValue - 0x0FFFFFUL |] |> ignore
        )
        Assert.StartsWith( "Unexpected the payload block offset value", r.Message )
