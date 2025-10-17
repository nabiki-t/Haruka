//=============================================================================
// Haruka Software Storage.
// CommandReader.fs : Enter and execute the command.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Generic
open System.Threading.Tasks
open System.Net
open System.Net.Sockets

open Haruka.IODataTypes
open Haruka.Constants
open Haruka.Commons
open System.Diagnostics

//=============================================================================
// Class implementation

/// <summary>
///  Definition of CommandReader class.
/// </summary>
/// <param name="m_Messages">
///   Message table.
/// </param>
/// <param name="m_InFile">
///   Stream which read command from.
/// </param>
/// <param name="m_OutFile">
///   The stream to use as the output destination for the execution results.
/// </param>
type CommandRunner( m_Messages : StringTable, m_InFile : TextReader, m_OutFile : TextWriter ) =

    /// Commands for NotConnected status.
    static let AccCmd_NotConnected : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_login;
        |]

    /// Commands for Controller.
    static let AccCmd_Controller : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_create_TargetDevice;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_add_IPWhiteList;
            CommandReader.CmdRule_clear_IPWhiteList;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
        |]

    /// Commands for TargetDevice.
    static let AccCmd_TargetDevice : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_setlogparam;
            CommandReader.CmdRule_getlogparam;
            CommandReader.CmdRule_addportal;
            CommandReader.CmdRule_create_TargetGroup;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sessions;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_connections;
        |]

    /// Commands for NetworkPortal.
    static let AccCmd_NetworkPortal : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_add_IPWhiteList;
            CommandReader.CmdRule_clear_IPWhiteList;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_connections;
        |]

    /// Commands for TargetGroup.
    static let AccCmd_TargetGroup : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_load;
            CommandReader.CmdRule_unload;
            CommandReader.CmdRule_activate;
            CommandReader.CmdRule_inactivate;
            CommandReader.CmdRule_create_Target;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sessions;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_connections;
        |]

    /// Commands for Target.
    static let AccCmd_Target : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_load;
            CommandReader.CmdRule_unload;
            CommandReader.CmdRule_activate;
            CommandReader.CmdRule_inactivate;
            CommandReader.CmdRule_setchap;
            CommandReader.CmdRule_unsetauth;
            CommandReader.CmdRule_create_LU;
            CommandReader.CmdRule_attach;
            CommandReader.CmdRule_detach;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sessions;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_connections;
        |]

    /// Commands for LU.
    static let AccCmd_LU : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_load;
            CommandReader.CmdRule_unload;
            CommandReader.CmdRule_activate;
            CommandReader.CmdRule_inactivate;
            CommandReader.CmdRule_create_Media_PlainFile;
            CommandReader.CmdRule_create_Media_MemBuffer;
            CommandReader.CmdRule_create_Media_Debug;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_lustatus;
            CommandReader.CmdRule_lureset;
        |]

    /// Commands for Plain file media.
    static let AccCmd_PlainFileMedia : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_load;
            CommandReader.CmdRule_unload;
            CommandReader.CmdRule_activate;
            CommandReader.CmdRule_inactivate;
            CommandReader.CmdRule_create_Media_PlainFile;
            CommandReader.CmdRule_create_Media_MemBuffer;
            CommandReader.CmdRule_create_Media_Debug;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_mediastatus;
        |]

    /// Commands for MemBuffer media.
    static let AccCmd_MemBufferMedia = AccCmd_PlainFileMedia    // Same as Plain file media

    /// Commands for Dummy media.
    static let AccCmd_DummyMedia = AccCmd_PlainFileMedia    // Same as Plain file media

    /// Commands for Debug media.
    static let AccCmd_DebugMedia : AcceptableCommand< CommandVarb > [] =
        [|
            CommandReader.CmdRule_exit;
            CommandReader.CmdRule_logout;
            CommandReader.CmdRule_reload;
            CommandReader.CmdRule_select;
            CommandReader.CmdRule_unselect;
            CommandReader.CmdRule_list;
            CommandReader.CmdRule_listparent;
            CommandReader.CmdRule_pwd;
            CommandReader.CmdRule_values;
            CommandReader.CmdRule_set;
            CommandReader.CmdRule_validate;
            CommandReader.CmdRule_publish;
            CommandReader.CmdRule_nop;
            CommandReader.CmdRule_statusall;
            CommandReader.CmdRule_status;
            CommandReader.CmdRule_delete;
            CommandReader.CmdRule_start;
            CommandReader.CmdRule_kill;
            CommandReader.CmdRule_load;
            CommandReader.CmdRule_unload;
            CommandReader.CmdRule_activate;
            CommandReader.CmdRule_inactivate;
            CommandReader.CmdRule_create_Media_PlainFile;
            CommandReader.CmdRule_create_Media_MemBuffer;
            CommandReader.CmdRule_create_Media_Debug;
            CommandReader.CmdRule_initmedia_PlainFile;
            CommandReader.CmdRule_imstatus;
            CommandReader.CmdRule_imkill;
            CommandReader.CmdRule_sesskill;
            CommandReader.CmdRule_mediastatus;
            CommandReader.CmdRule_add_trap;
            CommandReader.CmdRule_clear_trap;
            CommandReader.CmdRule_traps;
        |]

    /// <summary>
    ///  Input and run commands.
    /// </summary>
    /// <remarks>
    ///  This function does not return until exit command is executed. Initial status is "Not Connected".
    /// </remarks>
    member this.Run() : unit =
        Functions.loopAsyncWithState this.CommandLoop None
        |> Functions.RunTaskSynchronously
        |> ignore

    /// <summary>
    ///  Input and run commands.
    /// </summary>
    /// <param name="force">
    ///  force connection or not.
    /// </param>
    /// <param name="host">
    ///  host name.
    /// </param>
    /// <param name="port">
    ///  port number.
    /// </param>
    /// <remarks>
    ///  This function does not return until exit command is executed or login failed.
    /// </remarks>
    member this.RunWithLogin ( force : bool ) ( host : string ) ( port : int32 ) : unit =
        task {
            let! initStat = this.Login force host port
            if initStat.IsSome then
                let! _ = Functions.loopAsyncWithState this.CommandLoop initStat
                ()
        }
        |> Functions.RunTaskSynchronously

    /// <summary>
    ///  Output message.
    /// </summary>
    /// <param name="indent">
    ///  Specify the number of indents to be printed at the beginning of a line.
    /// </param>
    /// <param name="msg">
    ///  Specify the message to be output.
    /// </param>
    member private _.Output ( indent : int ) ( msg : string ) =
        for _ = 0 to indent do
            m_OutFile.Write( "  " )
        m_OutFile.WriteLine( msg )

    /// <summary>
    ///  Read one command from standerd input and run the command.
    /// </summary>
    /// <param name="stat">
    ///  Current status of configurations, connection and current node.
    /// </param>
    /// <returns>
    ///  Next status and boolean value that shows continues or not.
    /// </returns>
    member private this.CommandLoop ( stat : ( ServerStatus * CtrlConnection * IConfigureNode ) option ) : 
            Task< struct( bool * ( ServerStatus * CtrlConnection * IConfigureNode ) option ) > =
        task {
            try
                if stat.IsNone then
                    let! cmd = CommandReader.InputCommand m_InFile m_OutFile m_Messages AccCmd_NotConnected "--"
                    match cmd.Varb with
                    | CommandVarb.Exit ->
                        return struct( false, None )
                    | CommandVarb.Login ->
                        let! nextStat = this.Command_Login cmd
                        return struct( true, nextStat )
                    | _ ->
                        this.Output 0 "Unknown command."
                        return struct( true, stat )
                else
                    let ss, cc, cn = stat.Value
                    let accCmd, prompt =
                        match cn with
                        | :? ConfNode_Controller -> AccCmd_Controller, "CR"
                        | :? ConfNode_TargetDevice -> AccCmd_TargetDevice, "TD"
                        | :? ConfNode_NetworkPortal -> AccCmd_NetworkPortal, "NP"
                        | :? ConfNode_TargetGroup -> AccCmd_TargetGroup, "TG"
                        | :? ConfNode_Target -> AccCmd_Target, "T "
                        | :? ILUNode -> AccCmd_LU, "LU"
                        | :? ConfNode_PlainFileMedia -> AccCmd_PlainFileMedia, "MD"
                        | :? ConfNode_MemBufferMedia -> AccCmd_MemBufferMedia, "MD"
                        | :? ConfNode_DummyMedia -> AccCmd_DummyMedia, "MD"
                        | :? ConfNode_DebugMedia -> AccCmd_DebugMedia, "MD"
                        | _ ->
                            raise <| Exception "Unexpected error."

                    let! cmd = CommandReader.InputCommand m_InFile m_OutFile m_Messages accCmd prompt
                    match cmd.Varb with
                    | CommandVarb.Exit ->
                        let! nextStat = this.Command_Exit cmd ss cc cn
                        return struct( nextStat.IsSome, nextStat )

                    | CommandVarb.Logout ->
                        // Same procedure as exit command
                        let! nextStat = this.Command_Exit cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.Reload ->
                        let! nextStat = this.Command_Reload cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.Select ->
                        let nextStat = this.Command_Select cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.UnSelect ->
                        let nextStat = this.Command_UnSelect cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.List ->
                        this.Command_List cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.ListParent ->
                        this.Command_ListParent cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Pwd ->
                        this.Output 0 cn.ShortDescriptString
                        return struct( true, stat )

                    | CommandVarb.Values ->
                        cn.FullDescriptString
                        |> List.iter ( this.Output 0 )
                        return struct( true, stat )

                    | CommandVarb.Set ->
                        let! nextStat = this.Command_Set cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.Validate ->
                        this.Command_Validate cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Publish ->
                        do! ss.Publish cc
                        this.Output 0 ( m_Messages.GetMessage "CMDMSG_CONFIGURATION_PUBLISHED" )
                        return struct( true, stat )

                    | CommandVarb.Nop ->
                        do! cc.NoOperation()
                        return struct( true, stat )

                    | CommandVarb.StatusAll ->
                        do! this.Command_StatusAll cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_TargetDevice ->
                        do! this.Command_Create_TargetDevice cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Status ->
                        do! this.Command_Status cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Delete ->
                        let! nextStat = this.Command_Delete cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.Start ->
                        do! this.Command_Start cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Kill ->
                        do! this.Command_Kill cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.SetLogParam ->
                        do! this.Command_SetLogParam cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.GetLogParam ->
                        do! this.Command_GetLogParam cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_NetworkPortal ->
                        do! this.Command_AddPortal cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_TargetGroup ->
                        do! this.Command_Create_TargetGroup cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Add_IPWhiteList ->
                        let! nextStat = this.Command_Add_IPWhiteList cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.Clear_IPWhiteList ->
                        let! nextStat = this.Command_Clear_IPWhiteList cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.Load ->
                        do! this.Command_Load cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.UnLoad ->
                        do! this.Command_Unload cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Activate ->
                        do! this.Command_Activate cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Inactivate ->
                        do! this.Command_Inactivate cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_Target ->
                        do! this.Command_Create_Target cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.SetChap ->
                        let! nextStat = this.Command_SetChap cmd ss cc cn
                        return struct( true, nextStat )

                    | CommandVarb.UnsetAuth ->
                        do! this.Command_UnsetAuth cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_LU ->
                        do! this.Command_Create_LU cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Attach ->
                        do! this.Command_Attach cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Detach ->
                        do! this.Command_Detach cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_Media_PlainFile ->
                        do! this.Command_Create_Media_PlainFile cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_Media_MemBuffer ->
                        do! this.Command_Create_Media_MemBuffer cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Create_Media_Debug ->
                        do! this.Command_Create_Media_Debug cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.InitMedia_PlainFile ->
                        do! this.Command_InitMedia_PlainFile cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.IMStatus ->
                        do! this.Command_IMStatus cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.IMKill ->
                        do! this.Command_IMKill cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Sessions ->
                        do! this.Command_Sessions cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.SessKill ->
                        do! this.Command_SessKill cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Connections ->
                        do! this.Command_Connections cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.LUStatus ->
                        do! this.Command_LUStatus cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.LUReset ->
                        do! this.Command_LUReset cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.MediaStatus ->
                        do! this.Command_MediaStatus cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Add_Trap ->
                        do! this.Command_AddTrap cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Clear_Trap ->
                        do! this.Command_ClearTrap cmd ss cc cn
                        return struct( true, stat )

                    | CommandVarb.Traps ->
                        do! this.Command_Traps cmd ss cc cn
                        return struct( true, stat )

                    | _ ->
                        this.Output 0 "Unknown command."
                        return struct( true, stat )
            with
            | :? CommandInputError as x ->
                this.Output 0 x.Message
                return true, stat
            | :? RequestError as x ->
                m_Messages.GetMessage( "CMDERR_UNEXPECTED_REQUEST_ERROR", x.Message )
                |> this.Output 0
                return true, stat
            | :? SocketException
            | :? IOException as x ->
                m_Messages.GetMessage( "CMDERR_CONNECTION_ERROR", x.Message )
                |> this.Output 0
                return true, stat
            | :? EditError as x ->
                m_Messages.GetMessage( "CMDERR_UNEXPECTED_EDIT_ERROR", x.Message )
                |> this.Output 0
                return true, stat
            | :? ConfigurationError as x ->
                this.Output 0 x.Message
                return true, stat
        }

    /// <summary>
    ///  Execute login command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <returns>
    ///  Next status. If login was success, it returns pair of ServerStatus, CtrlConnection and IConfigureNode(=Controller object).
    ///  If failed, it returns None.
    /// </returns>
    member private this.Command_Login ( cmd : CommandParser<CommandVarb> ) : Task< ( ServerStatus * CtrlConnection * IConfigureNode ) option > =
        let force = cmd.NamedArgs.ContainsKey( "/f" )
        let host = cmd.DefaultNamedString "/h" "::1"
        let port = cmd.DefaultNamedInt32 "/p" ( int32 Constants.DEFAULT_MNG_CLI_PORT_NUM )
        this.Login force host port

    /// <summary>
    ///  Execute login command.
    /// </summary>
    /// <param name="force">
    ///  force connection or not.
    /// </param>
    /// <param name="host">
    ///  host name.
    /// </param>
    /// <param name="port">
    ///  port number.
    /// </param>
    /// <returns>
    ///  Next status. If login was success, it returns pair of ServerStatus, CtrlConnection and IConfigureNode(=Controller object).
    ///  If failed, it returns None.
    /// </returns>
    member private this.Login ( force : bool ) ( host : string ) ( port : int32 ) : Task< ( ServerStatus * CtrlConnection * IConfigureNode ) option > =
        task {
            try
                let! cc1 = CtrlConnection.Connect m_Messages host port force
                let ss = new ServerStatus( m_Messages )
                do! ss.LoadConfigure cc1 true
                return Some ( ss, cc1, ss.ControllerNode :> IConfigureNode )
            with
            | :? RequestError as x ->
                m_Messages.GetMessage( "CMDERR_FAILED_LOGIN", x.Message )
                |> this.Output 0
                return None
            | :? SocketException
            | :? IOException as x ->
                m_Messages.GetMessage( "CMDERR_FAILED_CONNECT_CTRL", x.Message )
                |> this.Output 0
                return None
        }


    /// <summary>
    ///  Execute exit command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    /// <returns>
    ///  Next status.
    /// </returns>
    member private this.Command_Exit
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task< ( ServerStatus * CtrlConnection * IConfigureNode ) option > =

        task {
            let force = cmd.NamedArgs.ContainsKey( "/y" )
            if ss.IsModified && not force then
                m_Messages.GetMessage( "CMDMSG_CONFIG_MODIFIED" )
                |> this.Output 0
                return Some ( ss, cc, cn )
            else
                try
                    do! cc.Logout()
                with
                | :? RequestError as x ->
                    m_Messages.GetMessage( "CMDERR_UNEXPECTED_REQUEST_ERROR", x.Message )
                    |> this.Output 0
                | :? SocketException
                | :? IOException as x ->
                    m_Messages.GetMessage( "CMDERR_CONNECTION_ERROR", x.Message )
                    |> this.Output 0

                // Even if logout was failed, disconnect the connection.
                cc.Dispose()
                return None
        }

    /// <summary>
    ///  Execute reload command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    /// <returns>
    ///  Next status.
    /// </returns>
    member private this.Command_Reload
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task< ( ServerStatus * CtrlConnection * IConfigureNode ) option > =

        task {
            let force = cmd.NamedArgs.ContainsKey( "/y" )
            if ss.IsModified && not force then
                m_Messages.GetMessage( "CMDMSG_CONFIG_MODIFIED" )
                |> this.Output 0
                return Some ( ss, cc, cn )
            else
                let ss2 = new ServerStatus( m_Messages )
                do! ss2.LoadConfigure cc true
                return Some( ss2, cc, ss2.ControllerNode :> IConfigureNode )
        }

    /// <summary>
    ///  Execute select command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    /// <returns>
    ///  Next status.
    /// </returns>
    member private this.Command_Select
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : ( ServerStatus * CtrlConnection * IConfigureNode ) option =

        let idx = cmd.DefaultNamelessUInt32 0 0u
        let child = cn.GetChildNodes<IConfigureNode>()
        if idx >= uint32 child.Length then
            m_Messages.GetMessage( "CMDMSG_MISSING_NODE", sprintf "%d" idx )
            |> this.Output 0
            Some ( ss, cc, cn )
        else
            Some ( ss, cc, child.[ int idx ] )

    /// <summary>
    ///  Execute unselect command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    /// <returns>
    ///  Next status.
    /// </returns>
    member private this.Command_UnSelect
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : ( ServerStatus * CtrlConnection * IConfigureNode ) option =

        let idx = cmd.DefaultNamedUInt32 "/p" 0u
        let parent = cn.GetParentNodes<IConfigureNode>()
        if idx >= uint32 parent.Length then
            m_Messages.GetMessage( "CMDMSG_MISSING_NODE", sprintf "%d" idx )
            |> this.Output 0
            Some ( ss, cc, cn )
        elif parent.Length = 0 then
            // Nothing to do
            Some ( ss, cc, cn )
        else
            Some ( ss, cc, parent.[ int idx ] )

    /// <summary>
    ///  Execute list command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    member private this.Command_List
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode ) : unit =

        let child = cn.GetChildNodes<IConfigureNode>()
        if 0 < child.Length then
            child
            |> List.iteri ( fun i itr ->
                this.Output 1 ( sprintf "% 3d : %s" i itr.ShortDescriptString )
            )
        else
            this.Output 0 ( m_Messages.GetMessage "CMDMSG_MISSING_CHILD_NODE" )

    /// <summary>
    ///  Execute listparent command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    member private this.Command_ListParent
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode ) : unit =

        let parent = cn.GetParentNodes<IConfigureNode>()
        if 0 < parent.Length then
            parent
            |> List.iteri ( fun i itr ->
                this.Output 1 ( sprintf "% 3d : %s" i itr.ShortDescriptString )
            )
        else
            this.Output 0 ( m_Messages.GetMessage "CMDMSG_MISSING_PARENT_NODE" )

    /// <summary>
    ///  Execute set command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    /// <returns>
    ///  Next status.
    /// </returns>
    member private this.Command_Set
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task<( ServerStatus * CtrlConnection * IConfigureNode ) option> =

        let entName = cmd.DefaultNamelessString 0 ""
        let entNameUp =
            entName
                .Trim()
                .ToUpperInvariant()
                .Replace( ':', '.' )
                .Replace( '_', '.' )
                .Replace( '-', '.' )
                .Replace( '/', '.' )
                .Replace( ';', '.' )
        let entValue = cmd.DefaultNamelessString 1 ""

        task {
            match cn with
            | :? ConfNode_Controller as x ->
                match entNameUp with
                | "REMOTECTRL.PORTNUMBER"
                | "PORTNUMBER" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                RemoteCtrl = Some {
                                    oldVal.RemoteCtrl.Value with
                                        PortNum = v;
                                };
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "REMOTECTRL.ADDRESS"
                | "ADDRESS" ->
                    let oldVal = x.GetConfigureData()
                    let newVal = {
                        oldVal with
                            RemoteCtrl = Some {
                                oldVal.RemoteCtrl.Value with
                                    Address = entValue;
                            };
                    }
                    let n = ss.UpdateControllerNode newVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGMAINTENANCE.OUTPUTSTDOUT"
                | "OUTPUTSTDOUT" ->
                    let r, v = Boolean.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "Boolean" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogMaintenance =
                                    Some {
                                        OutputDest = 
                                            match oldVal.LogMaintenance.Value.OutputDest with
                                            | HarukaCtrlConf.U_ToFile( x ) ->
                                                if v then
                                                    HarukaCtrlConf.U_ToStdout( x.TotalLimit )
                                                else
                                                    oldVal.LogMaintenance.Value.OutputDest
                                            | HarukaCtrlConf.U_ToStdout( x ) ->
                                                if v then
                                                    HarukaCtrlConf.U_ToStdout( x )
                                                else
                                                    HarukaCtrlConf.U_ToFile({
                                                        TotalLimit = x;
                                                        MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT;
                                                        ForceSync = Constants.LOGMNT_DEF_FORCESYNC;
                                                    })
                                    }
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGMAINTENANCE.TOTALLIMIT"
                | "TOTALLIMIT" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogMaintenance =
                                    Some {
                                        OutputDest = 
                                            match oldVal.LogMaintenance.Value.OutputDest with
                                            | HarukaCtrlConf.U_ToFile( x ) ->
                                                HarukaCtrlConf.U_ToFile({
                                                    x with
                                                        TotalLimit = v;
                                                })
                                            | HarukaCtrlConf.U_ToStdout( _ ) ->
                                                HarukaCtrlConf.U_ToStdout( v )
                                    }
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGMAINTENANCE.MAXFILECOUNT"
                | "MAXFILECOUNT" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogMaintenance =
                                    Some {
                                        OutputDest = 
                                            match oldVal.LogMaintenance.Value.OutputDest with
                                            | HarukaCtrlConf.U_ToFile( x ) ->
                                                HarukaCtrlConf.U_ToFile({
                                                    x with
                                                        MaxFileCount = v;
                                                })
                                            | HarukaCtrlConf.U_ToStdout( x ) ->
                                                HarukaCtrlConf.U_ToFile({
                                                    TotalLimit = x;
                                                    MaxFileCount = v;
                                                    ForceSync = Constants.LOGMNT_DEF_FORCESYNC;
                                                })
                                    }
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGMAINTENANCE.FORCESYNC"
                | "FORCESYNC" ->
                    let r, v = Boolean.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "bool" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogMaintenance =
                                    Some {
                                        OutputDest = 
                                            match oldVal.LogMaintenance.Value.OutputDest with
                                            | HarukaCtrlConf.U_ToFile( x ) ->
                                                HarukaCtrlConf.U_ToFile({
                                                    x with
                                                        ForceSync = v;
                                                })
                                            | HarukaCtrlConf.U_ToStdout( x ) ->
                                                HarukaCtrlConf.U_ToFile({
                                                    TotalLimit = x;
                                                    MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT;
                                                    ForceSync = v;
                                                })
                                    }
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGPARAMETERS.SOFTLIMIT"
                | "SOFTLIMIT" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogParameters = Some {
                                    oldVal.LogParameters.Value with
                                        SoftLimit = v;
                                };
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGPARAMETERS.HARDLIMIT"
                | "HARDLIMIT" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogParameters = Some {
                                    oldVal.LogParameters.Value with
                                        HardLimit = v;
                                };
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGPARAMETERS.LOGLEVEL"
                | "LOGLEVEL" ->
                    let lvValues =
                        LogLevel.Values
                        |> Seq.map LogLevel.toString
                        |> String.concat ","

                    let r, v = LogLevel.tryFromString entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", sprintf "LogLevel(%s)" lvValues )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let oldVal = x.GetConfigureData()
                        let newVal = {
                            oldVal with
                                LogParameters = Some {
                                    oldVal.LogParameters.Value with
                                        LogLevel = v;
                                };
                        }
                        let n = ss.UpdateControllerNode newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "PortNumber,Address,TotalLimit,MaxFileCount,ForceSync,SoftLimit,HardLimit,LogLevel"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_TargetDevice as x ->
                match entNameUp with
                | "ID" ->
                    try
                        let tdid = tdid_me.fromString entValue
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x tdid x.TargetDeviceName x.NegotiableParameters x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )
                    with
                    | :? FormatException ->
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "TargetDeviceID" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )

                | "NAME" ->
                    do! ss.CheckTargetDeviceUnloaded cc x
                    let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID entValue x.NegotiableParameters x.LogParameters
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NEGOTIABLEPARAMETERS.MAXRECVDATASEGMENTLENGTH"
                | "MAXRECVDATASEGMENTLENGTH" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.NegotiableParameters with
                                MaxRecvDataSegmentLength = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName newVal x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NEGOTIABLEPARAMETERS.MAXBURSTLENGTH"
                | "MAXBURSTLENGTH" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.NegotiableParameters with
                                MaxBurstLength = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName newVal x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NEGOTIABLEPARAMETERS.FIRSTBURSTLENGTH"
                | "FIRSTBURSTLENGTH" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.NegotiableParameters with
                                FirstBurstLength = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName newVal x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NEGOTIABLEPARAMETERS.DEFAULTTIME2WAIT"
                | "DEFAULTTIME2WAIT" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.NegotiableParameters with
                                DefaultTime2Wait = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName newVal x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NEGOTIABLEPARAMETERS.DEFAULTTIME2RETAIN"
                | "DEFAULTTIME2RETAIN" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.NegotiableParameters with
                                DefaultTime2Retain = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName newVal x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NEGOTIABLEPARAMETERS.MAXOUTSTANDINGR2T"
                | "MAXOUTSTANDINGR2T" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.NegotiableParameters with
                                MaxOutstandingR2T = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName newVal x.LogParameters
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGPARAMETERS.SOFTLIMIT"
                | "SOFTLIMIT" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.LogParameters with
                                SoftLimit = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName x.NegotiableParameters newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGPARAMETERS.HARDLIMIT"
                | "HARDLIMIT" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.LogParameters with
                                HardLimit = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName x.NegotiableParameters newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "LOGPARAMETERS.LOGLEVEL"
                | "LOGLEVEL" ->
                    let lvValues =
                        LogLevel.Values
                        |> Seq.map LogLevel.toString
                        |> String.concat ","
                    let r, v = LogLevel.tryFromString entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", sprintf "LogLevel(%s)" lvValues )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let newVal = {
                            x.LogParameters with
                                LogLevel = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateTargetDeviceNode x x.TargetDeviceID x.TargetDeviceName x.NegotiableParameters newVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "ID,Name,MaxRecvDataSegmentLength,MaxBurstLength,FirstBurstLength,DefaultTime2Wait,DefaultTime2Retain,MaxOutstandingR2T,SoftLimit,HardLimit,LogLevel"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_DummyDeviceLU as x ->
                match entNameUp with
                | "LUN" ->
                    try
                        let lun = lun_me.fromStringValue entValue
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateDummyDeviceLUNode x lun ( x :> ILUNode ).LUName
                        return Some ( ss, cc, ( n :> IConfigureNode ) )
                    with
                    | :? FormatException
                    | :? OverflowException ->
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "LUN" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )

                | "NAME" ->
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateDummyDeviceLUNode x ( x :> ILUNode ).LUN entValue
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", "LUN,Name" )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_BlockDeviceLU as x ->
                match entNameUp with
                | "LUN" ->
                    try
                        let lun = lun_me.fromStringValue entValue
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateBlockDeviceLUNode x lun ( x :> ILUNode ).LUName
                        return Some ( ss, cc, ( n :> IConfigureNode ) )
                    with
                    | :? FormatException
                    | :? OverflowException ->
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "LUN" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )

                | "NAME" ->
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateBlockDeviceLUNode x ( x :> ILUNode ).LUN entValue
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", "LUN,Name" )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_TargetGroup as x ->
                match entNameUp with
                | "ID" ->
                    try
                        let tgid = tgid_me.fromString entValue
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateTargetGroupNode x tgid x.TargetGroupName x.EnabledAtStart
                        return Some ( ss, cc, ( n :> IConfigureNode ) )
                    with
                    | :? FormatException ->
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "TargetGroupID" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )

                | "NAME" ->
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateTargetGroupNode x x.TargetGroupID entValue x.EnabledAtStart
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "ENABLEDATSTART" ->
                    let r, v = Boolean.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "bool" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateTargetGroupNode x x.TargetGroupID x.TargetGroupName v
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", "ID,Name,EnabledAtStart" )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_Target as x ->
                match entNameUp with
                | "ID" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                IdentNumber = tnodeidx_me.fromPrim v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateTargetNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "TPGT" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                TargetPortalGroupTag = tpgt_me.fromPrim v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateTargetNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "NAME" ->
                    let nextVal = {
                        x.Values with
                            TargetName = entValue;
                    }
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateTargetNode x nextVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "ALIAS" ->
                    let nextVal = {
                        x.Values with
                            TargetAlias = entValue;
                    }
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateTargetNode x nextVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", "ID,TPGT,Name,Alias" )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_NetworkPortal as x ->
                match entNameUp with
                | "ID" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.NetworkPortal with
                                IdentNumber = netportidx_me.fromPrim v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateNetworkPortalNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "TPGT" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.NetworkPortal with
                                TargetPortalGroupTag = tpgt_me.fromPrim v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateNetworkPortalNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "TARGETADDRESS" ->
                    let nextVal = {
                        x.NetworkPortal with
                            TargetAddress = entValue;
                    }
                    do! ss.CheckTargetDeviceUnloaded cc x
                    let n = ss.UpdateNetworkPortalNode x nextVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "PORTNUMBER" ->
                    let r, v = UInt16.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint16" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.NetworkPortal with
                                PortNumber = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateNetworkPortalNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "DISABLENAGLE" ->
                    let r, v = Boolean.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "bool" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.NetworkPortal with
                                DisableNagle = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateNetworkPortalNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "RECEIVEBUFFERSIZE" ->
                    let r, v = Int32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "int" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.NetworkPortal with
                                ReceiveBufferSize = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateNetworkPortalNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "SENDBUFFERSIZE" ->
                    let r, v = Int32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "int" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.NetworkPortal with
                                SendBufferSize = v;
                        }
                        do! ss.CheckTargetDeviceUnloaded cc x
                        let n = ss.UpdateNetworkPortalNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "ID,TPGT,TargetAddress,PortNumber,DisableNagle,ReceiveBufferSize,SendBufferSize"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_PlainFileMedia as x ->
                match entNameUp with
                | "ID" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                IdentNumber = mediaidx_me.fromPrim v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdatePlainFileMediaNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "MEDIANAME" ->
                    let nextVal = {
                        x.Values with
                            MediaName = entValue;
                    }
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdatePlainFileMediaNode x nextVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "FILENAME" ->
                    let nextVal = {
                        x.Values with
                            FileName = entValue;
                    }
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdatePlainFileMediaNode x nextVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "MAXMULTIPLICITY" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint64" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                MaxMultiplicity = v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdatePlainFileMediaNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "QUEUEWAITTIMEOUT" ->
                    let r, v = Int32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "int" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                QueueWaitTimeOut = v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdatePlainFileMediaNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "WRITEPROTECT" ->
                    let r, v = Boolean.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "bool" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                WriteProtect = v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdatePlainFileMediaNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "ID,MediaName,FileName,BlockSize,MaxMultiplicity,QueueWaitTimeOut,WriteProtect"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_MemBufferMedia as x ->
                match entNameUp with
                | "ID" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                IdentNumber = mediaidx_me.fromPrim v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateMemBufferMediaNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "MEDIANAME" ->
                    let nextVal = {
                        x.Values with
                            MediaName = entValue;
                    }
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateMemBufferMediaNode x nextVal
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "BYTESCOUNT" ->
                    let r, v = UInt64.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint64" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        let nextVal = {
                            x.Values with
                                BytesCount = v;
                        }
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateMemBufferMediaNode x nextVal
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "ID,BytesCount,MediaName"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_DummyMedia as x ->
                match entNameUp with
                | "ID" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateDummyMediaNode x ( mediaidx_me.fromPrim v ) ( x :> IMediaNode ).Name
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "MEDIANAME" ->
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateDummyMediaNode x ( x :> IMediaNode ).IdentNumber entValue
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "ID,MediaName"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | :? ConfNode_DebugMedia as x ->
                match entNameUp with
                | "ID" ->
                    let r, v = UInt32.TryParse entValue
                    if not r then
                        m_Messages.GetMessage( "CMDMSG_PARAMVAL_DATATYPE_MISMATCH", "uint32" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    else
                        do! ss.CheckTargetGroupUnloaded cc x
                        let n = ss.UpdateDebugMediaNode x ( mediaidx_me.fromPrim v ) ( x :> IMediaNode ).Name
                        return Some ( ss, cc, ( n :> IConfigureNode ) )

                | "MEDIANAME" ->
                    do! ss.CheckTargetGroupUnloaded cc x
                    let n = ss.UpdateDebugMediaNode x ( x :> IMediaNode ).IdentNumber entValue
                    return Some ( ss, cc, ( n :> IConfigureNode ) )

                | _ ->
                    let paramname = "ID,MediaName"
                    m_Messages.GetMessage( "CMDMSG_UNKNOWN_PARAMETER_NAME", paramname )
                    |> this.Output 0
                    return Some ( ss, cc, cn )

            | _ ->
                raise <| Exception "Unexpected error."
                return Some ( ss, cc, cn )
        }

    /// <summary>
    ///  Execute Validate command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    member private this.Command_Validate
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode ) : unit =

        let r = ss.Validate()
        if r.Length = 0 then
            m_Messages.GetMessage( "CMDMSG_ALL_VALIDATED" )
            |> this.Output 0
        else
            r
            |> List.iter ( fun ( nid, msg ) ->
                let node = ss.GetNode nid
                sprintf "%s : %s" node.ShortDescriptString msg
                |> this.Output 0
            )
        
    /// <summary>
    ///  Execute StatusAll command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    member private this.Command_StatusAll
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            // Show controller status
            let ctrlnode = ss.ControllerNode :> IConfigFileNode
            if ctrlnode.Modified = ModifiedStatus.Modified then
                this.Output 0 ( sprintf "MODIFIED     : %s" ctrlnode.ShortDescriptString )
            else
                this.Output 0 ( sprintf "NOT MODIFIED : %s" ctrlnode.ShortDescriptString )

            let tdnodes = ss.GetTargetDeviceNodes()
            let! tdprocs = cc.GetTargetDeviceProcs()
            for itrtd in tdnodes do
                // Show target device status
                let tdid = itrtd.TargetDeviceID
                let tddesc = ( itrtd :> IConfigureNode ).ShortDescriptString
                let isRunning = List.exists ( (=) tdid ) tdprocs
                if isRunning then
                    this.Output 1 ( sprintf "RUNNING      : %s" tddesc )
                elif ( itrtd :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                    this.Output 1 ( sprintf "UNLOADED(MOD): %s" tddesc )
                else
                    this.Output 1 ( sprintf "UNLOADED     : %s" tddesc )

                // Show target group status
                let! activeTgs =
                    if isRunning then
                        cc.GetActiveTargetGroups tdid
                    else
                        Task.FromResult []
                let activeTgsHash = activeTgs |> Seq.map _.ID |> HashSet
                let! loadedTgs =
                    if isRunning then
                        cc.GetLoadedTargetGroups tdid
                    else
                        Task.FromResult []
                let loadedTgsHash = loadedTgs |> Seq.map _.ID |> HashSet
                let tgnodes = ( itrtd :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                for itrtg in tgnodes do
                    let tgid = itrtg.TargetGroupID
                    let tgdesc = ( itrtg :> IConfigureNode ).ShortDescriptString
                    if activeTgsHash.Contains tgid then
                        this.Output 2 ( sprintf "ACTIVE       : %s" tgdesc )
                    elif loadedTgsHash.Contains tgid then
                        this.Output 2 ( sprintf "LOADED       : %s" tgdesc )
                    elif ( itrtg :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                        this.Output 2 ( sprintf "UNLOADED(MOD): %s" tgdesc )
                    else
                        this.Output 2( sprintf "UNLOADED     : %s" tgdesc )
        }

    /// <summary>
    ///  Execute Create target device command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    member private this.Command_Create_TargetDevice
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let oldtds = ss.GetTargetDeviceNodes()

            // gen new TargetDeviceID
            let newTdid = ConfNode_TargetDevice.GenNewTargetDeviceID oldtds

            // gen default name
            let defTDName = ConfNode_TargetDevice.GenDefaultTargetDeviceName oldtds

            // Get target device name
            let tdName = cmd.DefaultNamedString "/n" defTDName

            // check child node count
            if List.length( oldtds ) >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                let newNegParam : TargetDeviceConf.T_NegotiableParameters = {
                    MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                    MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
                    FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
                    DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                    DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                    MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
                }
                let newLogParam : TargetDeviceConf.T_LogParameters = {
                    SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                    HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                    LogLevel = LogLevel.LOGLEVEL_INFO;
                }
                let newnode = ss.AddTargetDeviceNode newTdid tdName newNegParam newLogParam
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute Status command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="cc">
    ///  Editing configuration data.
    /// </param>
    /// <param name="ss">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    member private this.Command_Status
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            // Show controller status
            let ctrlnode = ss.ControllerNode :> IConfigFileNode
            if ctrlnode.Modified = ModifiedStatus.Modified then
                this.Output 0 ( sprintf "MODIFIED     : %s" ctrlnode.ShortDescriptString )
            else
                this.Output 0 ( sprintf "NOT MODIFIED : %s" ctrlnode.ShortDescriptString )

            // Show target device status
            match ss.GetAncestorTargetDevice cn with
            | Some ( tdnode ) ->
                let! tdprocs = cc.GetTargetDeviceProcs()
                let tdid = tdnode.TargetDeviceID
                let tddesc = ( tdnode :> IConfigureNode ).ShortDescriptString
                let isRunning = List.exists ( (=) tdid ) tdprocs
                if isRunning then
                    this.Output 1 ( sprintf "RUNNING      : %s" tddesc )
                elif ( tdnode :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                    this.Output 1 ( sprintf "UNLOADED(MOD): %s" tddesc )
                else
                    this.Output 1 ( sprintf "UNLOADED     : %s" tddesc )


                // Show target group status
                match ss.GetAncestorTargetGroup cn with
                | Some ( tgnode ) ->
                    let! activeTgs = 
                        if isRunning then
                            cc.GetActiveTargetGroups tdid
                        else
                            Task.FromResult []
                    let! loadedTgs =
                        if isRunning then
                            cc.GetLoadedTargetGroups tdid
                        else
                            Task.FromResult []
                    let tgid = tgnode.TargetGroupID
                    let tgdesc = ( tgnode :> IConfigureNode ).ShortDescriptString
                    let isActive = activeTgs |> Seq.map _.ID |> Seq.exists ( (=) tgid )
                    let isLoaded = loadedTgs |> Seq.map _.ID |> Seq.exists ( (=) tgid )
                    if isActive then
                        this.Output 2 ( sprintf "ACTIVE       : %s" tgdesc )
                    elif isLoaded then
                        this.Output 2 ( sprintf "LOADED       : %s" tgdesc )
                    elif ( tgnode :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                        this.Output 2 ( sprintf "UNLOADED(MOD): %s" tgdesc )
                    else
                        this.Output 2 ( sprintf "UNLOADED     : %s" tgdesc )
                | _ -> ()
            | _ -> ()
        }

    /// <summary>
    ///  Execute delete command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node.
    /// </param>
    /// <returns>
    ///  Next status.
    /// </returns>
    member private this.Command_Delete
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task<( ServerStatus * CtrlConnection * IConfigureNode ) option> =

        task {
            // Get delete target index
            match cmd.NamedUInt32 "/i" with
            | Some objidx ->
                let child = cn.GetChildNodes<IConfigureNode>()
                if objidx >= uint child.Length then
                    m_Messages.GetMessage( "CMDMSG_MISSING_NODE", sprintf "%d" objidx )
                    |> this.Output 0
                    return Some ( ss, cc, cn )
                else
                    match child.[ int objidx ] with
                    | :? ConfNode_Controller as x ->
                        m_Messages.GetMessage( "CMDMSG_CTRL_NODE_NOT_DELETABLE" )
                        |> this.Output 0
                    | :? ConfNode_TargetDevice as x ->
                        do! ss.CheckTargetDeviceUnloaded cc x
                        ss.DeleteTargetDeviceNode x
                        this.Output 0 ( sprintf "Deleted : %s" child.[ int objidx ].ShortDescriptString )
                    | :? ConfNode_NetworkPortal as x ->
                        do! ss.CheckTargetDeviceUnloaded cc x
                        ss.DeleteNetworkPortalNode x
                        this.Output 0 ( sprintf "Deleted : %s" child.[ int objidx ].ShortDescriptString )
                    | :? ConfNode_TargetGroup as x ->
                        do! ss.CheckTargetGroupUnloaded cc x
                        ss.DeleteTargetGroupNode x
                        this.Output 0( sprintf "Deleted : %s" child.[ int objidx ].ShortDescriptString )
                    | :? ConfNode_Target
                    | :? ConfNode_BlockDeviceLU
                    | :? ConfNode_DummyDeviceLU
                    | :? ConfNode_PlainFileMedia
                    | :? ConfNode_MemBufferMedia
                    | :? ConfNode_DummyMedia
                    | :? ConfNode_DebugMedia as x ->
                        do! ss.CheckTargetGroupUnloaded cc x
                        ss.DeleteNodeInTargetGroup x
                        this.Output 0 ( sprintf "Deleted : %s" child.[ int objidx ].ShortDescriptString )
                    | _ ->
                        raise <| Exception "Unexpected error."
                    return Some ( ss, cc, cn )
            | None ->
                let parents = cn.GetParentNodes<IConfigureNode>()
                if parents.Length <= 0 then
                    m_Messages.GetMessage( "CMDMSG_CTRL_NODE_NOT_DELETABLE" )
                    |> this.Output 0
                    return Some ( ss, cc, cn )
                else
                    match cn with
                    | :? ConfNode_Controller as x ->
                        m_Messages.GetMessage( "CMDMSG_CTRL_NODE_NOT_DELETABLE" )
                        |> this.Output 0
                        return Some ( ss, cc, cn )
                    | :? ConfNode_TargetDevice as x ->
                        do! ss.CheckTargetDeviceUnloaded cc x
                        ss.DeleteTargetDeviceNode x
                        this.Output 0( sprintf "Deleted : %s" cn.ShortDescriptString )
                        return Some ( ss, cc, parents.[0] )
                    | :? ConfNode_NetworkPortal as x ->
                        do! ss.CheckTargetDeviceUnloaded cc x
                        ss.DeleteNetworkPortalNode x
                        this.Output 0 ( sprintf "Deleted : %s" cn.ShortDescriptString )
                        return Some ( ss, cc, parents.[0] )
                    | :? ConfNode_TargetGroup as x ->
                        do! ss.CheckTargetGroupUnloaded cc x
                        ss.DeleteTargetGroupNode x
                        this.Output 0 ( sprintf "Deleted : %s" cn.ShortDescriptString )
                        return Some ( ss, cc, parents.[0] )
                    | :? ConfNode_Target
                    | :? ConfNode_BlockDeviceLU
                    | :? ConfNode_DummyDeviceLU
                    | :? ConfNode_PlainFileMedia
                    | :? ConfNode_MemBufferMedia
                    | :? ConfNode_DummyMedia
                    | :? ConfNode_DebugMedia ->
                        do! ss.CheckTargetGroupUnloaded cc cn
                        ss.DeleteNodeInTargetGroup cn
                        this.Output 0 ( sprintf "Deleted : %s" cn.ShortDescriptString )
                        return Some ( ss, cc, parents.[0] )
                    | _ ->
                        raise <| Exception "Unexpected error."
                        return Some ( ss, cc, parents.[0] )
        }

    /// <summary>
    ///  Execute start command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetDevice or its descendants.
    /// </param>
    member private this.Command_Start
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            // Show target device status
            match ss.GetAncestorTargetDevice cn with
            | Some ( tdnode ) ->
                do! cc.StartTargetDeviceProc tdnode.TargetDeviceID
                this.Output 0( sprintf "Started : %s" ( tdnode :> IConfigureNode ).ShortDescriptString )
            | _ ->
                raise <| Exception "Unexpected error."
        }

    /// <summary>
    ///  Execute kill command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetDevice or its descendants.
    /// </param>
    member private this.Command_Kill
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            match ss.GetAncestorTargetDevice cn with
            | Some ( tdnode ) ->
                // Specified target device should be killed by the controller.
                do! cc.KillTargetDeviceProc tdnode.TargetDeviceID
                this.Output 0 ( sprintf "Killed : %s" ( tdnode :> IConfigureNode ).ShortDescriptString )
            | _ ->
                raise <| Exception "Unexpected error."
        }

    /// <summary>
    ///  Execute SetLogParam command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetDevice.
    /// </param>
    member private this.Command_SetLogParam
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = cn :?> ConfNode_TargetDevice

            // Check the target device is activated nor not.
            let! tdlist = cc.GetTargetDeviceProcs()
            if List.exists ( (=) tdnode.TargetDeviceID ) tdlist then

                // Get current effective log params
                let! curparam = cc.GetLogParameters tdnode.TargetDeviceID

                // Get intput value
                let softLimit = cmd.DefaultNamedUInt32 "/s" curparam.SoftLimit
                let hardLimit = cmd.DefaultNamedUInt32 "/h" curparam.HardLimit
                let logLevel = 
                    let x = cmd.DefaultNamedString "/l" ( LogLevel.toString curparam.LogLevel )
                    LogLevel.fromString( x.ToUpperInvariant() )

                // Set log level
                let conf : TargetDeviceConf.T_LogParameters = {
                    SoftLimit = softLimit;
                    HardLimit = hardLimit;
                    LogLevel = logLevel;
                }
                do! cc.SetLogParameters tdnode.TargetDeviceID conf
                m_Messages.GetMessage( "CMDMSG_LOG_PARAM_UPDATED" )
                |> this.Output 0
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute GetLogParam command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetDevice.
    /// </param>
    member private this.Command_GetLogParam
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = cn :?> ConfNode_TargetDevice

            // Check the target device is activated nor not.
            let! tdlist = cc.GetTargetDeviceProcs()
            if List.exists ( (=) tdnode.TargetDeviceID ) tdlist then

                let! r = cc.GetLogParameters tdnode.TargetDeviceID
                this.Output 0 ( sprintf "SoftLimit : %d" r.SoftLimit )
                this.Output 0 ( sprintf "HardLimit : %d" r.HardLimit )
                this.Output 0 ( sprintf "LogLevel  : %s" ( LogLevel.toString r.LogLevel ) )
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute AddPortal command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetDevice.
    /// </param>
    member private this.Command_AddPortal
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = cn :?> ConfNode_TargetDevice

            // gen ident number
            let newIdent =
                ( tdnode :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                |> ConfNode_NetworkPortal.GenNewID

            let address = cmd.DefaultNamedString "/a" ""
            let portno =
                cmd.DefaultNamedUInt32 "/p" ( uint32 Constants.DEFAULT_ISCSI_PORT_NUM )
                |> uint16

            // check child node count
            let childCount = 
                ( tdnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                |> List.length
            if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                let conf : TargetDeviceConf.T_NetworkPortal = {
                    IdentNumber = newIdent;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetAddress = address;
                    PortNumber = portno;
                    DisableNagle = Constants.DEF_DISABLE_NAGLE_IN_NP;
                    ReceiveBufferSize = Constants.DEF_RECEIVE_BUFFER_SIZE_IN_NP;
                    SendBufferSize = Constants.DEF_SEND_BUFFER_SIZE_IN_NP;
                    WhiteList = [];
                }
                do! ss.CheckTargetDeviceUnloaded cc tdnode
                let newnode = ss.AddNetworkPortalNode tdnode conf
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute Create target group command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetDevice.
    /// </param>
    member private this.Command_Create_TargetGroup
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = cn :?> ConfNode_TargetDevice
            let oldtgs = ( tdnode :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()

            // gen new TargetGroupID
            let newTgid = ConfNode_TargetGroup.GenNewTargetGroupID oldtgs

            // gen default name
            let defTGName = ConfNode_TargetGroup.GenDefaultTargetGroupName oldtgs

            // Get target group name
            let tgName = cmd.DefaultNamedString "/n" defTGName

            // check child node count
            let childCount = 
                ( tdnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                |> List.length
            if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                // create target group node
                let newnode = ss.AddTargetGroupNode tdnode newTgid tgName true
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute Add IPWhiteList command
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_NetworkPortal or ConfNode_Controller.
    /// </param>
    member private this.Command_Add_IPWhiteList
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task< ( ServerStatus * CtrlConnection * IConfigureNode ) option > =

        task {
            let fadr = cmd.NamedString "/fadr"
            let fmask = cmd.NamedString "/fmask"
            let t = cmd.NamedString "/t"

            // The only arguments that can be specified are A and B, or C.
            if not ( ( fadr.IsSome && fmask.IsSome && t.IsNone ) || ( fadr.IsNone && fmask.IsNone && t.IsSome ) ) then
                m_Messages.GetMessage( "CMDMSG_PARAMVAL_INVALID_PARAM_PATTERN" )
                |> this.Output 0
                return Some ( ss, cc, cn )

            else
                let condType =
                    if t.IsSome then
                        IPCondition.ParseUserInput t.Value
                    else
                        IPCondition.ParseUserInput ( fadr.Value, fmask.Value )
                if condType.IsNone then
                    m_Messages.GetMessage( "CMDMSG_PARAMVAL_INVALID_PARAM_PATTERN" )
                    |> this.Output 0
                    return Some ( ss, cc, cn )
                else
                    match cn with
                    | :? ConfNode_Controller as crNode ->
                        let oldVal = crNode.GetConfigureData()
                        if oldVal.RemoteCtrl.Value.WhiteList.Length >= Constants.MAX_IP_WHITELIST_COUNT then
                            m_Messages.GetMessage( "CHKMSG_IP_WHITELIST_TOO_LONG", ( sprintf "%d" Constants.MAX_IP_WHITELIST_COUNT ) )
                            |> this.Output 0
                            return Some ( ss, cc, cn )
                        else
                            let nextVal = {
                                oldVal with
                                    RemoteCtrl = Some {
                                        oldVal.RemoteCtrl.Value with
                                            WhiteList = oldVal.RemoteCtrl.Value.WhiteList @ [ condType.Value ]
                                    }
                            }
                            let n = ss.UpdateControllerNode nextVal
                            this.Output 0 "IP white list updated"
                            return Some ( ss, cc, ( n :> IConfigureNode ) )
                    | :? ConfNode_NetworkPortal as npNode ->
                        if npNode.NetworkPortal.WhiteList.Length >= Constants.MAX_IP_WHITELIST_COUNT then
                            m_Messages.GetMessage( "CHKMSG_IP_WHITELIST_TOO_LONG", ( sprintf "%d" Constants.MAX_IP_WHITELIST_COUNT ) )
                            |> this.Output 0
                            return Some ( ss, cc, cn )
                        else
                            let nextVal = {
                                npNode.NetworkPortal with
                                    WhiteList = npNode.NetworkPortal.WhiteList @ [ condType.Value ]
                            }
                            do! ss.CheckTargetDeviceUnloaded cc cn
                            let n = ss.UpdateNetworkPortalNode npNode nextVal
                            this.Output 0 "IP white list updated"
                            return Some ( ss, cc, ( n :> IConfigureNode ) )
                    | _ ->
                        raise <| Exception "Unexpected error."
                        return Some ( ss, cc, cn )
        }

    /// <summary>
    ///  Execute Clear IPWhiteList command on NetworkPortal node.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_NetworkPortal or ConfNode_Controller.
    /// </param>
    member private this.Command_Clear_IPWhiteList
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task< ( ServerStatus * CtrlConnection * IConfigureNode ) option > =

        task {
            match cn with
            | :? ConfNode_Controller as crNode ->
                let oldVal = crNode.GetConfigureData()
                let nextVal = {
                    oldVal with
                        RemoteCtrl = Some {
                            oldVal.RemoteCtrl.Value with
                                WhiteList = []
                        }
                }
                let n = ss.UpdateControllerNode nextVal
                this.Output 0 "IP white list cleared"
                return Some ( ss, cc, ( n :> IConfigureNode ) )
            | :? ConfNode_NetworkPortal as npNode ->
                let nextVal = {
                    npNode.NetworkPortal with
                        WhiteList = [];
                }
                do! ss.CheckTargetDeviceUnloaded cc cn
                let n = ss.UpdateNetworkPortalNode npNode nextVal
                this.Output 0 "IP white list cleared"
                return Some ( ss, cc, ( n :> IConfigureNode ) )
            | _ ->
                raise <| Exception "Unexpected error."
                return Some ( ss, cc, cn )
        }

    /// <summary>
    ///  Execute load command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetGroup or its descendants.
    /// </param>
    member private this.Command_Load
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            let tgnode = ss.GetAncestorTargetGroup cn
            if tdnode.IsSome && tgnode.IsSome then
                let! tdlist = cc.GetTargetDeviceProcs()
                let tdNodeId = tdnode.Value.TargetDeviceID
                if List.exists ( (=) tdNodeId ) tdlist then
                    do! cc.LoadTargetGroup tdnode.Value.TargetDeviceID tgnode.Value.TargetGroupID
                    this.Output 0 ( sprintf "Loaded : %s" ( tgnode.Value :> IConfigureNode ).ShortDescriptString )
                else
                    this.Output 0 ( sprintf "%s" ( m_Messages.GetMessage "ERRMSG_TARGET_DEVICE_NOT_RUNNING" ) )
            else
                raise <| Exception "Unexpected error."
        }

    /// <summary>
    ///  Execute unload command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetGroup node.
    /// </param>
    member private this.Command_Unload
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            let tgnode = ss.GetAncestorTargetGroup cn
            if tdnode.IsSome && tgnode.IsSome then
                let! tdlist = cc.GetTargetDeviceProcs()
                let tdNodeId = tdnode.Value.TargetDeviceID
                if List.exists ( (=) tdNodeId ) tdlist then
                    do! cc.UnloadTargetGroup tdnode.Value.TargetDeviceID tgnode.Value.TargetGroupID
                    this.Output 0( sprintf "Unloaded : %s" ( tgnode.Value :> IConfigureNode ).ShortDescriptString )
                else
                    this.Output 0 ( sprintf "%s" ( m_Messages.GetMessage "ERRMSG_TARGET_DEVICE_NOT_RUNNING" ) )
            else
                raise <| Exception "Unexpected error."
        }
        
    /// <summary>
    ///  Execute activate command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetGroup node.
    /// </param>
    member private this.Command_Activate
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            let tgnode = ss.GetAncestorTargetGroup cn
            if tdnode.IsSome && tgnode.IsSome then
                let! tdlist = cc.GetTargetDeviceProcs()
                let tdNodeId = tdnode.Value.TargetDeviceID
                if List.exists ( (=) tdNodeId ) tdlist then
                    do! cc.ActivateTargetGroup tdnode.Value.TargetDeviceID tgnode.Value.TargetGroupID
                    this.Output 0 ( sprintf "Activated : %s" ( tgnode.Value :> IConfigureNode ).ShortDescriptString )
                else
                    this.Output 0 ( sprintf "%s" ( m_Messages.GetMessage "ERRMSG_TARGET_DEVICE_NOT_RUNNING" ) )
            else
                raise <| Exception "Unexpected error."
        }
        
    /// <summary>
    ///  Execute inactivate command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetGroup node.
    /// </param>
    member private this.Command_Inactivate
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            let tgnode = ss.GetAncestorTargetGroup cn
            if tdnode.IsSome && tgnode.IsSome then
                let! tdlist = cc.GetTargetDeviceProcs()
                let tdNodeId = tdnode.Value.TargetDeviceID
                if List.exists ( (=) tdNodeId ) tdlist then
                    do! cc.InactivateTargetGroup tdnode.Value.TargetDeviceID tgnode.Value.TargetGroupID
                    this.Output 0 ( sprintf "Inactivated : %s" ( tgnode.Value :> IConfigureNode ).ShortDescriptString )
                else
                    this.Output 0 ( sprintf "%s" ( m_Messages.GetMessage "ERRMSG_TARGET_DEVICE_NOT_RUNNING" ) )
            else
                raise <| Exception "Unexpected error."
        }
        
    /// <summary>
    ///  Execute create target command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_TargetGroup node.
    /// </param>
    member private this.Command_Create_Target
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            let tgnode = cn :?> ConfNode_TargetGroup
            if tdnode.IsNone then raise <| Exception "Unexpected error."

            // get all target nodes in the ancestor target device
            let targetNodes = 
                ( tdnode.Value :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                |> Seq.map ( fun itr -> ( itr :> IConfigureNode ).GetChildNodes<ConfNode_Target>() )
                |> Seq.concat

            // generate default target name
            let defName = ConfNode_Target.GenDefaultTargetName targetNodes

            // Get target name
            let tname = cmd.DefaultNamedString "/n" defName

            // gen ident number
            let newIdent = ConfNode_Target.GenNewID targetNodes

            // check child node count
            let childCount = 
                ( tgnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                |> List.length
            if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                let conf : TargetGroupConf.T_Target = {
                    IdentNumber = newIdent;
                    TargetPortalGroupTag = tpgt_me.zero;
                    TargetName = tname;
                    TargetAlias = "";
                    LUN = [];
                    Auth = TargetGroupConf.T_Auth.U_None();
                }
                do! ss.CheckTargetGroupUnloaded cc cn
                let newnode = ss.AddTargetNode tgnode conf
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute SetChap command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_Target node.
    /// </param>
    member private this.Command_SetChap
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task<( ServerStatus * CtrlConnection * IConfigureNode ) option> =

        task {
            let tnode = cn :?> ConfNode_Target
            let iu = cmd.DefaultNamedString "/iu" "";
            let ip = cmd.DefaultNamedString "/ip" "";
            let tu = cmd.DefaultNamedString "/tu" "";
            let tp = cmd.DefaultNamedString "/tp" "";
            let conf = {
                tnode.Values with
                    Auth = TargetGroupConf.U_CHAP({
                        InitiatorAuth = {
                            UserName = iu;
                            Password = ip;
                        };
                        TargetAuth = {
                            UserName = if tu.Length > 0 && tp.Length > 0 then tu else "";
                            Password = if tu.Length > 0 && tp.Length > 0 then tp else "";
                        }
                    })
            }
            do! ss.CheckTargetGroupUnloaded cc cn
            let nedNode = ss.UpdateTargetNode tnode conf :> IConfigureNode
            this.Output 0 ( sprintf "Set CHAP authentication : %s" nedNode.ShortDescriptString )
            return Some ( ss, cc, nedNode )
        }

    /// <summary>
    ///  Execute unsetauth command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_Target node.
    /// </param>
    member private this.Command_UnsetAuth
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tnode = cn :?> ConfNode_Target
            let conf = {
                tnode.Values with
                    Auth = TargetGroupConf.U_None()
            }
            do! ss.CheckTargetGroupUnloaded cc cn
            let nedNode = ss.UpdateTargetNode tnode conf
            this.Output 0 ( sprintf "Authentication reset : %s" ( nedNode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute create LU command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_Target node.
    /// </param>
    member private this.Command_Create_LU
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tnode = cn :?> ConfNode_Target
            match cmd.NamedLUN "/l" with
            | None ->
                this.Output 0 ( m_Messages.GetMessage "CMDMSG_ADDPARAM_LUN" )
            | Some lun ->
                let childCount = 
                    ( tnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                    |> List.length
                if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                    m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                    |> this.Output 0
                else
                    let luname = cmd.DefaultNamedString "/n" ( sprintf "LU_%d" ( lun_me.toPrim lun ) )
                    do! ss.CheckTargetGroupUnloaded cc cn
                    let newnode = ss.AddBlockDeviceLUNode tnode lun luname
                    this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute Attach command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_Target node.
    /// </param>
    member private this.Command_Attach
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tnode = cn :?> ConfNode_Target
            let tgnode = ss.GetAncestorTargetGroup tnode |> Option.get
            let lunodes = tgnode.GetAccessibleLUNodes()

            match cmd.NamedLUN "/l" with
            | None ->
                this.Output 0 ( m_Messages.GetMessage "CMDMSG_ADDPARAM_LUN" )
            | Some lun ->
                match lunodes |> Seq.tryFind ( _.LUN >> (=) lun ) with
                | None ->
                    this.Output 0 ( m_Messages.GetMessage "CMDMSG_ADDPARAM_MISSING_LUN" )
                | Some x ->
                    let childCount = 
                        ( tnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
                        |> List.length
                    if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                        m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                        |> this.Output 0
                    else
                        do! ss.CheckTargetGroupUnloaded cc cn
                        ss.AddTargetLURelation tnode x
                        this.Output 0 ( sprintf "Attach LU : %s" ( tnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute Detach command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be ConfNode_Target node.
    /// </param>
    member private this.Command_Detach
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tnode = cn :?> ConfNode_Target
            let lunodes = ( tnode :> IConfigureNode ).GetChildNodes<ILUNode>()

            match cmd.NamedLUN "/l" with
            | None ->
                this.Output 0 ( m_Messages.GetMessage "CMDMSG_ADDPARAM_LUN" )
            | Some lun ->
                match lunodes |> Seq.tryFind ( _.LUN >> (=) lun ) with
                | None ->
                    this.Output 0 ( m_Messages.GetMessage "CMDMSG_ADDPARAM_MISSING_LUN" )
                | Some x ->
                    do! ss.CheckTargetGroupUnloaded cc cn
                    ss.DeleteTargetLURelation tnode x
                    this.Output 0 ( sprintf "Detach LU : %s" ( tnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute create_media_PlainFile command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_Create_Media_PlainFile
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            if tdnode.IsNone then raise <| Exception "Unexpected error."

            // gen ident number
            let newIdent =
                ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()
                |> ConfNode_PlainFileMedia.GenNewID

            // get file name
            let fname = cmd.DefaultNamedString "/n" ""

            // check child node count
            let childCount =  cn.GetChildNodes<IConfigureNode>() |> List.length
            if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                // create
                let conf : TargetGroupConf.T_PlainFile = {
                    IdentNumber = newIdent;
                    MediaName = "";
                    FileName = fname;
                    MaxMultiplicity = Constants.PLAINFILE_DEF_MAXMULTIPLICITY;
                    QueueWaitTimeOut = Constants.PLAINFILE_DEF_QUEUEWAITTIMEOUT;
                    WriteProtect = false;
                }
                do! ss.CheckTargetGroupUnloaded cc cn
                let newnode = ss.AddPlainFileMediaNode cn conf
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute create_media_MemBuffer command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_Create_Media_MemBuffer
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            if tdnode.IsNone then raise <| Exception "Unexpected error."

            // gen ident number
            let newIdent =
                ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()
                |> ConfNode_MemBufferMedia.GenNewID

            // get memory buffer size
            let bcnt = cmd.DefaultNamedUInt64 "/s" 0UL

            // check child node count
            let childCount =  cn.GetChildNodes<IConfigureNode>() |> List.length
            if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                // create
                let conf : TargetGroupConf.T_MemBuffer = {
                    IdentNumber = newIdent;
                    MediaName = "";
                    BytesCount = bcnt;
                }
                do! ss.CheckTargetGroupUnloaded cc cn
                let newnode = ss.AddMemBufferMediaNode cn conf
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute create_media_Debug command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_Create_Media_Debug
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            if tdnode.IsNone then raise <| Exception "Unexpected error."

            // gen ident number
            let newIdent =
                ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()
                |> ConfNode_DebugMedia.GenNewID

            // check child node count
            let childCount =  cn.GetChildNodes<IConfigureNode>() |> List.length
            if childCount >= int ClientConst.MAX_CHILD_NODE_COUNT then
                m_Messages.GetMessage( "CMDMSG_TOO_MANY_CHILD" )
                |> this.Output 0
            else
                // create
                do! ss.CheckTargetGroupUnloaded cc cn
                let newnode = ss.AddDebugMediaNode cn newIdent ""
                this.Output 0 ( sprintf "Created : %s" ( newnode :> IConfigureNode ).ShortDescriptString )
        }

    /// <summary>
    ///  Execute InitMedia_PlainFile command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_InitMedia_PlainFile
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let fname = cmd.DefaultNamelessString 0 ""
            let fbytes = cmd.DefaultNamelessInt64 1 0L 

            // Start init media process
            let! pid = cc.CreateMediaFile_PlainFile fname fbytes
            this.Output 0 ( sprintf "Started : ProcID=%d" pid )
        }

    /// <summary>
    ///  Execute IMStatus command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_IMStatus
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            // Get init media process status
            let! stat = cc.GetInitMediaStatus()
            for itr in stat do
                let statStr =
                    match itr.Status with
                    | HarukaCtrlerCtrlRes.U_NotStarted( _ ) ->
                                "NotStarted   "
                    | HarukaCtrlerCtrlRes.U_ProgressCreation( p ) ->
                        sprintf "Progress(%3d%%)" p
                    | HarukaCtrlerCtrlRes.U_Recovery( p ) ->
                        sprintf "Recovery(%3d%%)" p
                    | HarukaCtrlerCtrlRes.U_NormalEnd( _ ) ->
                                "Succeeded    "
                    | HarukaCtrlerCtrlRes.U_AbnormalEnd( _ ) ->
                                "Failed       "
                    
                this.Output 0 ( sprintf "ProcID=%d, %s, %s, %s" itr.ProcID statStr itr.FileType itr.PathName )
                itr.ErrorMessage |> List.iter ( this.Output 1 )
        }

    /// <summary>
    ///  Execute IMKill command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_IMKill
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let pid = cmd.DefaultNamelessUInt64 0 0UL

            // Terminate init media process status
            do! cc.KillInitMediaProc pid
            this.Output 0 ( sprintf "Terminated : %d" pid )
        }

    /// <summary>
    ///  Execute Sessions command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_Sessions
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            // get current target device node.
            match ss.GetAncestorTargetDevice cn with
            | None ->
                raise <| Exception "Unexpected error."
            | Some( tdnode ) ->
                // Check the target device is activated nor not.
                let! tdlist = cc.GetTargetDeviceProcs()
                if List.exists ( (=) tdnode.TargetDeviceID ) tdlist then

                    let! sessList =
                        match cn with
                        | :? ConfNode_TargetDevice as x ->
                            cc.GetSession_InTargetDevice x.TargetDeviceID
                        | :? ConfNode_TargetGroup as x ->
                            cc.GetSession_InTargetGroup ( tdnode.TargetDeviceID ) x.TargetGroupID
                        | :? ConfNode_Target as x ->
                            cc.GetSession_InTarget ( tdnode.TargetDeviceID ) x.Values.IdentNumber
                        | _ ->
                            raise <| Exception "Unexpected error."

                    for itrs in sessList do
                        this.Output 0 ( sprintf "Session( TSIH : %d )" ( tsih_me.toPrim itrs.TSIH ) )
                        this.Output 1 ( sprintf "I_T Nexus       : %s" ( itrs.ITNexus.ToString() ) )
                        this.Output 1 ( sprintf "Target group ID : %s" ( tgid_me.toString itrs.TargetGroupID ) )
                        this.Output 1 ( sprintf "Target node ID  : %d" ( tnodeidx_me.toPrim itrs.TargetNodeID ) )
                        this.Output 1 ( sprintf "Establish time  : %s" ( itrs.EstablishTime.ToString( "YYYY/MM/DD hh:mm:ss" ) ) )
                        this.Output 1 ( sprintf "Session parameters : {"  )
                        this.Output 2 ( sprintf "MaxConnections      : %d" ( itrs.SessionParameters.MaxConnections ) )
                        this.Output 2 ( sprintf "InitiatorAlias      : %s" ( itrs.SessionParameters.InitiatorAlias ) )
                        this.Output 2 ( sprintf "InitialR2T          : %b" ( itrs.SessionParameters.InitialR2T ) )
                        this.Output 2 ( sprintf "ImmediateData       : %b" ( itrs.SessionParameters.ImmediateData ) )
                        this.Output 2 ( sprintf "MaxBurstLength      : %d" ( itrs.SessionParameters.MaxBurstLength ) )
                        this.Output 2 ( sprintf "FirstBurstLength    : %d" ( itrs.SessionParameters.FirstBurstLength ) )
                        this.Output 2 ( sprintf "DefaultTime2Wait    : %d" ( itrs.SessionParameters.DefaultTime2Wait ) )
                        this.Output 2 ( sprintf "DefaultTime2Retain  : %d" ( itrs.SessionParameters.DefaultTime2Retain ) )
                        this.Output 2 ( sprintf "MaxOutstandingR2T   : %d" ( itrs.SessionParameters.MaxOutstandingR2T ) )
                        this.Output 2 ( sprintf "DataPDUInOrder      : %b" ( itrs.SessionParameters.DataPDUInOrder ) )
                        this.Output 2 ( sprintf "DataSequenceInOrder : %b" ( itrs.SessionParameters.DataSequenceInOrder ) )
                        this.Output 2 ( sprintf "ErrorRecoveryLevel  : %d" ( itrs.SessionParameters.ErrorRecoveryLevel ) )
                        this.Output 1 ( sprintf "}"  )
                else
                    m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                    |> this.Output 0
        }

    /// <summary>
    ///  Execute SessKill command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_SessKill
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            // get current target device node.
            let tdnoe = ss.GetAncestorTargetDevice cn
            let tsih = cmd.NamelessUInt32 0
            let! tdlist = cc.GetTargetDeviceProcs()

            if tdnoe.IsNone || tsih.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnoe.Value.TargetDeviceID ) tdlist then
                let tsihv = tsih_me.fromPrim ( uint16 tsih.Value )
                do! cc.DestructSession tdnoe.Value.TargetDeviceID tsihv
                this.Output 0( sprintf "Session terminated. TSIH : %d" tsih.Value )
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute Connections command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_Connections
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            // get current target device node.
            match ss.GetAncestorTargetDevice cn with
            | None ->
                raise <| Exception "Unexpected error."
            | Some( tdnode ) ->
                // Check the target device is activated nor not.
                let! tdlist = cc.GetTargetDeviceProcs()
                if List.exists ( (=) tdnode.TargetDeviceID ) tdlist then

                    let tsih = cmd.NamedUInt32 "/s"

                    let! conList =
                        if tsih.IsSome then
                            cc.GetConnection_InSession tdnode.TargetDeviceID ( tsih_me.fromPrim ( uint16 tsih.Value ) )
                        else
                            match cn with
                            | :? ConfNode_TargetDevice as _ ->
                                cc.GetConnection_InTargetDevice tdnode.TargetDeviceID
                            | :? ConfNode_NetworkPortal as x ->
                                cc.GetConnection_InNetworkPortal tdnode.TargetDeviceID x.NetworkPortal.IdentNumber
                            | :? ConfNode_TargetGroup as x ->
                                cc.GetConnection_InTargetGroup ( tdnode.TargetDeviceID ) x.TargetGroupID
                            | :? ConfNode_Target as x ->
                                cc.GetConnection_InTarget ( tdnode.TargetDeviceID ) x.Values.IdentNumber
                            | _ ->
                                raise <| Exception "Unexpected error."

                    conList |> List.iter ( fun itrc ->
                        this.Output 0 ( sprintf "Connection( CID : %d, Counter : %d )" ( cid_me.toPrim itrc.ConnectionID ) ( concnt_me.toPrim itrc.ConnectionCount ) )
                        this.Output 1 ( sprintf "TSIH       : %d" ( tsih_me.toPrim itrc.TSIH ) )
                        this.Output 1 ( sprintf "Establish time  : %s" ( itrc.EstablishTime.ToString( "YYYY/MM/DD hh:mm:ss" ) ) )
                        this.Output 1 ( sprintf "Connection parameters : {"  )
                        this.Output 2 ( sprintf "AuthMethod                          : %s" ( itrc.ConnectionParameters.AuthMethod ) )
                        this.Output 2 ( sprintf "HeaderDigest                        : %s" ( itrc.ConnectionParameters.HeaderDigest ) )
                        this.Output 2 ( sprintf "DataDigest                          : %s" ( itrc.ConnectionParameters.DataDigest ) )
                        this.Output 2 ( sprintf "MaxRecvDataSegmentLength(Initiator) : %d" ( itrc.ConnectionParameters.MaxRecvDataSegmentLength_I ) )
                        this.Output 2 ( sprintf "MaxRecvDataSegmentLength(Target)    : %d" ( itrc.ConnectionParameters.MaxRecvDataSegmentLength_T ) )
                        this.Output 1 ( sprintf "}" )
                        this.Output 1 ( sprintf "Usage( Time, Recv Bytes/s, Send Bytes/s )" )
                        let usageseq = Functions.PairByIndex [| itrc.ReceiveBytesCount; itrc.SentBytesCount |] ( fun i -> i.Time ) ( fun i -> i.Value )
                        usageseq
                        |> Seq.iter ( fun ( us_dt, us_val ) ->
                            let dtstr = us_dt.ToString( "YYYY/MM/DD hh:mm:ss" )
                            let recvval = ( Option.defaultValue 0L us_val.[0] ) / Constants.RECOUNTER_SPAN_SEC
                            let sendval = ( Option.defaultValue 0L us_val.[1] ) / Constants.RECOUNTER_SPAN_SEC
                            this.Output 2 ( sprintf "%s, %d, %d" dtstr recvval sendval )
                        )
                    )

                else
                    m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                    |> this.Output 0
        }

    /// <summary>
    ///  Execute LUStatus command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_LUStatus
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnode = ss.GetAncestorTargetDevice cn
            let! tdlist = cc.GetTargetDeviceProcs()

            if tdnode.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnode.Value.TargetDeviceID ) tdlist then
                match cn with
                | :? ILUNode as x ->
                    let! lustat = cc.GetLUStatus tdnode.Value.TargetDeviceID x.LUN
                    this.Output 0 ( sprintf "LU Status( LUN : %s )" ( lun_me.toString x.LUN ) )

                    if lustat.ACAStatus.IsNone then
                        this.Output 1 "ACA : None"
                    else
                        let statusCodeStr =
                            lustat.ACAStatus.Value.StatusCode
                            |> Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue< byte, ScsiCmdStatCd >
                            |> Constants.getScsiCmdStatNameFromValue
                        let senseKeyStr =
                            lustat.ACAStatus.Value.SenseKey
                            |> Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue< byte, SenseKeyCd >
                            |> Constants.getSenseKeyNameFromValue
                        let ascStr =
                            lustat.ACAStatus.Value.AdditionalSenseCode
                            |> Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue< uint16, ASCCd >
                            |> Constants.getAscAndAscqNameFromValue
                        this.Output 1 ( sprintf "ACA : {" )
                        this.Output 2 ( sprintf "I_T Nexus : %s" ( lustat.ACAStatus.Value.ITNexus.ToString() ) )
                        this.Output 2 ( sprintf "Status Code : %s" statusCodeStr )
                        this.Output 2 ( sprintf "Sense Key : %s" senseKeyStr )
                        this.Output 2 ( sprintf "Additional Sense Code : %s" ascStr )
                        this.Output 2 ( sprintf "Current : %b" lustat.ACAStatus.Value.IsCurrent )
                        this.Output 1 ( sprintf "}" )

                    this.Output 1 "Usage( Time, Read Bytes/s, Written Bytes/s, Avg Read Sec, Avg Write Sec )"
                    let usageseq =
                        Functions.PairByIndex
                            [| lustat.ReadBytesCount; lustat.WrittenBytesCount; lustat.ReadTickCount; lustat.WriteTickCount |]
                            ( fun i -> i.Time )
                            ( fun i -> i.Value )
                    usageseq
                    |> Seq.iter ( fun ( us_dt, us_val ) ->
                        let dtstr = us_dt.ToString( "YYYY/MM/DD hh:mm:ss" )
                        let readBytesSec = ( Option.defaultValue 0L us_val.[0] ) / Constants.RECOUNTER_SPAN_SEC
                        let writtenBytesSec = ( Option.defaultValue 0L us_val.[1] ) / Constants.RECOUNTER_SPAN_SEC
                        let avgReadSec =
                            ( float ( Option.defaultValue 0L us_val.[2] ) ) / ( float ( Constants.RECOUNTER_SPAN_SEC * Stopwatch.Frequency ) )
                        let avgWriteSec =
                            ( float ( Option.defaultValue 0L us_val.[3] ) ) / ( float ( Constants.RECOUNTER_SPAN_SEC * Stopwatch.Frequency ) )
                        this.Output 2 ( sprintf "%s, %d, %d, %f, %f" dtstr readBytesSec writtenBytesSec avgReadSec avgWriteSec )
                    )
                | _ ->
                    raise <| Exception "Unexpected error."
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute LUReset command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_LUReset
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnoe = ss.GetAncestorTargetDevice cn
            let! tdlist = cc.GetTargetDeviceProcs()

            if tdnoe.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnoe.Value.TargetDeviceID ) tdlist then
                match cn with
                | :? ILUNode as x ->
                    do! cc.LUReset tdnoe.Value.TargetDeviceID x.LUN
                    this.Output 0 "LU Reseted"
                | _ ->
                    raise <| Exception "Unexpected error."
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute MediaStatus command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_MediaStatus
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnoe = ss.GetAncestorTargetDevice cn
            let lunode = ss.GetAncestorLogicalUnit cn
            let! tdlist = cc.GetTargetDeviceProcs()

            if tdnoe.IsNone || lunode.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnoe.Value.TargetDeviceID ) tdlist then
                match cn with
                | :? IMediaNode as x ->
                    let! mediaStat = cc.GetMediaStatus tdnoe.Value.TargetDeviceID lunode.Value.LUN x.IdentNumber
                    this.Output 0 ( sprintf "Media Status( ID : %d )" ( mediaidx_me.toPrim x.IdentNumber ) )
                    this.Output 1 "Usage( Time, Read Bytes/s, Written Bytes/s, Avg Read Sec, Avg Write Sec )"
                    let usageseq =
                        Functions.PairByIndex
                            [| mediaStat.ReadBytesCount; mediaStat.WrittenBytesCount; mediaStat.ReadTickCount; mediaStat.WriteTickCount |]
                            ( fun i -> i.Time )
                            ( fun i -> i.Value )
                    usageseq
                    |> Seq.iter ( fun ( us_dt, us_val ) ->
                        let dtstr = us_dt.ToString( "YYYY/MM/DD hh:mm:ss" )
                        let readBytesSec = ( Option.defaultValue 0L us_val.[0] ) / Constants.RECOUNTER_SPAN_SEC
                        let writtenBytesSec = ( Option.defaultValue 0L us_val.[1] ) / Constants.RECOUNTER_SPAN_SEC
                        let avgReadSec =
                            ( float ( Option.defaultValue 0L us_val.[2] ) ) / ( float ( Constants.RECOUNTER_SPAN_SEC * Stopwatch.Frequency ) )
                        let avgWriteSec =
                            ( float ( Option.defaultValue 0L us_val.[3] ) ) / ( float ( Constants.RECOUNTER_SPAN_SEC * Stopwatch.Frequency ) )
                        this.Output 2 ( sprintf "%s, %d, %d, %f, %f" dtstr readBytesSec writtenBytesSec avgReadSec avgWriteSec )
                    )
                | _ ->
                    raise <| Exception "Unexpected error."
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute Add Trap command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_AddTrap
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =

        task {
            let tdnoe = ss.GetAncestorTargetDevice cn
            let lunode = ss.GetAncestorLogicalUnit cn
            let! tdlist = cc.GetTargetDeviceProcs()

            let eventStr = cmd.DefaultNamedString "/e" ""
            let actionStr = cmd.DefaultNamedString "/a" ""
            let slba = cmd.DefaultNamedUInt64 "/slba" 0UL
            let elba = cmd.DefaultNamedUInt64 "/elba" UInt64.MaxValue
            let msg = cmd.DefaultNamedString "/msg" ""
            let idx = cmd.DefaultNamedInt32 "/idx" 0
            let msec = cmd.DefaultNamedInt32 "/ms" 0

            let eventVal =
                if String.Compare( eventStr, "TestUnitReady", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_TestUnitReady()
                elif String.Compare( eventStr, "ReadCapacity", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_ReadCapacity()
                elif String.Compare( eventStr, "Read", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_Read( { StartLBA = slba; EndLBA = elba; } )
                elif String.Compare( eventStr, "Write", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_Write( { StartLBA = slba; EndLBA = elba; } )
                elif String.Compare( eventStr, "Format", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_Format()
                else
                    raise <| Exception "Unexpected error."

            let actionVal =
                if String.Compare( actionStr, "ACA", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_ACA( msg )
                elif String.Compare( actionStr, "LUReset", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_LUReset( msg )
                elif String.Compare( actionStr, "Count", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_Count( idx )
                elif String.Compare( actionStr, "Delay", StringComparison.OrdinalIgnoreCase ) = 0 then
                    MediaCtrlReq.U_Delay( msec )
                else
                    raise <| Exception "Unexpected error."

            if tdnoe.IsNone || lunode.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnoe.Value.TargetDeviceID ) tdlist then
                match cn with
                | :? ConfNode_DebugMedia as x ->
                    do! cc.DebugMedia_AddTrap tdnoe.Value.TargetDeviceID lunode.Value.LUN ( x :> IMediaNode ).IdentNumber eventVal actionVal
                    this.Output 0 "Trap added."
                | _ ->
                    raise <| Exception "Unexpected error."
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute Clear Trap command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_ClearTrap
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnoe = ss.GetAncestorTargetDevice cn
            let lunode = ss.GetAncestorLogicalUnit cn
            let! tdlist = cc.GetTargetDeviceProcs()

            if tdnoe.IsNone || lunode.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnoe.Value.TargetDeviceID ) tdlist then
                match cn with
                | :? ConfNode_DebugMedia as x ->
                    do! cc.DebugMedia_ClearTraps tdnoe.Value.TargetDeviceID lunode.Value.LUN ( x :> IMediaNode ).IdentNumber
                    this.Output 0 "Traps cleared."
                | _ ->
                    raise <| Exception "Unexpected error."
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }

    /// <summary>
    ///  Execute Traps command.
    /// </summary>
    /// <param name="cmd">
    ///  User entered command.
    /// </param>
    /// <param name="ss">
    ///  Editing configuration data.
    /// </param>
    /// <param name="cc">
    ///  Connection to the controller.
    /// </param>
    /// <param name="cn">
    ///  Current node. cn must be LU or media node.
    /// </param>
    member private this.Command_Traps
        ( cmd : CommandParser<CommandVarb> ) ( ss : ServerStatus ) ( cc : CtrlConnection ) ( cn : IConfigureNode )
        : Task =
        task {
            let tdnoe = ss.GetAncestorTargetDevice cn
            let lunode = ss.GetAncestorLogicalUnit cn
            let! tdlist = cc.GetTargetDeviceProcs()

            if tdnoe.IsNone || lunode.IsNone then
                raise <| Exception "Unexpected error."
            elif List.exists ( (=) tdnoe.Value.TargetDeviceID ) tdlist then
                match cn with
                | :? ConfNode_DebugMedia as x ->
                    let mediaidx = ( x :> IMediaNode ).IdentNumber
                    let! tlist = cc.DebugMedia_GetAllTraps tdnoe.Value.TargetDeviceID lunode.Value.LUN mediaidx
                    this.Output 0 ( sprintf "Registered traps( ID : %d )" ( mediaidx_me.toPrim mediaidx ) )
                    for itr in tlist do
                        let eventStr =
                            match itr.Event with
                            | MediaCtrlRes.U_TestUnitReady() ->
                                "TestUnitReady"
                            | MediaCtrlRes.U_ReadCapacity() ->
                                "ReadCapacity"
                            | MediaCtrlRes.U_Read( x ) ->
                                sprintf "Read( StartLBA=%d, EndLBA=%d )" x.StartLBA x.EndLBA
                            | MediaCtrlRes.U_Write( x ) ->
                                sprintf "Write( StartLBA=%d, EndLBA=%d )" x.StartLBA x.EndLBA
                            | MediaCtrlRes.U_Format() ->
                                "Format"
                        let actionStr =
                            match itr.Action with
                            | MediaCtrlRes.U_ACA( x ) ->
                                sprintf "ACA( \"%s\" )" x
                            | MediaCtrlRes.U_LUReset( x ) ->
                                sprintf "LUReset( \"%s\" )" x
                            | MediaCtrlRes.U_Count( x ) ->
                                sprintf "Count( Index=%d, Count=%d )" x.Index x.Value
                            | MediaCtrlRes.U_Delay( x ) ->
                                sprintf "Delay( MiliSec=%d )" x
                        this.Output 1 ( sprintf "%s : %s" eventStr actionStr )
                | _ ->
                    raise <| Exception "Unexpected error."
            else
                m_Messages.GetMessage( "ERRMSG_TARGET_DEVICE_NOT_RUNNING" )
                |> this.Output 0
        }
