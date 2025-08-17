//=============================================================================
// Haruka Software Storage.
// InitMediaCmdArgs.fs : Recognize arguments.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Controller

//=============================================================================
// Import declaration

open System

open Haruka.Constants
open Haruka.Commons


//=============================================================================
// Type definition

/// Represents the type of instruction to the controller.
type CtrlCmdType =
    | Server                // Start haruka controller process.
    | InitWorkDir           // Create a work folder and generate an initial definition file.
    | InitMedia_PlainFile   // Create a "plain file" media file.

type CmdArgs() =

    /// "SV" arguments rule
    static member ArgRule_SV : AcceptableCommand<CtrlCmdType> =
        {
            Command = [| "SV"; |];
            Varb = CtrlCmdType.Server;
            NamedArgs = Array.empty;
            ValuelessArgs = Array.empty;
            NamelessArgs = [| CRV_String( Constants.MAX_FILENAME_STR_LENGTH ); |];
        }

    /// "ID" arguments rule
    static member ArgRule_ID : AcceptableCommand<CtrlCmdType> =
        {
            Command = [| "ID"; |];
            Varb = CtrlCmdType.InitWorkDir;
            NamedArgs = [| ( "/p", CRV_uint32( 1u, 65535u ) ); ( "/a", CRV_String( Constants.MAX_CTRL_ADDRESS_STR_LENGTH ) ) |];
            ValuelessArgs = [| "/o" |];
            NamelessArgs = [| CRV_String( Constants.MAX_FILENAME_STR_LENGTH ); |];
        }

    /// "PlainFile" arguments rule
    static member ArgRule_InitMedia_PlainFile : AcceptableCommand<CtrlCmdType> =
        {
            Command = [| "IM"; "PLAINFILE" |];
            Varb = CtrlCmdType.InitMedia_PlainFile;
            NamedArgs = [| ( "/f", CRVM_String( Constants.MAX_FILENAME_STR_LENGTH ) ); ( "/s", CRVM_int64( 1L, Int64.MaxValue ) ); |];
            ValuelessArgs = [| "/x" |];
            NamelessArgs = Array.empty;
        }

    /// Command arguments rules.
    static member ArgRules : AcceptableCommand<CtrlCmdType> [] =
        [|
            CmdArgs.ArgRule_SV;
            CmdArgs.ArgRule_ID;
            CmdArgs.ArgRule_InitMedia_PlainFile;
        |]

    /// <summary>
    ///  Recognize arguments.
    /// </summary>
    /// <param name="st">
    ///  Message table
    /// </param>
    /// <param name="argv">
    ///  arguments
    /// </param>
    /// <returns>
    ///  Recognized arguments.
    /// </returns>
    static member Recognize ( st : StringTable ) ( argv: string[] ) : CommandParser<CtrlCmdType> =
        CommandParser.FromStringArray st CmdArgs.ArgRules argv

