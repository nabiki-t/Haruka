//=============================================================================
// Haruka Software Storage.
// IscsiTCPSvPortTest.fs : Test cases for IscsiTCPSvPort class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Net
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type IscsiTCPSvPort_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.CPort_001() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetAddress = "";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }
        let k = new HKiller() :> IKiller
        new IscsiTCPSvPort( stat, rNP, k ) :> IPort |> ignore
        k.NoticeTerminate()

    [<Fact>]
    member _.CPort_002() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat1 =  new CStatus_Stub()
        let rNP1 : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetAddress = "";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }
        let rNP2 : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetAddress = "";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }

        let k1 = HKiller() :> IKiller
        let k2 = HKiller() :> IKiller
        let p1 = new IscsiTCPSvPort( stat1, rNP1, k1 ) :> IPort
        let p2 = new IscsiTCPSvPort( stat1, rNP2, k2 ) :> IPort

        try
            Assert.True( p1.Start() )
            Assert.False( p2.Start() )
        with
        | _ as x ->
            k1.NoticeTerminate()
            k2.NoticeTerminate()
            reraise ()
        
        k1.NoticeTerminate()
        k2.NoticeTerminate()

    [<Fact>]
    member _.CPort_003() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 1u;
                TargetAddress = "localhost";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }

        let k1 = HKiller() :> IKiller
        let p = new IscsiTCPSvPort( stat, rNP, k1 ) :> IPort

        let swait = new SemaphoreSlim( 1 )

        let mutable count = 0
        stat.p_CreateLoginNegociator <-
            (
                fun w d x np -> 
                    Assert.True( ( x = tpgt_me.zero ) )
                    Assert.True( ( np = netportidx_me.fromPrim 1u ) )
                    Interlocked.Add( &count, 1 ) |> ignore
                    w.Close()
                    let lns = new CLoginNegociator_Stub()
                    lns.p_Start <- ( fun _ -> true )
                    swait.Release() |> ignore
                    lns :> ILoginNegociator
            )

        Assert.True( p.Start() )
        Thread.Sleep 10

        let mutable tryCount = 0
        for i = 0 to 10 do
            try
                swait.Wait()
                use s = new TcpClient( "localhost", portNo )
                s.Close()
                swait.Wait()
                swait.Release() |> ignore
                tryCount <- tryCount + 1
            with
            | :? SocketException as x ->
                swait.Release() |> ignore
                Thread.Sleep 50
        
        Assert.True( ( count = tryCount ) )
        
        k1.NoticeTerminate()

    [<Fact>]
    member _.CPort_004() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetAddress = "::1";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }

        let k1 = HKiller() :> IKiller
        let p = new IscsiTCPSvPort( stat, rNP, k1 ) :> IPort

        let mutable count = 0
        stat.p_CreateLoginNegociator <-
            (
                fun w d x npidx -> 
                    Assert.Fail __LINE__
                    Interlocked.Add( &count, 1 ) |> ignore
                    w.Close()
                    new CLoginNegociator_Stub() :> ILoginNegociator
            )

        k1.NoticeTerminate()
        try
            p.Start() |> ignore
            Assert.Fail __LINE__
        with
        | _ as x ->
            Assert.True(( x.Message = "Terminated requested." ))
        Assert.True( ( count = 0 ) )
        k1.NoticeTerminate()

    [<Fact>]
    member _.CPort_005() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 0u;
                TargetAddress = "::1";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }

        let k1 = HKiller() :> IKiller
        let p = new IscsiTCPSvPort( stat, rNP, k1 ) :> IPort

        let mutable count = 0
        stat.p_CreateLoginNegociator <-
            (
                fun w d x _ -> 
                    Assert.Fail __LINE__
                    Interlocked.Add( &count, 1 ) |> ignore
                    w.Close()
                    new CLoginNegociator_Stub() :> ILoginNegociator
            )

        Assert.True( p.Start() )

        try
            let listener = new TcpListener( IPAddress.IPv6Loopback, portNo )
            listener.Start()
            Assert.Fail __LINE__
        with
        | :? SocketException as x ->
            ()

        k1.NoticeTerminate()

        let listener = new TcpListener( IPAddress.IPv6Loopback, portNo )
        listener.Start()
        listener.Stop()

    [<Fact>]
    member _.CPort_006() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 35u;
                TargetAddress = "";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [];
            }

        let k1 = HKiller() :> IKiller
        let p = new IscsiTCPSvPort( stat, rNP, k1 ) :> IPort

        let swait = new SemaphoreSlim( 1 )

        let mutable count = 0
        stat.p_CreateLoginNegociator <-
            (
                fun w d x npidx -> 
                    Assert.True( ( x = tpgt_me.zero ) )
                    Assert.True( ( npidx = netportidx_me.fromPrim 35u ) )
                    Interlocked.Add( &count, 1 ) |> ignore
                    w.Close()
                    let lns = new CLoginNegociator_Stub()
                    lns.p_Start <- ( fun _ -> true )
                    swait.Release() |> ignore
                    lns :> ILoginNegociator
            )

        Assert.True( p.Start() )
        Thread.Sleep 10

        let hostEntry = Dns.GetHostEntry ""
        
        let v = Array.append hostEntry.AddressList [| IPAddress.Loopback; IPAddress.IPv6Loopback; |]
        for i = 0 to v.Length - 1 do
            let addressStr =
                let w = v.[i].ToString()
                let widx = w.LastIndexOf "%"
                if widx > 0 then
                    w.[ 0 .. widx - 1 ]
                else
                    w

            swait.Wait()
            use s = new TcpClient( addressStr, portNo )
            s.Close()
            swait.Wait()
            swait.Release() |> ignore
        
        k1.NoticeTerminate()

    [<Fact>]
    member _.CPort_007() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 1u;
                TargetAddress = "::1";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [ IPCondition.Loopback; ];
            }

        let k1 = HKiller() :> IKiller
        let p = new IscsiTCPSvPort( stat, rNP, k1 ) :> IPort

        let swait = new SemaphoreSlim( 1 )

        let mutable count = 0
        stat.p_CreateLoginNegociator <-
            (
                fun w d x np -> 
                    Interlocked.Add( &count, 1 ) |> ignore
                    w.Close()
                    let lns = new CLoginNegociator_Stub()
                    lns.p_Start <- ( fun _ -> true )
                    swait.Release() |> ignore
                    lns :> ILoginNegociator
            )

        Assert.True( p.Start() )
        Thread.Sleep 10

        swait.Wait()
        use s = new TcpClient( "::1", portNo )
        s.Close()
        swait.Wait()
        swait.Release() |> ignore
        
        Assert.True(( count = 1 ))
        
        k1.NoticeTerminate()

    [<Fact>]
    member _.CPort_008() =
        let portNo = GlbFunc.nextTcpPortNo()
        let stat =  new CStatus_Stub()
        let rNP : TargetDeviceConf.T_NetworkPortal =
            {
                IdentNumber = netportidx_me.fromPrim 1u;
                TargetAddress = "::1";
                PortNumber = uint16 portNo;
                DisableNagle = false;
                ReceiveBufferSize = 8192;
                SendBufferSize = 8192;
                TargetPortalGroupTag = tpgt_me.zero;
                WhiteList = [ IPCondition.Global; ];
            }

        let k1 = HKiller() :> IKiller
        let p = new IscsiTCPSvPort( stat, rNP, k1 ) :> IPort

        let mutable count = 0
        stat.p_CreateLoginNegociator <-
            (
                fun w d x np ->
                    Assert.Fail __LINE__
                    Interlocked.Add( &count, 1 ) |> ignore
                    w.Close()
                    new CLoginNegociator_Stub()
            )

        Assert.True( p.Start() )
        Thread.Sleep 10

        use s = new TcpClient( "::1", portNo )
        s.Close()
        Assert.True(( count = 0 ))
        
        k1.NoticeTerminate()