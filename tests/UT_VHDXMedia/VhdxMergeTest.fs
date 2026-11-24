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
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr blkcnt_me.zero64 blkcnt_me.one32
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
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr blkcnt_me.zero64 blkcnt_me.one32
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
                    do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr blkcnt_me.zero64 blkcnt_me.one32
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
                    VhdxWriter.AllocatePayloadBlock dstStr blkcnt_me.zero64 blkcnt_me.one64
                let! _ = VhdxWriter.WriteUpdatedBAT dstFA dstStr dstStr.LoadedVarHeader ( Seq.toArray updated4KSecsForBAT ) requiredFileSize

                // update the destination payload block status
                dstStr.BAT.Payloads.[0] <- {
                    dstStr.BAT.Payloads.[0] with
                        State = dstate;
                }

                do! VhdxMerge.CopyData srcFA srcStr dstFA dstStr blkcnt_me.zero64 blkcnt_me.one32

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

    [<Fact>]
    member _.UpdateMetadata_Fail_001 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str = VhdxReader.ReadVhdx fa
            let str2 = { str with Region.Entries = []; }
            let! _ =
                Assert.ThrowsAnyAsync<Exception>( fun _ -> task {
                    let! _ = VhdxMerge.UpdateMetadata fa str2 str2.LoadedVarHeader
                    ()
                } )
            ()
        } )

    [<Fact>]
    member _.UpdateMetadata_Fail_002 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str = VhdxReader.ReadVhdx fa
            let b =
                str.Region.Entries |> List.find ( _.Guid >> (=) VhdxCommons.REGENT_TYPE_BAT )
            let m =
                let m2 =
                    str.Region.Entries |> List.find ( _.Guid >> (=) VhdxCommons.REGENT_TYPE_METADATA )
                { m2 with Length = 0u }
            let str2 = { str with Region.Entries = [ b; m; ]; }

            let! r =
                Assert.ThrowsAnyAsync<VhdxMediaException>( fun _ -> task {
                    let! _ = VhdxMerge.UpdateMetadata fa str2 str2.LoadedVarHeader
                    ()
                } )
            Assert.StartsWith( "Unexpected metadata region length", r.Message )
        } )

    [<Fact>]
    member _.UpdateMetadata_001 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str = VhdxReader.ReadVhdx fa
            let str2 = {
                str with
                    VDI.HasParent = true;
                    VDI.VirtualDiskId = Guid.NewGuid()
                    VDI.ParentLocator = 
                        [|
                            ( "parent_linkage", "{83ed0ec3-24c8-49a6-a959-5e4bf1288bfb}" );
                            ( "relative_path", "../../../../../../../../../../../../../../aaa/bbb.vhdx" );
                        |]
                        |> Map;
            }
            let! r = VhdxMerge.UpdateMetadata fa str2 str2.LoadedVarHeader
            Assert.StrictEqual( Guid(), r.LogGuid )
            Assert.NotStrictEqual( str2.LoadedVarHeader.SequenceNumber, r.SequenceNumber )

            let! str3 = VhdxReader.ReadVhdx fa
            Assert.True str3.VDI.HasParent
            Assert.StrictEqual( str2.VDI.VirtualDiskId, str3.VDI.VirtualDiskId )
            Assert.StrictEqual( str2.VDI.ParentLocator, str3.VDI.ParentLocator )
        } )

    [<Fact>]
    member _.UpdateMetadata_002 () =
        VhdxTestFunc.CreateDynamicVHDXFileAndTest ( fun fa -> task {
            let! str = VhdxReader.ReadVhdx fa
            let! r = VhdxMerge.UpdateMetadata fa str str.LoadedVarHeader
            Assert.StrictEqual( str.LoadedVarHeader, r )
        } )

    static member m_UpdateBATForDeleteRoot_Fail_001_data : obj[][] = [|
        [| 0; Blocksize.BS_512; Blocksize.BS_512; 0x100000UL; 0x100000UL; "updateLbaBuf must not be empty" |];
        [| 1; Blocksize.BS_512; Blocksize.BS_4096; 0x100000UL; 0x100000UL; "Logical Sector Size unmatch" |];
        [| 1; Blocksize.BS_512; Blocksize.BS_512; 0x100000UL; 0x200000UL; "Virtual Disk Size unmatch" |];
    |]

    [<Theory>]
    [<MemberData( "m_UpdateBATForDeleteRoot_Fail_001_data" )>]
    member _.UpdateBATForDeleteRoot_Fail_001 ( bufsize : int ) ( bs1 : Blocksize ) ( bs2 : Blocksize ) ( vds1 : uint64 ) ( vds2 : uint64 ) ( expmsg : string ) =
        let testcase ( dfa : FileAccessor ) ( mfa : FileAccessor ) : Task = task {
            let! dstr = VhdxReader.ReadVhdx dfa
            let! mstr = VhdxReader.ReadVhdx mfa
            let r =
                Assert.ThrowsAny<VhdxMediaException>( fun _ ->
                    let v = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > bufsize
                    let _ = VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 v
                    ()
                )
            Assert.StartsWith( expmsg, r.Message )
        }
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u vds1 bs1 0x100000u vds2 bs2 testcase

    static member m_UpdateBATForDeleteRoot_Fail_002_data : obj[][] = [|
        [| 0u; 0x100000u; |];
        [| 0x100000u; 0u; |];
    |]

    [<Theory>]
    [<MemberData( "m_UpdateBATForDeleteRoot_Fail_002_data" )>]
    member _.UpdateBATForDeleteRoot_Fail_002 ( pb1 : uint32 ) ( pb2 : uint32 ) =
        let testcase ( dfa : FileAccessor ) ( mfa : FileAccessor ) : Task = task {
            let! dstr = VhdxReader.ReadVhdx dfa
            let! mstr = VhdxReader.ReadVhdx mfa
            let dstr2 = { dstr with VDI.PayloadBlockSize = pb1; }
            let mstr2 = { mstr with VDI.PayloadBlockSize = pb2; }
            let r =
                Assert.ThrowsAny<VhdxMediaException>( fun _ ->
                    let v = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 1
                    let _ = VhdxMerge.UpdateBATForDeleteRoot dstr2 mstr2 blkcnt_me.zero64 v
                    ()
                )
            Assert.StartsWith( "Unexpected payload block size", r.Message )
        }
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x100000UL Blocksize.BS_512 0x100000u 0x100000UL Blocksize.BS_512 testcase

    [<Theory>]
    [<InlineData( true, true, "A root VHDX file must not have a parent" )>]
    [<InlineData( false, false, "A child VHDX file must have a parent" )>]
    member _.UpdateBATForDeleteRoot_Fail_003 ( hp1 : bool ) ( hp2 : bool ) ( expmsg : string ) =
        let testcase ( dfa : FileAccessor ) ( mfa : FileAccessor ) : Task = task {
            let! dstr = VhdxReader.ReadVhdx dfa
            let! mstr = VhdxReader.ReadVhdx mfa
            let dstr2 = { dstr with VDI.HasParent = hp1 }
            let mstr2 = { mstr with VDI.HasParent = hp2 }
            let r =
                Assert.ThrowsAny<VhdxMediaException>( fun _ ->
                    let v = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 1
                    let _ = VhdxMerge.UpdateBATForDeleteRoot dstr2 mstr2 blkcnt_me.zero64 v
                    ()
                )
            Assert.StartsWith( expmsg, r.Message )
        }
        VhdxTestFunc.CreateTwoDynamicVHDXFileAndTest 0x100000u 0x100000UL Blocksize.BS_512 0x100000u 0x100000UL Blocksize.BS_512 testcase

    static member m_UpdateBATForDeleteRoot_Fail_003_data : obj[][] = [|
        [| 0x100000UL; Blocksize.BS_512; 2048UL; |];
        [| 0x100000UL; Blocksize.BS_4096; 256UL; |];
    |]

    [<Theory>]
    [<MemberData( "m_UpdateBATForDeleteRoot_Fail_003_data" )>]
    member _.UpdateBATForDeleteRoot_Fail_004 ( vdsize : uint64 ) ( bs : Blocksize ) ( lba : uint64 ) =
        let testcase ( dfa : FileAccessor ) ( mfa : FileAccessor ) : Task = task {
            let! dstr = VhdxReader.ReadVhdx dfa
            let! mstr = VhdxReader.ReadVhdx mfa
            let r =
                Assert.ThrowsAny<VhdxMediaException>( fun _ ->
                    let v = Array.zeroCreate< struct( BLKCNT64_T * BLKCNT32_T ) > 1
                    let _ = VhdxMerge.UpdateBATForDeleteRoot dstr mstr ( blkcnt_me.ofUInt64 lba ) v
                    ()
                )
            Assert.StartsWith( "The value of startLba is out of range", r.Message )
        }
        VhdxTestFunc.CreateDifferencingVHDXFileAndTest vdsize bs 0x100000u 0x100000u testcase

    static member m_UpdateBATForDeleteRoot_001_data : obj[][] = [|
        [| BatEntryStatePB.PayloadNotPresent; |];
        [| BatEntryStatePB.PayloadUndefined; |];
        [| BatEntryStatePB.PayloadUnapped; |];
    |]

    // If the parent payload is unassigned, the process terminates without taking any action.
    [<Theory>]
    [<MemberData( "m_UpdateBATForDeleteRoot_001_data" )>]
    member _.UpdateBATForDeleteRoot_001 ( dstate : BatEntryStatePB ) =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = dstate }

        let updateLbaBuf = [| struct( blkcnt_me.ofUInt64 123UL, blkcnt_me.ofUInt32 456u ) |]
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )
        let struct( start, count ) = updateLbaBuf.[0]
        Assert.StrictEqual( blkcnt_me.ofUInt64 UInt64.MaxValue, start )
        Assert.StrictEqual( blkcnt_me.ofUInt32 UInt32.MaxValue, count )

    static member m_UpdateBATForDeleteRoot_002_data : obj[][] = [|
        [| BatEntryStatePB.PayloadZero;             true; |];
        [| BatEntryStatePB.PayloadPartiallyPresent; true; |];
        [| BatEntryStatePB.PayloadFullyPresent;     true; |];
        [| BatEntryStatePB.PayloadZero;             false; |];
        [| BatEntryStatePB.PayloadPartiallyPresent; false; |];
        [| BatEntryStatePB.PayloadFullyPresent;     false; |];
    |]

    // The parent payload is already assigned.
    // If a child sector is unused, include it in the copy range; if it is in use, do not include it.
    [<Theory>]
    [<MemberData( "m_UpdateBATForDeleteRoot_002_data" )>]
    member _.UpdateBATForDeleteRoot_002 ( dstate : BatEntryStatePB ) ( copySector : bool ) =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = dstate }
        if copySector then
            Array.fill mstr.BAT.SectorBitmap.[0].Bitmap 0 mstr.BAT.SectorBitmap.[0].Bitmap.Length 0uy
        else
            Array.fill mstr.BAT.SectorBitmap.[0].Bitmap 0 mstr.BAT.SectorBitmap.[0].Bitmap.Length 0xFFuy

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )
        if copySector then
            let struct( start, count ) = updateLbaBuf.[0]
            Assert.StrictEqual( blkcnt_me.zero64, start )
            Assert.StrictEqual( blkcnt_me.ofUInt32 2048u, count )
            Assert.All( mstr.BAT.SectorBitmap.[0].Bitmap.[ 0 .. 255 ], fun value -> Assert.Equal( 0xFFuy, value ) )
            Assert.All( mstr.BAT.SectorBitmap.[0].Bitmap.[ 256 .. ], fun value -> Assert.Equal( 0uy, value ) )
            Assert.StrictEqual( 1uy, mstr.BAT.SectorBitmap.[0].Bitmap.[0] &&& 1uy )
        else
            let struct( start, _ ) = updateLbaBuf.[0]
            Assert.StrictEqual( blkcnt_me.ofUInt64 UInt64.MaxValue, start )
            Assert.All( mstr.BAT.SectorBitmap.[0].Bitmap, fun value -> Assert.Equal( 0xFFuy, value ) )

    // Terminate processing once the copy range buffer is exhausted, and return the next unprocessed LBA.
    [<Fact>]
    member _.UpdateBATForDeleteRoot_003 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        mstr.BAT.SectorBitmap.[0].Bitmap.[0] <- 0x02uy

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 2UL, nextLba )
        let struct( start, count ) = updateLbaBuf.[0]
        Assert.StrictEqual( blkcnt_me.zero64, start )
        Assert.StrictEqual( blkcnt_me.one32, count )

    // When the copy range is fragmented by used sectors
    [<Fact>]
    member _.UpdateBATForDeleteRoot_004 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadZero }
        mstr.BAT.SectorBitmap.[0].Bitmap.[0] <- 0x02uy

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 2
        let _, _, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        let struct( start1, count1 ) = updateLbaBuf.[0]
        let struct( start2, count2 ) = updateLbaBuf.[1]
        Assert.StrictEqual( blkcnt_me.zero64, start1 )
        Assert.StrictEqual( blkcnt_me.one32, count1 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 2UL, start2 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2046u, count2 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )

    // Case where the child payload is unassigned
    [<Fact>]
    member _.UpdateBATForDeleteRoot_005 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        mstr.BAT.Payloads.[0] <- { mstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadNotPresent }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.StrictEqual( BatEntryStatePB.PayloadPartiallyPresent, mstr.BAT.Payloads.[0].State )
        Assert.True( updated.Count > 0 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )

    // Cases where a sector bitmap is not allocated
    [<Fact>]
    member _.UpdateBATForDeleteRoot_006 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        mstr.BAT.SectorBitmap.[0] <- { mstr.BAT.SectorBitmap.[0] with Bitmap = Array.empty }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let r =
            Assert.Throws<VhdxMediaException>( fun _ ->
                VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf |> ignore
            )
        Assert.StartsWith( "Sector bitmap is not allocated", r.Message )

    // Cases that begin at the boundary of a parent payload
    [<Fact>]
    member _.UpdateBATForDeleteRoot_007 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x200000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x200000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[1] <- { dstr.BAT.Payloads.[1] with State = BatEntryStatePB.PayloadPartiallyPresent }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr ( blkcnt_me.ofUInt64 2048UL ) updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 4096UL, nextLba )
        let struct( start, count ) = updateLbaBuf.[0]
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, start )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2048u, count )

    static member m_UpdateBATForDeleteRoot_008_data : obj[][] = [|
        [| 0x180000u; 0x280000u; 3072UL; 2048UL; |];
        [| 0x280000u; 0x180000u; 3072UL; 2048UL; |];
    |]

    // Cases where parent and child payload sizes differ.
    [<Theory>]
    [<MemberData( "m_UpdateBATForDeleteRoot_008_data" )>]
    member _.UpdateBATForDeleteRoot_008 ( parentPbSize : uint32 ) ( childPbSize : uint32 ) ( firstRangeCount : uint64 ) ( secondRangeCount : uint64 ) =
        let dstr = VhdxTestFunc.GenStructures 0x100000u parentPbSize 0xC00000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u childPbSize 0xC00000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        dstr.BAT.Payloads.[1] <- { dstr.BAT.Payloads.[1] with State = BatEntryStatePB.PayloadFullyPresent }
        Array.fill mstr.BAT.SectorBitmap.[0].Bitmap 0 mstr.BAT.SectorBitmap.[0].Bitmap.Length 0uy

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 2
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated

        let struct( start1, count1 ) = updateLbaBuf.[0]
        let struct( start2, count2 ) = updateLbaBuf.[1]
        Assert.StrictEqual( blkcnt_me.zero64, start1 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 ( uint32 firstRangeCount ), count1 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 firstRangeCount, start2 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 ( uint32 secondRangeCount ), count2 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 5120UL, nextLba )

    // Cases where the copy range is fragmented due to unallocated payloads in the parent.
    [<Fact>]
    member _.UpdateBATForDeleteRoot_009 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x300000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x300000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        dstr.BAT.Payloads.[1] <- { dstr.BAT.Payloads.[1] with State = BatEntryStatePB.PayloadNotPresent }
        dstr.BAT.Payloads.[2] <- { dstr.BAT.Payloads.[2] with State = BatEntryStatePB.PayloadFullyPresent }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 2
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 6144UL, nextLba )
        let struct( start1, count1 ) = updateLbaBuf.[0]
        let struct( start2, count2 ) = updateLbaBuf.[1]
        Assert.StrictEqual( blkcnt_me.zero64, start1 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2048u, count1 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 4096UL, start2 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2048u, count2 )

    // Cases where the end of a virtual disk does not align with a payload boundary
    [<Fact>]
    member _.UpdateBATForDeleteRoot_010 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x180000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x180000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        dstr.BAT.Payloads.[1] <- { dstr.BAT.Payloads.[1] with State = BatEntryStatePB.PayloadFullyPresent }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 2
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 3072UL, nextLba )
        let struct( start1, count1 ) = updateLbaBuf.[0]
        let struct( start2, count2 ) = updateLbaBuf.[1]
        Assert.StrictEqual( blkcnt_me.zero64, start1 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2048u, count1 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, start2 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 1024u, count2 )

    // Cases in which a child file's size is expanded
    [<Fact>]
    member _.UpdateBATForDeleteRoot_011 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        mstr.BAT.Payloads.[0] <- { mstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadNotPresent }
        mstr.BAT.SectorBitmap.[0] <- { mstr.BAT.SectorBitmap.[0] with FileOffset = 0x400000UL }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( 0x600000UL, required )
        Assert.StrictEqual( BatEntryStatePB.PayloadPartiallyPresent, mstr.BAT.Payloads.[0].State )
        Assert.StrictEqual( 0x500000UL, mstr.BAT.Payloads.[0].FileOffset )
        Assert.True( updated.Contains( sec4k_me.ofUInt64 768UL ) )
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )
        let struct( start, count ) = updateLbaBuf.[0]
        Assert.StrictEqual( blkcnt_me.zero64, start )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2048u, count )

    // Ensure that the copy range is split at both the bit boundaries and the byte boundaries of the sector bitmap.
    [<Fact>]
    member _.UpdateBATForDeleteRoot_012 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }
        mstr.BAT.SectorBitmap.[0].Bitmap.[0] <- 0x01uy
        mstr.BAT.SectorBitmap.[0].Bitmap.[1] <- 0x01uy

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 2
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr blkcnt_me.zero64 updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )
        let struct( start1, count1 ) = updateLbaBuf.[0]
        let struct( start2, count2 ) = updateLbaBuf.[1]
        Assert.StrictEqual( blkcnt_me.ofUInt64 1UL, start1 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 7u, count1 )
        Assert.StrictEqual( blkcnt_me.ofUInt64 9UL, start2 )
        Assert.StrictEqual( blkcnt_me.ofUInt32 2039u, count2 )

    // Verify that, when starting from the final valid LBA,
    // only one sector is processed and the position immediately following the end of the disk is returned.
    [<Fact>]
    member _.UpdateBATForDeleteRoot_013 () =
        let dstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 false
        let mstr = VhdxTestFunc.GenStructures 0x100000u 0x100000u 0x100000UL Blocksize.BS_512 true
        dstr.BAT.Payloads.[0] <- { dstr.BAT.Payloads.[0] with State = BatEntryStatePB.PayloadFullyPresent }

        let updateLbaBuf = Array.zeroCreate<struct( BLKCNT64_T * BLKCNT32_T )> 1
        let required, updated, nextLba =
            VhdxMerge.UpdateBATForDeleteRoot dstr mstr ( blkcnt_me.ofUInt64 2047UL ) updateLbaBuf

        Assert.StrictEqual( mstr.LastFileSize, required )
        Assert.Empty updated
        Assert.StrictEqual( blkcnt_me.ofUInt64 2048UL, nextLba )
        let struct( start, count ) = updateLbaBuf.[0]
        Assert.StrictEqual( blkcnt_me.ofUInt64 2047UL, start )
        Assert.StrictEqual( blkcnt_me.one32, count )
        Assert.StrictEqual( 0x80uy, mstr.BAT.SectorBitmap.[0].Bitmap.[255] &&& 0x80uy )


