//=============================================================================
// Haruka Software Storage.
// RWLockTest.fs : Test cases for RWLock class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks

open Xunit

open Haruka.Commons


////////////////////////////////
// Test cases
#if false

R1  F1
W1  F1
F1

R1  R2  F1  F2
R1  R2  F2  F1
R1  W2  F1  F2
R1  F1  R2  F2
R1  F1  W2  F2
W1  R2  F1  F2
W1  W2  F1  F2
W1  F1  R2  F2
W1  F1  W2  F2

R1  R2  R3  F1  F2  F3 *
R1  R2  W3  F1  F2  F3 *
R1  R2  F1  R3  F2  F3 *
R1  R2  F1  W3  F2  F3 *
R1  R2  F1  F2  R3  F3 *
R1  R2  F1  F2  W3  F3 *
R1  W2  R3  F1  F2  F3 *
R1  W2  W3  F1  F2  F3
R1  W2  F1  R3  F2  F3
R1  W2  F1  W3  F2  F3
R1  W2  F1  F2  R3  F3
R1  W2  F1  F2  W3  F3
W1  R2  R3  F1  F2  F3
W1  R2  W3  F1  F2  F3
W1  R2  F1  R3  F2  F3
W1  R2  F1  W3  F2  F3
W1  R2  F1  F2  R3  F3
W1  R2  F1  F2  W3  F3
W1  W2  R3  F1  F2  F3
W1  W2  W3  F1  F2  F3
W1  W2  F1  R3  F2  F3
W1  W2  F1  W3  F2  F3
W1  W2  F1  F2  R3  F3
W1  W2  F1  F2  W3  F3

#endif



//=============================================================================
// Class implementation

type RWLockTest() =

    let wait ( f : unit -> bool ) : Task<unit> =
        task {
            do! Task.Delay 5
            while ( f() ) do
                do! Task.Delay 5
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases ( one thread )

    [<Fact>]
    member _.R1_F1() =
        let l = RWLock()
        async {
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_F1() =
        let l = RWLock()
        async {
            do! l.WLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 1, 0, 0 ) )

            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.F1() =
        let l = RWLock()
        async {
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    ///////////////////////////////////////////////////////////////////////////
    // Test cases ( two thread )

    [<Fact>]
    member _.R1_R2_F1_F2() =
        let l = RWLock()
        let br =
            [| 2; 2; 2; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_R2_F2_F1() =
        let l = RWLock()
        let br =
            [| 2; 2; 2; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_F1_F2() =
        let l = RWLock()
        let br =
            [| 2; 1; 2; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait ( fun () -> l.WWaiter = 0 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_F1_R2_F2() =
        let l = RWLock()
        async {
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_F1_W2_F2() =
        let l = RWLock()
        async {
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            do! l.WLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 1, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_F1_F2() =
        let l = RWLock()
        let br =
            [| 2; 1; 2; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait ( fun () -> l.RWaiter = 0 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_F1_F2() =
        let l = RWLock()
        let br =
            [| 2; 1; 2; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait ( fun () -> l.WWaiter = 0 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_F1_R2_F2() =
        let l = RWLock()
        async {
            do! l.WLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 1, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_F1_W2_F2() =
        let l = RWLock()
        async {
            do! l.WLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 1, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            do! l.WLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 1, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    ///////////////////////////////////////////////////////////////////////////
    // Test cases ( three thread )

    [<Fact>]
    member _.R1_R2_R3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 3; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 3, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_R2_W3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 3; 2; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[1].SignalAndWait()

                do! wait ( fun () -> l.WWaiter = 0 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 1 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_R2_F1_R3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 3; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_R2_F1_W3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 3; 3; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                do! wait ( fun () -> l.WWaiter = 0 ) |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_R2_F1_F2_R3_F3() =
        let l = RWLock()
        async {
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 2, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_R2_F1_F2_W3_F3() =
        let l = RWLock()
        async {
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0 ,0 ) )
            do! l.RLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 2, 0, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            do! l.WLock() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 1, 0 ,0 ) )
            do! l.Release() |> Async.AwaitTask
            Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
        }
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_R3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 1; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 1, 1 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_W3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 1; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 2 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 2 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_F1_R3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_F1_W3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_F1_F2_R3_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.R1_W2_F1_F2_W3_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.RLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_R3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 1; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 2 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 2, 0 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_W3_F1_F3_F2() =
        let l = RWLock()
        let br =
            [| 3; 2; 1; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 1 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_F1_R3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 2, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_F1_W3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_F1_F2_R3_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_R2_F1_F2_W3_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_R3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 1; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 1 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_W3_F1_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 1; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 2 ) |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 2 ) )
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_F1_R3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                do! wait( fun () -> l.RWaiter <> 1 ) |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 1, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_F1_W3_F2_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 2; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_F1_F2_R3_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 1, 0, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.RLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    [<Fact>]
    member _.W1_W2_F1_F2_W3_F3() =
        let l = RWLock()
        let br =
            [| 3; 2; 3; 3; 3; |]
            |> Array.map ( fun itr -> new Barrier( itr ) )
        [|
            async {
                do! l.WLock() |> Async.AwaitTask

                br.[0].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[0].SignalAndWait()

                do! wait( fun () -> l.WWaiter <> 1 ) |> Async.AwaitTask

                br.[1].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 1 ) )
                br.[1].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[2].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                Assert.True( l.Stat = ( 0, 1, 0, 0 ) )
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                do! l.Release() |> Async.AwaitTask

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()
            };

            async {
                br.[0].SignalAndWait()
                br.[0].SignalAndWait()

                br.[1].SignalAndWait()
                br.[1].SignalAndWait()

                br.[2].SignalAndWait()
                br.[2].SignalAndWait()

                br.[3].SignalAndWait()
                br.[3].SignalAndWait()

                do! l.WLock() |> Async.AwaitTask

                br.[4].SignalAndWait()
                br.[4].SignalAndWait()

                do! l.Release() |> Async.AwaitTask
                Assert.True( l.Stat = ( 0, 0, 0, 0 ) )
            };
        |]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

