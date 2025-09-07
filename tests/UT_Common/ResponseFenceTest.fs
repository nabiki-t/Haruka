namespace Haruka.Test.UT.Commons

open System
open System.Collections.Generic

open Xunit

open Haruka.Commons
open Haruka.Test

type ResponseFence_Test1() =

    [<Fact>]
    member _.Test_WF() =  // W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRFF() =  // W->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRRFFF() =  // W->R->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 5 )
        wli.Add 3
        Assert.True(( rfl.Count = 2 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRWFFF() =  // W->R->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 3
        Assert.True(( rfl.Count = 2 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWFF() =  // W->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWRFFF() =  // W->W->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 3
        Assert.True(( rfl.Count = 2 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))
        Assert.True(( rfl.Count = 0 ))

    [<Fact>]
    member _.Test_WWWFFF() =  // W->W->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 3
        Assert.True(( rfl.Count = 2 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RF() =  // R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRFF() =  // R->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRRFFF() =  // R->R->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 3L ))

        rfl.Free()
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))
        Assert.True(( rfl.Count = 0 ))

    [<Fact>]
    member _.Test_RRWFFF() =  // R->R->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 4
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWFF() =  // R->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWRFFF() =  // R->W->R->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 3
        Assert.True(( rfl.Count = 2 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWWFFF() =  // R->W->W->F->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 3
        Assert.True(( rfl.Count = 2 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFWF() =  // W->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFRF() =  // W->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFWF() =  // R->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFRF() =  // R->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWFWFF() =  // W->W->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WWFRFF() =  // W->W->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRFWFF() =  // W->R->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WRFRFF() =  // W->R->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 5 )
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWFWFF() =  // R->W->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RWFRFF() =  // R->W->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 3 )
        wli.Add 2
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.NormalLock ( fun () -> wli.Add 6 )
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRFWFF() =  // R->R->F->W->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.RFLock ( fun () -> wli.Add 6 )
        wli.Add 5
        Assert.True(( rfl.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RRFRFF() =  // R->R->F->R->F->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 2 )
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 4
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.NormalLock ( fun () -> wli.Add 5 )
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 2L ))

        rfl.Free()
        wli.Add 7
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 8
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFFWF() =  // W->F->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.Free()
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_WFFRF() =  // W->F->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.RFLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.Free()
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFFWF() =  // R->F->F->W->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.Free()
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.RFLock ( fun () -> wli.Add 4 )
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = -1L ))

        rfl.Free()
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Test_RFFRF() =  // R->F->F->R->F
        let wli = List<int>()
        let rfl = ResponseFence()

        rfl.NormalLock ( fun () -> wli.Add 0 )
        wli.Add 1
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 2
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.Free()
        wli.Add 3
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        rfl.NormalLock ( fun () -> wli.Add 4 )
        wli.Add 5
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 1L ))

        rfl.Free()
        wli.Add 6
        Assert.True(( rfl.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

        Assert.True(( wli.ToArray() = [| 0 .. wli.Count - 1 |] ))

    [<Fact>]
    member _.Lock_Irrelevant_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.Irrelevant ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 0 ))
        Assert.True(( rfl.LockStatus = 0L ))

    [<Fact>]
    member _.Lock_Immediately_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.Immediately ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 1 ))
        Assert.True(( rfl.LockStatus = 0L ))

    [<Fact>]
    member _.Lock_R_Mode_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.R_Mode ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

    [<Fact>]
    member _.Lock_W_Mode_001() =
        let wli = List<int>()
        let rfl = ResponseFence()
        rfl.Lock ResponseFenceNeedsFlag.W_Mode ( fun () -> wli.Add 0 )
        Assert.True(( wli.Count = 1 ))
        Assert.True(( rfl.LockStatus = -1L ))
