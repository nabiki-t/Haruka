namespace Haruka.Test.UT.Commons

open System
open System.IO
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

#if false

type CryptoTransceiver_Test () =
   
    [<Theory>]
    [<InlineData( "" )>]
    [<InlineData( "012345678" )>]
    [<InlineData( "0123456789abcdef" )>]
    [<InlineData( "0123456789abcdef012" )>]
    [<InlineData( "0123456789abcdef0123456789ABCDEF" )>]
    member _.SendNormalText_001 ( d : string ) =
        let svr, cli = GlbFunc.GetNetConn()

        let svrTask = task {
            let! svrSender = EncryptSender.InitialKeyExchange( svr )
            do! svrSender.Send svr d
        }
        let cliTask = task {
            let! clrRecver = EncryptReceiver.InitialKeyExchange( cli )
            let! s1 = clrRecver.Receive cli
            Assert.True(( s1 = d ))
        }
        Task.WaitAll( svrTask, cliTask )
        GlbFunc.AllDispose [ svr; cli; ]

    [<Fact>]
    member _.SendNormalText_002 () =
        let svr, cli = GlbFunc.GetNetConn()
        let text = String.replicate ( 1024 * 256 + 3 ) "A"
        let mutable cnt = 0

        let svrTask = task {
            let! svrSender = EncryptSender.InitialKeyExchange( svr )
            for i = 0 to 99 do
                do! svrSender.Send svr text
        }
        let cliTask = task {
            let! clrRecver = EncryptReceiver.InitialKeyExchange( cli )
            for i = 0 to 99 do
                let! s1 = clrRecver.Receive cli
                Assert.True(( s1 = text ))
                cnt <- cnt + 1
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 100 ))
        GlbFunc.AllDispose [ svr; cli; ]

    [<Fact>]
    member _.SendNormalText_003 () =
        let svr, cli = GlbFunc.GetNetConn()
        let mutable cnt = 0

        let svrTask = task {
            let! svrSender = EncryptSender.InitialKeyExchange( svr )
            let! svrReceiver = EncryptReceiver.InitialKeyExchange( svr )

            for i = 0 to 99 do
                let! lineStr = svrReceiver.Receive svr
                let r, d = Int32.TryParse lineStr
                Assert.True( r )
                do! svrSender.Send svr ( sprintf "%d" ( d + 1 ) )
        }
        let cliTask = task {
            let! clrRecver = EncryptReceiver.InitialKeyExchange( cli )
            let! clrSender = EncryptSender.InitialKeyExchange( cli )

            for i = 10000 to 10099 do
                do! clrSender.Send cli ( sprintf "%d" i )
                let! s1 = clrRecver.Receive cli
                Assert.True(( s1 = sprintf "%d" ( i + 1 ) ))
                cnt <- cnt + 1
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 100 ))
        GlbFunc.AllDispose [ svr; cli; ]

    [<Fact>]
    member _.InitialKeyExchange_001() =
        let svr, cli = GlbFunc.GetNetConn()
        let mutable cnt = 0
        let svrTask = task {
            try
                let! _ = EncryptSender.InitialKeyExchange( svr )
                Assert.Fail __LINE__
            with
            | :? Xunit.Sdk.FailException -> ()
            | _ ->
                cnt <- cnt + 1
        }
        let cliTask = task {
            let wb = Array.zeroCreate<byte>( 1024 )
            do! cli.WriteAsync( wb, 0, 1024 )
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 1 ))
        GlbFunc.AllDispose [ svr; cli; ]

    [<Fact>]
    member _.InitialKeyExchange_002() =
        let svr, cli = GlbFunc.GetNetConn()
        let mutable cnt = 0
        let svrTask = task {
            try
                let! _ = EncryptReceiver.InitialKeyExchange( svr )
                Assert.Fail __LINE__
            with
            | :? Xunit.Sdk.FailException -> ()
            | _ ->
                cnt <- cnt + 1
        }
        let cliTask = task {
            let wb = Array.zeroCreate<byte>( 1024 )
            do! cli.WriteAsync( wb, 0, 1024 )
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 1 ))
        GlbFunc.AllDispose [ svr; cli; ]

    [<Fact>]
    member _.Send_001() =
        let svr, cli = GlbFunc.GetNetConn()
        let mutable cnt = 0
        let m_Sema = new System.Threading.SemaphoreSlim( 1 )
        m_Sema.Wait()

        let svrTask = task {
            let! svrSender = EncryptSender.InitialKeyExchange( svr )
            try
                m_Sema.Wait()
                do! svrSender.Send svr "abc"
                Assert.Fail __LINE__
            with
            | :? Xunit.Sdk.FailException -> ()
            | _ ->
                cnt <- cnt + 1
        }
        let cliTask = task {
            let! _ = EncryptReceiver.InitialKeyExchange( cli )
            cli.Dispose()
            do! Task.Delay 10
            m_Sema.Release() |> ignore
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 1 ))
        GlbFunc.AllDispose [ svr; cli; ]

    [<Fact>]
    member _.Receive_001() =
        let svr1, cli1 = GlbFunc.GetNetConn()
        let svr2, cli2 = GlbFunc.GetNetConn()
        let mutable cnt = 0

        let svrTask = task {
            let! svrSender = EncryptSender.InitialKeyExchange( svr1 )
            do! svrSender.Send svr1 "abc"
        }
        let cliTask = task {
            let! cliRecver = EncryptReceiver.InitialKeyExchange( cli1 )
            
            let w = Array.zeroCreate<byte>( 84 )
            let! _ =
                Functions.loopAsyncWithState
                    ( fun s -> task {
                        let! ww = cli1.ReadAsync( w, s, 84 - s )
                        return ( s + ww < 84, s + ww )
                    })
                    0

            w.[0] <- 1uy
            w.[1] <- 1uy

            svr2.Write( w, 0, 84 )

            try
                let! _ = cliRecver.Receive cli2
                Assert.Fail __LINE__
            with
            | :? IOException as x ->
                Assert.True(( x.Message = "Invalid header HMAC." ))
                cnt <- cnt + 1
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 1 ))
        GlbFunc.AllDispose [ svr1; cli1; svr2; cli2; ]

    [<Fact>]
    member _.Receive_002() =
        let svr1, cli1 = GlbFunc.GetNetConn()
        let svr2, cli2 = GlbFunc.GetNetConn()
        let mutable cnt = 0

        let svrTask = task {
            let! svrSender = EncryptSender.InitialKeyExchange( svr1 )
            do! svrSender.Send svr1 "abc"
        }
        let cliTask = task {
            let! cliRecver = EncryptReceiver.InitialKeyExchange( cli1 )
            
            let w = Array.zeroCreate<byte>( 84 )
            let! _ =
                Functions.loopAsyncWithState
                    ( fun s -> task {
                        let! ww = cli1.ReadAsync( w, s, 84 - s )
                        return ( s + ww < 84, s + ww )
                    })
                    0

            w.[40] <- ~~~w.[40]
            w.[41] <- ~~~w.[41]

            svr2.Write( w, 0, 84 )

            try
                let! _ = cliRecver.Receive cli2
                Assert.Fail __LINE__
            with
            | :? IOException as x ->
                Assert.True(( x.Message = "Invalid data HMAC." ))
                cnt <- cnt + 1
        }
        Task.WaitAll( svrTask, cliTask )
        Assert.True(( cnt = 1 ))
        GlbFunc.AllDispose [ svr1; cli1; svr2; cli2; ]

#endif
