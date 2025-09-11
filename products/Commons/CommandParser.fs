//=============================================================================
// Haruka Software Storage.
// CommandParser.fs : CLI command parser.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// Exception that is raised when read command is invalid.
type CommandInputError( m_Message : string ) =
    inherit Exception( m_Message )

/// Validation type
[<NoComparison>]
type CRValidateType =
    | CRV_int32 of ( int * int )        // ( min_value, max_value ), Optional
    | CRV_uint32 of ( uint * uint )     // ( min_value, max_value ), Optional
    | CRV_int64 of ( int64 * int64 )    // ( min_value, max_value ), Optional
    | CRV_uint64 of ( uint64 * uint64 ) // ( min_value, max_value ), Optional
    | CRV_String of int                 // max length, Optional
    | CRV_Regex of Regex                // Regular Expression, Optional
    | CRV_LUN                           // LUN, Optional
    | CRVM_int32 of ( int * int )        // ( min_value, max_value ), Mandatory
    | CRVM_uint32 of ( uint * uint )     // ( min_value, max_value ), Mandatory
    | CRVM_int64 of ( int64 * int64 )    // ( min_value, max_value ), Mandatory
    | CRVM_uint64 of ( uint64 * uint64 ) // ( min_value, max_value ), Mandatory
    | CRVM_String of int                 // max length, Mandatory
    | CRVM_Regex of Regex                // Regular Expression, Mandatory
    | CRVM_LUN                           // LUN, Mandatory

/// Data type that specifies the acceptable commands.
[<NoComparison>]
type AcceptableCommand<'a> = {
    /// Command string name
    Command : string array;

    /// Command varb type value
    Varb : 'a

    /// named arguments
    NamedArgs : ( string * CRValidateType )[];

    // Valueless arguments
    ValuelessArgs : string[];

    /// nameless arguments
    NamelessArgs : CRValidateType[];
}

/// Entered value type
type EnteredValue =
    | EV_int32 of int32
    | EV_uint32 of uint32
    | EV_int64 of int64
    | EV_uint64 of uint64
    | EV_String of string
    | EV_LUN of LUN_T
    | EV_NoValue

//=============================================================================
// Class implementation

