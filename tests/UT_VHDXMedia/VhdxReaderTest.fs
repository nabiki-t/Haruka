//=============================================================================
// Haruka Software Storage.
// VhdxReaderTest.fs : Test cases for VhdxReader class.
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
// Class implementation

type VhdxReaderTest_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.ReadFileTypeIdentifier_Fail_001() =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 519UL
            let! _ =
                Assert.ThrowsAnyAsync<Exception>( fun () -> task {
                    let! _ = VhdxReader.ReadFileTypeIdentifier fa
                    ()
                } )
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadFileTypeIdentifier_Fail_002() =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 520UL
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadFileTypeIdentifier fa
                    ()
                } )
            Assert.StartsWith( "File type identifier", r.Message )
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 255 )>]
    [<InlineData( 256 )>]
    member _.ReadFileTypeIdentifier_001 ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let creatorStr = String.replicate len "a"

            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 520UL

            do! fa.Write 0UL ( ArraySegment( Encoding.UTF8.GetBytes "vhdxfile" ) )
            do! fa.Write 8UL ( ArraySegment( Encoding.Unicode.GetBytes creatorStr ) )

            let! r = VhdxReader.ReadFileTypeIdentifier fa
            Assert.StrictEqual( creatorStr, r )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadHeaders_Fail_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 135167UL // 128K + 4096 - 1
            let! _ =
                Assert.ThrowsAnyAsync<Exception>( fun () -> task {
                    let! _ = VhdxReader.ReadFileTypeIdentifier fa
                    ()
                } )
            fa.Close()
            File.Delete fname
        }


