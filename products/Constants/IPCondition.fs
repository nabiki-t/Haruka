//=============================================================================
// Haruka Software Storage.
// IPCondition.fs : Defines IPCondition class that is used to describe constraints on IP addresses.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Constants

//=============================================================================
// Import declaration

open System
open System.Net
open System.Net.Sockets

//=============================================================================
// Type definition

/// Specify the constraints that an IP address must meet.
type IPCondition =
    /// Any of IP address
    | Any
    /// IPv4 Loopback or IPv6 Loopback
    | Loopback
    /// IPv4 Linklocal or IPv6 Linklocal
    | Linklocal
    /// IPv4 Private or IPv6 Private
    | Private
    /// IPv4 Multicast or IPv6 Multicast
    | Multicast
    /// Anything other than Loopback, Linklocal, Private, or Multicast
    | Global
    /// Any of IPv4 address
    | IPv4Any
    /// IPv4 Loopback address ( 127.0.0.0/8 )
    | IPv4Loopback
    /// IPv4 Linklocal address ( 169.254.0.0/16 )
    | IPv4Linklocal
    /// IPv4 Private address ( 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16 )
    | IPv4Private
    /// IPv4 Multicast address ( 224.0.0.0/4 )
    | IPv4Multicast
    /// An IPv4 address that is not a Loopback, Linklocal, Private, or Multicast
    | IPv4Global
    /// Any of IPv6 address
    | IPv6Any
    /// IPv6 Loopback address ( ::1 )
    | IPv6Loopback
    /// IPv6 Linklocal address ( fe80::/10 )
    | IPv6Linklocal
    /// IPv6 Private address ( fc00::/7 )
    | IPv6Private
    /// IPv6 Multicast address ( ff00::/8 )
    | IPv6Multicast
    /// An IPv6 address that is not a Loopback, Linklocal, Private, or Multicast
    | IPv6Global
    /// Specify any address and mask
    | IPFilter of ( byte[] * byte[] )

    /// <summary>
    ///  Compare the contents of the byte array with the mask.
    /// </summary>
    /// <param name="v1">bytes array 1.</param>
    /// <param name="v2">bytes array 2.</param>
    /// <param name="m">mask</param>
    /// <returns>
    ///  If ( v1 and m ) equals ( v2 and m ), it returns true. Otherwise false.
    ///  If the length of specified array is not match, it returns false.
    /// </returns>
    static member private MatchBytes ( v1 : byte[] ) ( v2 : byte[] ) ( m : byte[] ) : bool =
        if v1.Length <> v2.Length || v1.Length <> m.Length then
            false
        else
            Array.map3 ( fun a b c -> ( a &&& c ) = ( b &&& c ) ) v1 v2 m
            |> Array.contains false
            |> not

    /// <summary>
    ///  Get address bytes from IPAddress
    /// </summary>
    /// <param name="argA">
    ///  IPAddress
    /// </param>
    /// <returns>
    ///  Convertes bytes array.
    /// </returns>
    /// <remarks>
    ///  If specified IP address is IPv4 mapped IPv6 address, it returns simple IPv4 address.
    ///  ex. "::FFFF:192.168.1.1" is converted [| 192uy; 168uy; 1uy; 1uy |]
    /// </remarks>
    static member AdrToBytes ( argA : IPAddress ) : byte[] =
        if argA.AddressFamily = AddressFamily.InterNetworkV6 && argA.IsIPv4MappedToIPv6 then
            argA.GetAddressBytes() |> Array.skip 12 
        else
            argA.GetAddressBytes()

    /// <summary>
    ///  Determine whether the specified IP address matches the specified condition.
    /// </summary>
    /// <param name="a">
    ///  IP address
    /// </param>
    /// <param name="c">
    ///  Condition
    /// </param>
    /// <returns>
    ///  If IP address "a" matches condition "c", it returns true. Otherwise false.
    /// </returns>
    /// <remarks>
    ///  <code>
    ///   > MatchIPCondition ( IPAddress.Parse "127.0.0.1" ) ( IPCondition.IPv4Loopback );;
    ///   val it: bool = true
    ///   > MatchIPCondition ( IPAddress.Parse "192.168.1.1" ) ( IPCondition.IPv6Private );;
    ///   val it: bool = false
    ///  </code>
    /// </remarks>
    static member private MatchIPCondition ( a : IPAddress ) ( c : IPCondition ) : bool =

        let isIPv4Loopback ( argA : IPAddress ) =
            let Ipaddr = [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; |]
            let Ipmask = [| 0xFFuy; 0x00uy; 0x00uy; 0x00uy; |];
            IPCondition.MatchBytes ( IPCondition.AdrToBytes argA ) Ipaddr Ipmask

        let isIPv4Linklocal ( argA : IPAddress ) =
            let Ipaddr = [| 0xA9uy; 0xFEuy; 0x00uy; 0x00uy; |]
            let Ipmask = [| 0xFFuy; 0xFFuy; 0x00uy; 0x00uy; |];
            IPCondition.MatchBytes ( IPCondition.AdrToBytes argA ) Ipaddr Ipmask

        let ipIPv4Private ( argA : IPAddress ) =
            let aaddr = [| 0x0Auy; 0x00uy; 0x00uy; 0x00uy; |]
            let amask = [| 0xFFuy; 0x00uy; 0x00uy; 0x00uy; |]
            let baddr = [| 0xACuy; 0x10uy; 0x00uy; 0x00uy; |]
            let bmask = [| 0xFFuy; 0xF0uy; 0x00uy; 0x00uy; |]
            let caddr = [| 0xC0uy; 0xA8uy; 0x00uy; 0x00uy; |]
            let cmask = [| 0xFFuy; 0xFFuy; 0x00uy; 0x00uy; |]
            let adrb = IPCondition.AdrToBytes argA
            ( IPCondition.MatchBytes adrb aaddr amask ) || ( IPCondition.MatchBytes adrb baddr bmask ) || ( IPCondition.MatchBytes adrb caddr cmask )

        let isIPv4Multicast ( argA : IPAddress ) =
            let Ipaddr = [| 0xE0uy; 0x00uy; 0x00uy; 0x00uy; |]
            let Ipmask = [| 0xF0uy; 0x00uy; 0x00uy; 0x00uy; |];
            IPCondition.MatchBytes ( IPCondition.AdrToBytes argA ) Ipaddr Ipmask

        let isIPv6Loopback ( argA : IPAddress ) =
            let adrb = argA.GetAddressBytes()
            let Ipaddr = [| yield! Seq.replicate 15 0uy; yield 0x01uy |]
            let Ipmask = Array.replicate 16 0xFFuy
            IPCondition.MatchBytes adrb Ipaddr Ipmask

        let isIPv6Private ( argA : IPAddress ) =
            let adrb = argA.GetAddressBytes()
            let Ipaddr = [| yield 0xFCuy; yield! Seq.replicate 15 0uy |]
            let Ipmask = [| yield 0xFEuy; yield! Seq.replicate 15 0uy |]
            IPCondition.MatchBytes adrb Ipaddr Ipmask

        let isIPv6Multicast ( argA : IPAddress ) =
            let adrb = argA.GetAddressBytes()
            let Ipaddr = [| yield 0xFFuy; yield! Seq.replicate 15 0uy |]
            let Ipmask = [| yield 0xFFuy; yield! Seq.replicate 15 0uy |]
            IPCondition.MatchBytes adrb Ipaddr Ipmask

        match c with
        | IPCondition.Any ->
            true
        | IPCondition.Loopback ->
            ( isIPv4Loopback a ) || ( isIPv6Loopback a )
        | IPCondition.Linklocal ->
            ( isIPv4Linklocal a ) || ( a.IsIPv6LinkLocal )
        | IPCondition.Private ->
            ( ipIPv4Private a ) || ( isIPv6Private a )
        | IPCondition.Multicast ->
            ( isIPv4Multicast a ) || ( isIPv6Multicast a )
        | IPCondition.Global ->
            not (   ( isIPv4Loopback a ) || ( isIPv4Linklocal a ) || ( ipIPv4Private a ) || ( isIPv4Multicast a ) ||
                    ( isIPv6Loopback a ) || ( a.IsIPv6LinkLocal ) || ( isIPv6Private a ) || ( isIPv6Multicast a ) )
        | IPCondition.IPv4Any ->
            a.AddressFamily = AddressFamily.InterNetwork || a.IsIPv4MappedToIPv6
        | IPCondition.IPv4Loopback ->
            isIPv4Loopback a
        | IPCondition.IPv4Linklocal ->
            isIPv4Linklocal a
        | IPCondition.IPv4Private ->
            ipIPv4Private a
        | IPCondition.IPv4Multicast ->
            isIPv4Multicast a
        | IPCondition.IPv4Global ->
            ( a.AddressFamily = AddressFamily.InterNetwork || a.IsIPv4MappedToIPv6 ) &&
            ( not ( ( isIPv4Loopback a ) || ( isIPv4Linklocal a ) || ( ipIPv4Private a ) || ( isIPv4Multicast a ) ) )
        | IPCondition.IPv6Any ->
            a.AddressFamily = AddressFamily.InterNetworkV6 && ( not a.IsIPv4MappedToIPv6 )
        | IPCondition.IPv6Loopback ->
            isIPv6Loopback a
        | IPCondition.IPv6Linklocal ->
            a.IsIPv6LinkLocal
        | IPCondition.IPv6Private ->
            isIPv6Private a
        | IPCondition.IPv6Multicast ->
            isIPv6Multicast a
        | IPCondition.IPv6Global ->
            ( a.AddressFamily = AddressFamily.InterNetworkV6 && ( not a.IsIPv4MappedToIPv6 ) ) &&
            ( not ( ( isIPv6Loopback a ) || ( a.IsIPv6LinkLocal ) || ( isIPv6Private a ) || ( isIPv6Multicast a ) ) )
        | IPCondition.IPFilter ( fadr, mask ) ->
            let fadrb = fadr
            let maskb = mask
            let adrb = IPCondition.AdrToBytes a
            IPCondition.MatchBytes adrb fadrb maskb

    /// <summary>
    ///  Check the specified IP address matches some conditions.
    /// </summary>
    /// <param name="a">IP address.</param>
    /// <param name="cond">Conditions</param>
    /// <returns>
    ///  If matched condition is exist in conditions specified at 'cond', it returns true.
    ///  Otherwise false.
    /// </returns>
    static member Match ( a : IPAddress ) ( cond : IPCondition seq ) : bool =
        cond
        |> Seq.tryFind ( IPCondition.MatchIPCondition a )
        |> Option.isSome

    /// <summary>
    ///  Parse input string to IPCondition value.
    /// </summary>
    /// <param name="s">
    ///  Input string
    /// </param>
    /// <returns>
    ///  Converted IPCondition value.
    /// </returns>
    /// <exceptions>
    ///  If invalid string was specified, it raise FormatException.
    /// </exceptions>
    static member Parse ( s : string ) : IPCondition =
        match s with
        | "Any" ->
            IPCondition.Any
        | "Loopback" ->
            IPCondition.Loopback
        | "Linklocal" ->
            IPCondition.Linklocal
        | "Private" ->
            IPCondition.Private
        | "Multicast" ->
            IPCondition.Multicast
        | "Global" ->
            IPCondition.Global
        | "IPv4Any" ->
            IPCondition.IPv4Any
        | "IPv4Loopback" ->
            IPCondition.IPv4Loopback
        | "IPv4Linklocal" ->
            IPCondition.IPv4Linklocal
        | "IPv4Private" ->
            IPCondition.IPv4Private
        | "IPv4Multicast" ->
            IPCondition.IPv4Multicast
        | "IPv4Global" ->
            IPCondition.IPv4Global
        | "IPv6Any" ->
            IPCondition.IPv6Any
        | "IPv6Loopback" ->
            IPCondition.IPv6Loopback
        | "IPv6Linklocal" ->
            IPCondition.IPv6Linklocal
        | "IPv6Private" ->
            IPCondition.IPv6Private
        | "IPv6Multicast" ->
            IPCondition.IPv6Multicast
        | "IPv6Global" ->
            IPCondition.IPv6Global
        | x ->
            let m = System.Text.RegularExpressions.Regex( "^IPFilter\( *([^ ,\)]{1,}) *, *([^ ,\)]{1,}) *\)$" ).Match( x )
            if not m.Success then
                raise <| System.FormatException( "Unexpected IPFilter strings." )
            let ip1 = IPAddress.Parse( m.Groups.[1].Value )
            let ip2 = IPAddress.Parse( m.Groups.[2].Value )
            if ( ip1.AddressFamily = AddressFamily.InterNetwork || ip1.IsIPv4MappedToIPv6 ) &&
                not ( ip2.AddressFamily = AddressFamily.InterNetwork || ip2.IsIPv4MappedToIPv6 ) then
                    raise <| System.FormatException( "Unexpected IPFilter strings." )
            if ( ip1.AddressFamily = AddressFamily.InterNetworkV6 && ( not ip1.IsIPv4MappedToIPv6 ) ) &&
                not ( ip2.AddressFamily = AddressFamily.InterNetworkV6 && ( not ip2.IsIPv4MappedToIPv6 ) ) then
                    raise <| System.FormatException( "Unexpected IPFilter strings." )

            IPCondition.IPFilter( IPCondition.AdrToBytes ip1, IPCondition.AdrToBytes ip2 )

    /// <summary>
    ///  Parse user input string to IPCondition value.
    /// </summary>
    /// <param name="s">
    ///  Input string. It must be from "Any" to "IPv6Global". IPFilter is not supportted.
    /// </param>
    /// <returns>
    ///  Converted IPCondition value. If invalid value has been specified, it returns None.
    /// </returns>
    static member ParseUserInput ( s : string ) : IPCondition voption =
        match s.ToUpperInvariant() with
        | "ANY" ->
            ValueSome IPCondition.Any
        | "LOOPBACK" ->
            ValueSome IPCondition.Loopback
        | "LINKLOCAL" ->
            ValueSome IPCondition.Linklocal
        | "PRIVATE" ->
            ValueSome IPCondition.Private
        | "MULTICAST" ->
            ValueSome IPCondition.Multicast
        | "GLOBAL" ->
            ValueSome IPCondition.Global
        | "IPV4ANY" ->
            ValueSome IPCondition.IPv4Any
        | "IPV4LOOPBACK" ->
            ValueSome IPCondition.IPv4Loopback
        | "IPV4LINKLOCAL" ->
            ValueSome IPCondition.IPv4Linklocal
        | "IPv$PRIVATE" ->
            ValueSome IPCondition.IPv4Private
        | "IPV4MULTICAST" ->
            ValueSome IPCondition.IPv4Multicast
        | "IPV4GLOBAL" ->
            ValueSome IPCondition.IPv4Global
        | "IPV6ANY" ->
            ValueSome IPCondition.IPv6Any
        | "IPV6LOOPBACK" ->
            ValueSome IPCondition.IPv6Loopback
        | "IPV6LINKLOCAL" ->
            ValueSome IPCondition.IPv6Linklocal
        | "IPV6PRIVATE" ->
            ValueSome IPCondition.IPv6Private
        | "IPV6MULTICAST" ->
            ValueSome IPCondition.IPv6Multicast
        | "IPV6GLOBAL" ->
            ValueSome IPCondition.IPv6Global
        | x ->
            ValueNone

    /// <summary>
    ///  Parse user input string to IPCondition value.
    /// </summary>
    /// <param name="adr">
    ///  Input string. It must be IPv4 adderess or IPv6 address.
    /// </param>
    /// <param name="mask">
    ///  Input string. It must be IPv4 adderess mask pattern or IPv6 address mask pattern.
    /// </param>
    /// <returns>
    ///  Converted IPCondition value. If invalid value has been specified, it returns None.
    /// </returns>
    static member ParseUserInput ( adr : string, mask : string ) : IPCondition voption =
        let ip1flg, ip1 = IPAddress.TryParse adr
        let ip2flg, ip2 = IPAddress.TryParse mask
        if not ip1flg || not ip2flg then
            ValueNone
        elif ( ip1.AddressFamily = AddressFamily.InterNetwork || ip1.IsIPv4MappedToIPv6 ) &&
            not ( ip2.AddressFamily = AddressFamily.InterNetwork || ip2.IsIPv4MappedToIPv6 ) then
                ValueNone
        elif ( ip1.AddressFamily = AddressFamily.InterNetworkV6 && ( not ip1.IsIPv4MappedToIPv6 ) ) &&
            not ( ip2.AddressFamily = AddressFamily.InterNetworkV6 && ( not ip2.IsIPv4MappedToIPv6 ) ) then
                ValueNone
        else
            ValueSome( IPCondition.IPFilter( IPCondition.AdrToBytes ip1, IPCondition.AdrToBytes ip2 ) )

    /// <summary>
    ///  Convert IPCondition value to string..
    /// </summary>
    /// <param name="ipv">
    ///  IPCondition value
    /// </param>
    /// <returns>
    ///  Converted string value.
    /// </returns>
    /// <exceptions>
    ///  If invalid value was specified, it raise FormatException or ArgumentException.
    /// </exceptions>
    static member ToString ( ipv : IPCondition ) : string =
        match ipv with
        | IPCondition.Any ->
            "Any"
        | IPCondition.Loopback ->
            "Loopback"
        | IPCondition.Linklocal ->
            "Linklocal"
        | IPCondition.Private ->
            "Private"
        | IPCondition.Multicast ->
            "Multicast"
        | IPCondition.Global ->
            "Global"
        | IPCondition.IPv4Any ->
            "IPv4Any"
        | IPCondition.IPv4Loopback ->
            "IPv4Loopback"
        | IPCondition.IPv4Linklocal ->
            "IPv4Linklocal"
        | IPCondition.IPv4Private ->
            "IPv4Private"
        | IPCondition.IPv4Multicast ->
            "IPv4Multicast"
        | IPCondition.IPv4Global ->
            "IPv4Global"
        | IPCondition.IPv6Any ->
            "IPv6Any"
        | IPCondition.IPv6Loopback ->
            "IPv6Loopback"
        | IPCondition.IPv6Linklocal ->
            "IPv6Linklocal"
        | IPCondition.IPv6Private ->
            "IPv6Private"
        | IPCondition.IPv6Multicast ->
            "IPv6Multicast"
        | IPCondition.IPv6Global ->
            "IPv6Global"
        | IPCondition.IPFilter( bv1, bv2 ) ->
            let ip1 = IPAddress bv1
            let ip2 = IPAddress bv2
            if ( ip1.AddressFamily = AddressFamily.InterNetwork || ip1.IsIPv4MappedToIPv6 ) &&
                not ( ip2.AddressFamily = AddressFamily.InterNetwork || ip2.IsIPv4MappedToIPv6 ) then
                    raise <| System.FormatException( "Address family mismatch." )
            if ( ip1.AddressFamily = AddressFamily.InterNetworkV6 && ( not ip1.IsIPv4MappedToIPv6 ) ) &&
                not ( ip2.AddressFamily = AddressFamily.InterNetworkV6 && ( not ip2.IsIPv4MappedToIPv6 ) ) then
                    raise <| System.FormatException( "Address family mismatch." )
            sprintf "IPFilter( %s, %s )" ( ip1.ToString() ) ( ip2.ToString() )

    /// <summary>
    ///  convert ot string.
    /// </summary>
    override this.ToString() : string =
        IPCondition.ToString this
