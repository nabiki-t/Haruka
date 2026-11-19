//=============================================================================
// Haruka Software Storage.
// VhdxMergeTest.fs : Test cases for VhdxMerge class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.VHDXMedia

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test

//=============================================================================
// Type definitions

//=============================================================================
// Class implementation

type VhdxMergeTest_Test () =

    [<Theory>]
    [<InlineData( 0x100000UL, 0x200000UL, 0UL,                  0x801u, "The source region is outside the bounds of the media" )>]
    [<InlineData( 0x100000UL, 0x200000UL, 0x800UL,              1u,     "The source region is outside the bounds of the media" )>]
    [<InlineData( 0x100000UL, 0x200000UL, 0xFFFFFFFFFFFFFFFFUL, 2u,     "The source region is outside the bounds of the media" )>]
    [<InlineData( 0x200000UL, 0x100000UL, 0UL,                  0x801u, "The destination region is outside the bounds of the media" )>]
    [<InlineData( 0x200000UL, 0x100000UL, 0x800UL,              1u,     "The destination region is outside the bounds of the media" )>]
    member _.CopyData_Fail_001 ( srcsize : uint64 ) ( dstsize : uint64 ) ( lba : uint64 ) ( cnt : uint32 ) ( expmsg : string ) =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let! srcStr = VhdxReader.ReadVhdx srcFA
            let! dstStr = VhdxReader.ReadVhdx dstFA
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun _ -> task {
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr ( blkcnt_me.ofUInt64 lba ) ( blkcnt_me.ofUInt32 cnt )
                } )
            Assert.StartsWith( expmsg, r.Message )
        }
        let bs512 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u srcsize bs512 0x100000u dstsize bs512 testcase

    [<Fact>]
    member _.CopyData_Fail_002 () =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let! srcStr = VhdxReader.ReadVhdx srcFA
            let! dstStr = VhdxReader.ReadVhdx dstFA
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun _ -> task {
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr ( blkcnt_me.zero64 ) ( blkcnt_me.ofUInt32 1u )
                } )
            Assert.StartsWith( "The block lengths of the source and destination differ", r.Message )
        }
        let bs5 = Blocksize.BS_512
        let bs4 = Blocksize.BS_4096
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x100000UL bs5 0x100000u 0x100000UL bs4 testcase
