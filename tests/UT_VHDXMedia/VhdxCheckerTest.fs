//=============================================================================
// Haruka Software Storage.
// VhdxCheckerTest.fs : Test cases for VhdxChecker class.
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

type VhdxChecker_Test () =

    [<Fact>]
    member _.Check_Fail_001() =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let! _ = VhdxChecker.Check fa
                        ()
                    } )
                Assert.StartsWith( "The VHDX file is too small", r.Message )
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    [<Fact>]
    member _.Check_001() =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa

            let idx = [
                sec4k_me.ofUInt64 48UL;    // Region Table 1
                sec4k_me.ofUInt64 64UL;    // Region Table 2
                for itr in str1.Region.Entries do
                    sec4k_me.ofUInt64( itr.FileOffset / 4096UL )
            ]
            do! VhdxCorrupter.Inject fa idx
            let! str2 = VhdxReader.ReadVhdx fa

            Assert.NotStrictEqual( Guid(), str2.LoadedVarHeader.LogGuid )

            let! verHeade = VhdxChecker.Check fa
            Assert.StrictEqual( str2.LoadedVarHeader.DataWriteGuid, verHeade.DataWriteGuid )
            Assert.NotStrictEqual( str2.LoadedVarHeader.FileWriteGuid, verHeade.FileWriteGuid )
            Assert.StrictEqual( Guid(), verHeade.LogGuid )
            Assert.True( str2.LoadedVarHeader.SequenceNumber < verHeade.SequenceNumber )

            let! str3 = VhdxReader.ReadVhdx fa
            Assert.StrictEqual( verHeade.DataWriteGuid, str3.LoadedVarHeader.DataWriteGuid )
            Assert.StrictEqual( verHeade.FileWriteGuid, str3.LoadedVarHeader.FileWriteGuid )
            Assert.StrictEqual( verHeade.LogGuid, str3.LoadedVarHeader.LogGuid )
            Assert.StrictEqual( verHeade.SequenceNumber, str3.LoadedVarHeader.SequenceNumber + 1UL )
        } )

    [<Fact>]
    member _.Replay_001() =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa
            Assert.Empty( str1.Log )
            let! verHeade = VhdxChecker.Check fa
            let! str2 = VhdxReader.ReadVhdx fa
            Assert.Empty( str2.Log )
        } )

    [<Fact>]
    member _.Replay_002() =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa

            let logLength = ( int32 str1.ImmHeader.LogLength )
            let logEntry = [| {
                PatchPosition = 0;
                PatchData = [||];
                Descriptor = [||];
            } |]
            let logGuid = Guid.NewGuid()
            let lastFileSize = fa.FileSize
            let lastFileOffset = ( fa.FileSize + 0x100000UL )
            let logData = VhdxTestFunc.GenLogData logLength 0 logEntry logGuid 1UL lastFileSize lastFileOffset
            let hdBytes = VhdxTestFunc.GenHeaderBytes str1.ImmHeader { str1.LoadedVarHeader with LogGuid = logGuid }

            do! fa.Write str1.ImmHeader.LogOffset ( ArraySegment logData )
            do! fa.Write 65536UL ( hdBytes |> ArraySegment )
            do! fa.Write 131072UL ( [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |> ArraySegment )

            let! str2 = VhdxReader.ReadVhdx fa
            Assert.StrictEqual( 1, str2.Log.Length )
            Assert.StrictEqual( 0u, str2.Log.[0].DescriptorCount )

            let! verHeade = VhdxChecker.Check fa
            let! str3 = VhdxReader.ReadVhdx fa
            Assert.Empty( str3.Log )

            Assert.StrictEqual( lastFileOffset, fa.FileSize )
        } )

    [<Fact>]
    member _.Replay_003() =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa
            let logLength = ( int32 str1.ImmHeader.LogLength )
            let rndData = 
                let v = Array.zeroCreate<byte> 4096
                Random.Shared.NextBytes v
                v
            let rndDataPos = fa.FileSize
            let logEntry = [| {
                PatchPosition = 0;
                PatchData = [||];
                Descriptor = [|
                    TestLogDesc.Data( rndData, rndDataPos )
                |];
            } |]
            let logGuid = Guid.NewGuid()
            let lastFileSize = fa.FileSize
            let lastFileOffset = ( fa.FileSize + 0x100000UL )
            let logData = VhdxTestFunc.GenLogData logLength 0 logEntry logGuid 1UL lastFileSize lastFileOffset
            let hdBytes = VhdxTestFunc.GenHeaderBytes str1.ImmHeader { str1.LoadedVarHeader with LogGuid = logGuid }

            do! fa.Write str1.ImmHeader.LogOffset ( ArraySegment logData )
            do! fa.Write 65536UL ( hdBytes |> ArraySegment )
            do! fa.Write 131072UL ( [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |> ArraySegment )

            let! verHeade = VhdxChecker.Check fa
            let! str3 = VhdxReader.ReadVhdx fa
            Assert.Empty( str3.Log )

            let rndData2 = Array.zeroCreate<byte> 4096
            do! fa.Read rndDataPos ( ArraySegment rndData2 )
            Assert.True(( rndData = rndData2 ))
        } )

    [<Fact>]
    member _.Replay_004() =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa

            let rndData = 
                let v = Array.zeroCreate<byte> 8192
                Random.Shared.NextBytes v
                v
            let rndDataPos = fa.FileSize

            do! fa.SetFileSize ( fa.FileSize + 0x100000UL )
            do! fa.Write rndDataPos ( ArraySegment rndData )

            let logLength = ( int32 str1.ImmHeader.LogLength )
            let logEntry = [| {
                PatchPosition = 0;
                PatchData = [||];
                Descriptor = [|
                    TestLogDesc.Zero( 8192UL, rndDataPos )
                |];
            } |]
            let logGuid = Guid.NewGuid()
            let lastFileSize = fa.FileSize
            let lastFileOffset = fa.FileSize
            let logData = VhdxTestFunc.GenLogData logLength 0 logEntry logGuid 1UL lastFileSize lastFileOffset
            let hdBytes = VhdxTestFunc.GenHeaderBytes str1.ImmHeader { str1.LoadedVarHeader with LogGuid = logGuid }

            do! fa.Write str1.ImmHeader.LogOffset ( ArraySegment logData )
            do! fa.Write 65536UL ( hdBytes |> ArraySegment )
            do! fa.Write 131072UL ( [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |> ArraySegment )

            let! verHead = VhdxChecker.Check fa
            let! str3 = VhdxReader.ReadVhdx fa
            Assert.Empty( str3.Log )

            let rndData2 = Array.zeroCreate<byte> 8192
            do! fa.Read rndDataPos ( ArraySegment rndData2 )
            for i in rndData2 do
                Assert.StrictEqual( 0uy, i )
        } )