//=============================================================================
// Haruka Software Storage.
// VhdxCommonsTest.fs : Test cases for VhdxCommons class.
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

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test

//=============================================================================
// Class implementation

type VhdxCommons_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let zeroHeader : VhdxHeader = {
        Signature = 0u;
        Checksum = 0u;
        LogVersion = 0us;
        Version = 0us;
        LogLength = 0u;
        LogOffset = 0UL;
        Offset = 0UL;
        Index = 0;
    }

    let zeroVarHeader : VhdxMutableHeader = {
        SequenceNumber = 0UL;
        FileWriteGuid = Guid();
        DataWriteGuid = Guid();
        LogGuid = Guid();
    }

    let GenStructures ( payloadBlockSize : uint32 ) ( virtualDiskSize : uint64 ) ( initialPB : BatEntryStatePB ) ( initialSB : BatEntryStateSB ) ( hasParent : bool ) =
        let chunkSize = 512UL * 8388608UL;                          // Blocksize * 2^23
        let chunkRatio = chunkSize / ( uint64 payloadBlockSize )    // ChunkSize / PayloadBlockSize
        let payloadBlockCount =                                     // ceil( PayloadBlockCount / ChunkRatio )
                ( virtualDiskSize + ( uint64 payloadBlockSize ) - 1UL ) / ( uint64 payloadBlockSize )
        let SectorBitmapBlockCount =                                // ceil( PayloadBlockCount / ChunkRatio )
                ( payloadBlockCount + chunkRatio - 1UL ) / chunkRatio
        let batEntryCount =
                if hasParent then
                    // SectorBitmapBlockCount * ( ChunkRatio + 1 )
                    SectorBitmapBlockCount * ( chunkRatio + 1UL )
                else
                    // PayloadBlockCount + floor( ( PayloadBlockCount - 1 ) / ChunkRatio )
                    payloadBlockCount + ( payloadBlockCount - 1UL ) / chunkRatio

        let structure = {
            Creator = "";
            ImmHeader = { zeroHeader with Offset = 0x10000UL };
            LoadedVarHeader = zeroVarHeader;
            Log = [];
            LastFileSize = 1024UL;
            Region = {
                Signature = 0u;
                Checksum = 0u;
                EntryCount = 0u;
                Entries = [];
            }
            VDI = {
                PayloadBlockSize = payloadBlockSize;
                LeaveBlockAllocated = false;
                HasParent = hasParent;
                VirtualDiskSize = virtualDiskSize;
                VirtualDiskId = Guid();
                LogicalSectorSize = Blocksize.BS_512;
                PhysicalSectorSize = Blocksize.BS_512;
                ParentLocator = Map<string,string>( [] );
            };
            BAT = {
                BATRegionOffset = 0UL;
                BATRegionLength = 4096u;
                ChunkSize = chunkSize;
                ChunkRatio = chunkRatio;
                PayloadBlockCount = payloadBlockCount;
                SectorBitmapBlockCount = SectorBitmapBlockCount;
                BatEntryCount = batEntryCount;
                Payloads = [|
                    for i in 0UL .. payloadBlockCount - 1UL ->{
                        BatEntryIndex = i;
                        State = initialPB;
                        FileOffset = ( uint64 payloadBlockSize ) * i;
                    };
                |];
                SectorBitmap = [|
                    for i in 0UL .. SectorBitmapBlockCount - 1UL -> {
                        BatEntryIndex = i;
                        SBState = initialSB;
                        FileOffset = 0UL;
                        Bitmap = Array.zeroCreate<byte> 1048576
                    }
                |];
            }
        }
        structure


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 7 )>]
    member _.CheckHeaderChecksum_001( len : int32 ) =
        let v = Array.zeroCreate<byte> len
        Assert.ThrowsAny<Exception>( fun () ->
                VhdxCommons.CheckHeaderChecksum v 0u |> ignore
        )
        |> ignore

    [<Fact>]
    member _.CheckHeaderChecksum_002() =
        let v = [| 0uy; 1uy; 2uy; 3uy; 0uy; 0uy; 0uy; 0uy; |]
        let crc = Crc32C.Compute v
        ByteFunc.WriteU32LE v 4u crc
        let r = VhdxCommons.CheckHeaderChecksum v crc
        Assert.True( r )
        Assert.StrictEqual( crc, ByteFunc.ReadU32LE v 4u )

    [<Theory>]
    [<InlineData( 64UL * 1024UL, 100UL, 101UL, 100UL, 102UL )>]
    [<InlineData( 128UL * 1024UL, 100UL, 100UL, 101UL, 102UL )>]
    member _.UpdateHeader_001 ( hdoffset : uint64 ) ( sq : uint64 ) ( extsq0 : uint64 ) ( extsq1 : uint64 ) ( extrsq : uint64 ) =
        task {
            let fname = Path.GetTempFileName()
            let ms = new MemoryStream()
            let fa = FileAccessor( fname, 1u, false, fun _ _ _ _ -> ms )
            do! fa.SetFileSize( 192UL * 1024UL )

            let header : VhdxHeader = {
                Signature = 0x00112233u;
                Checksum = 0xFFFFFFFFu;
                LogVersion = 0x2233us;
                Version = 0x4455us;
                LogLength = 0x66778899u;
                LogOffset = 0xAABBCCDDEEFF1122UL;
                Offset = hdoffset;
                Index = 0;
            }
            let verheader : VhdxMutableHeader = {
                SequenceNumber = sq;
                FileWriteGuid = Guid.NewGuid();
                DataWriteGuid = Guid.NewGuid();
                LogGuid = Guid.NewGuid();
            }
            let! r = VhdxCommons.UpdateHeader fa header verheader
            Assert.StrictEqual( extrsq, r.SequenceNumber )

            ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
            let v = ms.ToArray()
            Assert.StrictEqual( 192 * 1024, v.Length )

            let hdpos = [| 64u * 1024u; 128u * 1024u; |]
            let seqnum = [| extsq0; extsq1; |]
            for i = 0 to 1 do
                Assert.StrictEqual( header.Signature, ByteFunc.ReadU32BE v ( hdpos.[i] + 0u ) )
                let crc1 = ByteFunc.ReadU32LE v ( hdpos.[i] + 4u )
                Assert.True( VhdxCommons.CheckHeaderChecksum v.[ int hdpos.[i] .. int( hdpos.[i] + 4095u ) ] crc1 )
                Assert.StrictEqual( seqnum.[i], ByteFunc.ReadU64LE v ( hdpos.[i] + 8u ) )
                Assert.StrictEqual( verheader.FileWriteGuid, ByteFunc.ReadGuid v ( hdpos.[i] + 16u ) )
                Assert.StrictEqual( verheader.DataWriteGuid, ByteFunc.ReadGuid v ( hdpos.[i] + 32u ) )
                Assert.StrictEqual( verheader.LogGuid, ByteFunc.ReadGuid v ( hdpos.[i] + 48u ) )
                Assert.StrictEqual( header.LogVersion, ByteFunc.ReadU16LE v ( hdpos.[i] + 64u ) )
                Assert.StrictEqual( header.Version, ByteFunc.ReadU16LE v ( hdpos.[i] + 66u ) )
                Assert.StrictEqual( header.LogLength, ByteFunc.ReadU32LE v ( hdpos.[i] + 68u ) )
                Assert.StrictEqual( header.LogOffset, ByteFunc.ReadU64LE v ( hdpos.[i] + 72u ) )

            File.Delete fname
        }

    [<Fact>]
    member _.UpdateHeader_FileTooSmall_001 () =
        task {
            let fname = Path.GetTempFileName()
            try
                let ms = new MemoryStream()
                let fa = FileAccessor( fname, 1u, false, fun _ _ _ _ -> ms )
                do! fa.SetFileSize( 192UL * 1024UL - 1UL )
                let! _ =
                    Assert.ThrowsAsync<ArgumentOutOfRangeException>( fun () -> task {
                        let! _ = VhdxCommons.UpdateHeader fa zeroHeader zeroVarHeader
                        ()
                    } )
                ()
            finally
                File.Delete fname
        }

    [<Fact>]
    member _.UpdateHeader_ReadOnly_001 () =
        task {
            let fname = Path.GetTempFileName()
            try
                let ms = new MemoryStream()
                let fa = FileAccessor( fname, 1u, true, fun _ _ _ _ -> ms )
                let! r =
                    Assert.ThrowsAnyAsync<Exception>( fun () -> task {
                        let! _ = VhdxCommons.UpdateHeader fa zeroHeader zeroVarHeader
                        ()
                    } )
                Assert.StartsWith( "File opened read-only", r.Message )
            finally
                File.Delete fname
        }

    static member m_ResolvLBA_Dynamic_NoAllocated_001_Data : obj[][] = [|
        [| BatEntryStatePB.PayloadNotPresent |]
        [| BatEntryStatePB.PayloadUndefined |]
        [| BatEntryStatePB.PayloadZero |]
        [| BatEntryStatePB.PayloadUnapped |]
    |]

    [<Theory>]
    [<MemberData( "m_ResolvLBA_Dynamic_NoAllocated_001_Data" )>]
    member _.ResolvLBA_Dynamic_NoAllocated_001 ( stat : BatEntryStatePB ) =
        let structure = GenStructures 2048u 4096UL stat BatEntryStateSB.SectorBitmapNotPresent false
        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 0UL ) [| structure |]
        Assert.True( r.IsNone )

    [<Theory>]
    [<InlineData( 0UL, 0UL )>]
    [<InlineData( 4UL, 2048UL )>]
    [<InlineData( 7UL, 3584UL )>]
    member _.ResolvLBA_Dynamic_Allocated_001 ( lba : uint64 ) ( offset : uint64 ) =
        let structure = GenStructures 2048u 4096UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 lba ) [| structure |]
        Assert.StrictEqual( ValueSome( struct( 0, offset ) ), r )

    [<Fact>]
    member _.ResolvLBA_Dynamic_Allocated_002 () =
        let structure = GenStructures 4194304u 8589934592UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 16777215UL ) [| structure |]
        Assert.StrictEqual( ValueSome( struct( 0, 8589934080UL ) ), r )

    [<Theory>]
    [<InlineData( 0UL, true, 0UL )>]
    [<InlineData( 3UL, false, 0UL )>]
    [<InlineData( 4UL, true, 2048UL )>]
    [<InlineData( 7UL, false, 0UL )>]
    member _.ResolvLBA_Dynamic_Partially_001 ( lba : uint64 ) ( exist : bool ) ( offset : uint64 ) =
        let structure = GenStructures 2048u 4096UL BatEntryStatePB.PayloadPartiallyPresent BatEntryStateSB.SectorBitmapPresent false
        structure.BAT.SectorBitmap.[0].Bitmap.[0] <- 0x33uy
        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 lba ) [| structure |]
        if exist then
            Assert.StrictEqual( ValueSome( struct( 0, offset ) ), r )
        else
            Assert.StrictEqual( ValueNone, r )

    static member m_ResolvLBA_Diff_NoAllocated_001_Data : obj[][] = [|
        [| true; BatEntryStatePB.PayloadNotPresent; true; |]
        [| true; BatEntryStatePB.PayloadUndefined; false; |]
        [| true; BatEntryStatePB.PayloadZero; false; |]
        [| true; BatEntryStatePB.PayloadUnapped; false; |]
        [| false; BatEntryStatePB.PayloadNotPresent; false; |]
        [| false; BatEntryStatePB.PayloadUndefined; false; |]
        [| false; BatEntryStatePB.PayloadZero; false; |]
        [| false; BatEntryStatePB.PayloadUnapped; false; |]
    |]

    [<Theory>]
    [<MemberData( "m_ResolvLBA_Diff_NoAllocated_001_Data" )>]
    member _.ResolvLBA_Diff_NoAllocated_001 ( pexist : bool ) ( stat : BatEntryStatePB ) ( rexist : bool ) =
        let structure0 =
            let pstat = if pexist then BatEntryStatePB.PayloadFullyPresent else BatEntryStatePB.PayloadNotPresent
            GenStructures 2048u 4096UL pstat BatEntryStateSB.SectorBitmapNotPresent false
        let structure1 = GenStructures 2048u 4096UL stat BatEntryStateSB.SectorBitmapNotPresent true
        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 0UL ) [| structure0; structure1 |]
        if rexist then
            Assert.StrictEqual( ValueSome( struct( 0, 0UL ) ), r )
        else
            Assert.True( r.IsNone )

    [<Fact>]
    member _.ResolvLBA_Diff_NoAllocated_002 () =
        let structure0 = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let structure1 = GenStructures 4096u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent true

        structure0.BAT.Payloads.[2] <- {
            BatEntryIndex = 2UL;
            State = BatEntryStatePB.PayloadFullyPresent;
            FileOffset = 0x00000000AAAA0000UL;
        }

        structure1.BAT.Payloads.[1] <- {
            BatEntryIndex = 1UL;
            State = BatEntryStatePB.PayloadNotPresent;
            FileOffset = 0UL;
        }

        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 9UL ) [| structure0; structure1 |]
        Assert.StrictEqual( ValueSome( struct( 0, 0x00000000AAAA0200UL ) ), r )

    [<Theory>]
    [<InlineData( 0UL, true, 0UL )>]
    [<InlineData( 3UL, false, 1536UL )>]
    [<InlineData( 4UL, true, 2048UL )>]
    [<InlineData( 7UL, false, 3584UL )>]
    member _.ResolvLBA_Diff_Partially_001 ( lba : uint64 ) ( exist : bool ) ( offset : uint64 ) =
        let structure0 = GenStructures 2048u 4096UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let structure1 = GenStructures 2048u 4096UL BatEntryStatePB.PayloadPartiallyPresent BatEntryStateSB.SectorBitmapPresent true
        structure1.BAT.SectorBitmap.[0].Bitmap.[0] <- 0x33uy
        let r = VhdxCommons.ResolvLBA ( blkcnt_me.ofUInt64 lba ) [| structure0; structure1; |]
        if exist then
            Assert.StrictEqual( ValueSome( struct( 1, offset ) ), r )
        else
            Assert.StrictEqual( ValueSome( struct( 0, offset ) ), r )

    [<Theory>]
    [<InlineData( 0UL, 0u, 0u )>]
    [<InlineData( 1UL, 0u, 1u )>]
    [<InlineData( 3UL, 0u, 3u )>]
    [<InlineData( 4UL, 1u, 0u )>]
    [<InlineData( 15UL, 3u, 3u )>]
    member _.LBAtoPayloadBlockIndex_001 ( lba : uint64 ) ( pbidx : uint32 ) ( offset : uint32 ) =
        let structure = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let struct( a, b ) = VhdxCommons.LBAtoPayloadBlockIndex ( blkcnt_me.ofUInt64 lba ) structure
        Assert.StrictEqual( pbidx, a )
        Assert.StrictEqual( blkcnt_me.ofUInt32 offset, b )

    [<Theory>]
    [<InlineData( 0UL, 0u, 0u, 0u )>]
    [<InlineData( 7UL, 0u, 0u, 7u )>]
    [<InlineData( 8UL, 0u, 1u, 0u )>]
    [<InlineData( 15UL, 0u, 1u, 7u )>]
    [<InlineData( 8388607UL, 0u, 1048575u, 7u )>]
    [<InlineData( 8388608UL, 1u, 0u, 0u )>]
    member _.LBAtoSectorBitmapIndex_001 ( lba : uint64 ) ( sbidx : uint32 ) ( byteoff : uint32 ) ( bitoff : uint32 ) =
        let structure = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let struct( a, b, c ) = VhdxCommons.LBAtoSectorBitmapIndex ( blkcnt_me.ofUInt64 lba ) structure
        Assert.StrictEqual( sbidx, a )
        Assert.StrictEqual( byteoff, b )
        Assert.StrictEqual( bitoff, c )

    [<Theory>]
    [<InlineData( false, 0UL, 0L )>]
    [<InlineData( false, 1UL, 1048576L )>]
    [<InlineData( false, 2UL, 2097152L )>]
    [<InlineData( true, 1UL, 1048576L )>]
    member _.CreateRandomFile_001 ( initexist : bool ) ( req : uint64 ) ( res : int64 ) =
        let fname = Path.GetTempFileName()
        if not initexist then
            File.Delete fname

        VhdxCommons.CreateRandomFile fname req

        Assert.True( File.Exists fname )
        let s = File.OpenRead fname
        Assert.StrictEqual( res, s.Length )
        s.Close()
        s.Dispose()
        File.Delete fname

    [<Fact>]
    member _.CreateRandomFile_002 () =
        let fname = Path.GetTempFileName()
        File.Delete fname
        Directory.CreateDirectory fname |> ignore

        Assert.ThrowsAny<Exception>( fun () ->
            VhdxCommons.CreateRandomFile fname 1UL
        ) |> ignore

        Directory.Delete fname

    static member m_GetParentFileName_001_Data : obj[][] = [|
        [| [| ( "relative_path", "a" ); |]; RelativePath( "a" ); |];
        [| [| ( "volume_path", "b" ); |]; VolumePath( "b" ); |];
        [| [| ( "absolute_win32_path", "c" ); |]; AbsoluteWin32Path( "c" ); |];
        [| [| ( "volume_path", "a" ); ( "relative_path", "b" ); |]; RelativePath( "b" ); |];
        [| [| ( "absolute_win32_path", "a" ); ( "volume_path", "b" ); |]; VolumePath( "b" ); |];
        [| [| ( "relative_path", "a" ); ( "absolute_win32_path", "b" ); |]; RelativePath( "a" ); |];
    |]

    [<Theory>]
    [<MemberData( "m_GetParentFileName_001_Data" )>]
    member _.GetParentFileName_001 ( data : ( string * string ) [] ) ( exp : ParentLocatorType ) =
        let structure1 = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let prguid = Guid.NewGuid()
        let pd = 
            Map<string,string> data
            |> Map.add "parent_linkage" ( prguid.ToString "D" )
        let structure2 = {
            structure1 with
                VDI.ParentLocator = pd
        }
        let struct( a, b ) = VhdxCommons.GetParentFileName structure2
        Assert.StrictEqual( prguid, a )
        Assert.StrictEqual( exp, b )

    [<Fact>]
    member _.GetParentFileName_002 () =
        let structure1 = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let structure2 = {
            structure1 with
                VDI.ParentLocator = Map<string,string> [| ( "relative_path", "a" ) |]
        }
        Assert.ThrowsAny<Exception>( fun () ->
            VhdxCommons.GetParentFileName structure2 |> ignore
        ) |> ignore

    [<Fact>]
    member _.GetParentFileName_003 () =
        let structure1 = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
        let structure2 = {
            structure1 with
                VDI.ParentLocator = Map<string,string> [| ( "parent_linkage", Guid.NewGuid() |> _.ToString() ) |]
        }
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxCommons.GetParentFileName structure2 |> ignore
            )
        Assert.StartsWith( "Unable to identify the parent VHDX file name", r.Message )

    [<Fact>]
    member _.UpdateFileWriteGuidAndDataWriteGuid_001 () =
        task {
            let fname = Path.GetTempFileName()
            let ms = new MemoryStream()
            let fa = FileAccessor( fname, 1u, false, fun _ _ _ _ -> ms )
            do! fa.SetFileSize( 192UL * 1024UL )

            let structure = GenStructures 2048u 8192UL BatEntryStatePB.PayloadFullyPresent BatEntryStateSB.SectorBitmapNotPresent false
            let verheader : VhdxMutableHeader = {
                SequenceNumber = 0UL;
                FileWriteGuid = Guid.NewGuid();
                DataWriteGuid = Guid.NewGuid();
                LogGuid = Guid.NewGuid();
            }
            let! r = VhdxCommons.UpdateFileWriteGuidAndDataWriteGuid fa structure verheader
            Assert.NotStrictEqual( verheader.FileWriteGuid, r.FileWriteGuid )
            Assert.NotStrictEqual( verheader.DataWriteGuid, r.DataWriteGuid )
            Assert.StrictEqual( Guid(), r.LogGuid )
            Assert.StrictEqual( 3UL, r.SequenceNumber )

            ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
            let v = ms.ToArray()
            Assert.StrictEqual( 192 * 1024, v.Length )

            let hdpos = [| 64u * 1024u; 128u * 1024u; |]
            for i = 0 to 1 do
                Assert.StrictEqual( r.FileWriteGuid, ByteFunc.ReadGuid v ( hdpos.[i] + 16u ) )
                Assert.StrictEqual( r.DataWriteGuid, ByteFunc.ReadGuid v ( hdpos.[i] + 32u ) )
                Assert.StrictEqual( Guid(), ByteFunc.ReadGuid v ( hdpos.[i] + 48u ) )

            File.Delete fname
        }
