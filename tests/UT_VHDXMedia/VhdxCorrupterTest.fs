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

    [<Fact>]
    member _.CreateLogEntry_DataSector_002 () =
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
