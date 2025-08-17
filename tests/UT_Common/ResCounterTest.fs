namespace Haruka.Test.UT.Commons

open System
open System.Threading.Tasks
open System.Collections.Concurrent

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

type ResCounter_Test1() =

    [<Fact>]
    member _.ResCntRec_Constructor_001() =
        let n = new ResCntRec( 1L, 2L )
        Assert.True(( n.TimeStamp = 1L ))
        Assert.True(( n.Value = 2L ))
        Assert.True(( n.Count = 1L ))

    [<Fact>]
    member _.ResCntRec_Add_001() =
        let n = new ResCntRec( 0L, 0L )
        Assert.True(( n.TimeStamp = 0L ))
        Assert.True(( n.Value = 0L ))
        Assert.True(( n.Count = 1L ))
        n.Add( 1 )
        Assert.True(( n.TimeStamp = 0L ))
        Assert.True(( n.Value = 1L ))
        Assert.True(( n.Count = 2L ))
        n.Add( 2 )
        Assert.True(( n.TimeStamp = 0L ))
        Assert.True(( n.Value = 3L ))
        Assert.True(( n.Count = 3L ))

    [<Fact>]
    member _.ResCntRec_Add_002() =
        let n = new ResCntRec( 0L, 0L )
        [|
            for i = 0 to 127 do
                yield ( fun () -> task {
                    do! Task.Delay 0
                    n.Add( 1 )
                } )
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore
        Assert.True(( n.TimeStamp = 0L ))
        Assert.True(( n.Value = 128L ))
        Assert.True(( n.Count = 129L ))

    [<Fact>]
    member _.ResCounter_Constructor_001() =
        let tps = TimeSpan.TicksPerSecond
        let n = new ResCounter( 0L, 7L )
        n.AddCount ( DateTime( tps * 1L ) ) 1L
        n.AddCount ( DateTime( tps * 2L ) ) 1L
        n.AddCount ( DateTime( tps * 3L ) ) 1L
        n.AddCount ( DateTime( tps * 4L ) ) 1L
        n.AddCount ( DateTime( tps * 5L ) ) 1L
        n.AddCount ( DateTime( tps * 6L ) ) 1L
        n.AddCount ( DateTime( tps * 7L ) ) 1L
        n.AddCount ( DateTime( tps * 8L ) ) 1L
        n.AddCount ( DateTime( tps * 9L ) ) 1L
        let v = n.Get ( DateTime( tps * 9L ) )
        Assert.True(( v.Length = 8 ))
        Assert.True(( v.[0].Time = DateTime( tps * 1L ) ))
        Assert.True(( v.[0].Value = 1L ))
        Assert.True(( v.[0].Count = 1L ))
        Assert.True(( v.[7].Time = DateTime( tps * 8L ) ))
        Assert.True(( v.[7].Value = 1L ))
        Assert.True(( v.[7].Count = 1L ))

    [<Fact>]
    member _.ResCounter_Constructor_002() =
        let tps = TimeSpan.TicksPerSecond
        let n = new ResCounter( 1L, 1025L )
        for i = 1 to 1025 do
            n.AddCount ( DateTime( tps * ( int64 i ) ) ) 1L
        let v = n.Get ( DateTime( tps * 1025L ) )
        Assert.True(( v.Length = 1024 ))
        Assert.True(( v.[0].Time = DateTime( tps * 1L ) ))
        Assert.True(( v.[0].Value = 1L ))
        Assert.True(( v.[0].Count = 1L ))
        Assert.True(( v.[1023].Time = DateTime( tps * 1024L ) ))
        Assert.True(( v.[1023].Value = 1L ))
        Assert.True(( v.[1023].Count = 1L ))

    [<Fact>]
    member _.ResCounter_AddCount_001() =
        let tps = TimeSpan.TicksPerSecond
        let n = new ResCounter( 3L, 24L )
        n.AddCount ( DateTime( tps * 0L ) ) 10L
        n.AddCount ( DateTime( tps * 1L ) ) 10L
        n.AddCount ( DateTime( tps * 2L ) ) 10L
        n.AddCount ( DateTime( tps * 3L ) ) 10L
        let v = n.Get ( DateTime( tps * 3L ) )
        Assert.True(( v.Length = 1 ))
        Assert.True(( v.[0].Time = DateTime( tps * 0L ) ))
        Assert.True(( v.[0].Value = 30L ))
        Assert.True(( v.[0].Count = 4L ))

    [<Fact>]
    member _.ResCounter_AddCount_002() =
        let tps = TimeSpan.TicksPerSecond
        let n = new ResCounter( 3L, 24L )
        n.AddCount ( DateTime( tps * 3L ) ) 10L
        n.AddCount ( DateTime( tps * 1L ) ) 10L
        n.AddCount ( DateTime( tps * 6L ) ) 10L
        let v = n.Get ( DateTime( tps * 4L ) )
        Assert.True(( v.Length = 2 ))
        Assert.True(( v.[0].Time = DateTime( tps * 0L ) ))
        Assert.True(( v.[0].Value = 0L ))
        Assert.True(( v.[0].Count = 1L ))
        Assert.True(( v.[1].Time = DateTime( tps * 3L ) ))
        Assert.True(( v.[1].Value = 10L ))
        Assert.True(( v.[1].Count = 1L ))

    [<Fact>]
    member _.ResCounter_AddCount_003() =
        let tps = TimeSpan.TicksPerSecond
        let n = new ResCounter( 3L, 24L )
        let pc = PrivateCaller( n )
        let m_CntHist = pc.GetField( "m_CntHist" ) :?> ConcurrentDictionary< int64, ResCntRec >
        for i = 1 to 24 do
            n.AddCount ( DateTime( tps * ( int64 i ) ) ) 1L
        Assert.True(( m_CntHist.Count = 8 ))
        n.AddCount ( DateTime( tps * 27L ) ) 1L
        Assert.True(( m_CntHist.Count = 9 ))
        n.AddCount ( DateTime( tps * 30L ) ) 1L
        Assert.True(( m_CntHist.Count = 9 ))

    [<Fact>]
    member _.ResCounter_Get_001() =
        let tps = TimeSpan.TicksPerSecond
        let n = new ResCounter( 3L, 24L )

        n.AddCount ( DateTime( tps * 3L ) ) 1L

        let v1 = n.Get ( DateTime( tps * 24L ) )
        Assert.True(( v1.Length = 1 ))
        Assert.True(( v1.[0].Time = DateTime( tps * 0L ) ))

        let v2 = n.Get ( DateTime( tps * 25L ) )
        Assert.True(( v2.Length = 0 ))
        
