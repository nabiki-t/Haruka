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
                do! fa.ReadArray ( uint64 offset ) buf
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

    [<Fact>]
    member _.WriteLogEntry_003 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 0x100000UL
                let rle = Array.zeroCreate<byte> 8192
                Random.Shared.NextBytes rle
                do! VhdxCorrupter.WriteLogEntry fa defaultImmHeader 0x100000u List.Empty rle

                let buf = Array.zeroCreate<byte> 8192
                do! fa.ReadArray 0UL buf
                Array.iter2 ( fun i j -> Assert.StrictEqual( i, j ) ) rle buf
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.WriteLogEntry_004 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 0x100000UL
                let rle = Array.zeroCreate<byte> 4096
                Random.Shared.NextBytes rle
                let dle = Array.zeroCreate<byte> 4096
                Random.Shared.NextBytes dle
                do! VhdxCorrupter.WriteLogEntry fa defaultImmHeader 0u [ dle ] rle

                let buf = Array.zeroCreate<byte> 8192
                do! fa.ReadArray 0UL buf
                for i = 0 to 4095 do
                    Assert.StrictEqual( buf.[i], dle.[i] )
                for i = 0 to 4095 do
                    Assert.StrictEqual( buf.[ i + 4096 ], rle.[i] )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }
