namespace Haruka.Test.UT.Commons

open System
open System.Threading

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

type CKillerTestStub() = 
    let mutable count = 0
    interface IComponent with
        override this.Terminate() =
            count <- count + 1
    member this.GetCount() = count


type HKiller_Test () =

    [<Fact>]
    member _.HKiller_001() =
        let k1 = new HKiller() :> IKiller

        Assert.False( k1.IsNoticed )
        k1.NoticeTerminate()
        Assert.True( k1.IsNoticed )

        let k2 = new HKiller() :> IKiller
        let s1 = new CKillerTestStub()
        let s2 = new CKillerTestStub()
        k2.Add s1
        k2.Add s2

        Assert.False( k2.IsNoticed )
        k2.NoticeTerminate()
        Assert.True( k2.IsNoticed )

        Assert.True( s1.GetCount() = 1 )
        Assert.True( s2.GetCount() =1 )

        Assert.True( k2.IsNoticed )
        k2.NoticeTerminate()
        Assert.True( s1.GetCount() = 1 )
        Assert.True( s2.GetCount() = 1 )

