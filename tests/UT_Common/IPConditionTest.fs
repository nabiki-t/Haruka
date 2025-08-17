namespace Haruka.Test.UT.Commons

open System
open System.Net

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test


type IPCondition_Test () =

    [<Fact>]
    member _.IPCondition_001 () =
        let r = IPCondition.Match ( IPAddress.Parse "192.168.1.1" ) Array.empty
        Assert.False r

    [<Fact>]
    member _.IPCondition_002 () =
        let r = IPCondition.Match ( IPAddress.Parse "192.168.1.1" ) [| IPCondition.IPv4Loopback; IPCondition.IPv4Private; |]
        Assert.True r

    [<Fact>]
    member _.IPCondition_003 () =
        let r = IPCondition.Match ( IPAddress.Parse "192.168.1.1" ) [| IPCondition.IPv4Loopback; IPCondition.IPv6Any; |]
        Assert.False r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_Any_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Any |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_Loopback_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Loopback |]
        Assert.False r

    [<Theory>]
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "::FFFF:127.0.0.1" )>]            // IPv4 Loopback ( IPv4 Mapped IPv6 )
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    member _.IPCondition_Loopback_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Loopback |]
        Assert.True r

    [<Theory>]
    [<InlineData( "169.254.0.1" )>]                 // IPv4 Linklocal
    [<InlineData( "::FFFF:169.254.0.1" )>]          // IPv4 Linklocal ( IPv4 Mapped IPv6 )
    [<InlineData( "fe80::1" )>]                     // IPv6 Linklocal
    member _.IPCondition_Linklocal_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Linklocal |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_Linklocal_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Linklocal |]
        Assert.False r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    member _.IPCondition_Private_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Private |]
        Assert.True r

    [<Theory>]
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "::FFFF:127.0.0.1" )>]            // IPv4 Loopback ( IPv4 Mapped IPv6 )
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    member _.IPCondition_Private_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Private |]
        Assert.False r

    [<Theory>]
    [<InlineData( "224.0.0.1" )>]                   // IPv4 Multicast
    [<InlineData( "::FFFF:224.0.0.1" )>]            // IPv4 Multicast ( IPv4 Mapped IPv6 )
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    member _.IPCondition_Multicast_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Multicast |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_Multicast_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Multicast |]
        Assert.False r

    [<Theory>]
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "::FFFF:111.22.33.44" )>]         // IPv4 Global ( IPv4 Mapped IPv6 )
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_Global_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Global |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    member _.IPCondition_Global_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.Global |]
        Assert.False r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    member _.IPCondition_IPv4Any_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Any |]
        Assert.True r

    [<Theory>]
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv4Any_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Any |]
        Assert.False r
    
    [<Theory>]
    [<InlineData( "127.0.0.0" )>]                   // IPv4 Loopback
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "127.255.255.254" )>]             // IPv4 Loopback
    [<InlineData( "127.255.255.255" )>]             // IPv4 Loopback
    [<InlineData( "::FFFF:127.0.0.1" )>]            // IPv4 Loopback ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Loopback_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Loopback |]
        Assert.True r

    [<Theory>]
    [<InlineData( "126.255.255.255" )>]
    [<InlineData( "128.0.0.0" )>]
    [<InlineData( "::FFFF:128.0.0.0" )>]
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv4Loopback_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Loopback |]
        Assert.False r

    [<Theory>]
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "169.254.255.255" )>]             // IPv4 Linklocal
    [<InlineData( "::FFFF:169.254.0.0" )>]          // IPv4 Linklocal  ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Linklocal_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Linklocal |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    [<InlineData( "169.253.255.255" )>]
    [<InlineData( "169.255.0.0" )>]
    member _.IPCondition_IPv4Linklocal_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Linklocal |]
        Assert.False r
    
    [<Theory>]
    [<InlineData( "10.0.0.0" )>]                    // IPv4 class A Private
    [<InlineData( "10.0.0.1" )>]                    // IPv4 class A Private
    [<InlineData( "10.255.255.254" )>]              // IPv4 class A Private
    [<InlineData( "10.255.255.255" )>]              // IPv4 class A Private
    [<InlineData( "::FFFF:10.0.0.1" )>]             // IPv4 class A Private ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Private_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.True r

    [<Theory>]
    [<InlineData( "9.255.255.255" )>]
    [<InlineData( "11.0.0.0" )>]
    [<InlineData( "::FFFF:9.255.255.255" )>]
    member _.IPCondition_IPv4Private_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.False r

    [<Theory>]
    [<InlineData( "172.16.0.0" )>]                  // IPv4 class B Private
    [<InlineData( "172.16.0.1" )>]                  // IPv4 class B Private
    [<InlineData( "172.31.255.254" )>]              // IPv4 class B Private
    [<InlineData( "172.31.255.255" )>]              // IPv4 class B Private
    [<InlineData( "::FFFF:172.16.0.1" )>]           // IPv4 class B Private ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Private_003 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.True r

    [<Theory>]
    [<InlineData( "172.15.255.255" )>]
    [<InlineData( "172.32.0.0" )>]
    [<InlineData( "::FFFF:172.15.255.255" )>]
    member _.IPCondition_IPv4Private_004 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.False r

    [<Theory>]
    [<InlineData( "192.168.0.0" )>]                  // IPv4 class C Private
    [<InlineData( "192.168.0.1" )>]                  // IPv4 class C Private
    [<InlineData( "192.168.255.254" )>]              // IPv4 class C Private
    [<InlineData( "192.168.255.255" )>]              // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.0.1" )>]           // IPv4 class C Private ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Private_005 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.167.255.255" )>]
    [<InlineData( "192.169.0.0" )>]
    [<InlineData( "::FFFF:192.167.255.255" )>]
    member _.IPCondition_IPv4Private_006 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.False r

    [<Theory>]
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv4Private_007 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Private |]
        Assert.False r

    [<Theory>]
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "239.255.255.255" )>]             // IPv4 Multicast
    [<InlineData( "::FFFF:224.0.0.0" )>]            // IPv4 Multicast ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Multicast_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Multicast |]
        Assert.True r

    [<Theory>]
    [<InlineData( "223.255.255.255" )>]
    [<InlineData( "240.0.0.0" )>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv4Multicast_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Multicast |]
        Assert.False r

    [<Theory>]
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "::FFFF:111.22.33.44" )>]         // IPv4 Global ( IPv4 Mapped IPv6 )
    member _.IPCondition_IPv4Global_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Global |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv4Global_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv4Global |]
        Assert.False r


    [<Theory>]
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv6Any_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Any |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    member _.IPCondition_IPv6Any_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Any |]
        Assert.False r

    [<Theory>]
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    member _.IPCondition_IPv6Loopback_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Loopback |]
        Assert.True r

    [<Theory>]
    [<InlineData( "::0" )>]
    [<InlineData( "::2" )>]
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    member _.IPCondition_IPv6Loopback_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Loopback |]
        Assert.False r

    [<Theory>]
    [<InlineData( "fe80::0" )>]                                 // IPv6 Linklocal
    [<InlineData( "fe80::1" )>]                                 // IPv6 Linklocal
    [<InlineData( "febf:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE" )>] // IPv6 Linklocal
    [<InlineData( "febf:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>] // IPv6 Linklocal
    member _.IPCondition_IPv6Linklocal_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Linklocal |]
        Assert.True r

    [<Theory>]
    [<InlineData( "fe7F:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE" )>]
    [<InlineData( "fec0::0" )>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv6Linklocal_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Linklocal |]
        Assert.False r

    [<Theory>]
    [<InlineData( "fc00::0" )>]                                 // IPv6 Private
    [<InlineData( "fc00::1" )>]                                 // IPv6 Private
    [<InlineData( "fcff:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE" )>] // IPv6 Private
    [<InlineData( "fcff:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>] // IPv6 Private
    [<InlineData( "fd00::0" )>]                                 // IPv6 Private
    [<InlineData( "fd00::1" )>]                                 // IPv6 Private
    [<InlineData( "fdff:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE" )>] // IPv6 Private
    [<InlineData( "fdff:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>] // IPv6 Private
    member _.IPCondition_IPv6Private_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Private |]
        Assert.True r

    [<Theory>]
    [<InlineData( "fbff:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "fe00::0" )>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv6Private_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Private |]
        Assert.False r

    [<Theory>]
    [<InlineData( "FF00::0" )>]                                 // IPv6 Multicast
    [<InlineData( "FF00::1" )>]                                 // IPv6 Multicast
    [<InlineData( "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE" )>] // IPv6 Multicast
    [<InlineData( "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>] // IPv6 Multicast
    member _.IPCondition_IPv6Multicast_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Multicast |]
        Assert.True r

    [<Theory>]
    [<InlineData( "FEFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "::0" )>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv6Multicast_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Multicast |]
        Assert.False r

    [<Theory>]
    [<InlineData( "2405:6582::FFFF:FFFF" )>]        // IPv6 Global
    member _.IPCondition_IPv6Global_001 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Global |]
        Assert.True r

    [<Theory>]
    [<InlineData( "192.168.1.1" )>]                 // IPv4 class C Private
    [<InlineData( "::FFFF:192.168.1.1" )>]          // IPv4 class C Private ( IPv4 Mapped IPv6 )
    [<InlineData( "172.16.1.1" )>]                  // IPv4 class B Private
    [<InlineData( "10.1.1.1" )>]                    // IPv4 class A Private
    [<InlineData( "127.0.0.1" )>]                   // IPv4 Loopback
    [<InlineData( "169.254.0.0" )>]                 // IPv4 Linklocal
    [<InlineData( "224.0.0.0" )>]                   // IPv4 Multicast
    [<InlineData( "11.22.33.44" )>]                 // IPv4 Global
    [<InlineData( "::FFFF:111.22.33.44" )>]         // IPv4 Global ( IPv4 Mapped IPv6 )
    [<InlineData( "fe80::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Linklocal
    [<InlineData( "fc00::FFFF:FFFF:FFFF:FFFF" )>]   // IPv6 Private
    [<InlineData( "::1" )>]                         // IPv6 Loopback
    [<InlineData( "FF00::1" )>]                     // IPv6 Multicast
    member _.IPCondition_IPv6Global_002 ( testadr : string ) =
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPv6Global |]
        Assert.False r

    [<Theory>]
    [<InlineData( "11.22.0.0", "255.255.0.0", "11.22.0.0" )>]
    [<InlineData( "11.22.0.0", "255.255.0.0", "11.22.255.255" )>]
    [<InlineData( "123.234.88.99", "255.255.0.0", "123.234.0.0" )>]
    [<InlineData( "123.234.88.99", "255.255.0.0", "123.234.255.255" )>]
    [<InlineData( "11.22.33.44", "0.0.0.0", "44.55.66.77" )>]
    [<InlineData( "85.170.85.170", "128.0.0.0", "127.255.255.255" )>]
    [<InlineData( "170.85.170.85", "128.0.0.0", "128.0.0.0" )>]
    [<InlineData( "85.170.85.170", "255.255.255.254", "85.170.85.170" )>]
    [<InlineData( "85.170.85.170", "255.255.255.254", "85.170.85.171" )>]
    [<InlineData( "85.170.85.170", "255.255.255.255", "85.170.85.170" )>]
    [<InlineData( "11.22.0.0", "255.255.0.0", "::FFFF:11.22.0.0" )>]
    [<InlineData( "11.22.0.0", "::FFFF:255.255.0.0", "11.22.0.0" )>]
    [<InlineData( "::FFFF:11.22.0.0", "255.255.0.0", "11.22.0.0" )>]
    member _.IPCondition_IPFilter_001 ( filteradr : string, filtermask : string, testadr : string ) =
        let filterip = IPAddress.Parse filteradr
        let filterbytes = IPCondition.AdrToBytes filterip
        let maskip = IPAddress.Parse filtermask
        let maskbytes = IPCondition.AdrToBytes maskip
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPFilter( filterbytes, maskbytes ) |]
        Assert.True r

    [<Theory>]
    [<InlineData( "11.22.0.0", "255.255.0.0", "11.21.255.255" )>]
    [<InlineData( "11.22.0.0", "255.255.0.0", "11.23.0.0" )>]
    [<InlineData( "123.234.88.99", "255.255.0.0", "123.233.255.255" )>]
    [<InlineData( "123.234.88.99", "255.255.0.0", "123.235.0.0" )>]
    [<InlineData( "85.170.85.170", "128.0.0.0", "128.0.0.0" )>]
    [<InlineData( "170.85.170.85", "128.0.0.0", "127.255.255.255" )>]
    [<InlineData( "85.170.85.170", "255.255.255.254", "85.170.85.169" )>]
    [<InlineData( "85.170.85.170", "255.255.255.254", "85.170.85.172" )>]
    [<InlineData( "85.170.85.170", "255.255.255.255", "85.170.85.169" )>]
    [<InlineData( "85.170.85.170", "255.255.255.255", "85.170.85.171" )>]
    [<InlineData( "11.22.0.0", "255.255.0.0", "::FFFF:11.21.255.255" )>]
    [<InlineData( "11.22.0.0", "::FFFF:255.255.0.0", "11.21.255.255" )>]
    [<InlineData( "::FFFF:11.22.0.0", "255.255.0.0", "11.21.255.255" )>]
    member _.IPCondition_IPFilter_002 ( filteradr : string, filtermask : string, testadr : string ) =
        let filterip = IPAddress.Parse filteradr
        let filterbytes = filterip.GetAddressBytes()
        let maskip = IPAddress.Parse filtermask
        let maskbytes = maskip.GetAddressBytes()
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPFilter( filterbytes, maskbytes ) |]
        Assert.False r

    [<Theory>]
    [<InlineData( "1111:2222:3333:4444::0", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4444::0" )>]
    [<InlineData( "1111:2222:3333:4444::0", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4444:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "1111:2222:3333:4444:5555:6666:7777:8888", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4444::0" )>]
    [<InlineData( "1111:2222:3333:4444:5555:6666:7777:8888", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4444:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "1111:2222:3333:4444:5555:6666:7777:8888", "::0", "AAAA:BBBB:CCCC:DDDD:EEEE:FFFF:1111:2222" )>]
    [<InlineData( "5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A", "8000::0", "::0" )>]
    [<InlineData( "5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A", "8000::0", "7FFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "8000::0", "8000::0" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "8000::0", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A4" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5" )>]
    member _.IPCondition_IPFilter_003 ( filteradr : string, filtermask : string, testadr : string ) =
        let filterip = IPAddress.Parse filteradr
        let filterbytes = filterip.GetAddressBytes()
        let maskip = IPAddress.Parse filtermask
        let maskbytes = maskip.GetAddressBytes()
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPFilter( filterbytes, maskbytes ) |]
        Assert.True r

    [<Theory>]
    [<InlineData( "1111:2222:3333:4444::0", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4443:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "1111:2222:3333:4444::0", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4445::0" )>]
    [<InlineData( "1111:2222:3333:4444:5555:6666:7777:8888", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4443:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "1111:2222:3333:4444:5555:6666:7777:8888", "FFFF:FFFF:FFFF:FFFF::0", "1111:2222:3333:4445::0" )>]
    [<InlineData( "5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A", "8000::0", "8000::0" )>]
    [<InlineData( "5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A:5A5A", "8000::0", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "8000::0", "::0" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "8000::0", "7FFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A3" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFE", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A6" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A4" )>]
    [<InlineData( "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5", "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", "A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A5:A5A6" )>]
    member _.IPCondition_IPFilter_004 ( filteradr : string, filtermask : string, testadr : string ) =
        let filterip = IPAddress.Parse filteradr
        let filterbytes = filterip.GetAddressBytes()
        let maskip = IPAddress.Parse filtermask
        let maskbytes = maskip.GetAddressBytes()
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPFilter( filterbytes, maskbytes ) |]
        Assert.False r

    [<Theory>]
    [<InlineData( "111.222.33.44", "255.255.0.0", "6FDE::0" )>]
    [<InlineData( "111.222.33.44", "FFFF::0", "111.222.33.44" )>]
    [<InlineData( "111.222.33.44", "FFFF::0", "6FDE::0" )>]
    [<InlineData( "6FDE:212C::0", "255.255.0.0", "111.222.33.44" )>]
    [<InlineData( "6FDE:212C::0", "255.255.0.0", "6FDE::0" )>]
    [<InlineData( "6FDE:212C::0", "FFFF::0", "111.222.33.44" )>]
    member _.IPCondition_IPFilter_005 ( filteradr : string, filtermask : string, testadr : string ) =
        let filterip = IPAddress.Parse filteradr
        let filterbytes = filterip.GetAddressBytes()
        let maskip = IPAddress.Parse filtermask
        let maskbytes = maskip.GetAddressBytes()
        let r = IPCondition.Match ( IPAddress.Parse testadr ) [| IPCondition.IPFilter( filterbytes, maskbytes ) |]
        Assert.False r

    [<Fact>]
    member _.IPCondition_Parse_001 () =
        let a = IPCondition.Parse "Any"
        Assert.True(( a = IPCondition.Any ))

    [<Fact>]
    member _.IPCondition_Parse_002 () =
        let a = IPCondition.Parse "Loopback"
        Assert.True(( a = IPCondition.Loopback ))

    [<Fact>]
    member _.IPCondition_Parse_003 () =
        let a = IPCondition.Parse "Linklocal"
        Assert.True(( a = IPCondition.Linklocal ))

    [<Fact>]
    member _.IPCondition_Parse_004 () =
        let a = IPCondition.Parse "Private"
        Assert.True(( a = IPCondition.Private ))

    [<Fact>]
    member _.IPCondition_Parse_005 () =
        let a = IPCondition.Parse "Multicast"
        Assert.True(( a = IPCondition.Multicast ))

    [<Fact>]
    member _.IPCondition_Parse_006 () =
        let a = IPCondition.Parse "Global"
        Assert.True(( a = IPCondition.Global ))

    [<Fact>]
    member _.IPCondition_Parse_007 () =
        let a = IPCondition.Parse "IPv4Any"
        Assert.True(( a = IPCondition.IPv4Any ))

    [<Fact>]
    member _.IPCondition_Parse_008 () =
        let a = IPCondition.Parse "IPv4Loopback"
        Assert.True(( a = IPCondition.IPv4Loopback ))

    [<Fact>]
    member _.IPCondition_Parse_009 () =
        let a = IPCondition.Parse "IPv4Linklocal"
        Assert.True(( a = IPCondition.IPv4Linklocal ))

    [<Fact>]
    member _.IPCondition_Parse_010 () =
        let a = IPCondition.Parse "IPv4Private"
        Assert.True(( a = IPCondition.IPv4Private ))

    [<Fact>]
    member _.IPCondition_Parse_011 () =
        let a = IPCondition.Parse "IPv4Multicast"
        Assert.True(( a = IPCondition.IPv4Multicast ))

    [<Fact>]
    member _.IPCondition_Parse_012 () =
        let a = IPCondition.Parse "IPv4Global"
        Assert.True(( a = IPCondition.IPv4Global ))

    [<Fact>]
    member _.IPCondition_Parse_013 () =
        let a = IPCondition.Parse "IPv6Any"
        Assert.True(( a = IPCondition.IPv6Any ))

    [<Fact>]
    member _.IPCondition_Parse_014 () =
        let a = IPCondition.Parse "IPv6Loopback"
        Assert.True(( a = IPCondition.IPv6Loopback ))

    [<Fact>]
    member _.IPCondition_Parse_015 () =
        let a = IPCondition.Parse "IPv6Linklocal"
        Assert.True(( a = IPCondition.IPv6Linklocal ))

    [<Fact>]
    member _.IPCondition_Parse_016 () =
        let a = IPCondition.Parse "IPv6Private"
        Assert.True(( a = IPCondition.IPv6Private ))

    [<Fact>]
    member _.IPCondition_Parse_017 () =
        let a = IPCondition.Parse "IPv6Multicast"
        Assert.True(( a = IPCondition.IPv6Multicast ))

    [<Fact>]
    member _.IPCondition_Parse_018 () =
        let a = IPCondition.Parse "IPv6Global"
        Assert.True(( a = IPCondition.IPv6Global ))

    [<Theory>]
    [<InlineData( "IPFilter( 192.168.1.1, 255.255.255.0 )", "192.168.1.1", "255.255.255.0" )>]
    [<InlineData( "IPFilter( 192.168.1.1, ::FFFF:255.255.255.0 )", "192.168.1.1", "255.255.255.0" )>]
    [<InlineData( "IPFilter( ::FFFF:192.168.1.1, 255.255.255.0 )", "192.168.1.1", "255.255.255.0" )>]
    [<InlineData( "IPFilter( 1:2:3:4:5:6:7:8, FFFF:FFFF:FFFF::0 )", "1:2:3:4:5:6:7:8", "ffff:ffff:ffff::" )>]
    member _.IPCondition_Parse_019 ( inputstr : string, result1 : string, result2 : string ) =
        let a = IPCondition.Parse inputstr
        match a with
        | IPCondition.IPFilter( v1, v2 ) ->
            Assert.True(( ( IPAddress v1 ).ToString() = result1 ))
            Assert.True(( ( IPAddress v2 ).ToString() = result2 ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( "IPFilter( FFFF:FFFF:FFFF::0, 255.255.0.0 )" )>]
    [<InlineData( "IPFilter( FFFF:FFFF:FFFF::0, ::FFFF:255.255.0.0 )" )>]
    [<InlineData( "IPFilter( 255.255.0.0, FFFF:FFFF:FFFF::0 )" )>]
    [<InlineData( "IPFilter( ::FFFF:255.255.0.0, FFFF:FFFF:FFFF::0 )" )>]
    [<InlineData( "IPFilter( abc, 255.255.0.0 )" )>]
    [<InlineData( "IPFilter( 255.255.0.0, aaa )" )>]
    [<InlineData( "aaaa" )>]
    member _.IPCondition_Parse_020 ( inputstr : string ) =
        try
            IPCondition.Parse inputstr |> ignore
            Assert.Fail __LINE__
        with
        | :? FormatException ->
            ()

    [<Fact>]
    member _.IPCondition_ToString_001 () =
        let a = IPCondition.ToString IPCondition.Any
        Assert.True(( a = "Any" ))

    [<Fact>]
    member _.IPCondition_ToString_002 () =
        let a = IPCondition.ToString IPCondition.Loopback
        Assert.True(( a = "Loopback" ))

    [<Fact>]
    member _.IPCondition_ToString_003 () =
        let a = IPCondition.ToString IPCondition.Linklocal
        Assert.True(( a = "Linklocal" ))

    [<Fact>]
    member _.IPCondition_ToString_004 () =
        let a = IPCondition.ToString IPCondition.Private
        Assert.True(( a = "Private" ))

    [<Fact>]
    member _.IPCondition_ToString_005 () =
        let a = IPCondition.ToString IPCondition.Multicast
        Assert.True(( a = "Multicast" ))

    [<Fact>]
    member _.IPCondition_ToString_006 () =
        let a = IPCondition.ToString IPCondition.Global
        Assert.True(( a = "Global" ))

    [<Fact>]
    member _.IPCondition_ToString_007 () =
        let a = IPCondition.ToString IPCondition.IPv4Any
        Assert.True(( a = "IPv4Any" ))

    [<Fact>]
    member _.IPCondition_ToString_008 () =
        let a = IPCondition.ToString IPCondition.IPv4Loopback
        Assert.True(( a = "IPv4Loopback" ))

    [<Fact>]
    member _.IPCondition_ToString_009 () =
        let a = IPCondition.ToString IPCondition.IPv4Linklocal
        Assert.True(( a = "IPv4Linklocal" ))

    [<Fact>]
    member _.IPCondition_ToString_010 () =
        let a = IPCondition.ToString IPCondition.IPv4Private
        Assert.True(( a = "IPv4Private" ))

    [<Fact>]
    member _.IPCondition_ToString_011 () =
        let a = IPCondition.ToString IPCondition.IPv4Multicast
        Assert.True(( a = "IPv4Multicast" ))

    [<Fact>]
    member _.IPCondition_ToString_012 () =
        let a = IPCondition.ToString IPCondition.IPv4Global
        Assert.True(( a = "IPv4Global" ))

    [<Fact>]
    member _.IPCondition_ToString_013 () =
        let a = IPCondition.ToString IPCondition.IPv6Any
        Assert.True(( a = "IPv6Any" ))

    [<Fact>]
    member _.IPCondition_ToString_014 () =
        let a = IPCondition.ToString IPCondition.IPv6Loopback
        Assert.True(( a = "IPv6Loopback" ))

    [<Fact>]
    member _.IPCondition_ToString_015 () =
        let a = IPCondition.ToString IPCondition.IPv6Linklocal
        Assert.True(( a = "IPv6Linklocal" ))

    [<Fact>]
    member _.IPCondition_ToString_016 () =
        let a = IPCondition.ToString IPCondition.IPv6Private
        Assert.True(( a = "IPv6Private" ))

    [<Fact>]
    member _.IPCondition_ToString_017 () =
        let a = IPCondition.ToString IPCondition.IPv6Multicast
        Assert.True(( a = "IPv6Multicast" ))

    [<Fact>]
    member _.IPCondition_ToString_018 () =
        let a = IPCondition.ToString IPCondition.IPv6Global
        Assert.True(( a = "IPv6Global" ))

    [<Theory>]
    [<InlineData( "192.168.1.1", "255.255.0.0", "IPFilter( 192.168.1.1, 255.255.0.0 )" )>]
    [<InlineData( "::FFFF:192.168.1.1", "::FFFF:255.255.0.0", "IPFilter( ::ffff:192.168.1.1, ::ffff:255.255.0.0 )" )>]
    [<InlineData( "::FFFF:192.168.1.1", "255.255.0.0", "IPFilter( ::ffff:192.168.1.1, 255.255.0.0 )" )>]
    [<InlineData( "192.168.1.1", "::FFFF:255.255.0.0", "IPFilter( 192.168.1.1, ::ffff:255.255.0.0 )" )>]
    [<InlineData( "1111:2222::0", "FFFF:FFFF::0", "IPFilter( 1111:2222::, ffff:ffff:: )" )>]
    member _.IPCondition_ToString_019 ( adr : string, mask : string, result : string ) =
        let filterip = IPAddress.Parse adr
        let filterbytes = filterip.GetAddressBytes()
        let maskip = IPAddress.Parse mask
        let maskbytes = maskip.GetAddressBytes()
        let a = IPCondition.ToString( IPCondition.IPFilter( filterbytes, maskbytes ) )
        Assert.True(( a = result ))

    [<Fact>]
    member _.IPCondition_ToString_020 () =
        try
            let _ = IPCondition.ToString( IPCondition.IPFilter( [| 0uy; 0uy; 0uy |], Array.empty ) )
            Assert.Fail __LINE__
        with
        | :? ArgumentException ->
            ()

    [<Theory>]
    [<InlineData( "111.222.33.44", "::1" )>]
    [<InlineData( "::FFFF:111.222.33.44", "::1" )>]
    [<InlineData( "::1", "111.222.33.44" )>]
    [<InlineData( "::1", "::FFFF:111.222.33.44" )>]
    member _.IPCondition_ToString_021 ( adr : string, mask : string ) =
        try
            let filterip = IPAddress.Parse adr
            let filterbytes = filterip.GetAddressBytes()
            let maskip = IPAddress.Parse mask
            let maskbytes = maskip.GetAddressBytes()
            let _ = IPCondition.ToString( IPCondition.IPFilter( filterbytes, maskbytes ) )
            Assert.Fail __LINE__
        with
        | :? FormatException ->
            ()
