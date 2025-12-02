//=============================================================================
// Haruka Software Storage.
// TaskWaiterTest.fs : Test cases for TaskWaiter class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks

open Xunit

open Haruka.Commons

//=============================================================================
// Class implementation

type TaskWaiter_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.WR_001() =
        let w = TaskWaiter<int, int>()
        
        [|
            fun () -> task {
                let! res = w.Wait( 0 )
                Assert.True(( res = 99 ))
            };
            fun () -> task {
                do! Task.Delay 500
                w.Release 0 99
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.WR_002() =
        let w = TaskWaiter<int, int>()
        
        [|
            fun () -> task {
                let! res1 = w.Wait( 1 )
                let! res2 = w.Wait( 2 )
                let! res3 = w.Wait( 3 )
                Assert.True(( res1 = 100 ))
                Assert.True(( res2 = 200 ))
                Assert.True(( res3 = 300 ))
            };
            fun () -> task {
                do! Task.Delay 500
                w.Release 1 100
                w.Release 2 200
                w.Release 3 300
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.WE_001() =
        let w = TaskWaiter<int, int>()
        
        [|
            fun () -> task {
                try
                    let! _ = w.Wait( 0 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "aaaa" ))
            };
            fun () -> task {
                do! Task.Delay 500
                w.SetException 0 ( ArgumentException "aaaa" )
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.WE_002() =
        let w = TaskWaiter<int, int>()
        
        [|
            fun () -> task {
                try
                    let! _ = w.Wait( 1 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "1111" ))

                try
                    let! _ = w.Wait( 2 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "2222" ))

                try
                    let! _ = w.Wait( 3 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "3333" ))

            };
            fun () -> task {
                do! Task.Delay 500
                w.SetException 1 ( ArgumentException "1111" )
                w.SetException 2 ( ArgumentException "2222" )
                w.SetException 3 ( ArgumentException "3333" )
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.RW_001() =
        task {
            let w = TaskWaiter<int, int>()
            w.Release 1 100
            let! res1 = w.Wait( 1 )
            Assert.True(( res1 = 100 ))
        }

    [<Fact>]
    member _.RW_002() =
        task {
            let w = TaskWaiter<int, int>()
            w.Release 1 100
            w.Release 2 200
            w.Release 3 300
            let! res1 = w.Wait( 1 )
            let! res2 = w.Wait( 2 )
            let! res3 = w.Wait( 3 )
            Assert.True(( res1 = 100 ))
            Assert.True(( res2 = 200 ))
            Assert.True(( res3 = 300 ))
        }

    [<Fact>]
    member _.EW_001() =
        task {
            let w = TaskWaiter<int, int>()
            w.SetException 0 ( ArgumentException "aaaa" )
            try
                let! _ = w.Wait( 0 )
                Assert.Fail __LINE__
            with
            | :? ArgumentException as x ->
                Assert.True(( x.Message = "aaaa" ))
        }

    [<Fact>]
    member _.EW_002() =
        task {
            let w = TaskWaiter<int, int>()
            w.SetException 1 ( ArgumentException "1111" )
            w.SetException 2 ( ArgumentException "2222" )
            w.SetException 3 ( ArgumentException "3333" )

            try
                let! _ = w.Wait( 1 )
                Assert.Fail __LINE__
            with
            | :? ArgumentException as x ->
                Assert.True(( x.Message = "1111" ))

            try
                let! _ = w.Wait( 2 )
                Assert.Fail __LINE__
            with
            | :? ArgumentException as x ->
                Assert.True(( x.Message = "2222" ))

            try
                let! _ = w.Wait( 3 )
                Assert.Fail __LINE__
            with
            | :? ArgumentException as x ->
                Assert.True(( x.Message = "3333" ))
        }

