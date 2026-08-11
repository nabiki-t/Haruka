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
    member _.Constractor_001() =
        let ps = PseudoSeq<int>( ValueSome 1 )
        Assert.StrictEqual( ValueSome 1, ps.NextValue )

    [<Fact>]
    member _.Constractor_002() =
        let ps = PseudoSeq<int>( ValueNone )
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.Constractor_003() =
        let ps = PseudoSeq<int>()
        Assert.StrictEqual( ValueNone, ps.NextValue )

    [<Fact>]
    member _.Constractor_004() =
        let ps = PseudoSeq<int>( 99 )
        Assert.StrictEqual( ValueSome 99, ps.NextValue )

    [<Fact>]
    member _.GetEnumerator_001() =
        let ps = PseudoSeq<int>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.GetEnumerator_002() =
        let ps = PseudoSeq<int>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current )

    [<Fact>]
    member _.GetEnumerator_003() =
        let ps = PseudoSeq<int>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.GetEnumerator_004() =
        let ps = PseudoSeq<int>( 99 )
        let en : System.Collections.IEnumerator = ( ps :> System.Collections.IEnumerable ).GetEnumerator()
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current :?> int )

    [<Fact>]
    member _.Break_001() =
        let ps = PseudoSeq<int>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        ps.Break()
        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.Break_002() =
        let ps = PseudoSeq<int>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 99, en.Current )

        ps.Break()

        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.StrictEqual( 99, en.Current )
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.Break_003() =
        let ps = PseudoSeq<int>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.StrictEqual( ValueNone, ps.NextValue )

        ps.Break()

        Assert.StrictEqual( ValueNone, ps.NextValue )
        Assert.False( en.MoveNext() )
        Assert.ThrowsAny<Exception> ( fun () ->
            en.Current |> ignore
        ) |> ignore

    [<Fact>]
    member _.Continue_001() =
        let ps = PseudoSeq<int>( 99 )
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()

        Assert.StrictEqual( ValueSome 99, ps.NextValue )
        ps.Continue( 80 )
        Assert.StrictEqual( ValueSome 80, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 80, en.Current )

    [<Fact>]
    member _.Continue_002() =
        let ps = PseudoSeq<int>( 99 )
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
    member _.Continue_003() =
        let ps = PseudoSeq<int>()
        let en : IEnumerator<int> = ( ps :> IEnumerable<int> ).GetEnumerator()
        Assert.StrictEqual( ValueNone, ps.NextValue )

        ps.Continue( 80 )

        Assert.StrictEqual( ValueSome 80, ps.NextValue )
        Assert.True( en.MoveNext() )
        Assert.StrictEqual( 80, en.Current )

    [<Fact>]
    member _.Loop_001() =
        let ps = PseudoSeq<int>( 0 )
        let mutable cnt = 0
        for itr in ps do
            Assert.StrictEqual( itr, cnt )
            cnt <- cnt + 1
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break()

    [<Fact>]
    member _.Loop_002() =
        let ps = PseudoSeq<int>()
        for _ in ps do
            Assert.Fail __LINE__

    [<Fact>]
    member _.Loop_003() =
        let ps = PseudoSeq<int>( 0 )
        ps
        |> Seq.iteri ( fun idx itr ->
            Assert.StrictEqual( itr, idx )
            if itr < 10 then
                ps.Continue( itr + 1 )
            else
                ps.Break()
        )
