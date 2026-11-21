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
    [<InlineData( 0x100000UL, 0x200000UL, 0x7FFUL,              2u,     "The source region is outside the bounds of the media" )>]
    [<InlineData( 0x100000UL, 0x200000UL, 0xFFFFFFFFFFFFFFFFUL, 2u,     "The source region is outside the bounds of the media" )>]
    [<InlineData( 0x200000UL, 0x100000UL, 0UL,                  0x801u, "The destination region is outside the bounds of the media" )>]
    [<InlineData( 0x200000UL, 0x100000UL, 0x7FFUL,              2u,     "The destination region is outside the bounds of the media" )>]
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

    [<Theory>]
    [<InlineData( 0x100000u, 0x200000u, 0UL,     0x801u, "The source region is not contained within a single payload" )>]
    [<InlineData( 0x100000u, 0x200000u, 0x7FFUL, 0x2u,   "The source region is not contained within a single payload" )>]
    [<InlineData( 0x200000u, 0x100000u, 0UL,     0x801u, "The destination region is not contained within a single payload" )>]
    [<InlineData( 0x200000u, 0x100000u, 0x7FFUL, 0x2u,   "The destination region is not contained within a single payload" )>]
    member _.CopyData_Fail_003 ( spbsize : uint32 ) ( dpbsize : uint32 ) ( lba : uint64 ) ( cnt : uint32 ) ( expmsg : string ) =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let! srcStr = VhdxReader.ReadVhdx srcFA
            let! dstStr = VhdxReader.ReadVhdx dstFA

            // Allocate all of payload blocks within the source VHDX file
            let struct( updated4KSecsForBAT, requiredFileSize ) =
                VhdxWriter.AllocatePayloadBlock srcStr blkcnt_me.zero64 ( blkcnt_me.ofUInt64 4096UL )
            let! _ = VhdxWriter.WriteUpdatedBAT srcFA srcStr srcStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun _ -> task {
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr ( blkcnt_me.ofUInt64 lba ) ( blkcnt_me.ofUInt32 cnt )
                } )
            Assert.StartsWith( expmsg, r.Message )
        }
        let bs5 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest spbsize 0x200000UL bs5 dpbsize 0x200000UL bs5 testcase

    static member m_CopyData_Fail_004_data : obj[][] = [|
        [| BatEntryStatePB.PayloadNotPresent |];
        [| BatEntryStatePB.PayloadUndefined |];
        [| BatEntryStatePB.PayloadUnapped |];
    |]

    [<Theory>]
    [<MemberData( "m_CopyData_Fail_004_data" )>]
    member _.CopyData_Fail_004 ( state : BatEntryStatePB ) =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let! srcStr = VhdxReader.ReadVhdx srcFA
            let! dstStr = VhdxReader.ReadVhdx dstFA

            // Allocate all of payload blocks within the source VHDX file
            let struct( updated4KSecsForBAT, requiredFileSize ) =
                VhdxWriter.AllocatePayloadBlock srcStr blkcnt_me.zero64 ( blkcnt_me.ofUInt64 4096UL )
            let! _ = VhdxWriter.WriteUpdatedBAT srcFA srcStr srcStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

            // Update first payload block status within the source VHDX file.
            srcStr.BAT.Payloads.[0] <- {
                srcStr.BAT.Payloads.[0] with
                    State = state;
            }

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun _ -> task {
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr ( blkcnt_me.zero64 ) ( blkcnt_me.ofUInt32 1u )
                } )
            Assert.StartsWith( "The source payload block is not allocated", r.Message )
        }
        let bs5 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x200000UL bs5 0x100000u 0x200000UL bs5 testcase

    static member m_CopyData_Fail_005_data : obj[][] = [|
        [| BatEntryStatePB.PayloadNotPresent |];
        [| BatEntryStatePB.PayloadUndefined |];
        [| BatEntryStatePB.PayloadUnapped |];
        [| BatEntryStatePB.PayloadZero |];
    |]

    [<Theory>]
    [<MemberData( "m_CopyData_Fail_005_data" )>]
    member _.CopyData_Fail_005 ( state : BatEntryStatePB ) =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let! srcStr = VhdxReader.ReadVhdx srcFA
            let! dstStr = VhdxReader.ReadVhdx dstFA

            // Allocate all of payload blocks within the source VHDX file
            let struct( updated4KSecsForBAT, requiredFileSize ) =
                VhdxWriter.AllocatePayloadBlock srcStr blkcnt_me.zero64 ( blkcnt_me.ofUInt64 4096UL )
            let! _ = VhdxWriter.WriteUpdatedBAT srcFA srcStr srcStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

            // Allocate all of payload blocks within the destination VHDX file
            let struct( updated4KSecsForBAT, requiredFileSize ) =
                VhdxWriter.AllocatePayloadBlock dstStr blkcnt_me.zero64 ( blkcnt_me.ofUInt64 4096UL )
            let! _ = VhdxWriter.WriteUpdatedBAT dstFA dstStr dstStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

            // Update first payload block status within the destination VHDX file.
            dstStr.BAT.Payloads.[0] <- {
                dstStr.BAT.Payloads.[0] with
                    State = state;
            }

            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun _ -> task {
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr ( blkcnt_me.zero64 ) ( blkcnt_me.ofUInt32 1u )
                } )
            Assert.StartsWith( "The destination payload block is not allocated", r.Message )
        }
        let bs5 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x200000UL bs5 0x100000u 0x200000UL bs5 testcase

    [<Theory>]
    [<InlineData( 0x100000UL, 0x100000u, 0UL,     1u )>]
    [<InlineData( 0x100000UL, 0x100000u, 0x7FFUL, 1u )>]
    [<InlineData( 0x100000UL, 0x100000u, 0UL,     0x800u )>]
    [<InlineData( 0x200000UL, 0x100000u, 0x800UL, 1u )>]
    [<InlineData( 0x200000UL, 0x200000u, 0x400UL, 0x800u )>]
    [<InlineData( 0x200000UL, 0x200000u, 0x400UL, 0xA00u )>]
    [<InlineData( 0x200000UL, 0x200000u, 0UL,     0x1000u )>]
    member _.CopyData_001 ( vdsize : uint64 ) ( pbsize : uint32 ) ( lba : uint64 ) ( cnt : uint32 ) =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let bllba = blkcnt_me.ofUInt64 lba
            let rndfname = Path.GetTempFileName()
            try
                // Write random data to the source VHDX file
                GlbFunc.CreateRandomFile rndfname ( int64 cnt * 512L )
                do! VhdxWriter.Write srcFA rndfname bllba

                let! srcStr = VhdxReader.ReadVhdx srcFA
                let! dstStr = VhdxReader.ReadVhdx dstFA

                // Allocate payload blocks within the destination VHDX file
                let struct( updated4KSecsForBAT, requiredFileSize ) =
                    VhdxWriter.AllocatePayloadBlock dstStr bllba ( blkcnt_me.ofUInt64 ( uint64 cnt ) )
                let! _ = VhdxWriter.WriteUpdatedBAT dstFA dstStr dstStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

                do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr bllba ( blkcnt_me.ofUInt32 cnt )

                // Read copied data
                let struct( idx, off ) = VhdxCommons.LBAtoPayloadBlockIndex bllba dstStr
                let resultdata = Array.zeroCreate<byte> ( int32 cnt * 512 )
                let readoffset = dstStr.BAT.Payloads.[ int idx ].FileOffset + ( uint64 off * 512UL )
                do! dstFA.ReadArray readoffset resultdata

                // compare
                let expdata = File.ReadAllBytes rndfname
                Assert.True(( expdata = resultdata ))

            finally
                GlbFunc.DeleteFile rndfname
        }
        let bs5 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest pbsize vdsize bs5 pbsize vdsize bs5 testcase

    static member m_CopyData_002_data : obj[][] = [|
        [| BatEntryStatePB.PayloadPartiallyPresent; BatEntryStatePB.PayloadFullyPresent; |];
        [| BatEntryStatePB.PayloadFullyPresent;     BatEntryStatePB.PayloadPartiallyPresent; |];
    |]

    [<Theory>]
    [<MemberData( "m_CopyData_002_data" )>]
    member _.CopyData_002 ( sstate : BatEntryStatePB ) ( dstate : BatEntryStatePB ) =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let rndfname = Path.GetTempFileName()
            try
                // Write random data to the source VHDX file
                GlbFunc.CreateRandomFile rndfname 512L
                do! VhdxWriter.Write srcFA rndfname blkcnt_me.zero64

                let! srcStr = VhdxReader.ReadVhdx srcFA
                let! dstStr = VhdxReader.ReadVhdx dstFA

                // update the source payload block status
                srcStr.BAT.Payloads.[0] <- {
                    srcStr.BAT.Payloads.[0] with
                        State = sstate;
                }

                // Allocate payload blocks within the destination VHDX file
                let struct( updated4KSecsForBAT, requiredFileSize ) =
                    VhdxWriter.AllocatePayloadBlock dstStr blkcnt_me.zero64 ( blkcnt_me.ofUInt64 1UL )
                let! _ = VhdxWriter.WriteUpdatedBAT dstFA dstStr dstStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

                // update the destination payload block status
                dstStr.BAT.Payloads.[0] <- {
                    dstStr.BAT.Payloads.[0] with
                        State = dstate;
                }

                do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr blkcnt_me.zero64 ( blkcnt_me.ofUInt32 1u )

                // Read copied data
                let resultdata = Array.zeroCreate<byte> 512
                do! dstFA.ReadArray dstStr.BAT.Payloads.[0].FileOffset resultdata

                // compare
                let expdata = File.ReadAllBytes rndfname
                Assert.True(( expdata = resultdata ))

            finally
                GlbFunc.DeleteFile rndfname
        }
        let bs5 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x100000UL bs5 0x100000u 0x100000UL bs5 testcase

    [<Fact>]
    member _.CopyData_003 () =
        let testcase ( srcFA : FileAccessor ) ( dstFA : FileAccessor ) : Task = task {
            let rndfname = Path.GetTempFileName()
            try
                // Write random data to the destination VHDX file
                GlbFunc.CreateRandomFile rndfname 0x100000L
                do! VhdxWriter.Write dstFA rndfname blkcnt_me.zero64

                let! srcStr = VhdxReader.ReadVhdx srcFA
                let! dstStr = VhdxReader.ReadVhdx dstFA

                // update the source payload block status
                srcStr.BAT.Payloads.[0] <- {
                    srcStr.BAT.Payloads.[0] with
                        State = BatEntryStatePB.PayloadZero;
                }

                do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr blkcnt_me.zero64 ( blkcnt_me.ofUInt32 0x400u )

                // Read copied data
                let resultdata = Array.zeroCreate<byte> 0x100000
                do! dstFA.ReadArray dstStr.BAT.Payloads.[0].FileOffset resultdata

                // compare
                let expdata = File.ReadAllBytes rndfname
                for i = 0 to 524287 do
                    Assert.StrictEqual( 0uy, resultdata.[i] )
                for i = 524288 to 1048575 do
                    Assert.StrictEqual( expdata.[i], resultdata.[i] )

            finally
                GlbFunc.DeleteFile rndfname
        }
        let bs5 = Blocksize.BS_512
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x100000UL bs5 0x100000u 0x100000UL bs5 testcase

