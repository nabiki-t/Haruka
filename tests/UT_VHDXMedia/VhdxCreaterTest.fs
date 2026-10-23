//=============================================================================
// Haruka Software Storage.
// VhdxCreaterTest.fs : Test cases for VhdxCreater class.
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

type VhdxCreater_Test () =

    [<Theory>]
    [<InlineData( 0x100001u, "Log are size must be multiples of 1MB" )>]
    [<InlineData( 0u,        "Log are size must be 1MB or more" )>]
    member _.Create_Fail_LogAreaSize_001 ( logsizse : uint32 ) ( expmsg : string ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        do! VhdxCreator.Create None fa logsizse 0x100000u false 0x4000000UL Blocksize.BS_512
                    } )
                Assert.StartsWith( expmsg, r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 0x100001u )>]
    [<InlineData( 0u )>]
    [<InlineData( 0x20000000u )>]
    [<InlineData( 0x300000u )>]
    member _.Create_Fail_PayloadBlockSize_001 ( pbsizse : uint32 ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        do! VhdxCreator.Create None fa 0x100000u pbsizse false 0x4000000UL Blocksize.BS_512
                    } )
                Assert.StartsWith( "The payload block length must be a power of 2", r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 0x400000000200UL, 512,  "The virtual disk size must be 64TB or less" )>]
    [<InlineData( 0UL,              512,  "The virtual disk size must be at least 1 byte" )>]
    [<InlineData( 1UL,              512,  "The virtual disk size must be a multiple of the sector length" )>]
    [<InlineData( 511UL,            512,  "The virtual disk size must be a multiple of the sector length" )>]
    [<InlineData( 513UL,            512,  "The virtual disk size must be a multiple of the sector length" )>]
    [<InlineData( 512UL,            4096, "The virtual disk size must be a multiple of the sector length" )>]
    [<InlineData( 4095UL,           4096, "The virtual disk size must be a multiple of the sector length" )>]
    [<InlineData( 4097UL,           4096, "The virtual disk size must be a multiple of the sector length" )>]
    member _.Create_Fail_VirtualDiskSize_001 ( vdsize : uint64 ) ( secsize : int32 ) ( expmsg : string ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let ss = if secsize = 512 then Blocksize.BS_512 else Blocksize.BS_4096
                        do! VhdxCreator.Create None fa 0x100000u 0x100000u false vdsize ss
                    } )
                Assert.StartsWith( expmsg, r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.Create_Fail_ReadOnly_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true )
            try
                let! r =
                    Assert.ThrowsAsync<InvalidOperationException>( fun () -> task {
                        do! VhdxCreator.Create None fa 0x100000u 0x100000u false 0x4000000UL Blocksize.BS_512
                    } )
                Assert.StartsWith( "File opened read-only", r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 0x100000u, 0x100000u,   0x4000000UL,      512,  false )>]
    [<InlineData( 0x200000u, 0x100000u,   0x4000000UL,      512,  false )>]
    [<InlineData( 0x100000u, 0x200000u,   0x4000000UL,      512,  false )>]
    [<InlineData( 0x100000u, 0x100000u,   512UL,            512,  false )>]
    [<InlineData( 0x100000u, 0x100000u,   0x8000000UL,      512,  false )>]
    [<InlineData( 0x100000u, 0x100000u,   0x4000000UL,      4096, false )>]
    [<InlineData( 0x100000u, 0x100000u,   0x400000000000UL, 512,  false )>]
    [<InlineData( 0x100000u, 0x10000000u, 0x400000000000UL, 4096, false )>]
    [<InlineData( 0x100000u, 0x100000u,   0x4000000UL,      512,  true )>]
    [<InlineData( 0x200000u, 0x100000u,   0x4000000UL,      512,  true )>]
    [<InlineData( 0x100000u, 0x200000u,   0x4000000UL,      512,  true )>]
    [<InlineData( 0x100000u, 0x100000u,   512UL,            512,  true )>]
    [<InlineData( 0x100000u, 0x100000u,   0x8000000UL,      512,  true )>]
    [<InlineData( 0x100000u, 0x100000u,   0x4000000UL,      4096, true )>]
    member _.Create_Dynamic_Fix_001 ( logsizse : uint32 ) ( pbsize : uint32 ) ( vdsize : uint64 ) ( bsize : int32 ) ( isFixed : bool ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            let ss = if bsize = 512 then Blocksize.BS_512 else Blocksize.BS_4096
            try
                do! VhdxCreator.Create None fa logsizse pbsize isFixed vdsize ss
                let! str1 = VhdxReader.ReadVhdx fa

                Assert.StrictEqual( Constants.VHDX_CREATOR_NAME, str1.Creator )
                Assert.StrictEqual( 0us, str1.ImmHeader.LogVersion )
                Assert.StrictEqual( 1us, str1.ImmHeader.Version )
                Assert.StrictEqual( logsizse, str1.ImmHeader.LogLength )
                Assert.StrictEqual( 0x100000UL, str1.ImmHeader.LogOffset )
                Assert.Empty str1.Log
                Assert.StrictEqual( fa.FileSize, str1.LastFileSize )
                Assert.StrictEqual( pbsize, str1.VDI.PayloadBlockSize )
                if isFixed then
                    Assert.True( str1.VDI.LeaveBlockAllocated )
                else
                    Assert.False( str1.VDI.LeaveBlockAllocated )
                Assert.False( str1.VDI.HasParent )
                Assert.StrictEqual( vdsize, str1.VDI.VirtualDiskSize )
                Assert.StrictEqual( ss, str1.VDI.LogicalSectorSize )
                Assert.StrictEqual( ss, str1.VDI.PhysicalSectorSize )
                Assert.Empty str1.VDI.ParentLocator
                Assert.StrictEqual( uint64 bsize * 0x800000UL, str1.BAT.ChunkSize )
                Assert.StrictEqual( str1.BAT.ChunkSize / uint64 pbsize, str1.BAT.ChunkRatio )
                Assert.StrictEqual( Functions.CeilDiv vdsize ( uint64 pbsize ), str1.BAT.PayloadBlockCount )
                Assert.StrictEqual( Functions.CeilDiv str1.BAT.PayloadBlockCount str1.BAT.ChunkRatio , str1.BAT.SectorBitmapBlockCount )
                Assert.StrictEqual( str1.BAT.PayloadBlockCount + ( str1.BAT.PayloadBlockCount - 1UL ) / str1.BAT.ChunkRatio, str1.BAT.BatEntryCount )
                Assert.StrictEqual( int str1.BAT.PayloadBlockCount, str1.BAT.Payloads.Length )
                Assert.StrictEqual( int str1.BAT.SectorBitmapBlockCount, str1.BAT.SectorBitmap.Length )

                if not isFixed then
                    for itr in str1.BAT.Payloads do
                        Assert.StrictEqual( BatEntryStatePB.PayloadNotPresent, itr.State )
                else
                    for itr in str1.BAT.Payloads do
                        Assert.StrictEqual( BatEntryStatePB.PayloadFullyPresent, itr.State )

                for itr in str1.BAT.SectorBitmap do
                    Assert.StrictEqual( BatEntryStateSB.SectorBitmapNotPresent, itr.SBState )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