/// <summary>
/// Entered command and value
/// </summary>
/// <param name="m_Varb">
/// Command varb.
/// </param>
/// <param name="m_NamedArgs">
/// Entered named arguments.
/// </param>
/// <param name="m_NamelessArgs">
/// Entered nameless arguments.
/// </param>
[<NoComparison>]
type CommandParser<'a> (
        m_Varb : 'a,
        m_NamedArgs : Dictionary< string, EnteredValue >,
        m_NamelessArgs : EnteredValue[]
    ) =

    /// property of m_Varb
    member _.Varb : 'a =
        m_Varb

    /// property of m_NamedArgs
    member _.NamedArgs : Dictionary< string, EnteredValue > =
        m_NamedArgs

    /// property of m_NamelessArgs
    member _.NamelessArgs : EnteredValue[] =
        m_NamelessArgs

    static member FromString ( st : StringTable ) ( accCommands : AcceptableCommand<'a> array ) ( lineStr : string ) : CommandParser<'a> =
        let divLine = CommandParser<'a>.DiviteInputString lineStr
        CommandParser<'a>.FromStringArray st accCommands divLine

    static member FromStringArray ( st : StringTable ) ( accCommands : AcceptableCommand<'a> array ) ( strv : string[] ) : CommandParser<'a> =
        CommandParser<'a>.RecognizeCommand st strv accCommands

    /// <summary>
    ///  Divite inputted string by space.
    /// </summary>
    /// <param name="intext">
    ///  inputted string.
    /// </param>
    /// <returns>
    ///  Divited string.
    /// </returns>
    static member private DiviteInputString ( intext : string ) : string[] =

        let lastStat, b, li =
            intext
            |> Seq.fold ( fun ( isEsc, b : StringBuilder, li ) ( c: char ) ->
                if isEsc then
                    ( false, b.Append c, li )
                elif c = '^' then
                    ( true, b, li )
                elif c = ' ' then
                    ( false, new StringBuilder(), b.ToString() :: li )
                else
                    ( false, b.Append c, li )
            ) ( false, new StringBuilder(), [] )
        if lastStat then
            b.Append '^' |> ignore
        b.ToString() :: li
        |> Seq.filter ( String.length >> (<>) 0 )
        |> Seq.rev
        |> Seq.toArray

    /// <summary>
    ///  Recognize and validate inputted command.
    /// </summary>
    /// <param name="st">
    ///  Message table.
    /// </param>
    /// <param name="args">
    ///  Entered strings, divided by space.
    /// </param>
    /// <param name="accCommands">
    ///  Acceptable command.
    /// </param>
    /// <returns>
    /// Instance of EnteredCommand.
    /// </returns>
    /// <exceptions>
    ///   If validation failed, CommandInputError exception is raised.
    /// </exceptions>
    static member private RecognizeCommand ( st : StringTable ) ( args : string[] ) ( accCommands : AcceptableCommand<'a> array ) : CommandParser<'a> =
        if args.Length < 1 then
            // Unexpected
            raise <| CommandInputError( "No command strings" )

        let r =
            let upargs = args |> Array.map ( fun itr -> itr.ToUpperInvariant() )
            CommandParser<'a>.SearchCommand upargs accCommands
        if r.IsNone then
            let msg = st.GetMessage( "CMDERR_UNKNOWN_COMMAND", args.[0] )
            raise <| CommandInputError( msg )
        let cmdval = r.Value

        let lastStat, valuelessArgList, namedArgList, namelessArgList =
            args.[ cmdval.Command.Length .. ]
            |> Array.fold ( fun ( s, li1, li2, li3 ) itr ->
                match s with
                | Some x ->
                    let ev = CommandParser<'a>.ValidateValue st ( snd cmdval.NamedArgs.[x] ) itr
                    ( None, li1, ( fst cmdval.NamedArgs.[x], ev ) :: li2, li3 )
                | None ->
                    if cmdval.ValuelessArgs |> Array.exists ( fun itr2 -> String.Equals( itr2, itr, StringComparison.Ordinal ) ) then
                        ( None, itr :: li1, li2, li3 )
                    else
                        let r = cmdval.NamedArgs |> Array.tryFindIndex ( fun ( sw, _ ) -> sw = itr )
                        match r with
                        | Some y ->
                            ( Some y, li1, li2, li3 )
                        | None ->
                            ( None, li1, li2, itr :: li3 )
            ) ( None, [], [], [] )

        if lastStat.IsSome then
            let msg = st.GetMessage( "CMDERR_LAST_ARG_VAL_MISSING" )
            raise <| CommandInputError msg
        if namelessArgList.Length <> cmdval.NamelessArgs.Length then
            let msg = st.GetMessage( "CMDERR_INVALID_ARG_COUNT", ( sprintf "%d" cmdval.NamelessArgs.Length ) )
            raise <| CommandInputError msg

        cmdval.NamedArgs
        |> Array.iter (
            function
            | ( n, CRVM_int32( _ ) )
            | ( n, CRVM_uint32( _ ) )
            | ( n, CRVM_int64( _ ) )
            | ( n, CRVM_uint64( _ ) )
            | ( n, CRVM_String( _ ) )
            | ( n, CRVM_Regex( _ ) )
            | ( n, CRVM_LUN( _ ) ) ->
                let r =
                    namedArgList
                    |> Seq.map fst
                    |> Seq.exists ( fun itr -> String.Equals( itr, n, StringComparison.Ordinal ) )
                    |> not
                if r then
                    let msg = st.GetMessage( "CMDERR_MISSING_MANDATORY_ARG" )
                    raise <| CommandInputError msg
            | _ -> ()
        )

        let allNamedArgs =
            let namedArgList2 = namedArgList |> Seq.distinctBy fst
            valuelessArgList
            |> Seq.distinct
            |> Seq.rev
            |> Seq.map ( fun itr -> ( itr, EV_NoValue ) )
            |> Seq.append namedArgList2
            |> Seq.map KeyValuePair
            |> Dictionary

        let allNamelessArgs =
            namelessArgList
            |> Seq.rev
            |> Seq.map2 ( CommandParser<'a>.ValidateValue st ) cmdval.NamelessArgs
            |> Seq.toArray

        new CommandParser<'a> ( cmdval.Varb, allNamedArgs, allNamelessArgs )

    /// <summary>
    ///  This function searches for which command was entered among the acceptable commands.
    /// </summary>
    /// <param name="args">
    ///  Entered string divited by space.
    /// </param>
    /// <param name="cmds">
    ///  Acceptable commands.
    /// </param>
    /// <returns>
    ///  Matched command or None, if no matching command is found.
    /// </returns>
    static member private SearchCommand ( args : string[] ) ( cmds : AcceptableCommand<'a> array ) : AcceptableCommand<'a> option =

        let rec loop ( idx : int ) ( argcmds : AcceptableCommand<'a> array ) : AcceptableCommand<'a> option =
            // Find exact match
            let r =
                argcmds
                |> Seq.filter ( fun itr -> String.Equals( itr.Command.[idx], args.[idx], StringComparison.Ordinal ) )
                |> Seq.toArray
            if r.Length > 0 then
                if 1 <> ( r |> Seq.map ( fun itr -> itr.Command.Length ) |> Seq.distinct |> Seq.length ) then
                    None    // Number of subcommands must match
                elif idx + 1 = r.[0].Command.Length then
                    if r.Length = 1 then
                        Some r.[0]
                    else
                        None
                elif idx + 1 = args.Length then
                    None
                else
                    loop ( idx + 1 ) r
            else
                // Search by prefix match
                let r2 =
                    argcmds
                    |> Seq.filter ( fun itr -> itr.Command.[idx].StartsWith( args.[idx], StringComparison.Ordinal ) )
                    |> Seq.toArray
                if r2.Length = 0 then
                    None
                else
                    if 1 <> ( r2 |> Seq.map ( fun itr -> itr.Command.[idx] ) |> Seq.distinct |> Seq.length ) then
                        None    // Unable to identify command
                    elif 1 <> ( r2 |> Seq.map ( fun itr -> itr.Command.Length ) |> Seq.distinct |> Seq.length ) then
                        None    // Number of subcommands must match
                    elif idx + 1 = r2.[0].Command.Length then
                        if r2.Length = 1 then
                            Some r2.[0]
                        else
                            None
                    elif idx + 1 = args.Length then
                        None
                    else 
                        loop ( idx + 1 ) r2

        if args.Length = 0 then
            None
        else
            loop 0 cmds

    /// <summary>
    ///  Validate and convert input string value.
    /// </summary>
    /// <param name="st">
    ///  Message table.
    /// </param>
    /// <param name="vt">
    ///   Validation information.
    /// </param>
    /// <param name="argval">
    ///   string value.
    /// </param>
    /// <returns>
    ///   validated value.
    /// </returns>
    /// <exceptions>
    ///   If validation failed, CommandInputError exception is raised.
    /// </exceptions>
    static member private ValidateValue ( st : StringTable ) ( vt : CRValidateType ) ( argval : string ) : EnteredValue =
        let ex = CommandInputError( st.GetMessage( "CMDERR_INVALID_ARG_VALUE", argval ) )
        match vt with
        | CRV_int32( min, max )
        | CRVM_int32( min, max ) ->
            let r, v = Int32.TryParse argval
            if not r || v < min || v > max then
                raise <| ex
            EV_int32( v )

        | CRV_uint32( min, max )
        | CRVM_uint32( min, max ) ->
            let r, v = UInt32.TryParse argval
            if not r || v < min || v > max then
                raise <| ex
            EV_uint32( v )

        | CRV_int64( min, max )
        | CRVM_int64( min, max ) ->
            let r, v = Int64.TryParse argval
            if not r || v < min || v > max then
                raise <| ex
            EV_int64( v )

        | CRV_uint64( min, max )
        | CRVM_uint64( min, max ) ->
            let r, v = UInt64.TryParse argval
            if not r || v < min || v > max then
                raise <| ex
            EV_uint64( v )

        | CRV_String( len )
        | CRVM_String( len ) ->
            if argval.Length > len then
                raise <| ex
            EV_String( argval )

        | CRV_Regex( r )
        | CRVM_Regex( r ) ->
            if not ( r.IsMatch argval ) then
                raise <| ex
            EV_String( argval )

        | CRV_LUN
        | CRVM_LUN ->
            try
                if argval.StartsWith( "0x", StringComparison.OrdinalIgnoreCase ) then
                    Convert.ToUInt64( argval, 16 )
                else
                    Convert.ToUInt64 argval
                |> lun_me.fromPrim
                |> EV_LUN
            with
            | :? OverflowException 
            | :? FormatException ->
                raise ex

    /// <summary>
    ///  Get named argument value by int32. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamedInt32 ( flg : string ) ( d : int32 ) : int32 =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_int32 x -> x
        | _ -> d

    /// <summary>
    ///  Get named argument value by int32. If specified value missing, it returns None.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamedInt32 ( flg : string ) : int32 option =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_int32 x -> Some x
        | _ -> None
            
    /// <summary>
    ///  Get named argument value by uint32. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamedUInt32 ( flg : string ) ( d : uint32 ) : uint32 =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_uint32 x -> x
        | _ -> d

    /// <summary>
    ///  Get named argument value by uint32. If specified value missing, it returns None.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamedUInt32 ( flg : string ): uint32 option =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_uint32 x -> Some x
        | _ -> None

    /// <summary>
    ///  Get named argument value by int64. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamedInt64 ( flg : string ) ( d : int64 ) : int64 =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_int64 x -> x
        | _ -> d

    /// <summary>
    ///  Get named argument value by int64. If specified value missing, it returns None.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamedInt64 ( flg : string ): int64 option =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_int64 x -> Some x
        | _ -> None

    /// <summary>
    ///  Get named argument value by int64. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamedUInt64 ( flg : string ) ( d : uint64 ) : uint64 =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_uint64 x -> x
        | _ -> d

    /// <summary>
    ///  Get named argument value by uint64. If specified value missing, it returns None.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamedUInt64 ( flg : string ): uint64 option =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_uint64 x -> Some x
        | _ -> None

    /// <summary>
    ///  Get named argument value by string. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamedString ( flg : string ) ( d : string ) : string =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_String x -> x
        | _ -> d

    /// <summary>
    ///  Get named argument value by string. If specified value missing, it returns None.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamedString ( flg : string ) : string option =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_String x -> Some x
        | _ -> None

    /// <summary>
    ///  Get named argument value by LUN_T. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamedLUN ( flg : string ) ( d : LUN_T ) : LUN_T =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_LUN x -> x
        | _ -> d

    /// <summary>
    ///  Get named argument value by LUN_T. If specified value missing, it returns None.
    /// </summary>
    /// <param name="flg">
    ///  Argument name.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamedLUN ( flg : string ) : LUN_T option =
        match m_NamedArgs.GetValueOrDefault( flg, EV_NoValue ) with
        | EV_LUN x -> Some x
        | _ -> None

    /// <summary>
    ///  Get nameless argument value by int32. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamelessInt32 ( idx : int ) ( d : int32 ) : int32 =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            d
        else
            match m_NamelessArgs.[idx] with
            | EV_int32 x -> x
            | _ -> d

    /// <summary>
    ///  Get nameless argument value by int32. If specified value missing, it returns None.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamelessInt32 ( idx : int ) : int32 option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_int32 x -> Some x
            | _ -> None

    /// <summary>
    ///  Get nameless argument value by uint32. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamelessUInt32 ( idx : int ) ( d : uint32 ) : uint32 =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            d
        else
            match m_NamelessArgs.[idx] with
            | EV_uint32 x -> x
            | _ -> d

    /// <summary>
    ///  Get nameless argument value by uint32. If specified value missing, it returns None.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamelessUInt32 ( idx : int ) : uint32 option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_uint32 x -> Some x
            | _ -> None

    /// <summary>
    ///  Get nameless argument value by int64. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamelessInt64 ( idx : int ) ( d : int64 ) : int64 =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            d
        else
            match m_NamelessArgs.[idx] with
            | EV_int64 x -> x
            | _ -> d

    /// <summary>
    ///  Get nameless argument value by int64. If specified value missing, it returns None.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamelessInt64 ( idx : int ) : int64 option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_int64 x -> Some x
            | _ -> None

    /// <summary>
    ///  Get nameless argument value by uint64. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamelessUInt64 ( idx : int ) ( d : uint64 ) : uint64 =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            d
        else
            match m_NamelessArgs.[idx] with
            | EV_uint64 x -> x
            | _ -> d

    /// <summary>
    ///  Get nameless argument value by uint64. If specified value missing, it returns None.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamelessUInt64 ( idx : int ) : uint64 option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_uint64 x -> Some x
            | _ -> None

    /// <summary>
    ///  Get nameless argument value by string. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamelessString ( idx : int ) ( d : string ) : string =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            d
        else
            match m_NamelessArgs.[idx] with
            | EV_String x -> x
            | _ -> d

    /// <summary>
    ///  Get nameless argument value by string. If specified value missing, it returns None.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamelessString ( idx : int ) : string option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_String x -> Some x
            | _ -> None

    /// <summary>
    ///  Get nameless argument value by LUN_T. If specified value missing, it returns default value.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <param name="d">
    ///  Default value.
    /// </param>
    /// <returns>
    ///  Entered argument value or default value.
    /// </returns>
    member _.DefaultNamelessLUN ( idx : int ) ( d : LUN_T ) : LUN_T =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            d
        else
            match m_NamelessArgs.[idx] with
            | EV_LUN x -> x
            | _ -> d

    /// <summary>
    ///  Get nameless argument value by LUN_T. If specified value missing, it returns None.
    /// </summary>
    /// <param name="idx">
    ///  Index number, starting from 0. It specifies arguments that is retrieved.
    /// </param>
    /// <returns>
    ///  Entered argument value or None.
    /// </returns>
    member _.NamelessLUN ( idx : int ) : LUN_T option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_LUN x -> Some x
            | _ -> None


