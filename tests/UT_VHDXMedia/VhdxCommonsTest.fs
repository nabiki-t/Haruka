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

    let zeroVerHeader : VhdxMutableHeader = {
        SequenceNumber = 0UL;
        FileWriteGuid = Guid();
        DataWriteGuid = Guid();
        LogGuid = Guid();
    }

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
                        let! _ = VhdxCommons.UpdateHeader fa zeroHeader zeroVerHeader
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
                        let! _ = VhdxCommons.UpdateHeader fa zeroHeader zeroVerHeader
                        ()
                    } )
                Assert.StartsWith( "File opened read-only", r.Message )
            finally
                File.Delete fname
        }
