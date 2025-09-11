//=============================================================================
// Haruka Software Storage.
// Logger.fs : Defines the interfaces of the Logger object.

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Diagnostics

open Haruka.Constants

//=============================================================================
// Type definition

/// <summary>
///   Message ID definition. LogID bitfield has "0xXYYYYYZZ" struture.
///   <list type="table">
///     <listheader>
///       <term>bitfield</term>
///       <description>description</description>
///     </listheader>
///     <item>
///       <term>X</term>
///       <description>Message level. 0x1:Verbose  0x2:Information  0x3:Warning  0x4:Error  0x5:Fatal</description>
///     </item>
///     <item>
///       <term>Y</term>
///       <description>sequential number.</description>
///     </item>
///     <item>
///       <term>Z</term>
///       <description>Reserved</description>
///     </item>
///   </list>
///   <para>
///     "Verbose" is used for debugging or for outputting less important information.
///   </para>
///   <para>
///     "Information" is used to output information that is not a failure, such as the progress of processing.
///   </para>
///   <para>
///     "Warning" is used to output information about recoverable errors.
///   </para>
///   <para>
///     "Error" is used when a failure occurs that does not result in an abnormal termination of the process,
///     but requires the current process to be interrupted.
///   </para>
///   <para>
///     "Fatal" is used when a failure occurs that causes the process to terminate abnormally.
///   </para>
/// </summary>
type LogID =
    | V_INTERFACE_CALLED                    = 0x10000100
    | V_TRACE                               = 0x10000200
    | V_CONFIG_REG_VALUE                    = 0x10000300
    | V_FALLBACK_CONFIG_VALUE               = 0x10000400
    | V_INITMEDIA_PROC_PROGRESS             = 0x10000700
    | V_CTRL_REQ_RECEIVED                   = 0x10000800
    | V_CTRL_REQ_NORMAL_END                 = 0x10000900
    | V_RETRY_EXCHANGE                      = 0x10000A00
    | V_SCSI_TASK_STARTED                   = 0x10000B00
    | V_SCSI_TASK_TERMINATED                = 0x10000C00

    | I_PROCESS_STARTED                     = 0x20000100
    | I_PROCESS_ENDED                       = 0x20000200
    | I_PROCESS_STARTED_BY_MANUALLY         = 0x20000300
    | I_PROCESS_STARTED_BY_SCM              = 0x20000400
    | I_OBJ_INSTANCE_CREATED                = 0x20000500
    | I_START_WAITING_CONNECTION            = 0x20000600
    | I_NEW_CONNECTION_ADDED_TO_SESSION     = 0x20000700
    | I_CONNECTION_REPLACED_IN_SESSION      = 0x20000800
    | I_COMPONENT_INITIALIZED               = 0x20000900
    | I_COMPONENT_NOTICE_DESTROY            = 0x20000A00
    | I_FAILED_GET_CONFFILE_IN_REG          = 0x20000B00
    | I_FAILED_GET_FALLBACK_CONFFILE        = 0x20000C00
    | I_ACCEPT_CONNECTION                   = 0x20000D00
    | I_AUTHENTICATION_SUCCEED              = 0x20000E00
    | I_ALL_CONNECTION_CLOSED               = 0x20000F00
    | I_CONNECTION_CLOSED_GRACEFULLY        = 0x20001000
    | I_CREATE_LU_COMPONENT                 = 0x20001100
    | I_ASSIGN_LU_TO_SESSION                = 0x20001200
    | I_FILE_OPENED                         = 0x20001300
    | I_FILE_CLOSED                         = 0x20001400
    | I_CONNECTION_REMOVED_IN_SESSION       = 0x20001500
    | I_CONNECTION_ALREADY_REMOVED          = 0x20001600
    | I_ACA_STAT_WAS_DUPLICATED             = 0x20001700
    | I_UNEXPECTED_ACA_EXCEPTION            = 0x20001800
    | I_IGNORED_REQ_IN_LURESET              = 0x20001900
    | I_LU_ALREADY_DELETED                  = 0x20001A00
    | I_LU_REMOVED                          = 0x20001B00
    | I_ACA_TASK_ABORTED_IN_NORMAL_STAT     = 0x20001C00
    | I_NORMAL_TASK_ABORTED_IN_ACA_STAT     = 0x20001D00
    | I_NOP_OUT_PING_RESPONSE_RECEIVED      = 0x20001E00
    | I_SESSION_ALREADY_TERMINATED          = 0x20001F00
    | I_MISSING_CID                         = 0x20002000
    | I_NEGOTIATION_RESET                   = 0x20002100
    | I_PDU_ALREADY_REMOVED                 = 0x20002200
    | I_SWP_CHANGED                         = 0x20002300
    | I_DSENSE_CHANGED                      = 0x20002400
    | I_TASK_NOTIFY_TERMINATE               = 0x20002500
    | I_TMF_REQUESTED                       = 0x20002600
    | I_REQUEST_IGNORED                     = 0x20002700
    | I_PR_FILE_VALIDATE_ERROR              = 0x20002800
    | I_FAILED_LOAD_PR_FILE                 = 0x20002900
    | I_SUCCEED_TO_SAVE_PR_FILE             = 0x20002A00
    | I_SUCCEED_TO_DELETE_PR_FILE           = 0x20002B00
    | I_PR_FILE_NOT_EXIST                   = 0x20002C00
    | I_RESERVATION_CONFLICT                = 0x20002D00
    | I_START_DISCOVERY_SESSION             = 0x20002E00
    | I_END_DISCOVERY_SESSION               = 0x20002F00
    | I_UNLOAD_TARGET_GROUP_CONFIG          = 0x20003000
    | I_LOAD_TARGET_GROUP_CONFIG            = 0x20003100
    | I_ENTER_CTRL_REQ_LOOP                 = 0x20003200
    | I_EXIT_CTRL_REQ_LOOP                  = 0x20003300
    | I_MGR_CLI_LOGOUT                      = 0x20003400
    | I_CREATE_TCP_SERVER_PORT              = 0x20003600
    | I_TARGET_GROUP_ACTIVATED              = 0x20003700
    | I_TARGET_GROUP_INACTIVATED            = 0x20003800
    | I_TARGET_DEVICE_PROC_STARTED          = 0x20003900
    | I_CONTROLLER_PROC_STARTED             = 0x20003A00
    | I_CREATE_EMPTY_CONTROLLER_CONF_FILE   = 0x20003B00
    | I_CONTROLLER_CONF_FILE_LOADED         = 0x20003C00
    | I_TARGET_DEVICE_PROC_MISSING          = 0x20003D00
    | I_MGR_CLI_LOGGED_IN                   = 0x20003E00
    | I_TD_PROC_ENTRY_ALREADY_REMOVED       = 0x20003F00
    | I_TD_PROC_TERMINATE_DETECTED          = 0x20004000
    | I_TRY_START_TD_PROC                   = 0x20004100
    | I_KILLED_TARGET_DEVICE_PROC           = 0x20004200
    | I_TRY_START_INITMEDIA_PROC            = 0x20004300
    | I_KILLED_INITMEDIA_PROC               = 0x20004400
    | I_INITMEDIA_PROC_STARTED              = 0x20004500
    | I_INITMEDIA_PROC_START_MSG            = 0x20004600
    | I_INITMEDIA_PROC_CREATE_FILE          = 0x20004700
    | I_INITMEDIA_PROC_END_MSG              = 0x20004800
    | I_INITMEDIA_PROC_NORMAL_END           = 0x20004900
    | I_INITMEDIA_PROC_TRY_DEL_FILE         = 0x20004A00
    | I_INITMEDIA_PROC_DLETED_FILE          = 0x20004B00
    | I_INITMEDIA_PROC_KILLING              = 0x20004C00
    | I_INITMEDIA_PROC_ENTRY_REMOVED        = 0x20004D00
    | I_TARGET_GROUP_STILL_ACTIVE           = 0x20004E00
    | I_TARGET_GROUP_MISSING                = 0x20004F00
    | I_TARGET_GROUP_STILL_USED             = 0x20005000
    | I_TARGET_GROUP_UNLOADED               = 0x20005100
    | I_TARGET_GROUP_ALREADY_INACTIVE       = 0x20005200
    | I_TARGET_DEVICE_STILL_USED            = 0x20005300
    | I_LOG_PARAM_UPDATED                   = 0x20005400
    | I_SESSION_DESTRUCTING                 = 0x20005500
    | I_MISSING_LU                          = 0x20005600
    | I_NOTIFY_LURESET_TO_MEDIA             = 0x20005700
    | I_EXCEPTION_IGNORED                   = 0x20005800
    | I_CTRL_REQ_LURESET                    = 0x20005900
    | I_CTRL_REQ_LURESET_IGNORE             = 0x20005A00
    | I_MISSING_MEDIA                       = 0x20005B00
    | I_UA_ESTABLISHED                      = 0x20005C00
    | I_TRACE                               = 0x20005D00
    | I_CREATE_NEW_SESSION                  = 0x20005E00
    | I_REINSTATE_NEW_SESSION               = 0x20005F00
    | I_SESSION_REMOVED                     = 0x20006000
    | I_SESSION_ALREADY_REINSTATED          = 0x20006100
    | I_LOGOUT_REQUESTED                    = 0x20006200

    | W_UNEXPECTED_ERROR                    = 0x30000000
    | W_DATA_DIGEST_ERROR                   = 0x30000100
    | W_SCSI_COMMAND_PDU_IGNORED            = 0x30000200
    | W_DATA_PDU_IGNORED                    = 0x30000300
    | W_OTHER_PDU_IGNORED                   = 0x30000400
    | W_NEGOTIATION_RESET                   = 0x30000500
    | W_OLD_PDU_DELETED                     = 0x30000600
    | W_SNACK_REQ_REJECTED                  = 0x30000700
    | W_INVALID_CDB_VALUE                   = 0x30000800
    | W_RESERVATION_CONFLICT                = 0x30000900
    | W_FAILED_SAVE_PR_FILE                 = 0x30000A00
    | W_TASK_SET_FULL                       = 0x30000B00
    | W_RETURN_DATA_DROPPED                 = 0x30000C00
    | W_FAILED_UNLOAD_TARGET_GROUP_CONF     = 0x30000D00
    | W_FAILED_LOAD_TARGET_GROUP_CONF       = 0x30000E00
    | W_DETECT_CTRL_PROC_TERMINATED         = 0x30000F00
    | W_FAILED_START_TARGET_DEVICE_PROC     = 0x30001000
    | W_FAILED_GET_DIR_INFO                 = 0x30001200
    | W_FAILED_CREATE_DIR                   = 0x30001300
    | W_FAILED_DELETE_DIR                   = 0x30001400
    | W_FAILED_READ_CONF_FILE               = 0x30001500
    | W_FAILED_WRITE_CONF_FILE              = 0x30001600
    | W_FAILED_DELETE_CONF_FILE             = 0x30001700
    | W_TARGET_DEVICE_PROC_MISSING          = 0x30001800
    | W_ANOTHER_MGR_CLI_USED                = 0x30001900
    | W_MGR_CLI_SESSION_ID_MISMATCH         = 0x30001A00
    | W_TRY_RESTART_TD_PROC                 = 0x30001B00
    | W_TD_PROC_ALREADY_STARTED             = 0x30001C00
    | W_TD_WORK_DIR_MISSING                 = 0x30001D00
    | W_TARGET_DEVICE_COUNT_OVER            = 0x30001E00
    | W_TARGET_GROUP_COUNT_OVER             = 0x30001F00
    | W_LOGICAL_UNIT_COUNT_OVER             = 0x30002000
    | W_CTRL_LOCAL_ADDRESS_NOT_SPECIFIED    = 0x30002100
    | W_FAILED_START_INITMEDIA_PROC         = 0x30002200
    | W_INITMEDIA_PROC_ERROR_MSG            = 0x30002300
    | W_INITMEDIA_PROC_STDERR               = 0x30002400
    | W_INITMEDIA_PROC_ABNORMAL_END         = 0x30002500
    | W_INITMEDIA_PROC_FAIL_DEL_FILE        = 0x30002600
    | W_INITMEDIA_PROC_MULTIPLICITY_OV      = 0x30002700
    | W_INITMEDIA_PROC_MISSING              = 0x30002800
    | W_INITMEDIA_UNEXPECTED_MSG            = 0x30002900
    | W_CTRL_REQ_ERROR_END                  = 0x30002A00
    | W_INVALID_LOG_PARAM                   = 0x30002B00
    | W_MISSING_TSIH                        = 0x30002C00
    | W_SCSI_TASK_TERMINATED_WITH_EXP       = 0x30002D00
    | W_UNIT_ATTENTION_EXISTED              = 0x30002E00
    | W_CONN_REJECTED_DUE_TO_WHITELIST      = 0x30002F00
    | W_CONNECTION_ERROR                    = 0x30003000
    | W_ISCSI_TASK_REMOVED                  = 0x30003100

    | E_UNEXPECTED_ERROR                    = 0x40000000
    | E_FAILED_CREATE_PORTAD                = 0x40000100
    | E_FAILED_CREATE_LU                    = 0x40000200
    | E_FAILED_CREATE_TCP_SERVER_PORT       = 0x40000300
    | E_FAILED_RESOLV_ADDRESS               = 0x40000400
    | E_CONNECTION_CLOSED                   = 0x40000500
    | E_ISCSI_FORMAT_ERROR                  = 0x40000600
    | E_HEADER_DIGEST_ERROR                 = 0x40000700
    | E_PDU_SEND_ERROR                      = 0x40000800
    | E_PDU_RECEIVE_ERROR                   = 0x40000900
    | E_UNSUPPORTED_ISCSI_VERSION           = 0x40000A00
    | E_UNKNOWN_TARGET_NAME                 = 0x40000B00
    | E_UNKNOWN_NEGOTIATION_ERROR           = 0x40000C00
    | E_AUTHENTICATION_FAILURE              = 0x40000D00
    | E_FAILED_CREATE_SESSION               = 0x40000E00
    | E_FAILED_REBUILD_SESSION              = 0x40000F00
    | E_FAILED_ADD_CONNECTION               = 0x40001000
    | E_FAILED_REBUILD_CONNECTION           = 0x40001100
    | E_MISSING_SESSION                     = 0x40001200
    | E_PROTOCOL_ERROR                      = 0x40001300
    | E_SESSION_RECOVERY                    = 0x40001400
    | E_SCSI_ACA_EXCEPTION_RAISED           = 0x40001500
    | E_UNSUPPORTED_SCSI_COMMAND_VALUE      = 0x40001600
    | E_MISSING_LU                          = 0x40001700
    | E_IO_ERROR_RETRY                      = 0x40001800
    | E_FILE_OPEN_ERROR                     = 0x40001900
    | E_FILE_FLUSH_ERROR                    = 0x40001A00
    | E_TASK_IRREGAL_TERMINATED             = 0x40001B00
    | E_UNEXPECTED_ACA_EXCEPTION            = 0x40001C00
    | E_LU_CREATE_RETRY_OVER                = 0x40001D00
    | E_TOO_MANY_CONNECTIONS                = 0x40001E00
    | E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC     = 0x40001F00
    | E_FAILED_RECEIVE_ALL_DATA_OUT_BYTES   = 0x40002000
    | E_STATSN_SIGNIFICANTLY_DIFFERENT      = 0x40002100
    | E_MISSING_RSNACK_REQED_STATUS_PDU     = 0x40002200
    | E_TASK_QUEUE_LIMIT_EXCEEDED           = 0x40002300
    | E_TD_PROC_RETRY_COUNT_OVER            = 0x40002400
    | E_FAILED_START_TD_PROC                = 0x40002500
    | E_FAILED_SEND_REQUEST_TO_TD           = 0x40002600

    | F_UNEXPECTED_ERROR                    = 0x50000000
    | F_UNKNOWN_LOG_MESSAGE_ID              = 0x50000100
    | F_FAILED_LOAD_CONFIG_FILE             = 0x50000200
    | F_CONFIG_FILE_VALIDATE_ERROR          = 0x50000300
    | F_INTERNAL_ASSERTION                  = 0x50000400
    | F_ERROR_EXIT                          = 0x50000500
    | F_STARTUP_ARGUMENT_ERROR              = 0x50000600
    | F_MISSING_CONTROLLER_CONF_FILE        = 0x50000700
    | F_LURESET_REQ_TO_DUMMY_LU             = 0x50000800

