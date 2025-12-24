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
    member _.Wait_Notify_Reset_001() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))

        [|
            fun () -> task {
                let! res = w.Wait( 0 )
                Assert.True(( res = 99 ))
                Assert.True(( w.Count = 1 ))

                w.Reset 0
                Assert.True(( w.Count = 0 ))
            };
            fun () -> task {
                do! Task.Delay 500
                Assert.True(( w.Count = 1 ))
                w.Notify 0 99
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.Wait_Notify_Reset_002() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
        [|
            fun () -> task {
                let! res1 = w.Wait( 1 )
                let! res2 = w.Wait( 2 )
                let! res3 = w.Wait( 3 )
                Assert.True(( res1 = 100 ))
                Assert.True(( res2 = 200 ))
                Assert.True(( res3 = 300 ))
                Assert.True(( w.Count = 3 ))

                w.Reset 1
                w.Reset 2
                w.Reset 3
                Assert.True(( w.Count = 0 ))
            };
            fun () -> task {
                do! Task.Delay 500
                w.Notify 1 100
                w.Notify 2 200
                w.Notify 3 300
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.Wait_Exception_Reset_001() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
        [|
            fun () -> task {
                try
                    let! _ = w.Wait( 0 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "aaaa" ))
                Assert.True(( w.Count = 1 ))

                w.Reset 0
                Assert.True(( w.Count = 0 ))
            };
            fun () -> task {
                do! Task.Delay 500
                Assert.True(( w.Count = 1 ))
                w.SetException 0 ( ArgumentException "aaaa" )
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.Wait_Exception_Reset_002() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
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

                Assert.True(( w.Count = 3 ))
                w.Reset 1
                w.Reset 2
                w.Reset 3
                Assert.True(( w.Count = 0 ))
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
    member _.Wait_Exception_Reset_003() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
        [|
            fun () -> task {
                try
                    let! _ = w.Wait( 1 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "aaaa" ))
                w.Reset 1
            };
            fun () -> task {
                try
                    let! _ = w.Wait( 2 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "aaaa" ))
                w.Reset 2
            };
            fun () -> task {
                try
                    let! _ = w.Wait( 3 )
                    Assert.Fail __LINE__
                with
                | :? ArgumentException as x ->
                    Assert.True(( x.Message = "aaaa" ))
                w.Reset 3
            };
            fun () -> task {
                do! Task.Delay 500
                Assert.True(( w.Count = 3 ))
                w.SetExceptionForAll ( ArgumentException "aaaa" )
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.Notify_Wait_Reset_001() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.Notify 1 100
            Assert.True(( w.Count = 1 ))

            let! res1 = w.Wait( 1 )
            Assert.True(( res1 = 100 ))
            Assert.True(( w.Count = 1 ))

            w.Reset 1
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Notify_Wait_Reset_002() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.Notify 1 100
            w.Notify 2 200
            w.Notify 3 300
            Assert.True(( w.Count = 3 ))

            let! res1 = w.Wait( 1 )
            let! res2 = w.Wait( 2 )
            let! res3 = w.Wait( 3 )
            Assert.True(( res1 = 100 ))
            Assert.True(( res2 = 200 ))
            Assert.True(( res3 = 300 ))
            Assert.True(( w.Count = 3 ))

            w.Reset 1
            w.Reset 2
            w.Reset 3
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Exception_Wait_Reset_001() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.SetException 0 ( ArgumentException "aaaa" )
            Assert.True(( w.Count = 1 ))

            try
                let! _ = w.Wait( 0 )
                Assert.Fail __LINE__
            with
            | :? ArgumentException as x ->
                Assert.True(( x.Message = "aaaa" ))
            Assert.True(( w.Count = 1 ))

            w.Reset 0
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Exception_Wait_Reset_002() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.SetException 1 ( ArgumentException "1111" )
            w.SetException 2 ( ArgumentException "2222" )
            w.SetException 3 ( ArgumentException "3333" )
            Assert.True(( w.Count = 3 ))

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

            Assert.True(( w.Count = 3 ))

            w.Reset 1
            w.Reset 2
            w.Reset 3
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Exception_Wait_Reset_003() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.SetExceptionForAll ( ArgumentException "aaaa" )
            Assert.True(( w.Count = 0 ))

            w.Notify 1 100
            Assert.True(( w.Count = 1 ))

            let! res1 = w.Wait( 1 )
            Assert.True(( res1 = 100 ))
            Assert.True(( w.Count = 1 ))

            w.Reset 1
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Notify_Exception_Wait_Reset_001() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.Notify 1 100
            Assert.True(( w.Count = 1 ))

            w.SetException 1 ( ArgumentException "1111" )
            Assert.True(( w.Count = 1 ))

            let! res1 = w.Wait( 1 )
            Assert.True(( res1 = 100 ))
            Assert.True(( w.Count = 1 ))

            w.Reset 1
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Notify_Exception_Wait_Reset_002() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.Notify 1 100
            w.Notify 2 200
            w.Notify 3 300
            Assert.True(( w.Count = 3 ))

            w.SetException 1 ( ArgumentException "1111" )
            w.SetException 2 ( ArgumentException "2222" )
            w.SetException 3 ( ArgumentException "3333" )
            Assert.True(( w.Count = 3 ))

            let! res1 = w.Wait( 1 )
            let! res2 = w.Wait( 2 )
            let! res3 = w.Wait( 3 )
            Assert.True(( res1 = 100 ))
            Assert.True(( res2 = 200 ))
            Assert.True(( res3 = 300 ))
            Assert.True(( w.Count = 3 ))

            w.Reset 1
            w.Reset 2
            w.Reset 3
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Notify_Exception_Wait_Reset_003() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.Notify 1 100
            w.Notify 2 200
            w.Notify 3 300
            Assert.True(( w.Count = 3 ))

            w.SetExceptionForAll ( ArgumentException "aaaa" )
            Assert.True(( w.Count = 3 ))

            let! res1 = w.Wait( 1 )
            let! res2 = w.Wait( 2 )
            let! res3 = w.Wait( 3 )
            Assert.True(( res1 = 100 ))
            Assert.True(( res2 = 200 ))
            Assert.True(( res3 = 300 ))
            Assert.True(( w.Count = 3 ))

            w.Reset 1
            w.Reset 2
            w.Reset 3
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Exception_Notify_Wait_Reset_001() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.SetException 1 ( ArgumentException "1111" )
            Assert.True(( w.Count = 1 ))

            w.Notify 1 100
            Assert.True(( w.Count = 1 ))

            try
                let! _ = w.Wait( 1 )
                Assert.Fail __LINE__
            with
            | :? ArgumentException as x ->
                Assert.True(( x.Message = "1111" ))
            Assert.True(( w.Count = 1 ))

            w.Reset 1
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Exception_Notify_Wait_Reset_002() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))

            w.SetException 1 ( ArgumentException "1111" )
            w.SetException 2 ( ArgumentException "2222" )
            w.SetException 3 ( ArgumentException "3333" )
            Assert.True(( w.Count = 3 ))

            w.Notify 1 100
            w.Notify 2 200
            w.Notify 3 300
            Assert.True(( w.Count = 3 ))

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

            Assert.True(( w.Count = 3 ))

            w.Reset 1
            w.Reset 2
            w.Reset 3
            Assert.True(( w.Count = 0 ))
        }

    [<Fact>]
    member _.Reset_001() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
        w.Reset 1
        Assert.True(( w.Count = 0 ))

    [<Fact>]
    member _.Reset_002() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
        w.Notify 99 99
        Assert.True(( w.Count = 1 ))
        w.Reset 1
        Assert.True(( w.Count = 1 ))

    [<Fact>]
    member _.Reset_003() =
        let w = TaskWaiter<int, int>()
        Assert.True(( w.Count = 0 ))
        w.Notify 99 99
        Assert.True(( w.Count = 1 ))
        w.Reset 99
        Assert.True(( w.Count = 0 ))

    [<Fact>]
    member _.WaitAndReset_001() =
        task {
            let w = TaskWaiter<int, int>()
            Assert.True(( w.Count = 0 ))
            w.Notify 99 99
            Assert.True(( w.Count = 1 ))
            let! res = w.WaitAndReset 99
            Assert.True(( res = 99 ))
            Assert.True(( w.Count = 0 ))
        }
        