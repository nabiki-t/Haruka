//=============================================================================
// Haruka Software Storage.
// InterProcessCounterTest.fs : Test cases for InterProcessCounter class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.IO

open Xunit

open Haruka.Commons

//=============================================================================
// Class implementation

type InterProcessCounterTest() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // The value is incremented when called repeatedly.
    [<Fact>]
    member _.InterProcessCounter_001() =
        let name = Guid.NewGuid() |> _.ToString()
        let fname = Path.Combine( Path.GetTempPath(), name )
        let counter = InterProcessCounter(name)
        Assert.True( 1UL = counter.Next() )
        Assert.True( 2UL = counter.Next() )
        Assert.True( 3UL = counter.Next() )
        File.Delete fname

    // If the same name is used in a different instance, the value will be inherited.
    [<Fact>]
    member _.InterProcessCounter_002() =
        let name = Guid.NewGuid() |> _.ToString()
        let fname = Path.Combine( Path.GetTempPath(), name )

        let c1 = InterProcessCounter( name )
        Assert.True( c1.Next() = 1UL )

        let c2 = InterProcessCounter( name )
        Assert.True( 2UL = c2.Next() )
        File.Delete fname

    // Counters with different names should not interfere with each other.
    [<Fact>]
    member _.InterProcessCounter_003() =
        let name1 = Guid.NewGuid() |> _.ToString()
        let fname1 = Path.Combine( Path.GetTempPath(), name1 )
        let name2 = Guid.NewGuid() |> _.ToString()
        let fname2 = Path.Combine( Path.GetTempPath(), name2 )

        let c1 = InterProcessCounter( name1 )
        let c2 = InterProcessCounter( name2 )

        Assert.True( 1UL = c1.Next() )
        Assert.True( 1UL = c2.Next() )
        Assert.True( 2UL = c1.Next() )
        File.Delete fname1
        File.Delete fname2

    // The count should increment correctly even in a multithreaded environment.
    [<Fact>]
    member _.InterProcessCounter_004() =
        let name = Guid.NewGuid() |> _.ToString()
        let fname = Path.Combine( Path.GetTempPath(), name )
        let counter = InterProcessCounter( name )
        let threadCount = 10
        let iterations = 20
        [|
            for _ = 1 to threadCount do
                yield async {
                    for _ = 1 to iterations do
                        counter.Next() |> ignore
                }
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

        let finalValue = counter.Next()
        let exp = uint64 ( threadCount * iterations + 1 )
        Assert.True (( exp = finalValue ))
        File.Delete fname

    // Recovering a file from scratch even if it is corrupted (non-numeric).
    [<Fact>]
    member _.InterProcessCounter_005() =
        let name = Guid.NewGuid() |> _.ToString()
        let filePath = Path.Combine( Path.GetTempPath(), name )
        File.WriteAllText(filePath, "invalid_number")

        let counter = InterProcessCounter( name )
        let result = counter.Next()
        Assert.True( 1UL = result )
        File.Delete filePath

    // The system should return 1 if the file does not exist.
    [<Fact>]
    member _.InterProcessCounter_006() =
        let name = Guid.NewGuid() |> _.ToString()
        let filePath = Path.Combine(Path.GetTempPath(), name)
        if File.Exists( filePath ) then
            File.Delete( filePath )
        let counter = InterProcessCounter( name )
        Assert.True( 1UL = counter.Next() )
        File.Delete filePath

    // If the file already exists
    [<Fact>]
    member _.InterProcessCounter_007() =
        let name = Guid.NewGuid() |> _.ToString()
        let filePath = Path.Combine( Path.GetTempPath(), name )
        File.WriteAllText( filePath, "100" )
        let counter = InterProcessCounter( name )
        Assert.True( 1UL = counter.Next() )
        File.Delete filePath

    // File deletion failed.
    [<Fact>]
    member _.InterProcessCounter_008() =
        let name = Guid.NewGuid() |> _.ToString()
        let filePath = Path.Combine( Path.GetTempPath(), name )
        Directory.CreateDirectory filePath |> ignore
        try
            let _ = InterProcessCounter( name )
            Assert.Fail __LINE__
        with
        | _ ->
            ()
        Directory.Delete filePath

    // The value resets to 0 when it exceeds the maximum value.
    [<Fact>]
    member _.InterProcessCounter_009() =
        let name = Guid.NewGuid() |> _.ToString()
        let filePath = Path.Combine( Path.GetTempPath(), name )
        let counter = InterProcessCounter( name )

        let maxValue = UInt64.MaxValue
        File.WriteAllText( filePath, maxValue.ToString() )

        Assert.True( 0UL = counter.Next() )
        Assert.True( 1UL = counter.Next() )
        File.Delete filePath

