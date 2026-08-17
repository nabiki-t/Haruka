//=============================================================================
// Haruka Software Storage.
// PseudoSeqTest.fs : Test cases for PseudoSeq class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.Collections.Generic

open Xunit

open Haruka.Commons
open Haruka.Constants

//=============================================================================
// Class implementation

type PseudoSeq_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.PseudoSeqStat_Constractor_001() =
        let ps = PseudoSeqStat<int32, uint32>( ValueSome 1 )
        Assert.StrictEqual( ValueSome 1, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqStat_Constractor_002() =
        let ps = PseudoSeqStat<int32, uint32>( ValueNone )
        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqStat_Constractor_003() =
        let ps = PseudoSeqStat<int32, uint32>()
        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqStat_Constractor_004() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqStat_GetEnumerator_001() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.PseudoSeqStat_GetEnumerator_002() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current )

    [<Fact>]
    member _.PseudoSeqStat_GetEnumerator_003() =
        let ps = PseudoSeqStat<int32, uint32>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.PseudoSeqStat_GetEnumerator_004() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : System.Collections.IEnumerator = ( ps :> System.Collections.IEnumerable ).GetEnumerator()
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current :?> int )

    [<Fact>]
    member _.PseudoSeqStat_Break_001() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        ps.Break()
        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.PseudoSeqStat_Break_002() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current )

        ps.Break()

        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )
        Assert.StrictEqual( 99, en.Current )
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.PseudoSeqStat_Break_003() =
        let ps = PseudoSeqStat<int32, uint32>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.StrictEqual( ValueNone, ps.NextValue )

        ps.Break()

        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.PseudoSeqStat_Break_004() =
        let ps = PseudoSeqStat<int32, uint32>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.StrictEqual( ValueNone, ps.NextValue )

        ps.Continue 99
        Assert.StrictEqual( ValueSome 99, ps.NextValue )

        ps.Break 98u

        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueSome 98u, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqStat_Continue_001() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        ps.Continue( 80 )
        Assert.StrictEqual( ValueSome 80, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 80, en.Current )

    [<Fact>]
    member _.PseudoSeqStat_Continue_002() =
        let ps = PseudoSeqStat<int32, uint32>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current )

        ps.Continue( 80 )

        Assert.StrictEqual( ValueSome 80, ps.NextValue )
        Assert.StrictEqual( 99, en.Current )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 80, en.Current )

    [<Fact>]
    member _.PseudoSeqStat_Continue_003() =
        let ps = PseudoSeqStat<int32, uint32>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.StrictEqual( ValueNone, ps.NextValue )

        ps.Continue( 80 )

        Assert.StrictEqual( ValueSome 80, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 80, en.Current )

    [<Fact>]
    member _.PseudoSeqStat_Loop_001() =
        let ps = PseudoSeqStat<int32, uint32>( 0 )
        let mutable cnt = 0
        for itr in ps do
            Assert.StrictEqual( itr, cnt )
            cnt <- cnt + 1
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break()

    [<Fact>]
    member _.PseudoSeqStat_Loop_002() =
        let ps = PseudoSeqStat<int32, uint32>()
        for _ in ps do
            Assert.Fail __LINE__

    [<Fact>]
    member _.PseudoSeqStat_Loop_003() =
        let ps = PseudoSeqStat<int32, uint32>( 0 )
        ps
        |> Seq.iteri ( fun idx itr ->
            Assert.StrictEqual( itr, idx )
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break()
        )

    [<Fact>]
    member _.PseudoSeqStat_Loop_004() =
        let ps = PseudoSeqStat<int32, uint32>( 0 )
        for itr in ps do
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break( uint32 itr + 1u )
        Assert.StrictEqual( ValueSome 11u, ps.LastValue )

    [<Fact>]
    member _.PseudoSeq_Constractor_001() =
        let ps = PseudoSeq<int32 >( ValueSome 1 )
        Assert.StrictEqual( ValueSome 1, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeq_Constractor_002() =
        let ps = PseudoSeq<int32>( ValueNone )
        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeq_Constractor_003() =
        let ps = PseudoSeq<int32>()
        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeq_Constractor_004() =
        let ps = PseudoSeq<int32>( 99 )
        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        Assert.StrictEqual( ValueNone, ps.LastValue )

    [<Fact>]
    member _.PseudoSeq_Loop_001() =
        let ps = PseudoSeq<int32>( 0 )
        let mutable cnt = 0
        for itr in ps do
            Assert.StrictEqual( itr, cnt )
            cnt <- cnt + 1
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break( itr + 1 )
        Assert.StrictEqual( ValueSome 11, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqCond_Constractor_001() =
        let ps = PseudoSeqCond<int>( ValueSome 1, ( fun a -> a = 1 ) )
        Assert.StrictEqual( ValueSome 1, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Constractor_002() =
        let ps = PseudoSeqCond<int>( ValueSome 0, ( fun a -> a = 1 ) )
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Constractor_003() =
        let ps = PseudoSeqCond<int>( ValueNone, ( fun a -> a = 1 ) )
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Constractor_004() =
        let ps = PseudoSeqCond<int>( fun a -> a = 1 )
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Constractor_005() =
        let ps = PseudoSeqCond<int>( 1, ( fun a -> a = 1 ) )
        Assert.StrictEqual( ValueSome 1, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Constractor_006() =
        let ps = PseudoSeqCond<int>( 0, ( fun a -> a = 1 ) )
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Next_001() =
        let ps = PseudoSeqCond<int>( 2, ( fun a -> a > 1 ) )
        ps.Next 3
        Assert.StrictEqual( ValueSome 3, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Next_002() =
        let ps = PseudoSeqCond<int>( 2, ( fun a -> a > 1 ) )
        ps.Next 1
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Continue_001() =
        let ps = PseudoSeqCond<int>( 2, ( fun a -> a > 1 ) )
        ps.Continue 3
        Assert.StrictEqual( ValueSome 3, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Continue_002() =
        let ps = PseudoSeqCond<int>( 2, ( fun a -> a > 1 ) )
        ps.Continue 1
        Assert.StrictEqual( ValueSome 1, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Break_001() =
        let ps = PseudoSeqCond<int>( 2, ( fun a -> a > 1 ) )
        Assert.StrictEqual( ValueSome 2, ps.NextValue )
        ps.Break()
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.PseudoSeqCond_Loop_001() =
        let ps = PseudoSeqCond<int>( 0, ( fun a -> a < 10 ) )
        let mutable cnt = 0
        for itr in ps do
            Assert.StrictEqual( itr, cnt )
            cnt <- cnt + 1
            ps.Next( itr + 1 )
        Assert.StrictEqual( 10, cnt )
        Assert.StrictEqual( ValueSome 10, ps.LastValue )

    [<Fact>]
    member _.PseudoSeqCond_Loop_002() =
        let ps = PseudoSeqCond<int>( 10, ( fun a -> a < 10 ) )
        for _ in ps do
            Assert.Fail __LINE__

    [<Fact>]
    member _.PseudoSeqCond_Loop_003() =
        let ps = PseudoSeqCond<int>( 0, ( fun a -> a < 10 ) )
        for itr in ps do
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break( itr + 90 )
        Assert.StrictEqual( ValueSome 100, ps.LastValue )