[<Struct; IsReadOnly>]
type GenLogMsg( m_LogID : LogID, m_LogLevel : int, m_MsgFormat : string, m_ProcName : string, m_ProcID : int ) =

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen0( objId, cid, conCounter, tsih, itt, lun, ?dummy : unit,
                                    [<CallerMemberName; Optional; DefaultParameterValue("")>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, "", "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen0( struct ( objId, cid, conCounter, tsih, itt, lun ), ?dummy : unit,
                                    [<CallerMemberName; Optional; DefaultParameterValue("")>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, "", "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen0( objId, cmdSource, itt, lun, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, "", "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen0( struct ( objId, cmdSource, itt, lun ), ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, "", "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen0( objId, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, "", "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )


    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen1( objId, cid, conCounter, tsih, itt, lun, a0 : obj, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )


    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen1( struct ( objId, cid, conCounter, tsih, itt, lun ), a0 : obj, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

        
    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen1( objId, cmdSource, itt, lun, a0 : obj, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen1( struct ( objId, cmdSource, itt, lun ), a0 : obj, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen1( objId, a0 : obj, ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, "", "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen2( objId, cid, conCounter, tsih, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen2( struct ( objId, cid, conCounter, tsih, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen2( objId, cmdSource, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen2( struct ( objId, cmdSource, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen2( objId,
                                    a0 : obj,
                                    a1 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen3( objId, cid, conCounter, tsih, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen3( struct ( objId, cid, conCounter, tsih, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen3( objId, cmdSource, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen3( struct ( objId, cmdSource, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen3( objId,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, "", "", "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen4( objId, cid, conCounter, tsih, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen4( struct ( objId, cid, conCounter, tsih, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen4( objId, cmdSource, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen4( struct ( objId, cmdSource, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen4( objId,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, "", "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen5( objId, cid, conCounter, tsih, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen5( struct ( objId, cid, conCounter, tsih, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen5( objId, cmdSource, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen5( struct ( objId, cmdSource, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen5( objId,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )


    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="a5">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen6( objId, cid, conCounter, tsih, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    a5 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, a5, "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )


    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="a5">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen6( struct ( objId, cid, conCounter, tsih, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    a5 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, a5, "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )


    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="a5">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen6( objId, cmdSource, itt, lun,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    a5 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, a5, "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="a5">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen6( struct ( objId, cmdSource, itt, lun ),
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    a5 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, a5, "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="a0">Message argument.</param>
    /// <param name="a1">Message argument.</param>
    /// <param name="a2">Message argument.</param>
    /// <param name="a3">Message argument.</param>
    /// <param name="a4">Message argument.</param>
    /// <param name="a5">Message argument.</param>
    /// <param name="dummy">Dummy argument for prevent mistakes in function names.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.Gen6( objId,
                                    a0 : obj,
                                    a1 : obj,
                                    a2 : obj,
                                    a3 : obj,
                                    a4 : obj,
                                    a5 : obj,
                                    ?dummy : unit,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let msg = String.Format( m_MsgFormat, a0, a1, a2, a3, a4, a5, "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate E_SCSI_ACA_EXCEPTION_RAISED message string.
    /// </summary>
    /// <param name="senseKey">SCSI Sense Key code.</param>
    /// <param name="acc">SCSI ASC code.</param>
    /// <param name="msg">Free format string message.</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.GenACA( struct ( objId, cmdSource, itt, lun ),
                                    senseKey : SenseKeyCd,
                                    acc : ASCCd,
                                    msg : string,
                                    fnname : string option,
                                    source : string option,
                                    line: int option ) : string =
        let p1 = Constants.getSenseKeyNameFromValue senseKey
        let p2 = Constants.getAscAndAscqNameFromValue acc
        let wmsg = String.Format( m_MsgFormat, p1, p2, msg )
        this.GenMessage( objId, cmdSource, itt, lun, wmsg, fnname, source, line )


    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string that represents an exception is ignored.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cid">ConnectionID</param>
    /// <param name="conCounter">Connection Counter</param>
    /// <param name="tsih">TSIH</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="e">Ignored exception</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.GenExp( objId, cid, conCounter, tsih, itt, lun, e : Exception, 
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let tn, msg =
            if e.InnerException <> null then
                let ie = e.InnerException
                ( ie.GetType().Name, ie.Message )
            else
                ( e.GetType().Name, e.Message )
        let msg = String.Format( m_MsgFormat, tn, msg, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string that represents an exception is ignored.
    /// </summary>
    /// <param name="e">Ignored exception</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.GenExp( struct ( objId, cid, conCounter, tsih, itt, lun ), e : Exception, 
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let tn, msg =
            if e.InnerException <> null then
                let ie = e.InnerException
                ( ie.GetType().Name, ie.Message )
            else
                ( e.GetType().Name, e.Message )
        let msg = String.Format( m_MsgFormat, tn, msg, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cid, conCounter, tsih, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string that represents an exception is ignored.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="cmdSource">Command source info</param>
    /// <param name="itt">Initiator Task Tag</param>
    /// <param name="lun">LUN</param>
    /// <param name="e">Ignored exception</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.GenExp( objId, cmdSource, itt, lun, e : Exception, 
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let tn, msg =
            if e.InnerException <> null then
                let ie = e.InnerException
                ( ie.GetType().Name, ie.Message )
            else
                ( e.GetType().Name, e.Message )
        let msg = String.Format( m_MsgFormat, tn, msg, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string that represents an exception is ignored.
    /// </summary>
    /// <param name="e">Ignored exception</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.GenExp( struct ( objId, cmdSource, itt, lun ), e : Exception, 
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let tn, msg =
            if e.InnerException <> null then
                let ie = e.InnerException
                ( ie.GetType().Name, ie.Message )
            else
                ( e.GetType().Name, e.Message )
        let msg = String.Format( m_MsgFormat, tn, msg, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, cmdSource, itt, lun, msg, fnname, source, line )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Generate message string that represents an exception is ignored.
    /// </summary>
    /// <param name="objId">Object ID that identify object instance.</param>
    /// <param name="e">Ignored exception</param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    /// <returns>Generated message string</returns>
    member public this.GenExp( objId, e : Exception, 
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : string =
        let tn, msg =
            if e.InnerException <> null then
                let ie = e.InnerException
                ( ie.GetType().Name, ie.Message )
            else
                ( e.GetType().Name, e.Message )
        let msg = String.Format( m_MsgFormat, tn, msg, "", "", "", "", "", "", "", "" )
        this.GenMessage( objId, ValueNone, ValueNone, ValueNone, msg, fnname, source, line )


    member private _.GenMessage
        (
            objId : OBJIDX_T,
            cid : CID_T ValueOption,
            conCounter : CONCNT_T ValueOption,
            tsih : TSIH_T ValueOption,
            itt : ITT_T ValueOption,
            lun : LUN_T ValueOption,
            message : string,
            fnname : string option,
            source : string option,
            line : int option
        ) : string =
            let dayTime = System.DateTime.UtcNow
            let w_fnname = Option.defaultValue "" fnname
            let w_source = Option.defaultValue "" source |> Path.GetFileName
            let w_line = Option.defaultValue 0 line
            let w_level_c =
                match m_LogLevel with
                | 1 -> 'V'
                | 2 -> 'I'
                | 3 -> 'W'
                | 4 -> 'E'
                | 5 -> 'F'
                | _ -> ' '

            let objIdStr = objidx_me.ToString objId
            let cidStr = if cid.IsSome then String.Format( "CID={0}", cid.Value ) else ""
            let conCounterStr = if conCounter.IsSome then String.Format( "ConCnt={0}", conCounter.Value ) else ""
            let tsihStr = if tsih.IsSome then String.Format( "TSIH={0}", tsih.Value ) else ""
            let ittStr = if itt.IsSome then String.Format( "ITT=0x{0:X8}", itt.Value ) else ""
            let lunStr = if lun.IsSome then String.Format( "LUN={0}", lun.Value ) else ""

            String.Format( "{0:o}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}",
                dayTime, m_ProcName, m_ProcID, w_source, w_line, w_fnname, w_level_c, int m_LogID, 
                objIdStr, cidStr, conCounterStr, tsihStr, ittStr, lunStr, message.Replace( '\n', ' ' ) 
            )
            (*
            sprintf "%s\t%s\t%d\t%s\t%d\t%s\t%c\t0x%08X\t%s\t%s\t%s\t%s\t%s\t%s\t%s"
                ( dayTime.ToString "o" )    // 0
                m_ProcName  // 1
                m_ProcID    // 2
                ( Path.GetFileName w_source ) // 3
                w_line  // 4
                w_fnname    // 5
                w_level_c   // 6
                ( int m_LogID ) // 7
                ( objidx_me.ToString objId )    // 8
                ( cid |> ValueOption.map ( sprintf "CID=%d" ) |> ValueOption.defaultValue "" )  // 9
                ( conCounter |> ValueOption.map ( sprintf "ConCnt=%d" ) |> ValueOption.defaultValue "" )    // 10
                ( tsih |> ValueOption.map ( sprintf "TSIH=%d" ) |> ValueOption.defaultValue "" )    // 11
                ( itt |> ValueOption.map ( sprintf "ITT=0x%08X" ) |> ValueOption.defaultValue "" )  // 12
                ( lun |> ValueOption.map ( lun_me.toString >> (+) "LUN=" ) |> ValueOption.defaultValue "" ) // 13
                ( message.Replace( '\n', ' ' ) )    // 14
                *)

    member private this.GenMessage
        (
            objId : OBJIDX_T,
            cmdSource : CommandSourceInfo ValueOption,
            itt : ITT_T ValueOption,
            lun : LUN_T ValueOption,
            msg : string,
            fnname : string option,
            source : string option,
            line : int option
        ) : string =
        match cmdSource with
        | ValueNone ->
            this.GenMessage( objId, ValueNone, ValueNone, ValueNone, itt, lun, msg, fnname, source, line )
        | ValueSome( x ) ->
            this.GenMessage( objId, ValueSome( x.CID ), ValueSome( x.ConCounter ), ValueSome( x.TSIH ), itt, lun, msg, fnname, source, line )

//=============================================================================
// Class implementation

/// <summary>
///  Log configuration record
/// </summary>
[<NoComparison>]
type LogConfg = {
    /// Maximum verbose message line count that is written per second.
    m_LogCountSoftLimit : uint32;

    /// Maximum log line count that is written per second.
    m_LogCountHardLimit : uint32;

    /// Log output cycle(ms).
    m_OutputCycle : uint32;

    /// Output log level
    m_LogOutputLevel : int;

    /// output target
    m_Writer : TextWriter;
}

[<NoComparison>]
[<NoEquality>]
type LogMessageType =
    | Message of string
    | Flush
    | Callback of ( unit -> unit )

/// <summary>
/// Log writer class.
/// </summary>
[<Serializable>]
type HLogger() =

    /// configuration values
    static let m_LogConfig = OptimisticLock< LogConfg >({
        m_LogCountSoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
        m_LogCountHardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
        m_OutputCycle = Constants.DEFAULT_LOG_OUTPUT_CYCLE;
        m_LogOutputLevel = LogLevel.toInt LOGLEVEL_INFO;
        m_Writer = stderr;
    })

    /// Log message queue
    static let m_MsgQueue = WorkingTaskQueue< LogMessageType >( fun msg -> task {
        try
            let writer = m_LogConfig.obj.m_Writer
            match msg with
            | Message x ->
                do! writer.WriteLineAsync x
            | Flush ->
                do! writer.FlushAsync()
            | Callback f ->
                f()
        with
        | _ -> ()
    })

    /// log output lines counter
    static let mutable m_OutputCounter = 0UL

    /// Current process ID nad name
    static let m_ProcID, m_ProcName =
        let p = Process.GetCurrentProcess()
        p.Id, p.ProcessName

    // ------------------------------------------------------------------------
    /// <summary>
    /// Log message definitions.
    /// It defines relationship of LogID and message. And It includes expected count of message argments.
    /// </summary>
    static let m_Messages =
        seq { 
            ( LogID.V_INTERFACE_CALLED,                 "Component interface is called. method={0}" )
            ( LogID.V_TRACE,                            "Trace. {0}" )
            ( LogID.V_CONFIG_REG_VALUE,                 "Config file name from registory is \"{0}\"" )
            ( LogID.V_FALLBACK_CONFIG_VALUE,            "Fallback config file name \"{0}\"" )
            ( LogID.V_INITMEDIA_PROC_PROGRESS,          "Media creation process progress={0}" )
            ( LogID.V_CTRL_REQ_RECEIVED,                "Control request is received. Message={0}" )
            ( LogID.V_CTRL_REQ_NORMAL_END,              "Control request has completed normaly. Request={0}, Additional Message={1}" )
            ( LogID.V_RETRY_EXCHANGE,                   "Retry update process due to conflict. Subject={0}" )
            ( LogID.V_SCSI_TASK_STARTED,                "SCSI task started. {0}" )
            ( LogID.V_SCSI_TASK_TERMINATED,             "SCSI task terminated. {0}" )

            ( LogID.I_PROCESS_STARTED,                  "Haruka main process started. Arguments={0}" )
            ( LogID.I_PROCESS_ENDED,                    "Haruka main process terminated." )
            ( LogID.I_PROCESS_STARTED_BY_MANUALLY,      "Haruka main process is start at manually. It run on foreground mode." )
            ( LogID.I_PROCESS_STARTED_BY_SCM,           "Haruka main process is start on SCM. It run as windows service." )
            ( LogID.I_OBJ_INSTANCE_CREATED,             "Object {0} is instantiated. {1}" )
            ( LogID.I_START_WAITING_CONNECTION,         "Start to wait a new connection. IdentNumber={0}, PortNumber={1}" )
            ( LogID.I_NEW_CONNECTION_ADDED_TO_SESSION,  "A new connection is added to session. I_T Nexus={0}" )
            ( LogID.I_CONNECTION_REPLACED_IN_SESSION,   "A existing connection is replaced in session. I_T Nexus={0}" )
            ( LogID.I_COMPONENT_INITIALIZED,            "Component {0} is initialized." )
            ( LogID.I_COMPONENT_NOTICE_DESTROY,         "the Destroy event was noticed. Component={0}" )
            ( LogID.I_FAILED_GET_CONFFILE_IN_REG,       "Failed to get configuration file name from registory. KeyName={0}, message={0}" )
            ( LogID.I_FAILED_GET_FALLBACK_CONFFILE,     "Failed to get fallback configuration file name message={0}" )
            ( LogID.I_ACCEPT_CONNECTION,                "Accept a connection. Source address={0}, source port={1}." )
            ( LogID.I_AUTHENTICATION_SUCCEED,           "Authentication is succeeded. Target name={0}" )
            ( LogID.I_ALL_CONNECTION_CLOSED,            "All of connection in the session are closed. This session will be dropeed. I_T Nexus={0}" )
            ( LogID.I_CONNECTION_CLOSED_GRACEFULLY,     "The connection is closed gracefully. TSIH={0}, CID={1}" )
            ( LogID.I_CREATE_LU_COMPONENT,              "A LU Component is created." )
            ( LogID.I_ASSIGN_LU_TO_SESSION,             "A LU Component is assigned to session." )
            ( LogID.I_FILE_OPENED,                      "A file is opened. FileName={0}" )
            ( LogID.I_FILE_CLOSED,                      "A file is closed. FileName={0}" )
            ( LogID.I_CONNECTION_REMOVED_IN_SESSION,    "A existing connection is removed in session. I_T Nexus={0}" )
            ( LogID.I_CONNECTION_ALREADY_REMOVED,       "To remove existing connection is requested, but specified CID is missing. I_T Nexus={0}" )
            ( LogID.I_ACA_STAT_WAS_DUPLICATED,          "ACA status was duplicated, and ignore second ACA status. Sense Key={0}, ASC={1}" )
            ( LogID.I_UNEXPECTED_ACA_EXCEPTION,         "Unexpected ACA exception is detected. Message={0}." )
            ( LogID.I_IGNORED_REQ_IN_LURESET,           "Request was ignored because logical unit reset is in progress. Method={0}" )
            ( LogID.I_LU_ALREADY_DELETED,               "Specified LU is already deleted. Nothing to do. LUN={0}" )
            ( LogID.I_LU_REMOVED,                       "LU object is deleted. LUN={0}" )
            ( LogID.I_ACA_TASK_ABORTED_IN_NORMAL_STAT,  "An ACA task was aborted, but ACA is not established." )
            ( LogID.I_NORMAL_TASK_ABORTED_IN_ACA_STAT,  "A normal task was aborted when ACA is established." )
            ( LogID.I_NOP_OUT_PING_RESPONSE_RECEIVED,   "Ping response of Nop-Out PDU is received. TargetTransferTag={0}" )
            ( LogID.I_SESSION_ALREADY_TERMINATED,       "This session is alresy terminated. This request was ignored." )
            ( LogID.I_MISSING_CID,                      "Specified CID is missing. Received PDU is ignored." )
            ( LogID.I_NEGOTIATION_RESET,                "Text negotiation reset is requested. ITT={0}" )
            ( LogID.I_PDU_ALREADY_REMOVED,              "Specified PDU already removed. {0}" )
            ( LogID.I_SWP_CHANGED,                      "Software Write Protect setting changed. Before={0}, After={1}" )
            ( LogID.I_DSENSE_CHANGED,                   "D_SENSE bit in mode parameter is changed. Before={0}, After={1}" )
            ( LogID.I_TASK_NOTIFY_TERMINATE,            "Task(ITT={0}) is notified tarmination. Description={1}" )
            ( LogID.I_TMF_REQUESTED,                    "Task management request is invoked. Func={0}" )
            ( LogID.I_REQUEST_IGNORED,                  "Request was ignored. Command={0}, Reason={1}." )
            ( LogID.I_PR_FILE_VALIDATE_ERROR,           "Persistent reservation file validation error, ignore this file. PR is all cleared. File={0}, Message={1}" )
            ( LogID.I_FAILED_LOAD_PR_FILE,              "Failed to load Persistent reservation file, ignore this file. PR is all cleared. File={0}, Message={1}" )
            ( LogID.I_SUCCEED_TO_SAVE_PR_FILE,          "Succeed to save persistent reservation file." )
            ( LogID.I_SUCCEED_TO_DELETE_PR_FILE,        "Succeed to delete persistent reservation file." )
            ( LogID.I_PR_FILE_NOT_EXIST,                "Persistent reservation file is not exist. File Name={0}" )
            ( LogID.I_RESERVATION_CONFLICT,             "Reservation conflict. Established reservation={0}, Holder={1}, Key={2}, Requester={3}, Command={4}" )
            ( LogID.I_START_DISCOVERY_SESSION,          "Start discovery session. Initiator Name={0}." )
            ( LogID.I_END_DISCOVERY_SESSION,            "Exit discovery session normaly. Initiator Name={0}" )
            ( LogID.I_UNLOAD_TARGET_GROUP_CONFIG,       "Unload target group configuration. Target group ID={0}" )
            ( LogID.I_LOAD_TARGET_GROUP_CONFIG,         "Load target group configuration. File={0}" )
            ( LogID.I_ENTER_CTRL_REQ_LOOP,              "Enter control request loop." )
            ( LogID.I_EXIT_CTRL_REQ_LOOP,               "Exit control request loop." )
            ( LogID.I_MGR_CLI_LOGOUT,                   "Management client logout. Session ID={0}" )
            ( LogID.I_CREATE_TCP_SERVER_PORT,           "TCP server port is created. Address={0}, Port={1}" )
            ( LogID.I_TARGET_GROUP_ACTIVATED,           "Target group is activated. TargetGroupID={0}" )
            ( LogID.I_TARGET_GROUP_INACTIVATED,         "Target group is inactivated. TargetGroupID={0}" )
            ( LogID.I_TARGET_DEVICE_PROC_STARTED,       "Target device process was started. Target device ID={0}, Process ID={1}" )
            ( LogID.I_CONTROLLER_PROC_STARTED,          "Haruka controller process started. Working dir={0}, Process ID={1}" )
            ( LogID.I_CREATE_EMPTY_CONTROLLER_CONF_FILE,"Empty controller configuration file was created. File={0}" )
            ( LogID.I_CONTROLLER_CONF_FILE_LOADED,      "Haruka controller configuration file was loaded. File={0}" )
            ( LogID.I_TARGET_DEVICE_PROC_MISSING,       "Specified target device process is missing. ID={0}" )
            ( LogID.I_MGR_CLI_LOGGED_IN,                "Management client login. Session ID={0}" )
            ( LogID.I_TD_PROC_ENTRY_ALREADY_REMOVED,    "Target device process entry is already removed. Target Device ID={0}" )
            ( LogID.I_TD_PROC_TERMINATE_DETECTED,       "Target device process was terminated. Target Device ID={0}" )
            ( LogID.I_TRY_START_TD_PROC,                "Try to start target device process. Target Device ID={0}" )
            ( LogID.I_KILLED_TARGET_DEVICE_PROC,        "Target device process was killed for user request. Target device ID={0}, Proccess ID={1}" )
            ( LogID.I_TRY_START_INITMEDIA_PROC,         "Try to start media creation process. Arguments={0}" )
            ( LogID.I_KILLED_INITMEDIA_PROC,            "Media creation process was killed for user request. Proccess ID={0}" )
            ( LogID.I_INITMEDIA_PROC_STARTED,           "Media creation process was started. Process ID={0}" )
            ( LogID.I_INITMEDIA_PROC_START_MSG,         "Media creation process reported start message." )
            ( LogID.I_INITMEDIA_PROC_CREATE_FILE,       "Media creation process will create a file. Name={0}" )
            ( LogID.I_INITMEDIA_PROC_END_MSG,           "Media creation process reported end message. Status={0}" )
            ( LogID.I_INITMEDIA_PROC_NORMAL_END,        "Media creation process has exited normally." )
            ( LogID.I_INITMEDIA_PROC_TRY_DEL_FILE,      "Try to delete a media file created in subprocess." )
            ( LogID.I_INITMEDIA_PROC_DLETED_FILE,       "Succeed to delete a media file. File name={0}" )
            ( LogID.I_INITMEDIA_PROC_KILLING,           "Killing the media creation process. PID={0}" )
            ( LogID.I_INITMEDIA_PROC_ENTRY_REMOVED,     "InitMedia process entry was removed. ProcID={0}" )
            ( LogID.I_TARGET_GROUP_STILL_ACTIVE,        "Specified target group is still active. Ignore request. TargetGroupID={0}" )
            ( LogID.I_TARGET_GROUP_MISSING,             "Specified target group is missing. Ignore request. TargetGroupID={0}" )
            ( LogID.I_TARGET_GROUP_STILL_USED,          "Specified target group is still used. Ignore request. TargetGroupID={0}" )
            ( LogID.I_TARGET_GROUP_UNLOADED,            "Target group has been unloaded. TargetGroupID={0}" )
            ( LogID.I_TARGET_GROUP_ALREADY_INACTIVE,    "Target group has not been activated, or missing. TargetGroupID={0}" )
            ( LogID.I_TARGET_DEVICE_STILL_USED,         "Specified target device is still used. Ignore request." )
            ( LogID.I_LOG_PARAM_UPDATED,                "Log parameters updated. SoftLimit={0}, HardLimit={1}, Level={2}" )
            ( LogID.I_SESSION_DESTRUCTING,              "Session has been destructing. TSIH={0}, I_T Nexus={1}" )
            ( LogID.I_MISSING_LU,                       "Specified LUN is missing. message={0}" )
            ( LogID.I_NOTIFY_LURESET_TO_MEDIA,          "Media termination requested. MediaIdx={0}, Media={1}" )
            ( LogID.I_EXCEPTION_IGNORED,                "An exceptions is ignored. Exception={0}, Message={1}" )
            ( LogID.I_CTRL_REQ_LURESET,                 "LU reset was requested. LUN={0}" )
            ( LogID.I_CTRL_REQ_LURESET_IGNORE,          "LU reset was requested but the LU object has not yet been created. LUN={0}" )
            ( LogID.I_MISSING_MEDIA,                    "Specified media is missing. message={0}" )
            ( LogID.I_UA_ESTABLISHED,                   "Unit attention established. InitiatorPort={0},UA={1}" )
            ( LogID.I_TRACE,                            "Trace. {0}" )
            ( LogID.I_CREATE_NEW_SESSION,               "A new session was created. I_T Nexus = {0}, TSIH={1}" )
            ( LogID.I_REINSTATE_NEW_SESSION,            "The session was rebuilt. I_T Nexus = {0}" )
            ( LogID.I_SESSION_REMOVED,                  "Session object removed." )
            ( LogID.I_SESSION_ALREADY_REINSTATED,       "Session object is already reinstated. Removing request is skipped." )
            ( LogID.I_LOGOUT_REQUESTED,                 "iSCSI logout requested. Reason={0}" )

            ( LogID.W_UNEXPECTED_ERROR,                 "Unexpedted error. Exception={0}, message={1}" )
            ( LogID.W_DATA_DIGEST_ERROR,                "Data digest error. Received PDU is discarded." )
            ( LogID.W_SCSI_COMMAND_PDU_IGNORED,         "SCSI Command PDU is ignored. Reason={0}" )
            ( LogID.W_DATA_PDU_IGNORED,                 "SCSI Data-Out PDU is ignored. Reason={0}" )
            ( LogID.W_OTHER_PDU_IGNORED,                "iSCSI PDU is ignored. Reason={0}" )
            ( LogID.W_NEGOTIATION_RESET,                "Text negotiation is reseted. ITT={0}. Reason={1}" )
            ( LogID.W_OLD_PDU_DELETED,                  "Old PDU that has duplicate StatSN({0}) is exist and delete this PDU." )
            ( LogID.W_SNACK_REQ_REJECTED,               "SNACK request PDU is rejected because ErrorRecoveryLevel is 0." )
            ( LogID.W_INVALID_CDB_VALUE,                "Invalid value in CDB was detected. msg={0}" )
            ( LogID.W_RESERVATION_CONFLICT,             "Reservation conflist. Commnad={0}, Reason={1}" )
            ( LogID.W_FAILED_SAVE_PR_FILE,              "Failed to save Persistent reservation file. File={0}, Message={1}" )
            ( LogID.W_TASK_SET_FULL,                    "Task set full. Task set size={0}, SCSI task count={1}." )
            ( LogID.W_RETURN_DATA_DROPPED,              "Return path already closed. Return PDU was dropped. message={0}" )
            ( LogID.W_FAILED_UNLOAD_TARGET_GROUP_CONF,  "Failed to unload target group configuration. Target group ID={0}" )
            ( LogID.W_FAILED_LOAD_TARGET_GROUP_CONF,    "Failed to load target group configuration. File={0}, Message={1}" )
            ( LogID.W_DETECT_CTRL_PROC_TERMINATED,      "Detected that the haruka controller process has terminated." )
            ( LogID.W_FAILED_START_TARGET_DEVICE_PROC,  "Failed to start target device process. ID={0} " )
            ( LogID.W_FAILED_GET_DIR_INFO,              "Failed to get directorys information. Path={0}, Message={1}" )
            ( LogID.W_FAILED_CREATE_DIR,                "Failed to create directory. Path={0}, Message={1}" )
            ( LogID.W_FAILED_DELETE_DIR,                "Failed to delete directory. Path={0}, Message={1}" )
            ( LogID.W_FAILED_READ_CONF_FILE,            "Failed to read configuration file. Name={0}, Message={1}" )
            ( LogID.W_FAILED_WRITE_CONF_FILE,           "Failed to write configuration file. Name={0}, Message={1}" )
            ( LogID.W_FAILED_DELETE_CONF_FILE,          "Failed to delete configuration file. Name={0}, Message={1}" )
            ( LogID.W_TARGET_DEVICE_PROC_MISSING,       "Specified target device process is missing. ID={0}" )
            ( LogID.W_ANOTHER_MGR_CLI_USED,             "Another management client is already connected." )
            ( LogID.W_MGR_CLI_SESSION_ID_MISMATCH,      "Management client session ID mismatch." )
            ( LogID.W_TRY_RESTART_TD_PROC,              "Target device proc terminated unexpectedly. Try to restart. Target Device ID={0}, Tarminated process ID={1}" )
            ( LogID.W_TD_PROC_ALREADY_STARTED,          "Target device proc already started. Target Device ID={0}, Process ID={1}" )
            ( LogID.W_TD_WORK_DIR_MISSING,              "Target device working directory missing. Device ID={0}" )
            ( LogID.W_TARGET_DEVICE_COUNT_OVER,         "Number of target devicees exceeds limit. Current proc count={0}" )
            ( LogID.W_TARGET_GROUP_COUNT_OVER,          "Number of target group exceeds limit. Target device ID={0}, Current target group count={1}" )
            ( LogID.W_LOGICAL_UNIT_COUNT_OVER,          "Number of logical unit exceeds limit. Target device ID={0}, Current LU count={1}" )
            ( LogID.W_CTRL_LOCAL_ADDRESS_NOT_SPECIFIED, "Local address that is used by controller server port is not specified." )
            ( LogID.W_FAILED_START_INITMEDIA_PROC,      "Failed to start media creation process." )
            ( LogID.W_INITMEDIA_PROC_ERROR_MSG,         "Media creation process reported end message. Message={0}" )
            ( LogID.W_INITMEDIA_PROC_STDERR,            "Media creation process stderr : {0}" )
            ( LogID.W_INITMEDIA_PROC_ABNORMAL_END,      "Media creation process has exited with some error. ExitStatus={0}" )
            ( LogID.W_INITMEDIA_PROC_FAIL_DEL_FILE,     "Failed to delete a media file. File name={0}, Message={1}" )
            ( LogID.W_INITMEDIA_PROC_MULTIPLICITY_OV,   "Maximum multiplicity of media creation process exceeded." )
            ( LogID.W_INITMEDIA_PROC_MISSING,           "Specified InitMedia process ID is missing. ProcID={0}" )
            ( LogID.W_INITMEDIA_UNEXPECTED_MSG,         "Unexpected response was received from media creation process.MEssage={0}" )
            ( LogID.W_CTRL_REQ_ERROR_END,               "Control request has terminated abnormally. Message={0}" )
            ( LogID.W_INVALID_LOG_PARAM,                "Specified log parameter value is invalid. Ignore request. SoftLimit={0}, HardLimit={1}, Level={2}" )
            ( LogID.W_MISSING_TSIH,                     "Specified TSIH missing. TSIH={0}" )
            ( LogID.W_SCSI_TASK_TERMINATED_WITH_EXP,    "SCSI task terminated with exception. {0}" )
            ( LogID.W_UNIT_ATTENTION_EXISTED,           "Unit Attention existed. UA={0}" )
            ( LogID.W_CONN_REJECTED_DUE_TO_WHITELIST,   "Connection rejected due to whilte list. Source address={0}, source port={1}." )
            ( LogID.W_CONNECTION_ERROR,                 "Communication error occured. Drop this connection. Message={0}." )
            ( LogID.W_ISCSI_TASK_REMOVED,               "iSCSI task removed. ITT={0}, CmdSN={1}, Reason={2}." )

            ( LogID.E_UNEXPECTED_ERROR,                 "Unexpedted error. Exception={0}, message={1}" )
            ( LogID.E_FAILED_CREATE_PORTAD,             "Failed to create Port. index={0}" )
            ( LogID.E_FAILED_CREATE_LU,                 "Failed to create LU. message={0}" )
            ( LogID.E_FAILED_CREATE_TCP_SERVER_PORT,    "Failed to create TCP server port. Address={0}, Port number={1}, message={2}" )
            ( LogID.E_FAILED_RESOLV_ADDRESS,            "Failed to create TCP server port. Address={0}, Port number={1}, message={2}" )
            ( LogID.E_CONNECTION_CLOSED,                "Connection is already closed. " )
            ( LogID.E_ISCSI_FORMAT_ERROR,               "iSCSI PDU format error. Try session recovery. Message={0}" )
            ( LogID.E_HEADER_DIGEST_ERROR,              "Header digest error. It occurs session recovery." )
            ( LogID.E_PDU_SEND_ERROR,                   "Failed to send PDU. Message={0}" )
            ( LogID.E_PDU_RECEIVE_ERROR,                "Failed to receive PDU. Message={0}" )
            ( LogID.E_UNSUPPORTED_ISCSI_VERSION,        "Requested iSCSI protocol version is unsupported. Version-Max={0}, Version-Min={1}" )
            ( LogID.E_UNKNOWN_TARGET_NAME,              "Login failed. Specified TargetName is missing. TargetName={0}" )
            ( LogID.E_UNKNOWN_NEGOTIATION_ERROR,        "Login failed. Negotiation result is invalid. Text key name = {0}" )
            ( LogID.E_AUTHENTICATION_FAILURE,           "Authentication failure. message={0}" )
            ( LogID.E_FAILED_CREATE_SESSION,            "Failed to create session component. I_T Nexus={0} message={1}" )
            ( LogID.E_FAILED_REBUILD_SESSION,           "Failed to rebuild session component. I_T Nexus={0}, message={1}" )
            ( LogID.E_FAILED_ADD_CONNECTION,            "Failed to add new connection to session. I_T Nexus={0}, message={1}" )
            ( LogID.E_FAILED_REBUILD_CONNECTION,        "Failed to rebuild connection. I_T Nexus={0}, message={1}" )
            ( LogID.E_MISSING_SESSION,                  "Specified session is missing." )
            ( LogID.E_PROTOCOL_ERROR,                   "Protocol error detected. message={0}" )
            ( LogID.E_SESSION_RECOVERY,                 "Session recovery occurred. message={0}" )
            ( LogID.E_SCSI_ACA_EXCEPTION_RAISED,        "SCSIACAException is raised. SenseKey={0}, ASCandASCQ={1}, msg={2}" )
            ( LogID.E_UNSUPPORTED_SCSI_COMMAND_VALUE,   "Unsupported SCSI command values was received. msg={0}" )
            ( LogID.E_MISSING_LU,                       "Specified LUN is missing. message={0}" )
            ( LogID.E_IO_ERROR_RETRY,                   "I/O error occurred. FileName={0}" )
            ( LogID.E_FILE_OPEN_ERROR,                  "File open failed. FileName={0}, Exception={1}, msg={2}" )
            ( LogID.E_FILE_FLUSH_ERROR,                 "Failed to flush file. FileName={0}, Exception={1}, msg={2}" )
            ( LogID.E_TASK_IRREGAL_TERMINATED,          "SCSI task is irregal terminated. Status={0}, msg={0}" )
            ( LogID.E_UNEXPECTED_ACA_EXCEPTION,         "Unexpected ACA exception is detected. Escalate to session recovary. Message={0}" )
            ( LogID.E_LU_CREATE_RETRY_OVER,             "Retry count was over, when trying to LU crate. " )
            ( LogID.E_TOO_MANY_CONNECTIONS,             "Too many connections in the session. MaxConnections={0}, Connection count={1}" )
            ( LogID.E_UNEXPECTED_PDU_IN_LOGIN_NEGOSEC,  "Unexpected PDU was received in login negosiation. Received PDU is {0}." )
            ( LogID.E_FAILED_RECEIVE_ALL_DATA_OUT_BYTES,"In SCSI write command, it failed to receive all of data-out bytes. It occurs session recovery when error recovery level is zero." )
            ( LogID.E_STATSN_SIGNIFICANTLY_DIFFERENT,   "Current StatSN({0}) and initiator's ExpStatSN({1}) are significantly different. In ErrorRecoverLevel=0, it occurs session recovery." )
            ( LogID.E_MISSING_RSNACK_REQED_STATUS_PDU,  "R-SNACK requested SCSI response PDU is missing." )
            ( LogID.E_TASK_QUEUE_LIMIT_EXCEEDED,        "Task queue size limit exceeded." )
            ( LogID.E_TD_PROC_RETRY_COUNT_OVER,         "Target device process restart count was over. Abort the restart. Target Device ID={0}, Last process ID={1}" )
            ( LogID.E_FAILED_START_TD_PROC,             "Failed to start target device process. Target device ID={0}, message={1}" )
            ( LogID.E_FAILED_SEND_REQUEST_TO_TD,        "Failed to send request to target device. Terminate process. Target device ID={0}, Process ID={1}, message={2}" )

            ( LogID.F_INTERNAL_ASSERTION,               "Internal consistency error was detected. message={0}" )
            ( LogID.F_UNEXPECTED_ERROR,                 "Unexpedted error. Exception={0}, message={1}" )
            ( LogID.F_UNKNOWN_LOG_MESSAGE_ID,           "Unknown log message. ID={0}." )
            ( LogID.F_FAILED_LOAD_CONFIG_FILE,          "Failed to load configuration file. filename={0}, message={1}" )
            ( LogID.F_CONFIG_FILE_VALIDATE_ERROR,       "Configuration file validate error. filename={0}, message={1}" )
            ( LogID.F_ERROR_EXIT,                       "*** ASSERTION ***.{0}" )
            ( LogID.F_STARTUP_ARGUMENT_ERROR,           "Invalid arguments. {0}" )
            ( LogID.F_MISSING_CONTROLLER_CONF_FILE,     "Haruka controller configuration file is missing. File={0}" )
            ( LogID.F_LURESET_REQ_TO_DUMMY_LU,          "LU reset was requested for dummy device LU. Reboot the target device." )
        }
        |> Functions.ToFrozenDictionary

    // --------------------------------------------------------------------
    /// <summary>
    ///   Initialize HLogger object.
    /// </summary>
    static member public Initialize () : unit =
        // Nothing to do
        ()

    // --------------------------------------------------------------------
    /// <summary>
    ///   Configure log parameters.
    /// </summary>
    /// <param name="argSoftLim">
    ///   Soft limit value. 
    /// </param>
    /// <param name="argHardLim">
    ///   Hard limit value.
    /// </param>
    /// <param name="cycle">
    ///  Output cycle value.
    /// </param>
    /// <param name="argOptLv">
    ///  log output level.
    /// </param>
    /// <param name="argWriter">
    ///  Log output target.
    /// </param>
    static member public SetLogParameters( argSoftLim, argHardLim, cycle, argOptLv, argWriter ) : unit =
        let newConf : LogConfg = {
            m_LogCountSoftLimit = argSoftLim |> max 0u |> min Constants.LOGPARAM_MAX_SOFTLIMIT;
            m_LogCountHardLimit = argHardLim |> max argSoftLim |> min Constants.LOGPARAM_MAX_HARDLIMIT;
            m_OutputCycle = cycle |> max 0u |> min 1000u;
            m_LogOutputLevel = LogLevel.toInt argOptLv;
            m_Writer = argWriter;
        }
        let newCounter = 
            if cycle = 0u then
                0UL
            else
                let w = ( uint32 Environment.TickCount ) / cycle
                ( ( uint64 w ) <<< 32 ) ||| 1UL

        // If the following update operations conflict, inconsistent results may occur.
        // However, since this is a log output process, we do not need to worry about the details.
        m_LogConfig.Update( fun _ -> newConf ) |> ignore
        Volatile.Write( &m_OutputCounter, newCounter )

    // --------------------------------------------------------------------
    /// <summary>
    ///   Get current configuration values.
    /// </summary>
    /// <returns>
    ///   Pair of soft limit, hard limit, output level
    /// </returns>
    static member public GetLogParameters () : ( uint32 * uint32 * LogLevel ) =
        let lc = m_LogConfig.obj
        lc.m_LogCountSoftLimit , lc.m_LogCountHardLimit , LogLevel.fromInt lc.m_LogOutputLevel

    // --------------------------------------------------------------------
    /// <summary>
    ///  Output trace message for ACA exception to the log.
    /// </summary>
    /// <param name="senseKey">
    ///  Sense key value.
    /// </param>
    /// <param name="acc">
    ///  Additional sense code and additional sense code qualifier.
    /// </param>
    /// <param name="msg">
    ///  Message string.
    /// </param>
    /// <param name="fnname">Function name which writing this log message.</param>
    /// <param name="source">Source code file name which writing this log message.</param>
    /// <param name="line">Source code line number which writing this log message.</param>
    static member public ACAException ( struct ( objId, cmdSource, itt, lun ),
                                    senseKey : SenseKeyCd,
                                    acc : ASCCd,
                                    msg : string,
                                    [<CallerMemberName>] ?fnname : string,
                                    [<CallerFilePath>] ?source : string,
                                    [<CallerLineNumber>] ?line: int ) : unit =
        let f ( g : GenLogMsg ) =
            g.GenACA( struct ( objId, cmdSource, itt, lun ), senseKey, acc, msg, fnname, source, line )
        HLogger.Trace( LogID.E_SCSI_ACA_EXCEPTION_RAISED, f )

    static member public IgnoreException ( f : ( GenLogMsg -> string ) ) : unit =
        HLogger.Trace( LogID.I_EXCEPTION_IGNORED, f )

    // --------------------------------------------------------------------
    /// <summary>
    ///  Output trace message for unexpected exception to the log.
    /// </summary>
    /// <param name="f">
    ///  Message generation function.
    /// </param>
    static member public UnexpectedException ( f : ( GenLogMsg -> string ) ) : unit =
        HLogger.Trace( LogID.E_UNEXPECTED_ERROR, f )

    // --------------------------------------------------------------------
    /// <summary>
    ///  Output trace message to the log.
    /// </summary>
    /// <param name="logID">
    ///  Log ID.
    /// </param>
    /// <param name="f">
    ///  Message generation function.
    /// </param>
    static member public Trace ( logID : LogID, f : ( GenLogMsg -> string ) ) : unit =
        let conf = m_LogConfig.obj

        // If current message level is under than log output level, ommit this message
        let sourceLevel = HLogger.LogIDtoLevel logID
        if sourceLevel >= conf.m_LogOutputLevel then
            try
                // decide write log or not
                let rec loop( cnt : int ) =
                    let savedTimeSlot = uint64 ( uint32 Environment.TickCount / conf.m_OutputCycle )
                    let init = Interlocked.Read( &m_OutputCounter )

                    let currentTimeSlot = init >>> 32
                    let wcnt = init &&& 0xFFFFFFFFUL
                    let next, result =
                        if currentTimeSlot <> savedTimeSlot then
                            ( ( savedTimeSlot <<< 32 ) ||| 1UL ), true
                        elif wcnt < uint64 conf.m_LogCountSoftLimit then
                            ( ( savedTimeSlot <<< 32 ) ||| ( wcnt + 1UL ) ), true
                        elif wcnt < uint64 conf.m_LogCountHardLimit && sourceLevel <> 1 then    // 1 = LOGLEVEL_VERBOSE
                            ( ( savedTimeSlot <<< 32 ) ||| ( wcnt + 1UL ) ), true
                        else
                            ( ( savedTimeSlot <<< 32 ) ||| wcnt ), false
                    if init = Interlocked.CompareExchange( &m_OutputCounter, next, init ) then
                        result
                    else
                        loop( cnt + 1 )

                if conf.m_OutputCycle = 0u || loop( 0 ) then
                    // Add specified message to queue
                    let msgFormat = HLogger.GetMsgFormat logID
                    let g = GenLogMsg( logID, sourceLevel, msgFormat, m_ProcName, m_ProcID )
                    let msg = f g

                    if conf.m_OutputCycle <= 0u then
                        conf.m_Writer.WriteLine msg
                        if sourceLevel >= 2 then    // 2 = LOGLEVEL_INFO
                            conf.m_Writer.Flush()
                    else
                        m_MsgQueue.Enqueue ( LogMessageType.Message msg )
                        if sourceLevel >= 2 then    // 2 = LOGLEVEL_INFO
                            m_MsgQueue.Enqueue LogMessageType.Flush

            with
            // All exceptions ralated to log output operations are ignored.
            | _ -> ()

    // ------------------------------------------------------------------------
    /// <summary>
    /// Force a log flush.
    /// </summary>
    static member public Flush () : unit =
        let conf = m_LogConfig.obj
        try
            if conf.m_OutputCycle <= 0u then
                conf.m_Writer.Flush()
            else
                m_MsgQueue.Enqueue LogMessageType.Flush
        with
        | _ -> ()

    // ------------------------------------------------------------------------
    /// <summary>
    /// Adding a logging callback.
    /// </summary>
    static member public Callback ( f : unit -> unit ) : unit =
        let conf = m_LogConfig.obj
        try
            if conf.m_OutputCycle <= 0u then
                f()
            else
                m_MsgQueue.Enqueue ( LogMessageType.Callback f )
        with
        | _ -> ()


    // ------------------------------------------------------------------------
    /// <summary>
    ///  Returns true if the log level is Verbose.
    /// </summary>
    static member public IsVerbose : bool =
        let conf = m_LogConfig.obj
        ( LogLevel.toInt LogLevel.LOGLEVEL_VERBOSE ) >= conf.m_LogOutputLevel

    // ------------------------------------------------------------------------
    /// <summary>
    /// convert from LogID to SourceLevels and TraceEventType
    /// </summary>
    static member private LogIDtoLevel ( id : LogID ) : int =
        ( ( int id ) >>> 28 ) &&& 0x0000000F

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get message format string from message ID.
    /// </summary>
    /// <param name="id">Log message ID</param>
    /// <returns>
    ///   Log message format string, or empty string.
    /// </returns>
    static member private GetMsgFormat( id : LogID ) : string =
        try
            m_Messages.Item( id )
        with
        | :? KeyNotFoundException -> ""
    