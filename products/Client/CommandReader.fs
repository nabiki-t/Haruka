//=============================================================================
// Haruka Software Storage.
// EnteredCommand.fs : CLI command specification.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text.RegularExpressions
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// Command varb type
type CommandVarb =
    | Exit
    | Help
    | Login
    | Logout
    | Reload
    | Select
    | UnSelect
    | List
    | ListParent
    | Pwd
    | Values
    | Set
    | Validate
    | Publish
    | Nop
    | StatusAll
    | Create_TargetDevice
    | Status
    | Delete
    | Start
    | Kill
    | SetLogParam
    | GetLogParam
    | Create_NetworkPortal
    | Create_TargetGroup
    | Add_IPWhiteList
    | Clear_IPWhiteList
    | Load
    | UnLoad
    | Activate
    | Inactivate
    | Create_Target
    | SetChap
    | UnsetAuth
    | Create_LU
    | Attach
    | Detach
    | Create_Media_PlainFile
    | Create_Media_MemBuffer
    | Create_Media_Debug
    | InitMedia_PlainFile
    | IMStatus
    | IMKill
    | Sessions
    | SessKill
    | Connections
    | LUStatus
    | LUReset
    | MediaStatus
    | Add_Trap
    | Clear_Trap
    | Traps
    | Task_List
    | Task_Resume

//=============================================================================
// Class implementation

