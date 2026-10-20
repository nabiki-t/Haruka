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
        }
