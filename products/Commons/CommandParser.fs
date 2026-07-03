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

type CIE_ErrorCode =
    | NoCommandString
    | UnknownCommand of string
    | InvalidArgValue of string
    | LastArgValMissing
    | InvalidArgCount
    | MissingMandatoryArg
    | NamelessPatternMismatch

/// Exception that is raised when read command is invalid.
type CommandInputError( cd : CIE_ErrorCode ) =
    inherit Exception( "" )
    member _.ErrorCode = cd

/// Validation type
[<NoComparison>]
type CRValidateType =
    | CRV_int32 of ( int32 * int32 )        // ( min_value, max_value ), Optional
    | CRV_uint32 of ( uint32 * uint32 )     // ( min_value, max_value ), Optional
    | CRV_int64 of ( int64 * int64 )    // ( min_value, max_value ), Optional
    | CRV_uint64 of ( uint64 * uint64 ) // ( min_value, max_value ), Optional
    | CRV_String of int32                 // max length, Optional
    | CRV_Regex of Regex                // Regular Expression, Optional
    | CRV_LUN                           // LUN, Optional
    | CRVM_int32 of ( int32 * int32 )        // ( min_value, max_value ), Mandatory
    | CRVM_uint32 of ( uint32 * uint32 )     // ( min_value, max_value ), Mandatory
    | CRVM_int64 of ( int64 * int64 )    // ( min_value, max_value ), Mandatory
    | CRVM_uint64 of ( uint64 * uint64 ) // ( min_value, max_value ), Mandatory
    | CRVM_String of int32                 // max length, Mandatory
    | CRVM_Regex of Regex                // Regular Expression, Mandatory
    | CRVM_LUN                           // LUN, Mandatory

    /// <summary>
    ///   Determine whether the value is optional or not.
    /// </summary>
    /// <param name="v">
    ///   CRValidateType value.
    /// </param>
    /// <returns>
    ///   Returns True if the specified Validation type indicates an optional value.
    /// </returns>
    static member isOptional ( v : CRValidateType ) =
        match v with
        | CRV_int32 _
        | CRV_uint32 _
        | CRV_int64 _
        | CRV_uint64 _
        | CRV_String _
        | CRV_Regex _
        | CRV_LUN ->
            true
        | _ ->
            false


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

    /// help message
    HelpMsgName : string;
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

    /// <summary>
    ///  Parsecommand string.
    /// </summary>
    /// <param name="accCommands">
    ///  Acceptable command.
    /// </param>
    /// <param name="lineStr">
    ///  Entered string.
    /// </param>
    /// <returns>
    ///  Parsed command and arguments.
    /// </returns>
    static member FromString ( accCommands : AcceptableCommand<'a> array ) ( lineStr : string ) : CommandParser<'a> =
        let divLine = CommandParser<'a>.DiviteInputString lineStr
        CommandParser<'a>.FromStringArray accCommands divLine

    /// <summary>
    ///  Parsecommand string.
    /// </summary>
    /// <param name="accCommands">
    ///  Acceptable command.
    /// </param>
    /// <param name="strv">
    ///  Entered string. Specify a string that has been split into individual elements.
    /// </param>
    /// <returns>
    ///  Parsed command and arguments.
    /// </returns>
    static member FromStringArray ( accCommands : AcceptableCommand<'a> array ) ( strv : string[] ) : CommandParser<'a> =
        CommandParser<'a>.RecognizeCommand strv accCommands

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
    static member private RecognizeCommand ( args : string[] ) ( accCommands : AcceptableCommand<'a> array ) : CommandParser<'a> =
        if args.Length < 1 then
            // Unexpected
            raise <| CommandInputError( CIE_ErrorCode.NoCommandString )

        let r =
            let upargs = args |> Array.map ( fun itr -> itr.ToUpperInvariant() )
            CommandParser<'a>.SearchCommand upargs accCommands
        if r.IsNone then
            raise <| CommandInputError( CIE_ErrorCode.UnknownCommand( args.[0] ) )
        let cmdval = r.Value

        let lastStat, valuelessArgList, namedArgList, namelessArgList =
            args.[ cmdval.Command.Length .. ]
            |> Array.fold ( fun ( s, li1, li2, li3 ) itr ->
                match s with
                | Some x ->
                    let ev = CommandParser<'a>.ValidateValue ( snd cmdval.NamedArgs.[x] ) itr
                    if ev.IsNone then
                        raise <| CommandInputError( CIE_ErrorCode.InvalidArgValue( itr ) )
                    ( None, li1, ( fst cmdval.NamedArgs.[x], ev.Value ) :: li2, li3 )
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
            raise <| CommandInputError CIE_ErrorCode.LastArgValMissing
        let lessMCnt = cmdval.NamelessArgs |> Array.sumBy ( fun itr -> if CRValidateType.isOptional itr then 0 else 1 )
        let lessOCnt = cmdval.NamelessArgs.Length - lessMCnt
        if namelessArgList.Length < lessMCnt || namelessArgList.Length > lessMCnt + lessOCnt then
            raise <| CommandInputError CIE_ErrorCode.InvalidArgCount

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
                    raise <| CommandInputError CIE_ErrorCode.MissingMandatoryArg
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
            match CommandParser<'a>.ValidateNamelessValues ( namelessArgList |> List.rev ) cmdval.NamelessArgs with
            | ValueNone ->
                raise <| CommandInputError( CIE_ErrorCode.NamelessPatternMismatch )
            | ValueSome x ->
                x

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
    static member SearchCommand ( args : string[] ) ( cmds : AcceptableCommand<'a> array ) : AcceptableCommand<'a> option =

        let rec loop ( idx : int32 ) ( argcmds : AcceptableCommand<'a> array ) : AcceptableCommand<'a> option =
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
    static member private ValidateValue ( vt : CRValidateType ) ( argval : string ) : EnteredValue voption =
        match vt with
        | CRV_int32( min, max )
        | CRVM_int32( min, max ) ->
            let r, v = Int32.TryParse argval
            if not r || v < min || v > max then
                ValueNone
            else
                EV_int32( v ) |> ValueSome

        | CRV_uint32( min, max )
        | CRVM_uint32( min, max ) ->
            let r, v = UInt32.TryParse argval
            if not r || v < min || v > max then
                ValueNone
            else
                EV_uint32( v ) |> ValueSome

        | CRV_int64( min, max )
        | CRVM_int64( min, max ) ->
            let r, v = Int64.TryParse argval
            if not r || v < min || v > max then
                ValueNone
            else
                EV_int64( v ) |> ValueSome

        | CRV_uint64( min, max )
        | CRVM_uint64( min, max ) ->
            let r, v = UInt64.TryParse argval
            if not r || v < min || v > max then
                ValueNone
            else
                EV_uint64( v ) |> ValueSome

        | CRV_String( len )
        | CRVM_String( len ) ->
            if argval.Length > len then
                ValueNone
            else
                EV_String( argval ) |> ValueSome

        | CRV_Regex( r )
        | CRVM_Regex( r ) ->
            if not ( r.IsMatch argval ) then
                ValueNone
            else
                EV_String( argval ) |> ValueSome

        | CRV_LUN
        | CRVM_LUN ->
            try
                if argval.StartsWith( "0x", StringComparison.OrdinalIgnoreCase ) then
                    Convert.ToUInt64( argval, 16 )
                else
                    Convert.ToUInt64 argval
                |> lun_me.fromPrim
                |> EV_LUN
                |> ValueSome
            with
            | :? OverflowException 
            | :? FormatException ->
                ValueNone

    /// <summary>
    ///  Check if the unnamed argument meets the condition.
    /// </summary>
    /// <param name="arglist">
    ///  The entered string.
    /// </param>
    /// <param name="cond">
    ///  Conditions that unnamed arguments must satisfy.
    /// </param>
    /// <returns>
    ///  Converted values, or None.
    /// </returns>
    static member private ValidateNamelessValues ( arglist : string list ) ( cond : CRValidateType[] ) : EnteredValue[] voption =
    
        let rec matchNext ( wArgList : string list ) ( condIdx : int32 ) ( acc : EnteredValue list ) : EnteredValue list voption =

            match wArgList with
            | [] ->
                if condIdx = cond.Length then
                    // If all conditions have been processed, the match is successful.
                    ValueSome( acc )
                else
                    // If the remaining conditions can be omitted, they will be considered omitted.
                    let currentCond = cond.[ condIdx ]
                    if CRValidateType.isOptional currentCond then
                        matchNext [] ( condIdx + 1 ) ( EV_NoValue :: acc )
                    else
                        ValueNone

            | currentArg :: tail ->
                if condIdx = cond.Length then
                    // If all conditions have been processed but there is still input data remaining, the match will fail.
                    ValueNone
                else
                    let currentCond = cond.[ condIdx ]

                    // Regardless of whether it can be omitted or not, 
                    // the match continues assuming it was not omitted.
                    let consumedResult =
                        match CommandParser<'a>.ValidateValue currentCond currentArg with
                        | ValueSome(ev) ->
                            matchNext tail ( condIdx + 1 ) ( ev :: acc )
                        | ValueNone ->
                            ValueNone

                    match consumedResult with
                    | ValueSome( result ) ->
                        ValueSome( result )
                    | ValueNone ->
                        if CRValidateType.isOptional currentCond then
                            // The match failed because the value was assumed to be omitted.
                            // The match proceeds assuming this value is omitted.
                            matchNext wArgList ( condIdx + 1 ) ( EV_NoValue :: acc )
                        else
                            // Match failed because a non-optional value does not meet the condition.
                            ValueNone

        matchNext arglist 0 []
        |> ValueOption.map ( List.rev >> List.toArray )

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
    member _.DefaultNamelessInt32 ( idx : int32 ) ( d : int32 ) : int32 =
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
    member _.NamelessInt32 ( idx : int32 ) : int32 option =
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
    member _.DefaultNamelessUInt32 ( idx : int32 ) ( d : uint32 ) : uint32 =
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
    member _.NamelessUInt32 ( idx : int32 ) : uint32 option =
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
    member _.DefaultNamelessInt64 ( idx : int32 ) ( d : int64 ) : int64 =
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
    member _.NamelessInt64 ( idx : int32 ) : int64 option =
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
    member _.DefaultNamelessUInt64 ( idx : int32 ) ( d : uint64 ) : uint64 =
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
    member _.NamelessUInt64 ( idx : int32 ) : uint64 option =
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
    member _.DefaultNamelessString ( idx : int32 ) ( d : string ) : string =
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
    member _.NamelessString ( idx : int32 ) : string option =
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
    member _.DefaultNamelessLUN ( idx : int32 ) ( d : LUN_T ) : LUN_T =
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
    member _.NamelessLUN ( idx : int32 ) : LUN_T option =
        if idx < 0 || m_NamelessArgs.Length <= idx then
            None
        else
            match m_NamelessArgs.[idx] with
            | EV_LUN x -> Some x
            | _ -> None


