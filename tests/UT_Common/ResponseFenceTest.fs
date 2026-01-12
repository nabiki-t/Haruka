//=============================================================================
// Haruka Software Storage.
// ResponseFenceTest.fs : Test cases for ResponseFence class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Collections.Generic

open Xunit

open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

type ResponseFence_Test1() =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Test_WF() =  // W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRFF() =  // W->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 = tick2 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRRFFF() =  // W->R->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 5 )
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick1 ))
        Assert.True(( tcnt = 2 ))
        Assert.True(( lcnt = -1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 6
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while tick4 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick5 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRWFFF() =  // W->R->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 = tick2 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 = tick3 ))
        Assert.True(( tcnt = 2 ))
        Assert.True(( lcnt = -1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while tick4 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick5 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWFF() =  // W->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWRFFF() =  // W->W->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick1 ))
        Assert.True(( tcnt = 2 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))
        Assert.True(( rfl.TaskCount = 0 ))

    [<Fact>]
    member _.Test_WWWFFF() =  // W->W->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick1 ))
        Assert.True(( tcnt = 2 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 <> tick1 ))
        Assert.True(( tick4 > 0L ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tick5 > 0L ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RF() =  // R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRFF() =  // R->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRRFFF() =  // R->R->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 5
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 3L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 6
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while tick4 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick5 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))
        Assert.True(( rfl.TaskCount = 0 ))

    [<Fact>]
    member _.Test_RRWFFF() =  // R->R->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick2 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 2L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while tick4 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick5 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWFF() =  // R->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWRFFF() =  // R->W->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick2 ))
        Assert.True(( tcnt = 2 ))
        Assert.True(( lcnt = 1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while tick4 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick5 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWWFFF() =  // R->W->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 = tick2 ))
        Assert.True(( tcnt = 2 ))
        Assert.True(( lcnt = 1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFWF() =  // W->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFRF() =  // W->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFWF() =  // R->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFRF() =  // R->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick1 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while tick2 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while tick3 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWFWFF() =  // W->W->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWFRFF() =  // W->W->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while tick0 = Environment.TickCount64 do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRFWFF() =  // W->R->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRFRFF() =  // W->R->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 5 )
        wli.Add 6
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWFWFF() =  // R->W->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWFRFF() =  // R->W->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRFWFF() =  // R->R->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 1 ))
        Assert.True(( lcnt = 1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRFRFF() =  // R->R->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 = tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 4
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 5 )
        wli.Add 6
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 = tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 2L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 7
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick5 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 8
        let struct( tick6, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick6 > tick5 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFFWF() =  // W->F->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 6
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFFRF() =  // W->F->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 6
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFFWF() =  // R->F->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = -1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 6
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFFRF() =  // R->F->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        let struct( tick0, _, _ ) = rfl.LockStatus
        while ( tick0 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        let struct( tick1, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick1 > tick0 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick1 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 2
        let struct( tick2, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick2 > tick1 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick2 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 3
        let struct( tick3, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick3 > tick2 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        while ( tick3 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 5
        let struct( tick4, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick4 > tick3 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 1L ))

        while ( tick4 = Environment.TickCount64 ) do
            Thread.Sleep 5

        rfl.Free()
        wli.Add 6
        let struct( tick5, lcnt, tcnt ) = rfl.LockStatus
        Assert.True(( tick5 > tick4 ))
        Assert.True(( tcnt = 0 ))
        Assert.True(( lcnt = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Lock_Irrelevant_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.Irrelevant ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 0 ))
        Assert.True(( rfl.LockCounter = 0L ))

    [<Fact>]
    member _.Lock_Immediately_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.Immediately ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 1 ))
        Assert.True(( rfl.LockCounter = 0L ))

    [<Fact>]
    member _.Lock_R_Mode_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.R_Mode ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 1 ))
        Assert.True(( rfl.LockCounter = 1L ))

    [<Fact>]
    member _.Lock_W_Mode_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.W_Mode ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 1 ))
        Assert.True(( rfl.LockCounter = -1L ))
