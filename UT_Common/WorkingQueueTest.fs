namespace Haruka.Test.UT.Commons

open System
open System.Threading
open System.Threading.Tasks
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

type WorkingQueue_Test() =

    [<Fact>]
    member _.Enqueue_001() =
        let mutable cnt = 0
        use b = new Barrier( 2 )
        let testfunc ( a : int ) =
            task {
                Assert.True(( a = 99 ))
                cnt <- cnt + 1
                b.SignalAndWait()
            }
        let wq = new WorkingTaskQueue< int >( testfunc )
        wq.Enqueue( 99 )
        b.SignalAndWait()
        Assert.True(( cnt = 1 ))
        wq.Stop()

    [<Fact>]
    member _.Enqueue_002() =
        let mutable cnt = 0
        use b = new Barrier( 2 )
        let testfunc ( a : int ) =
            task {
                cnt <- cnt + 1
                if cnt = 1 then
                    Assert.True(( a = 98 ))
                else
                    Assert.True(( a = 99 ))
                    b.SignalAndWait()
            }
        let wq = new WorkingTaskQueue< int >( testfunc )
        wq.Enqueue( 98 )
        wq.Enqueue( 99 )
        b.SignalAndWait()
        Assert.True(( cnt = 2 ))
        wq.Stop()

    [<Fact>]
    member _.Enqueue_003() =
        let mutable cnt = 0
        use b = new Barrier( 2 )
        let testfunc ( a : int ) =
            task {
                cnt <- cnt + 1
                b.SignalAndWait()
            }
        let wq = new WorkingTaskQueue< int >( testfunc )
        wq.Enqueue( 99 )
        b.SignalAndWait()
        wq.Stop()
        wq.Enqueue( 98 )
        wq.Enqueue( 97 )
        wq.Enqueue( 96 )
        Assert.True(( wq.Count <= 1 ))
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.Enqueue_004() =
        let mutable cnt = 0
        use b = new Barrier( 2 )
        let testfunc ( a : int ) =
            task {
                cnt <- cnt + 1
                b.SignalAndWait()
                b.SignalAndWait()
            }
        let wq = new WorkingTaskQueue< int >( testfunc )

        wq.Enqueue( 99 )
        b.SignalAndWait()
        Assert.True(( cnt = 1 ))
        Assert.True(( wq.Count = 0 ))
        Assert.True(( wq.RunWaitCount = 1u ))
        b.SignalAndWait()

        wq.Enqueue( 98 )
        b.SignalAndWait()
        Assert.True(( cnt = 2 ))
        Assert.True(( wq.Count = 0 ))
        Assert.True(( wq.RunWaitCount = 1u ))

        wq.Enqueue( 97 )
        Assert.True(( wq.Count = 1 ))
        Assert.True(( wq.RunWaitCount = 2u ))

        b.SignalAndWait()

        b.SignalAndWait()
        Assert.True(( cnt = 3 ))
        Assert.True(( wq.Count = 0 ))
        Assert.True(( wq.RunWaitCount = 1u ))
        b.SignalAndWait()

        wq.Stop()

    [<Fact>]
    member _.Enqueue_005() =
        let cnt = Array.zeroCreate< int >( 4 )
        let b = [| for i = 1 to 4 do new Barrier( 2 ) |]

        let testfunc ( a : int ) =
            task {
                cnt.[ a ] <- cnt.[ a ] + 1
                b.[a].SignalAndWait()
            }
        let wq = new WorkingTaskQueue< int >( testfunc )

        for i = 0 to 3 do
            wq.Enqueue( i )
            b.[i].SignalAndWait()

        for i = 0 to 3 do
            Assert.True(( cnt.[i] = 1 ))

        wq.Stop()
        for itr in b do itr.Dispose()

    [<Fact>]
    member _.TaskQueue_001() =
        let mutable cnt = 0
        use b = new Barrier( 2 )
        let wq = new TaskQueue()
        wq.Enqueue( fun () -> task {
            cnt <- cnt + 1
            b.SignalAndWait()
        })
        b.SignalAndWait()
        Assert.True(( cnt = 1 ))
        wq.Stop()

    [<Fact>]
    member _.TaskQueue_002() =
        let cnt = Array.zeroCreate< int >( 4 )
        let b = [| for i = 1 to 4 do new Barrier( 2 ) |]
        let wq = new TaskQueue( 4u )

        for i = 0 to 3 do
            wq.Enqueue( fun () -> task {
                cnt.[i] <- cnt.[i] + 1
                b.[i].SignalAndWait()
            })
            b.[i].SignalAndWait()

        for i = 0 to 3 do
            Assert.True(( cnt.[i] = 1 ))

        wq.Stop()
        for itr in b do itr.Dispose()

    [<Fact>]
    member _.TaskQueueWithState_001() =
        let q = new TaskQueueWithState<int>( 0 )
        let b = new Barrier( 2 )

        q.Enqueue( fun s -> task {
            Assert.True(( s = 0 ))
            return s + 1
        } )

        q.Enqueue( fun s -> task {
            Assert.True(( s = 1 ))
            return s + 1
        } )

        q.Enqueue( fun s -> task {
            Assert.True(( s = 2 ))
            b.SignalAndWait()
            return s + 1
        } )

        b.SignalAndWait()
        q.Stop()
