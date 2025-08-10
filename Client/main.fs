//=============================================================================
// Haruka Software Storage.
// main.fs : Harula client main module.
//

//=============================================================================
// module name

module Haruka.Client.main

//=============================================================================
// Import declaration


open System
open System.Text
open System.IO

open Haruka.Constants
open Haruka.Commons

/// <summary>
///  Show command instruction text.
/// </summary>
let help() : unit =
    fprintfn stderr "%s. Version %d.%d.%d" Constants.PRODUCT_DESCRIPTION Constants.MAJOR_VERSION Constants.MINOR_VERSION Constants.PRODUCT_RIVISION
    fprintfn stderr "Copyright (C) nabiki_t. All rights reserved."
    fprintfn stderr ""
    fprintfn stderr "Instructions : "
    fprintfn stderr "    client [/f] [/h host] [/p port]"
    fprintfn stderr "           [/f] [/p port] [/h host]"
    fprintfn stderr "           [/f] [host [port]]"
    fprintfn stderr ""

/// <summary>
///  start and login.
/// </summary>
/// <param name="cr">
///  CommandRunner instance.
/// </param>
/// <param name="force">
///  force connection or not.
/// </param>
/// <param name="host">
///  host name.
/// </param>
/// <param name="portStr">
///  port number.
/// </param>
let login ( cr : CommandRunner ) ( force : bool ) ( host : string ) ( portStr : string ) : unit =
    let r, port = Int32.TryParse portStr
    if host.Length <= 0 || host.Length > 256 then
        printfn "Invlaid host name : %s" host
        printfn ""
        help()
    elif not r || port < 1 || port > 65535 then
        printfn "Invlaid port number : %s" portStr
        printfn ""
        help()
    else
        cr.RunWithLogin force host port

[<EntryPoint>]
let main ( argv : string [] ) : int =
    let st = new StringTable( "Client" )
    let cr = new CommandRunner( st, stdin, stdout )
    let defHostName = "::1"
    let defPortNo = int32 Constants.DEFAULT_MNG_CLI_PORT_NUM
    let defPortNoStr = sprintf "%d" Constants.DEFAULT_MNG_CLI_PORT_NUM
    match argv with
    | [||] ->
        cr.Run()
    | [| "/f" |] ->
        cr.RunWithLogin true defHostName defPortNo
    | [| "/f"; "/h"; host |] ->
        login cr true host defPortNoStr
    | [| "/f"; "/p"; portStr |] ->
        login cr true defHostName portStr
    | [| "/f"; "/h"; host; "/p"; portStr |] ->
        login cr true host portStr
    | [| "/f"; "/p"; portStr; "/h"; host; |] ->
        login cr true host portStr
    | [| "/f"; host; |] ->
        login cr true host defPortNoStr
    | [| "/f"; host; portStr; |] ->
        login cr true host portStr
    | [| "/h"; host |] ->
        login cr false host defPortNoStr
    | [| "/p"; portStr |] ->
        login cr false defHostName portStr
    | [| "/h"; host; "/p"; portStr |] ->
        login cr false host portStr
    | [| "/p"; portStr; "/h"; host; |] ->
        login cr false host portStr
    | [| host; |] ->
        login cr false host defPortNoStr
    | [| host; portStr; |] ->
        login cr false host portStr
    | _ ->
        help()
    
    0
