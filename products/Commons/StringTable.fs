//=============================================================================
// Haruka Software Storage.
// StringTable.fs : It defines functions getting resource strings.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Reflection
open System.Threading
open System.Collections.Generic
open System.Collections.Frozen

open Haruka.Constants
open Haruka.IODataTypes


/// <summary>
///  string table reader class
/// </summary>
/// <param name="m_ResName">
///  Resource file name that is read by this class.
/// </param>
type StringTable( m_ResName : string ) =
    /// Messages
    let m_MessageRes =
        if m_ResName.Length > 0 then
            // Directory name of this assembly
            let asmPath =
                let s = Assembly.GetEntryAssembly()
                Path.GetDirectoryName s.Location

            // Directory name of resource files
            let dname = Functions.AppendPathName asmPath Constants.RESOURCE_DIR_NAME

            // primary resource file name
            let firstFName =
                let localeName = Thread.CurrentThread.CurrentCulture.Name
                Functions.AppendPathName dname ( sprintf "%s_%s.xml" m_ResName localeName )

            // fallback resource file name
            let defFName = Functions.AppendPathName dname ( sprintf "%s_%s.xml" m_ResName Constants.DEFAULT_CULTURE_NAME )

            // load resource file
            let wfname =  if File.Exists firstFName then firstFName else defFName
            try
                ( StringTableFormat.ReaderWriter.LoadFile wfname ).Section
                |> Seq.map ( fun ts ->
                    ts.Message
                    |> Seq.map ( fun tm -> struct( Option.defaultValue "" ts.Name, tm.Name, tm.Value ) )
                )
                |> Seq.concat
                |> Seq.sortWith ( fun struct( s1, n1, _ ) struct( s2, n2, _ ) ->
                    let sc = String.Compare( s1, s2, StringComparison.Ordinal )
                    if sc <> 0 then sc else String.Compare( n1, n2, StringComparison.Ordinal )
                )
                |> Seq.distinctBy ( fun struct( s1, n1, _ ) -> struct( s1, n1 ) )
                |> Seq.groupBy ( fun struct( s1, _, _ ) -> s1 )
                |> Seq.map ( fun ( key, ssec ) -> (
                    key,
                    ssec
                    |> Seq.map ( fun struct( _, n1, v1 ) -> ( n1, v1 ) )
                    |> Functions.ToFrozenDictionary
                ))
                |> Functions.ToFrozenDictionary
                |> Some
            with
            | _ -> None

        else
            None

    /// <summary>
    ///  Get resource string.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <param name="argcnt">Message arguments count.</param>
    /// <returns>
    ///  Defined message string. If specified message ID is missing, it returns default message string.
    /// </returns>
    member private _.GetResourceString( s : string ) ( n : string ) ( argcnt : int ) : string =
        let follbackArg =
            match argcnt with
            | 0 -> ""
            | 1 -> "{0}"
            | 2 -> "{0}, {1}"
            | 3 -> "{0}, {1}, {2}"
            | 4 -> "{0}, {1}, {2}, {3}"
            | _ -> "{0}, {1}, {2}, {3}, {4}"
        match m_MessageRes with
        | Some( x ) ->
            try
                x.[s].[n]
            with
            | _ ->
                sprintf "Unknown message section '%s' name '%s' was specified. Arguments=%s" s n follbackArg
        | None ->
            sprintf "%s : %s" n follbackArg

    /// <summary>
    ///  Get a message by specified message ID and default section name from loaded resource with no argument string.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <returns>Defined message string.</returns>
    /// <remarks>
    ///  If specified message by n requests more than one arguments, result string is not as expected.
    /// </remarks>
    member this.GetMessage( n : string ) : string =
        let s = this.GetResourceString "" n 0
        String.Format( s, "", "", "", "", "" )

    /// <summary>
    ///  Get a message by specified message ID and default section name from loaded resource with an argument string.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by n does not requests 1 argument, result string is not as expected.
    /// </remarks>
    member this.GetMessage ( n : string, a0 : string ) : string =
        let s = this.GetResourceString "" n 1
        String.Format( s, a0, "", "", "", "" )

    /// <summary>
    ///  Get a message by specified message ID and default section name from loaded resource with 2 argument strings.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by n does not requests 2 arguments, result string is not as expected.
    /// </remarks>
    member this.GetMessage ( n : string, a0 : string, a1 : string ) : string =
        let s = this.GetResourceString "" n 2
        String.Format( s, a0, a1, "", "", "" )

    /// <summary>
    ///  Get a message by specified message ID and default section name from loaded resource with 3 argument strings.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <param name="a2">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by n does not requests 3 arguments, result string is not as expected.
    /// </remarks>
    member this.GetMessage (
            n : string,
            a0 : string,
            a1 : string,
            a2 : string ) : string =
        let s = this.GetResourceString "" n 3
        String.Format( s, a0, a1, a2, "", "" )

    /// <summary>
    ///  Get a message by specified message ID and default section name from loaded resource with 4 argument strings.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <param name="a2">Argument string.</param>
    /// <param name="a3">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by n does not requests 4 arguments, result string is not as expected.
    /// </remarks>
    member this.GetMessage (
            n : string,
            a0 : string,
            a1 : string,
            a2 : string,
            a3 : string ) : string =
        let s = this.GetResourceString "" n 4
        String.Format( s, a0, a1, a2, a3, "" )

    /// <summary>
    ///  Get a message by specified message ID and default section name from loaded resource with 5 argument strings.
    /// </summary>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <param name="a2">Argument string.</param>
    /// <param name="a3">Argument string.</param>
    /// <param name="a4">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by n does not requests 5 arguments, result string is not as expected.
    /// </remarks>
    member this.GetMessage (
            n : string,
            a0 : string,
            a1 : string,
            a2 : string,
            a3 : string,
            a4 : string ) : string =
        let s = this.GetResourceString "" n 5
        String.Format( s, a0, a1, a2, a3, a4 )

    /// <summary>
    ///  Get a message by the section name and the message ID from loaded resource with no argument string.
    /// </summary>
    /// <param name="s">Section name</param>
    /// <param name="n">Message ID</param>
    /// <returns>Defined message string.</returns>
    /// <remarks>
    ///  If specified message by s and n requests more than one arguments, result string is not as expected.
    /// </remarks>
    member this.Get( s : string, n : string ) : string =
        let m = this.GetResourceString s n 0
        String.Format( m, "", "", "", "", "" )

    /// <summary>
    ///  Get a message by the section name and the message ID from loaded resource with an argument string.
    /// </summary>
    /// <param name="s">Section name</param>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by s and n does not requests 1 argument, result string is not as expected.
    /// </remarks>
    member this.Get ( s : string, n : string, a0 : string ) : string =
        let m = this.GetResourceString s n 1
        String.Format( m, a0, "", "", "", "" )

    /// <summary>
    ///  Get a message by the section name and the message ID from loaded resource with 2 argument strings.
    /// </summary>
    /// <param name="s">Section name</param>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by s and n does not requests 2 arguments, result string is not as expected.
    /// </remarks>
    member this.Get ( s : string, n : string, a0 : string, a1 : string ) : string =
        let m = this.GetResourceString s n 2
        String.Format( m, a0, a1, "", "", "" )

    /// <summary>
    ///  Get a message by the section name and the message ID from loaded resource with 3 argument strings.
    /// </summary>
    /// <param name="s">Section name</param>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <param name="a2">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by s and n does not requests 3 arguments, result string is not as expected.
    /// </remarks>
    member this.Get (
            s : string,
            n : string,
            a0 : string,
            a1 : string,
            a2 : string ) : string =
        let m = this.GetResourceString s n 3
        String.Format( m, a0, a1, a2, "", "" )

    /// <summary>
    ///  Get a message by the section name and the message ID from loaded resource with 4 argument strings.
    /// </summary>
    /// <param name="s">Section name</param>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <param name="a2">Argument string.</param>
    /// <param name="a3">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by s and n does not requests 4 arguments, result string is not as expected.
    /// </remarks>
    member this.Get (
            s : string,
            n : string,
            a0 : string,
            a1 : string,
            a2 : string,
            a3 : string ) : string =
        let m = this.GetResourceString s n 4
        String.Format( m, a0, a1, a2, a3, "" )

    /// <summary>
    ///  Get a message by the section name and the message ID from loaded resource with 5 argument strings.
    /// </summary>
    /// <param name="s">Section name</param>
    /// <param name="n">Message ID</param>
    /// <param name="a0">Argument string.</param>
    /// <param name="a1">Argument string.</param>
    /// <param name="a2">Argument string.</param>
    /// <param name="a3">Argument string.</param>
    /// <param name="a4">Argument string.</param>
    /// <returns>Formatted message string.</returns>
    /// <remarks>
    ///  If specified message by s and n does not requests 5 arguments, result string is not as expected.
    /// </remarks>
    member this.Get (
            s : string,
            n : string,
            a0 : string,
            a1 : string,
            a2 : string,
            a3 : string,
            a4 : string ) : string =
        let m = this.GetResourceString s n 5
        String.Format( m, a0, a1, a2, a3, a4 )


    /// <summary>
    ///  Get loaded sestion names.
    /// </summary>
    /// <returns>
    ///  Loaded section names array. If it failed to load the string table file, it returns empty sequence.
    /// </returns>
    member _.GetSectionNames() : string seq =
        if m_MessageRes.IsNone then
            Array.empty
        else
            m_MessageRes.Value.Keys

    /// <summary>
    ///  Get loaded key names.
    /// </summary>
    /// <param name="s">
    ///  Section name
    /// </param>
    /// <returns>
    ///  Loaded key names array. If it failed to load the string table file or specified section names is not exist, it returns empty sequence.
    /// </returns>
    member _.GetNames( s : string ) : string seq =
        if m_MessageRes.IsNone then
            Array.empty
        elif not ( m_MessageRes.Value.ContainsKey s ) then
            Array.empty
        else
            m_MessageRes.Value.[s].Keys

