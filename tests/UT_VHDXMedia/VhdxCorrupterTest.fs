//=============================================================================
// Haruka Software Storage.
// VhdxCorrupterTest.fs : Test cases for VhdxCorrupter class.
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

type VhdxCorrupter_Test () =

    let defaultImmHeader = {
        Signature = 0u;     // unused
        Checksum = 0u;      // unused
        LogVersion = 0us;   // unused
        Version = 0us;      // unused
        LogLength = 0x100000u;
        LogOffset = 0UL;
        Offset = 0UL;       // unused
        Index = 0;          // unised
    }

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 4095 )>]
    [<InlineData( 4097 )>]
    member _.CreateLogEntry_DataSector_001 ( len : int ) =
        let data = Array.zeroCreate<byte> len
        let r =
            Assert.Throws<ArgumentException>( fun () ->
                VhdxCorrupter.CreateLogEntry_DataSector sec4k_me.zero64 data 0UL |> ignore
            )
        Assert.StartsWith( "data must be 4096 bytes", r.Message )

        let data = Array.zeroCreate<byte> 4096
        Random.Shared.NextBytes data
        let fo = 0x1122334455667000UL
        let v1 = VhdxCorrupter.CreateLogEntry_DataSector ( sec4k_me.ofUInt64 ( fo / 4096UL ) ) data 0x8899AABBCCDDEEFFUL
        let v2 = VhdxTestFunc.GenDataDiscriptor data.[ 4092 .. 4095 ] data.[ 0 .. 7 ] fo 0x8899AABBCCDDEEFFUL
        Assert.True(( v1 = v2 ))

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 8 )>]
    member _.CreateLogEntry_001 ( descCount : int ) =
        let vdata = [|
            for i = 1 to descCount - 1 do
                let wv = Array.zeroCreate<byte> 4096
                Random.Shared.NextBytes wv
                yield wv
        |]
        let entry1 =
            vdata
            |> Array.mapi ( fun idx itr ->
                struct ( sec4k_me.ofUInt64 ( uint64 idx ), itr )
            )
        let tail = 0x0u
        let secnum = 0x2233445566778899UL
        let logguid = Guid.NewGuid()
        let flushedFileOffset = 0x33445566778899AAUL
        let lastFileOffset = 0x445566778899AABBUL
        let v1 = VhdxCorrupter.CreateLogEntry entry1 tail secnum logguid flushedFileOffset lastFileOffset

        let entry2 = [| {
            PatchPosition = 0;
            PatchData = [||];
            Descriptor =
                vdata
                |> Array.mapi ( fun idx itr ->
                    TestLogDesc.Data( itr, uint64 idx * 4096UL )
                )
        } |]
        let v2 = VhdxTestFunc.GenLogData 0x100000 0 entry2 logguid secnum flushedFileOffset lastFileOffset
        for i = 0 to v1.Length - 1 do
            Assert.StrictEqual( v1.[i], v2.[i] )

    static member m_WriteLogEntry_Fail_001_data : obj[][] = [|
        [| 1u;    [||];        4096;     "Argument offset must be multiple of 4KB"; |];
        [| 4095u; [||];        4096;     "Argument offset must be multiple of 4KB"; |];
        [| 0u;    [||];        0;        "Argument rightLogEntry must be 4 KB or larger"; |];
        [| 0u;    [||];        1;        "Argument rightLogEntry must be multiple of 4KB"; |];
        [| 0u;    [||];        4095;     "Argument rightLogEntry must be multiple of 4KB"; |];
        [| 0u;    [| 0; |];    4096;     "Argument dummyLogEntry must be 4 KB or larger"; |];
        [| 0u;    [| 4095; |]; 4096;     "Argument dummyLogEntry must be multiple of 4KB"; |];
        [| 0u;    [||];        0x101000; "The log exceeds the log area length"; |];
    |]

    [<Theory>]
    [<MemberData( "m_WriteLogEntry_Fail_001_data" )>]
    member _.WriteLogEntry_Fail_001 ( offset : uint32 ) ( dlelen : int[] ) ( rlelen : int ) ( expmsg : string ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let dle =
                    dlelen
                    |> Array.map Array.zeroCreate<byte>
                    |> Array.toList
                let rle = Array.zeroCreate<byte> rlelen
                let! r = Assert.ThrowsAsync<ArgumentException>( fun () -> task {
                    let! _ = VhdxCorrupter.WriteLogEntry fa defaultImmHeader offset dle rle
                    ()
                } )
                Assert.StartsWith( expmsg, r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 0u )>]
    [<InlineData( 0xFE000u )>]
    [<InlineData( 0x100000u )>]
    member _.WriteLogEntry_001 ( offset : uint ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 0x100000UL
                let rle = Array.zeroCreate<byte> 8192
                Random.Shared.NextBytes rle
                do! VhdxCorrupter.WriteLogEntry fa defaultImmHeader offset List.Empty rle

                let buf = Array.zeroCreate<byte> 8192
                do! fa.ReadArray ( uint64 offset % 0x100000UL ) buf
                Array.iter2 ( fun i j -> Assert.StrictEqual( i, j ) ) rle buf
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.WriteLogEntry_002 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 0x100000UL
                let rle = Array.zeroCreate<byte> 8192
                Random.Shared.NextBytes rle
                do! VhdxCorrupter.WriteLogEntry fa defaultImmHeader 0xFF000u List.Empty rle

                let buf = Array.zeroCreate<byte> 4096
                do! fa.ReadArray 0xFF000UL buf
                for i = 0 to 4095 do
                    Assert.StrictEqual( buf.[i], rle.[i] )

                do! fa.ReadArray 0UL buf
                for i = 0 to 4095 do
                    Assert.StrictEqual( buf.[i], rle.[ i + 4096 ] )

            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Theory>]
    [<InlineData( 1 )>]
    [<InlineData( 255 )>]
    member _.WriteLogEntry_003 ( dlecnt : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 0x100000UL
                let rle = Array.zeroCreate<byte> 4096
                Random.Shared.NextBytes rle
                let dle =
                    List.init dlecnt ( fun _ ->
                        let v = Array.zeroCreate<byte> 4096
                        Random.Shared.NextBytes v
                        v
                    )
                do! VhdxCorrupter.WriteLogEntry fa defaultImmHeader 0u dle rle

                let buf = Array.zeroCreate<byte> ( 4096 * dlecnt + 4096 )
                do! fa.ReadArray 0UL buf
                for i = 0 to dlecnt - 1 do
                    for j = 0 to 4095 do
                        Assert.StrictEqual( buf.[ i * 4096 + j ], dle.[i].[j] )
                for i = 0 to 4095 do
                    Assert.StrictEqual( buf.[ 4096 * dlecnt + i ], rle.[i] )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.Inject_001 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa
            let testDataPos4K = ( str1.ImmHeader.LogOffset + ( uint64 str1.ImmHeader.LogLength ) ) / 4096UL
            do! fa.SetFileSize ( max fa.FileSize ( ( testDataPos4K + 50UL ) * 4096UL ) )

            let si = [
                for i in 0UL .. 49UL do
                    sec4k_me.ofUInt64 ( testDataPos4K + i )
            ]
            do! VhdxCorrupter.Inject fa si

            let! str2 = VhdxReader.ReadVhdx fa
            Assert.True( str1.LoadedVarHeader.SequenceNumber < str2.LoadedVarHeader.SequenceNumber )
            Assert.NotStrictEqual( str1.LoadedVarHeader.FileWriteGuid, str2.LoadedVarHeader.FileWriteGuid )
            Assert.StrictEqual( str1.LoadedVarHeader.DataWriteGuid, str2.LoadedVarHeader.DataWriteGuid )
            Assert.NotStrictEqual( str1.LoadedVarHeader.LogGuid, str2.LoadedVarHeader.LogGuid )
            Assert.StrictEqual( 5, str2.Log.Length )
            for itr in str2.Log do
                Assert.StrictEqual( 50, itr.DataSectors.Length )

            Assert.StrictEqual( str1.Region, str2.Region )
            Assert.StrictEqual( str1.BAT, str2.BAT )
            Assert.StrictEqual( str1.VDI, str2.VDI )
        })

    [<Fact>]
    member _.Inject_Fail_001 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! r =
                Assert.ThrowsAsync<ArgumentException>( fun () -> task {
                    do! VhdxCorrupter.Inject fa []
                } )
            Assert.StartsWith( "Injected sectores must be at least 1", r.Message )
        } )

    [<Fact>]
    member _.Inject_Fail_002 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let sec = [
                for i in 0UL .. 50UL do
                    sec4k_me.ofUInt64 i
            ]
            let! r =
                Assert.ThrowsAsync<ArgumentException>( fun () -> task {
                    do! VhdxCorrupter.Inject fa sec
                } )
            Assert.StartsWith( "The required log capacity exceeds the log area length", r.Message )
        } )

    [<Fact>]
    member _.Inject_Fail_003 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! VhdxCreator.Create None fa 0x100000u 0x100000u false 0x4000000UL Blocksize.BS_512
            finally
                fa.Close()

            let fa2 = FileAccessor( fname, 1u, true )
            try
                let! r =
                    Assert.ThrowsAsync<InvalidOperationException>( fun () -> task {
                        do! VhdxCorrupter.Inject fa2 [ sec4k_me.ofUInt64 512UL ]
                    } )
                Assert.StartsWith( "File opened read-only; Write not allowed", r.Message )
            finally
                fa2.Close()
            GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.Inject_Fail_004 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            fa.Close()
            let! r =
                Assert.ThrowsAsync<InvalidOperationException>( fun () -> task {
                    do! VhdxCorrupter.Inject fa [ sec4k_me.ofUInt64 512UL ]
                } )
            Assert.StartsWith( "No available stream. Exceeds multiplicity", r.Message )
        } )

    [<Theory>]
    [<InlineData( 0u, 4096u )>]
    [<InlineData( 1u, 4096u )>]
    [<InlineData( 126u, 4096u )>]
    [<InlineData( 127u, 8192u )>]
    [<InlineData( 254u, 8192u )>]
    [<InlineData( 255u, 12288u )>]
    [<InlineData( 382u, 12288u )>]
    [<InlineData( 383u, 16384u )>]
    [<InlineData( 134217598u, 4294963200u )>]
    member _.LogEntryHeaderSizeFrom4KSecCount_001 ( entries : uint32 ) ( expr : uint32 ) =
        Assert.StrictEqual( expr, VhdxCorrupter.LogEntryHeaderSizeFrom4KSecCount entries )

    [<Fact>]
    member _.LogEntryHeaderSizeFrom4KSecCount_Fail_001 () =
        let r =
            Assert.Throws<ArgumentException>( fun () ->
                VhdxCorrupter.LogEntryHeaderSizeFrom4KSecCount 134217599u |> ignore
            )
        Assert.StartsWith( "entries must be 134217598 or less", r.Message )

    [<Theory>]
    [<InlineData( 0u, 4096u )>]
    [<InlineData( 1u, 8192u )>]
    [<InlineData( 126u, 520192u )>]
    [<InlineData( 127u, 528384u )>]
    [<InlineData( 254u, 1048576u )>]
    [<InlineData( 255u, 1056768u )>]
    [<InlineData( 382u, 1576960u )>]
    [<InlineData( 383u, 1585152u )>]
    [<InlineData( 1040446u, 4294963200u )>]
    member _.LogEntrySizeFrom4KSecCount_001 ( entries : uint32 ) ( expr : uint32 ) =
        Assert.StrictEqual( expr, VhdxCorrupter.LogEntrySizeFrom4KSecCount entries )

    [<Fact>]
    member _.LogEntrySizeFrom4KSecCount_Fail_001 () =
        let r =
            Assert.Throws<ArgumentException>( fun () ->
                VhdxCorrupter.LogEntrySizeFrom4KSecCount 1040447u |> ignore
            )
        Assert.StartsWith( "entries must be 1040446 or less", r.Message )

    [<Theory>]
    [<InlineData( 0x00100000u, 254u )>]
    [<InlineData( 0x00200000u, 508u )>]
    [<InlineData( 0x00300000u, 762u )>]
    [<InlineData( 0x04000000u, 16256u )>]
    [<InlineData( 0x04100000u, 16510u )>]
    [<InlineData( 0x04200000u, 16765u )>]
    [<InlineData( 0xFFE00000u, 1039939u )>]
    [<InlineData( 0xFFF00000u, 1040193u )>]
    [<InlineData( 0xFFFFFFFFu, 1040446u )>]
    member _.Max4KSectorCountFromLogCapacity_001 ( capacity : uint32 ) ( expr : uint32 ) =
        Assert.StrictEqual( expr, VhdxCorrupter.Max4KSectorCountFromLogCapacity capacity )