/// <summary>
/// Read entered command and value.
/// </summary>
[<NoComparison>]
type CommandReader () =

    /// "exit" command rule.
    static member CmdRule_exit : AcceptableCommand< CommandVarb > = {
        Command = [| "EXIT" |];
        Varb = CommandVarb.Exit;
        NamedArgs = Array.empty;
        ValuelessArgs = [| "/y" |];
        NamelessArgs = Array.empty;
        HelpMsgName = "EXIT";
    }

    /// "help" command rule.
    static member CmdRule_help : AcceptableCommand< CommandVarb > = {
        Command = [| "HELP" |];
        Varb = CommandVarb.Help;
        NamedArgs = Array.empty;
        ValuelessArgs = [||];
        NamelessArgs = [| CRV_String( 32 ); CRV_String( 32 ); CRV_String( 32 ); CRV_String( 32 ); CRV_String( 32 ); |];
        HelpMsgName = "HELP";
    }

    /// "login" command rule.
    static member CmdRule_login : AcceptableCommand< CommandVarb > = {
        Command = [| "LOGIN" |];
        Varb = CommandVarb.Login;
        NamedArgs = [| ( "/h", CRVM_String( 256 ) ); ( "/p", CRVM_int32( 1, int32 UInt16.MaxValue ) ); |];
        ValuelessArgs = [| "/f" |];
        NamelessArgs = Array.empty;
        HelpMsgName = "LOGIN";
    }

    /// "logout" command rule.
    static member CmdRule_logout : AcceptableCommand< CommandVarb > = {
        Command = [| "LOGOUT" |];
        Varb = CommandVarb.Logout;
        NamedArgs = Array.empty;
        ValuelessArgs = [| "/y" |];
        NamelessArgs = Array.empty;
        HelpMsgName = "LOGOUT";
    }

    /// "reload" command rule.
    static member CmdRule_reload : AcceptableCommand< CommandVarb > = {
        Command = [| "RELOAD" |];
        Varb = CommandVarb.Reload;
        NamedArgs = Array.empty;
        ValuelessArgs = [| "/y" |];
        NamelessArgs = Array.empty;
        HelpMsgName = "RELOAD";
    }

    /// "select" command rule.
    static member CmdRule_select : AcceptableCommand< CommandVarb > = {
        Command = [| "SELECT" |];
        Varb = CommandVarb.Select;
        NamedArgs = Array.empty
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_uint32( 0u, uint ClientConst.MAX_CHILD_NODE_COUNT - 1u ); |];
        HelpMsgName = "SELECT";
    }

    /// "unselect" command rule.
    static member CmdRule_unselect : AcceptableCommand< CommandVarb > = {
        Command = [| "UNSELECT" |];
        Varb = CommandVarb.UnSelect;
        NamedArgs = [| ( "/p", CRV_uint32( 0u, uint ClientConst.MAX_CHILD_NODE_COUNT - 1u ) ); |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "UNSELECT";
    }

    /// "list" command rule.
    static member CmdRule_list : AcceptableCommand< CommandVarb > = {
        Command = [| "LIST" |];
        Varb = CommandVarb.List;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "LIST";
    }

    /// "listparent" command rule.
    static member CmdRule_listparent : AcceptableCommand< CommandVarb > = {
        Command = [| "LISTPARENT" |];
        Varb = CommandVarb.ListParent;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "LISTPARENT";
    }

    /// "pwd" command rule.
    static member CmdRule_pwd : AcceptableCommand< CommandVarb > = {
        Command = [| "PWD" |];
        Varb = CommandVarb.Pwd;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "PWD";
    }

    /// "values" command rule.
    static member CmdRule_values : AcceptableCommand< CommandVarb > = {
        Command = [| "VALUES" |];
        Varb = CommandVarb.Values;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "VALUES";
    }

    /// "set" command rule.
    static member CmdRule_set : AcceptableCommand< CommandVarb > = {
        Command = [| "SET" |];
        Varb = CommandVarb.Set;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_String( 256 ); CRVM_String( 65536 ); |];
        HelpMsgName = "SET";
    }

    /// "validate" command rule.
    static member CmdRule_validate : AcceptableCommand< CommandVarb > = {
        Command = [| "VALIDATE" |];
        Varb = CommandVarb.Validate;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "VALIDATE";
    }

    /// "publish" command rule.
    static member CmdRule_publish : AcceptableCommand< CommandVarb > = {
        Command = [| "PUBLISH" |];
        Varb = CommandVarb.Publish;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "PUBLISH";
    }

    /// "nop" command rule.
    static member CmdRule_nop : AcceptableCommand< CommandVarb > = {
        Command = [| "NOP" |];
        Varb = CommandVarb.Nop;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "NOP";
    }

    /// "statusall" command rule.
    static member CmdRule_statusall : AcceptableCommand< CommandVarb > = {
        Command = [| "STATUSALL" |];
        Varb = CommandVarb.StatusAll;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "STATUSALL";
    }

    /// "create" command for controller rule.
    static member CmdRule_create_TargetDevice : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE" |];
        Varb = CommandVarb.Create_TargetDevice;
        NamedArgs = [| ( "/n", CRV_String( Constants.MAX_DEVICE_NAME_STR_LENGTH ) ); |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CREATE_TARGET_DEVICE";
    }

    /// "status" command rule.
    static member CmdRule_status : AcceptableCommand< CommandVarb > = {
        Command = [| "STATUS" |];
        Varb = CommandVarb.Status;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "STATUS";
    }

    /// "delete" command rule.
    static member CmdRule_delete : AcceptableCommand< CommandVarb > = {
        Command = [| "DELETE" |];
        Varb = CommandVarb.Delete;
        NamedArgs =
            [| ( "/i", CRV_uint32( 0u, uint ClientConst.MAX_CHILD_NODE_COUNT - 1u ) ); |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "DELETE";
    }

    /// "start" command rule.
    static member CmdRule_start : AcceptableCommand< CommandVarb > = {
        Command = [| "START" |];
        Varb = CommandVarb.Start;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "START";
    }

    /// "kill" command rule.
    static member CmdRule_kill : AcceptableCommand< CommandVarb > = {
        Command = [| "KILL" |];
        Varb = CommandVarb.Kill;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "KILL";
    }

    /// "setlogparam" command rule.
    static member CmdRule_setlogparam : AcceptableCommand< CommandVarb > = {
        Command = [| "SETLOGPARAM" |];
        Varb = CommandVarb.SetLogParam;
        NamedArgs =
            let lvRegexStr =
                LogLevel.Values
                |> Seq.map LogLevel.toString
                |> Seq.map ( fun itr -> "^" + itr + "$" )
                |> String.concat "|"
            [|
                ( "/s", CRV_uint32( Constants.LOGPARAM_MIN_SOFTLIMIT, Constants.LOGPARAM_MAX_SOFTLIMIT ) );
                ( "/h", CRV_uint32( Constants.LOGPARAM_MIN_HARDLIMIT, Constants.LOGPARAM_MAX_HARDLIMIT ) );
                ( "/l", CRV_Regex( Regex( lvRegexStr, RegexOptions.IgnoreCase ) ) );
            |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "SETLOGPARAM";
    }

    /// "getlogparam" command rule.
    static member CmdRule_getlogparam : AcceptableCommand< CommandVarb > = {
        Command = [| "GETLOGPARAM" |];
        Varb = CommandVarb.GetLogParam;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "GETLOGPARAM";
    }

    /// "create NetworkPortal" command rule.
    static member CmdRule_addportal : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE"; "NETWORKPORTAL" |];
        Varb = CommandVarb.Create_NetworkPortal;
        NamedArgs = [|
            ( "/a", CRV_String( Constants.MAX_TARGET_ADDRESS_STR_LENGTH ) );
            ( "/p", CRV_uint32( 1u, uint32 UInt16.MaxValue ) );
        |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CREATE_NETWORKPORTAL";
    }

    /// "create TargetGroup" command for target device rule.
    static member CmdRule_create_TargetGroup : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE"; "TARGETGROUP" |];
        Varb = CommandVarb.Create_TargetGroup;
        NamedArgs = [| ( "/n", CRV_String( Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH ) ); |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CREATE_TARGETGROUP";
    }

    /// "add IPWhiteList" command for network portal rule.
    static member CmdRule_add_IPWhiteList : AcceptableCommand< CommandVarb > = {
        Command = [| "ADD"; "IPWHITELIST" |];
        Varb = CommandVarb.Add_IPWhiteList;
        NamedArgs = [|
            ( "/fadr", CRV_String( 48 ) );
            ( "/fmask", CRV_String( 48 ) );
            ( "/t", CRV_String( 48 ) )
        |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "ADD_IPWHITELIST";
    }

    /// "clear IPWhiteList" command for network portal rule.
    static member CmdRule_clear_IPWhiteList : AcceptableCommand< CommandVarb > = {
        Command = [| "CLEAR"; "IPWHITELIST" |];
        Varb = CommandVarb.Clear_IPWhiteList;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CLEAR_IPWHITELIST";
    }

    /// "load" command rule.
    static member CmdRule_load : AcceptableCommand< CommandVarb > = {
        Command = [| "LOAD" |];
        Varb = CommandVarb.Load;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "LOAD";
    }

    /// "unload" command rule.
    static member CmdRule_unload : AcceptableCommand< CommandVarb > = {
        Command = [| "UNLOAD" |];
        Varb = CommandVarb.UnLoad;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "UNLOAD";
    }

    /// "activate" command rule.
    static member CmdRule_activate : AcceptableCommand< CommandVarb > = {
        Command = [| "ACTIVATE" |];
        Varb = CommandVarb.Activate;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "ACTIVATE";
    }

    /// "inactivate" command rule.
    static member CmdRule_inactivate : AcceptableCommand< CommandVarb > = {
        Command = [| "INACTIVATE" |];
        Varb = CommandVarb.Inactivate;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "INACTIVATE";
    }

    /// "create" command for target group rule.
    static member CmdRule_create_Target : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE" |];
        Varb = CommandVarb.Create_Target;
        NamedArgs = [| ( "/n", CRV_Regex( Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_OBJ ) ); |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CREATE_TARGET";
    }

    /// "setchap" command rule.
    static member CmdRule_setchap : AcceptableCommand< CommandVarb > = {
        Command = [| "SETCHAP" |];
        Varb = CommandVarb.SetChap;
        NamedArgs = [|
            ( "/iu", CRVM_Regex( Constants.USER_NAME_REGEX_OBJ ) );
            ( "/ip", CRVM_Regex( Constants.PASSWORD_REGEX_OBJ ) );
            ( "/tu", CRV_Regex( Constants.USER_NAME_REGEX_OBJ ) );
            ( "/tp", CRV_Regex( Constants.PASSWORD_REGEX_OBJ ) );
        |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "SETCHAP";
    }

    /// "unsetauth" command rule.
    static member CmdRule_unsetauth : AcceptableCommand< CommandVarb > = {
        Command = [| "UNSETAUTH" |];
        Varb = CommandVarb.UnsetAuth;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "UNSETAUTH";
    }

    /// "create" command for target rule.
    static member CmdRule_create_LU : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE" |];
        Varb = CommandVarb.Create_LU;
        NamedArgs = [|
            ( "/l", CRV_LUN );
            ( "/n", CRV_String( Constants.MAX_LU_NAME_STR_LENGTH ) );
        |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CREATE_LU";
    }

    /// "attach" command rule.
    static member CmdRule_attach : AcceptableCommand< CommandVarb > = {
        Command = [| "ATTACH" |];
        Varb = CommandVarb.Attach;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_LUN; |];
        HelpMsgName = "ATTACH";
    }

    /// "detach" command rule.
    static member CmdRule_detach : AcceptableCommand< CommandVarb > = {
        Command = [| "DETACH" |];
        Varb = CommandVarb.Detach;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_LUN; |];
        HelpMsgName = "DETACH";
    }

    /// "create plainfile" command for LU or media rule.
    static member CmdRule_create_Media_PlainFile : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE"; "PLAINFILE" |];
        Varb = CommandVarb.Create_Media_PlainFile;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_String( Constants.MAX_FILENAME_STR_LENGTH ) |];
        HelpMsgName = "CREATE_PLAINFILE";
    }

    /// "create membuffer" command for LU or media rule.
    static member CmdRule_create_Media_MemBuffer : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE"; "MEMBUFFER" |];
        Varb = CommandVarb.Create_Media_MemBuffer;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_uint64( 0UL, UInt64.MaxValue ) |];
        HelpMsgName = "CREATE_MEMBUFFER";
    }

    /// "create debug" command for LU or media rule.
    static member CmdRule_create_Media_Debug : AcceptableCommand< CommandVarb > = {
        Command = [| "CREATE"; "DEBUG" |];
        Varb = CommandVarb.Create_Media_Debug;
        NamedArgs = [||];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CREATE_DEBUG";
    }

    /// "initmedia plainfile" command for LU or media rule.
    static member CmdRule_initmedia_PlainFile : AcceptableCommand< CommandVarb > = {
        Command = [| "INITMEDIA"; "PLAINFILE" |];
        Varb = CommandVarb.InitMedia_PlainFile;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_String( Constants.MAX_FILENAME_STR_LENGTH ); CRVM_int64( 1L, Int64.MaxValue ) |];
        HelpMsgName = "INITMEDIA_PLAINFILE";
    }

    /// "imstatus" command for LU or media rule.
    static member CmdRule_imstatus : AcceptableCommand< CommandVarb > = {
        Command = [| "IMSTATUS"; |];
        Varb = CommandVarb.IMStatus;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "IMSTATUS";
    }

    /// "imkill" command for LU or media rule.
    static member CmdRule_imkill : AcceptableCommand< CommandVarb > = {
        Command = [| "IMKILL"; |];
        Varb = CommandVarb.IMKill;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_uint64( 0UL, UInt64.MaxValue ) |];
        HelpMsgName = "IMKILL";
    }

    /// "sessions" command for LU or media rule.
    static member CmdRule_sessions : AcceptableCommand< CommandVarb > = {
        Command = [| "SESSIONS"; |];
        Varb = CommandVarb.Sessions;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "SESSIONS";
    }

    /// "sesskill" command for LU or media rule.
    static member CmdRule_sesskill : AcceptableCommand< CommandVarb > = {
        Command = [| "SESSKILL"; |];
        Varb = CommandVarb.SessKill;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_uint32( 0u, uint32 UInt16.MaxValue ) |];
        HelpMsgName = "SESSKILL";
    }

    /// "connections" command for LU or media rule.
    static member CmdRule_connections : AcceptableCommand< CommandVarb > = {
        Command = [| "CONNECTIONS"; |];
        Varb = CommandVarb.Connections;
        NamedArgs = [| ( "/s", CRV_uint32( 0u, uint32 UInt16.MaxValue ) ) |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CONNECTIONS";
    }

    /// "lustatus" command for LU or media rule.
    static member CmdRule_lustatus : AcceptableCommand< CommandVarb > = {
        Command = [| "LUSTATUS"; |];
        Varb = CommandVarb.LUStatus;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "LUSTATUS";
    }

    /// "lureset" command for LU or media rule.
    static member CmdRule_lureset : AcceptableCommand< CommandVarb > = {
        Command = [| "LURESET"; |];
        Varb = CommandVarb.LUReset;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "LURESET";
    }

    /// "mediastatus" command for LU or media rule.
    static member CmdRule_mediastatus : AcceptableCommand< CommandVarb > = {
        Command = [| "MEDIASTATUS"; |];
        Varb = CommandVarb.MediaStatus;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "MEDIASTATUS";
    }

    /// "add trap" command for debug media rule.
    static member CmdRule_add_trap : AcceptableCommand< CommandVarb > = {
        Command = [| "ADD"; "TRAP";|];
        Varb = CommandVarb.Add_Trap;
        NamedArgs = [|
            ( "/e", CRVM_Regex( Regex( "TestUnitReady|ReadCapacity|Read|Write|Format", RegexOptions.IgnoreCase ) ) );
            ( "/slba", CRV_uint64( 0UL, UInt64.MaxValue ) );
            ( "/elba", CRV_uint64( 0UL, UInt64.MaxValue ) );
            ( "/a", CRVM_Regex( Regex( "ACA|LUReset|Count|Delay|Wait", RegexOptions.IgnoreCase ) ) );
            ( "/msg", CRV_String( 256 ) );
            ( "/idx", CRV_int32( Int32.MinValue, Int32.MaxValue ) );
            ( "/ms", CRV_int32( 0, Int32.MaxValue ) );
        |];
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "ADD_TRAP";
    }

    /// "clear trap" command for debug media rule.
    static member CmdRule_clear_trap : AcceptableCommand< CommandVarb > = {
        Command = [| "CLEAR"; "TRAP";|];
        Varb = CommandVarb.Clear_Trap;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "CLEAR_TRAP";
    }

    /// "traps" command for debug media rule.
    static member CmdRule_traps : AcceptableCommand< CommandVarb > = {
        Command = [| "TRAPS";|];
        Varb = CommandVarb.Traps;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "TRAPS";
    }

    /// "task list" command for debug media rule.
    static member CmdRule_task_list : AcceptableCommand< CommandVarb > = {
        Command = [| "TASK"; "LIST"; |];
        Varb = CommandVarb.Task_List;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = Array.empty;
        HelpMsgName = "TASK_LIST";
    }

    /// "task resume" command for debug media rule.
    static member CmdRule_task_resume : AcceptableCommand< CommandVarb > = {
        Command = [| "TASK"; "RESUME"; |];
        Varb = CommandVarb.Task_Resume;
        NamedArgs = Array.empty;
        ValuelessArgs = Array.empty;
        NamelessArgs = [| CRVM_uint32( 0u, uint32 UInt16.MaxValue ); CRVM_uint32( 0u, UInt32.MaxValue ) |];
        HelpMsgName = "TASK_RESUME";
    }

    /// <summary>
    ///  Get command from standerd input.
    /// </summary>
    /// <param name="infile">
    ///  Stream which read command from.
    /// </param>
    /// <param name="outfile">
    ///  Stream which output prompt string to.
    /// </param>
    /// <param name="accCommands">
    ///  Acceptable command.
    /// </param>
    /// <param name="prp">
    ///  prompt string.
    /// </param>
    /// <returns>
    ///  Instance of EnteredCommand.
    /// </returns>
    /// <exceptions>
    ///  If validation failed, CommandInputError exception is raised.
    /// </exceptions>
    static member InputCommand
        ( infile : TextReader )
        ( outfile : TextWriter )
        ( accCommands : AcceptableCommand<CommandVarb> array )
        ( prp : string ) : Task<CommandParser<CommandVarb>> =

        let loop ( _ : string ) =
            task {
                fprintf outfile "%s> "prp
                let! line = infile.ReadLineAsync()
                if line.Length = 0 then
                    return struct( true, "" )
                else
                    return struct( false, line )
            }
        task {
            let! line = Functions.loopAsyncWithState loop ""
            return CommandParser.FromString accCommands line
        }

