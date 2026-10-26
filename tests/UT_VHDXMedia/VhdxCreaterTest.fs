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

    [<Theory>]
    //            pLogsizse, cLogsizse, pPBSize,     cPBSize,     pVDSize,          pBlockSize, isFixed
    [<InlineData( 0x100000u, 0x100000u, 0x100000u,   0x100000u,   0x2000000UL,      512,        false )>]
    [<InlineData( 0x200000u, 0x100000u, 0x100000u,   0x100000u,   0x2000000UL,      512,        false )>]
    [<InlineData( 0x100000u, 0x200000u, 0x100000u,   0x100000u,   0x2000000UL,      512,        false )>]
    [<InlineData( 0x100000u, 0x100000u, 0x200000u,   0x100000u,   0x2000000UL,      512,        false )>]
    [<InlineData( 0x100000u, 0x100000u, 0x100000u,   0x200000u,   0x2000000UL,      512,        false )>]
    [<InlineData( 0x100000u, 0x100000u, 0x100000u,   0x100000u,   0xF000000UL,      512,        false )>]
    [<InlineData( 0x100000u, 0x100000u, 0x100000u,   0x100000u,   0x2000000UL,     4096,        false )>]
    [<InlineData( 0x100000u, 0x100000u, 0x100000u,   0x100000u,   0x2000000UL,      512,        true )>]
    member _.Create_Differential_001
            ( pLogsizse : uint32 )  // log sizse for parent
            ( cLogsizse : uint32 )  // log sizse for child
            ( pPBSize : uint32 )    // payload block size for parent
            ( cPBSize : uint32 )    // payload block sizse for child
            ( pVDSize : uint64 )    // virtual disk sizse for parent
            ( pBlockSize : int32 )  // sector block sizse for parent
            ( isFixed : bool ) =    // fixed or not for child
        task {
            let pfname = Path.GetTempFileName()
            let pfa = FileAccessor( pfname, 1u, false )
            let cfname = Path.GetTempFileName()
            let cfa = FileAccessor( cfname, 1u, false )
            let ss = if pBlockSize = 512 then Blocksize.BS_512 else Blocksize.BS_4096
            try
                do! VhdxCreator.Create None pfa pLogsizse pPBSize false pVDSize ss
                let! pstr1 = VhdxReader.ReadVhdx pfa

                do! VhdxCreator.Create ( Some pfa ) cfa cLogsizse cPBSize isFixed 0x400000000000UL Blocksize.BS_4096
                let! cstr1 = VhdxReader.ReadVhdx cfa

                Assert.StrictEqual( Constants.VHDX_CREATOR_NAME, cstr1.Creator )
                Assert.StrictEqual( 0us, cstr1.ImmHeader.LogVersion )
                Assert.StrictEqual( 1us, cstr1.ImmHeader.Version )
                Assert.StrictEqual( cLogsizse, cstr1.ImmHeader.LogLength )
                Assert.StrictEqual( 0x100000UL, cstr1.ImmHeader.LogOffset )
                Assert.Empty cstr1.Log
                Assert.StrictEqual( cfa.FileSize, cstr1.LastFileSize )
                Assert.StrictEqual( cPBSize, cstr1.VDI.PayloadBlockSize )
                Assert.False( cstr1.VDI.LeaveBlockAllocated )               // The specified value is ignored.
                Assert.True( cstr1.VDI.HasParent )
                Assert.StrictEqual( pstr1.LoadedVarHeader.DataWriteGuid.ToString "D", cstr1.VDI.ParentLocator.[ "parent_linkage" ] )

                Assert.StrictEqual( pVDSize, cstr1.VDI.VirtualDiskSize )    // The specified value for a child disk is ignored.
                Assert.StrictEqual( ss, cstr1.VDI.LogicalSectorSize )       // The specified value for a child disk is ignored.
                Assert.StrictEqual( ss, cstr1.VDI.PhysicalSectorSize )      // The specified value for a child disk is ignored.

                Assert.StrictEqual( uint64 pBlockSize * 0x800000UL, cstr1.BAT.ChunkSize )
                Assert.StrictEqual( cstr1.BAT.ChunkSize / uint64 cPBSize, cstr1.BAT.ChunkRatio )
                Assert.StrictEqual( Functions.CeilDiv pVDSize ( uint64 cPBSize ), cstr1.BAT.PayloadBlockCount )
                Assert.StrictEqual( Functions.CeilDiv cstr1.BAT.PayloadBlockCount cstr1.BAT.ChunkRatio , cstr1.BAT.SectorBitmapBlockCount )
                Assert.StrictEqual( cstr1.BAT.SectorBitmapBlockCount * ( cstr1.BAT.ChunkRatio + 1UL ), cstr1.BAT.BatEntryCount )
                Assert.StrictEqual( int cstr1.BAT.PayloadBlockCount, cstr1.BAT.Payloads.Length )
                Assert.StrictEqual( int cstr1.BAT.SectorBitmapBlockCount, cstr1.BAT.SectorBitmap.Length )

                for itr in cstr1.BAT.Payloads do
                    Assert.StrictEqual( BatEntryStatePB.PayloadNotPresent, itr.State )

                for itr in cstr1.BAT.SectorBitmap do
                    Assert.StrictEqual( BatEntryStateSB.SectorBitmapPresent, itr.SBState )
                    for itr2 in itr.Bitmap do
                        Assert.StrictEqual( 0uy, itr2 )
            finally
                pfa.Close()
                GlbFunc.DeleteFile pfname
                cfa.Close()
                GlbFunc.DeleteFile cfname
        }

    [<Theory>]
    [<InlineData(   @"aaa/bbb/aaa.txt", "relative_path", @"aaa/bbb/aaa.txt", @"aaa/bbb/aaa.txt" )>]
    [<InlineData(   @"aaa\bbb\aaa.txt", "relative_path", @"aaa/bbb/aaa.txt", @"aaa\bbb\aaa.txt" )>]
    [<InlineData(   @"\\?\Volume{a39f6765-c0e4-43a2-b175-54a46c2db4a9}\a.txt", "volume_path",
                    @"\\?\Volume{a39f6765-c0e4-43a2-b175-54a46c2db4a9}\a.txt",
                    @"\\?\Volume{a39f6765-c0e4-43a2-b175-54a46c2db4a9}\a.txt" )>]
    [<InlineData(   @"c:\aaa\bbb\aaa.txt", "absolute_win32_path", @"\\?\c:\aaa\bbb\aaa.txt", @"c:\aaa\bbb\aaa.txt" )>]
    [<InlineData(   @"\\?\c:\aaa\bbb\aaa.txt", "absolute_win32_path", @"\\?\c:\aaa\bbb\aaa.txt", @"\\?\c:\aaa\bbb\aaa.txt" )>]
    member _.Create_Differential_002 ( parentPathName : string ) ( expKeyName : string ) ( expPathName1 : string ) ( expPathName2 : string ) =
        task {
            let pfname = Path.GetTempFileName()
            let fact a b c d = File.Open( pfname, b, c, d ) :> Stream   // Disguise the file name.
            let pfa = FileAccessor( parentPathName, 1u, false, fact, fun _ -> true )
            let cfname = Path.GetTempFileName()
            let cfa = FileAccessor( cfname, 1u, false )
            try
                do! VhdxCreator.Create None pfa 0x100000u 0x100000u false 0x2000000UL Blocksize.BS_512
                let! pstr1 = VhdxReader.ReadVhdx pfa

                do! VhdxCreator.Create ( Some pfa ) cfa 0x100000u 0x100000u false 0x2000000UL Blocksize.BS_512
                let! cstr1 = VhdxReader.ReadVhdx cfa

                Assert.True( cstr1.VDI.HasParent )
                Assert.StrictEqual( pstr1.LoadedVarHeader.DataWriteGuid.ToString "D", cstr1.VDI.ParentLocator.[ "parent_linkage" ] )
                if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                    Assert.StrictEqual( expPathName1, cstr1.VDI.ParentLocator.[ expKeyName ] )
                else
                    Assert.StrictEqual( expPathName2, cstr1.VDI.ParentLocator.[ expKeyName ] )

            finally
                pfa.Close()
                GlbFunc.DeleteFile pfname
                cfa.Close()
                GlbFunc.DeleteFile cfname
        }

    [<Theory>]
    [<InlineData( "", "relative_path", 32767, 32767 )>]
    [<InlineData( @"\\?\Volume{a39f6765-c0e4-43a2-b175-54a46c2db4a9}\", "volume_path", 32718, 32718 )>]
    [<InlineData( @"c:\", "absolute_win32_path", 32760, 32764 )>]
    [<InlineData( @"\\?\c:\", "absolute_win32_path", 32760, 32760 )>]
    member _.Create_Differential_003 ( pathhdr : string ) ( expKeyName : string ) ( addlen1 : int32 ) ( addlen2 : int32 ) =
        task {
            let pfname = Path.GetTempFileName()
            let fact a b c d = File.Open( pfname, b, c, d ) :> Stream
            let waddlen =  if RuntimeInformation.IsOSPlatform OSPlatform.Windows then addlen1 else addlen2
            let parentPathName = pathhdr + String.replicate waddlen "a"
            let pfa = FileAccessor( parentPathName, 1u, false, fact, fun _ -> true )

            let cfname = Path.GetTempFileName()
            let cfa = FileAccessor( cfname, 1u, false )
            try
                do! VhdxCreator.Create None pfa 0x100000u 0x100000u false 0x2000000UL Blocksize.BS_512
                do! VhdxCreator.Create ( Some pfa ) cfa 0x100000u 0x100000u false 0x2000000UL Blocksize.BS_512
                let! cstr1 = VhdxReader.ReadVhdx cfa
                Assert.StrictEqual( 32767, cstr1.VDI.ParentLocator.[ expKeyName ].Length )

            finally
                pfa.Close()
                GlbFunc.DeleteFile pfname
                cfa.Close()
                GlbFunc.DeleteFile cfname
        }

    [<Theory>]
    [<InlineData( "", 32768, 32768 )>]
    [<InlineData( @"\\?\Volume{a39f6765-c0e4-43a2-b175-54a46c2db4a9}\", 32719, 32719 )>]
    [<InlineData( @"c:\", 32761, 32765 )>]
    [<InlineData( @"\\?\c:\", 32761, 32761 )>]
    member _.Create_Differential_Fail_001 ( pathhdr : string ) ( addlen1 : int32 ) ( addlen2 : int32 ) =
        task {
            let pfname = Path.GetTempFileName()
            let fact a b c d = File.Open( pfname, b, c, d ) :> Stream
            let waddlen =  if RuntimeInformation.IsOSPlatform OSPlatform.Windows then addlen1 else addlen2
            let parentPathName = pathhdr + String.replicate waddlen "a"
            let pfa = FileAccessor( parentPathName, 1u, false, fact, fun _ -> true )

            let cfname = Path.GetTempFileName()
            let cfa = FileAccessor( cfname, 1u, false )

            try
                do! VhdxCreator.Create None pfa 0x100000u 0x100000u false 0x2000000UL Blocksize.BS_512
                let! r = Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    do! VhdxCreator.Create ( Some pfa ) cfa 0x100000u 0x100000u false 0x2000000UL Blocksize.BS_512
                })
                Assert.StartsWith( "Parent VHDX file name is too long", r.Message )

            finally
                pfa.Close()
                GlbFunc.DeleteFile pfname
                cfa.Close()
                GlbFunc.DeleteFile cfname
        }

    [<Theory>]
    [<InlineData( 0x1000000UL,     0x100000u, 0x100000u, false, 512  )>]
    [<InlineData( 512UL,           0x100000u, 0x100000u, false, 512  )>]
    [<InlineData( 1UL,             0x100000u, 0x200000u, false, 512  )>]
    [<InlineData( 0x1000000UL,     0x100000u, 0x100000u, true,  4096 )>]
    member _.RawToVHDX_001 ( inputSizse : uint64 ) ( logsizse : uint32 ) ( pbsize : uint32 ) ( isFixed : bool ) ( bsize : int32 ) =
        task {
            let rawfname = Path.GetTempFileName()
            let rawfs = new FileStream( rawfname, FileMode.Open, FileAccess.Write, FileShare.None )
            try
                let cnt = Functions.CeilDiv inputSizse 0x100000UL
                let buf = Array.zeroCreate<byte> 0x100000
                for i in 0UL .. cnt - 1UL do
                    Random.Shared.NextBytes buf
                    let len = min 0x100000UL ( inputSizse - i * 0x100000UL )
                    rawfs.Write( buf, 0, int len )
                Assert.StrictEqual( inputSizse, uint64 rawfs.Length )
            finally
                rawfs.Close()
                rawfs.Dispose()

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            let ss = if bsize = 512 then Blocksize.BS_512 else Blocksize.BS_4096
            try
                do! VhdxCreator.RawToVHDX rawfname fa logsizse pbsize isFixed ss
                let! str1 = VhdxReader.ReadVhdx fa

                let rawfs = new FileStream( rawfname, FileMode.Open, FileAccess.Read, FileShare.None )
                try
                    let buf1 = Array.zeroCreate<byte> ( int pbsize )
                    let buf2 = Array.zeroCreate<byte> ( int pbsize )
                    for i in 0UL .. ( str1.BAT.PayloadBlockCount - 1UL ) do
                        let vdoffset = str1.BAT.Payloads.[ int i ].FileOffset
                        let rawoffset = i * ( uint64 pbsize )
                        let rawlen = min ( uint64 pbsize ) ( uint64 rawfs.Length - rawoffset )
                        do! fa.ReadArray vdoffset buf1
                        rawfs.Seek( int64 rawoffset, SeekOrigin.Begin ) |> ignore
                        rawfs.Read( buf2, 0, int rawlen ) |> ignore
                        for j = 0 to int rawlen - 1 do
                            Assert.StrictEqual( buf1.[j], buf2.[j] )
                finally
                    rawfs.Close()
                    rawfs.Dispose()
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
                GlbFunc.DeleteFile rawfname
        }

    [<Fact>]
    member _.RawToVHDX_Fail_001 () =
        task {
            let rawfname = Path.Combine( Path.GetTempPath(), Guid.NewGuid() |> _.ToString() )
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! _ = Assert.ThrowsAnyAsync<FileNotFoundException>( fun () -> task {
                    do! VhdxCreator.RawToVHDX rawfname fa 0x100000u 0x100000u true Blocksize.BS_512
                })
                ()
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.RawToVHDX_Fail_002 () =
        task {
            let rawfname = Path.GetTempFileName()
            try
                File.WriteAllBytes( rawfname, Array.zeroCreate<byte> 512 )

                let fname = Path.GetTempFileName()
                let fa = FileAccessor( fname, 1u, true )
                try
                    let! r = Assert.ThrowsAsync<InvalidOperationException>( fun () -> task {
                        do! VhdxCreator.RawToVHDX rawfname fa 0x100000u 0x100000u true Blocksize.BS_512
                    })
                    Assert.StartsWith( "File opened read-only", r.Message )
                finally
                    fa.Close()
                    GlbFunc.DeleteFile fname
            finally
                GlbFunc.DeleteFile rawfname
        }

