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

    static member genHeaderBytes ( imhd : VhdxHeader ) ( mhd : VhdxMutableHeader ) : byte[] =
        let v = Array.zeroCreate<byte> 4096
        Array.blit ( "head" |> Encoding.UTF8.GetBytes ) 0 v 0 4
        ByteFunc.WriteU64LE v 8u mhd.SequenceNumber
        ByteFunc.WriteGuid v 16u mhd.FileWriteGuid
        ByteFunc.WriteGuid v 32u mhd.DataWriteGuid
        ByteFunc.WriteGuid v 48u mhd.LogGuid
        ByteFunc.WriteU16LE v 64u imhd.LogVersion
        ByteFunc.WriteU16LE v 66u imhd.Version
        ByteFunc.WriteU32LE v 68u imhd.LogLength
        ByteFunc.WriteU64LE v 72u imhd.LogOffset
        Crc32C.Compute v
        |> ByteFunc.WriteU32LE v 4u
        v

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
        VhdxReader_Test3.createDynamicVHDXFileAndTest ( fun fa -> task {
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
        VhdxReader_Test3.createDynamicVHDXFileAndTest ( fun fa -> task {
            let! str1 = VhdxReader.ReadVhdx fa
            Assert.Empty( str1.Log )
            let! verHeade = VhdxChecker.Check fa
            let! str2 = VhdxReader.ReadVhdx fa
            Assert.Empty( str2.Log )
        } )

    [<Fact>]
    member _.Replay_002() =
        VhdxReader_Test3.createDynamicVHDXFileAndTest ( fun fa -> task {
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
            let logData = VhdxReader_Test1.genLogData logLength 0 logEntry logGuid 1UL lastFileSize lastFileOffset
            let hdBytes = VhdxChecker_Test.genHeaderBytes str1.ImmHeader { str1.LoadedVarHeader with LogGuid = logGuid }

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
